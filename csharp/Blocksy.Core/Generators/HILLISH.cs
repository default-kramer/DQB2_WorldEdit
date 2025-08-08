using System;
using System.Collections.Generic;
using System.Linq;

namespace Blocksy.Core.Generators
{
	/// <summary>
	/// C# port of the Racket HILLISH generator algorithm.
	/// This algorithm procedurally generates a hill as a series of 2D layers.
	/// </summary>
	public static class Hillish
	{
		/// <summary>
		/// Represents a marker for a corner in a layer, indicating the direction of the turn.
		/// </summary>
		public enum CornerMarker
		{
			Prev,
			Next
		}

		/// <summary>
		/// Represents a single cell in a hill layer.
		/// </summary>
		public class LayerCell
		{
			public int X { get; set; }
			public int Z { get; set; }
			public int Y { get; set; }
			/// <summary>
			/// Can be:
			/// - bool `false`: Not a corner.
			/// - `CornerMarker`: An unfilled corner.
			/// - `int`: A filled corner, storing the Y value of the filling cell.
			/// </summary>
			public object Corner { get; set; }

			public LayerCell(int x, int z, int y, object corner)
			{
				X = x;
				Z = z;
				Y = y;
				Corner = corner;
			}

			public LayerCell Clone()
			{
				return (LayerCell)MemberwiseClone();
			}
		}

		public static I2DSampler<int> Create(PRNG prng)
		{
			var layers = Generate(prng);
			var maxZ = layers.SelectMany(l => l).Max(cell => cell.Z);
			var box = new BoundingBox(new XZ(0, 0), new XZ(layers[0].Count, maxZ + 2));
			var array = new MutableArray2D<int>(box, -1);

			foreach (var layer in layers)
			{
				foreach (var cell in layer)
				{
					array.Put(new XZ(cell.X, cell.Z), cell.Y);
					if (cell.Corner is int cornerY)
					{
						array.Put(new XZ(cell.X, cell.Z + 1), cornerY);
					}
				}
			}

			return array;
		}

		/// <summary>
		/// The main method to generate the hill.
		/// </summary>
		/// <param name="minContourLength">The minimum length of the initial contour for the top of the hill.</param>
		/// <returns>A list of layers, from bottom to top, representing the hill.</returns>
		private static List<List<LayerCell>> Generate(PRNG prng, int minContourLength = 60)
		{
			var initialContour = BuildContour(minContourLength, prng);

			Func<List<LayerCell>, bool> firstLayerRejecter = run =>
				run.Any(cell =>
				{
					int y = cell.Y + cell.Z;
					return y < 10 || y > 14;
				});

			var firstLayer = ContourToLayer(initialContour, 12, firstLayerRejecter, prng);
			firstLayer = FillCorners(firstLayer);

			var layers = new List<List<LayerCell>> { firstLayer };
			int y = 12 - 3;

			while (true)
			{
				var prevLayer = layers.First();

				if (prevLayer.All(cell => cell.Y <= 0))
				{
					layers.RemoveAt(0);
					break;
				}

				var nextContour = LayerToContour(prevLayer);
				var rejecter = MakeRejecter(prevLayer, 1, 4);

				List<LayerCell>? nextLayer = null;
				// This retry logic is a direct port of the original Racket code's RETRY macro.
				// It attempts to generate a valid layer up to 101 times.
				// A more advanced implementation could use backtracking: if generation fails
				// repeatedly for a layer, it would undo the previous layer and retry it with
				// different random choices. This would be more robust against getting "stuck"
				// in a state where no valid next layer can be generated.
				for (int i = 0; i < 101; i++)
				{
					try
					{
						nextLayer = ContourToLayer(nextContour, y, rejecter, prng);
						if (nextLayer != null) break;
					}
					catch (Exception)
					{
						if (i == 100) throw new Exception("Hill generation failed after maximum retries.");
					}
				}

				if (nextLayer == null)
				{
					// Failed to generate a valid layer. Stop here.
					break;
				}

				nextLayer = FillCorners(nextLayer);
				layers.Insert(0, nextLayer);
				y -= 3;
			}

			layers.Reverse();
			return layers;
		}

