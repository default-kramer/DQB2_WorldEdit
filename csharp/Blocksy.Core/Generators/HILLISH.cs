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
		// these values should be configurable eventually:
		const int initialY = 12;
		const int initialRange = 2; // plus-or-minus
		const int yDrop = 3;
		const int minSeparation = 1;
		const int maxSeparation = 4;

		/// <summary>
		/// Represents a marker for a corner in a layer, indicating the direction of the turn.
		/// </summary>
		public enum CornerMarker
		{
			Prev,
			Next
		}

		/// <summary>
		/// Distinguishes between different types of corners in a hill layer cell.
		/// </summary>
		public enum CornerType
		{
			/// <summary>The cell is not a corner.</summary>
			None,
			/// <summary>The cell is an unfilled corner, awaiting connection to an adjacent cell.</summary>
			Unfilled,
			/// <summary>The cell is a filled corner, with its corner space occupied.</summary>
			Filled
		}

		/// <summary>
		/// Represents the state of a corner in a hill layer cell.
		/// This struct provides a type-safe way to handle the different corner states,
		/// replacing the original implementation's use of `object`.
		/// </summary>
		public readonly struct CornerInfo
		{
			/// <summary>Gets the type of the corner.</summary>
			public CornerType Type { get; }

			/// <summary>Gets the marker for an unfilled corner. Valid only if Type is Unfilled.</summary>
			public CornerMarker Marker { get; }

			/// <summary>Gets the Y-value of the cell filling the corner space. Valid only if Type is Filled.</summary>
			public int FillY { get; }

			private CornerInfo(CornerType type, CornerMarker marker, int fillY)
			{
				Type = type;
				Marker = marker;
				FillY = fillY;
			}

			/// <summary>Represents a cell that is not a corner.</summary>
			public static CornerInfo None => new CornerInfo(CornerType.None, default, 0);

			/// <summary>Creates an unfilled corner with a specific marker.</summary>
			public static CornerInfo Unfilled(CornerMarker marker) => new CornerInfo(CornerType.Unfilled, marker, 0);

			/// <summary>Creates a filled corner with a specific Y-value.</summary>
			public static CornerInfo Filled(int y) => new CornerInfo(CornerType.Filled, default, y);
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
			/// Describes the corner state of this cell.
			/// </summary>
			public CornerInfo Corner { get; set; }

			public LayerCell(int x, int z, int y, CornerInfo corner)
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
					if (cell.Corner.Type == CornerType.Filled)
					{
						array.Put(new XZ(cell.X, cell.Z + 1), cell.Corner.FillY);
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
					return y < (initialY - initialRange) || y > (initialY + initialRange);
				});

			var firstLayer = ContourToLayer(initialContour, initialY, firstLayerRejecter, prng);
			firstLayer = FillCorners(firstLayer);

			var layers = new List<List<LayerCell>> { firstLayer };
			int y = initialY - yDrop;

			while (true)
			{
				var prevLayer = layers.First();

				if (prevLayer.All(cell => cell.Y <= 0))
				{
					layers.RemoveAt(0);
					break;
				}

				var nextContour = LayerToContour(prevLayer);
				var rejecter = MakeRejecter(prevLayer);

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
				y -= yDrop;
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

				CornerInfo corner;
				if (z < currentPrevZ) corner = CornerInfo.Unfilled(CornerMarker.Prev);
				else if (z < nextZ) corner = CornerInfo.Unfilled(CornerMarker.Next);
				else corner = CornerInfo.None;

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

				if (a.Corner.Type == CornerType.Unfilled && a.Corner.Marker == CornerMarker.Next)
				{
					a.Corner = CornerInfo.Filled(b.Y);
				}

				if (b.Corner.Type == CornerType.Unfilled && b.Corner.Marker == CornerMarker.Prev)
				{
					b.Corner = CornerInfo.Filled(a.Y);
				}
			}
			return newLayer;
		}

		private static List<int> LayerToContour(List<LayerCell> layer)
		{
			return layer.Select(cell => cell.Z + (cell.Corner.Type == CornerType.Filled ? 2 : 1)).ToList();
		}

		private static Func<List<LayerCell>, bool> MakeRejecter(List<LayerCell> prevLayer)
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