		private static List<int> BuildContour(int minLength, PRNG prng)
		{
			const int minZ = 0;
			const int maxZ = 5;

			var contour = new List<int>();
			int z = prng.NextInt32(minZ, maxZ + 1);

			while (contour.Count < minLength)
			{
				int runLength = 3 + prng.NextInt32(6);
				int newZ = z + prng.RandomChoice(-1, 1);

				if (newZ >= minZ && newZ <= maxZ)
				{
					for (int i = 0; i < runLength; i++)
					{
						contour.Add(z);
					}
					z = newZ;
				}
			}
			return contour;
		}

		private static List<LayerCell> ContourToLayer(List<int> contour, int y, Func<List<LayerCell>, bool> runRejecter, PRNG prng)
		{
			const int runLengthMin = 2;
			const int runLengthRand = 4;
			int retryCounter = 0;

			var resultLayer = new List<LayerCell>();
			int currentIndex = 0;
			int currentX = 0;
			int currentY = y;

			while (currentIndex < contour.Count)
			{
				bool runAccepted = false;
				while (!runAccepted)
				{
					retryCounter++;
					if (retryCounter > 100)
					{
						throw new Exception("Too many retries in ContourToLayer! (probable infinite loop)");
					}

					int runLength = runLengthMin + prng.NextInt32(runLengthRand);
					runLength = Math.Min(runLength, contour.Count - currentIndex);

					var head = contour.GetRange(currentIndex, runLength);
					int tailIndex = currentIndex + runLength;

					if (tailIndex < contour.Count && head.Last() != contour[tailIndex])
					{
						continue;
					}

					int runY = currentY + prng.RandomChoice(-1, 1);

					var run = RunToCells(head, currentX, head[0], runY);

					if (!runRejecter(run))
					{
						resultLayer.AddRange(run);
						currentY = run.Last().Y;
						currentX += runLength;
						currentIndex += runLength;
						retryCounter = 0;
						runAccepted = true;
					}
				}
			}
			return resultLayer;
		}

		private static List<LayerCell> RunToCells(List<int> run, int startX, int prevZ, int y)
		{
			var cells = new List<LayerCell>();
			int currentX = startX;
			int currentPrevZ = prevZ;

			for (int i = 0; i < run.Count; i++)
			{
				int z = run[i];
				int dz = currentPrevZ - z;
				int newY;
				switch (dz)
				{
					case 0: newY = y; break;
					case -1: newY = y - 1; break;
					case 1: newY = y + 1; break;
					default: throw new Exception("Z cannot jump by more than 1!");
				}

				int nextZ = (i + 1 < run.Count) ? run[i + 1] : z;

				object corner;
				if (z < currentPrevZ) corner = CornerMarker.Prev;
				else if (z < nextZ) corner = CornerMarker.Next;
				else corner = false;

				cells.Add(new LayerCell(currentX, z, newY, corner));

				currentX++;
				currentPrevZ = z;
				y = newY;
			}
			return cells;
		}

		private static List<LayerCell> FillCorners(List<LayerCell> layer)
		{
			if (layer.Count < 2) return new List<LayerCell>(layer);

			var newLayer = layer.Select(c => c.Clone()).ToList();

			for (int i = 0; i < newLayer.Count - 1; i++)
			{
				var a = newLayer[i];
				var b = newLayer[i + 1];

				if (a.Corner is CornerMarker markerA && markerA == CornerMarker.Next)
				{
					a.Corner = b.Y;
				}

				if (b.Corner is CornerMarker markerB && markerB == CornerMarker.Prev)
				{
					b.Corner = a.Y;
				}
			}
			return newLayer;
		}

		private static List<int> LayerToContour(List<LayerCell> layer)
		{
			return layer.Select(cell => cell.Z + (cell.Corner is int ? 2 : 1)).ToList();
		}

		private static Func<List<LayerCell>, bool> MakeRejecter(List<LayerCell> prevLayer, int minSeparation, int maxSeparation)
		{
			return run => run.Any(cell =>
			{
				if (cell.X >= prevLayer.Count) return true; // Should not happen with valid contours
				var northCell = prevLayer[cell.X];
				int separation = northCell.Y - cell.Y;
				return separation < minSeparation || separation > maxSeparation;
			});
		}
	}
}
