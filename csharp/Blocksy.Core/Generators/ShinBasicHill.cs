using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Blocksy.Core.Generators;

// TODO this shouldn't be named "hill", it's a "wall" or "embankment"...

/// <remarks>
/// This algorithm operates by constructing layers one at a time.
/// The width of all layers is equal and constant.
/// Each successive layer will satisfy
/// * currentLayer[x].Y must be less than previousLayer[x].Y
/// * currentLayer[x].NorthernmostPoint.Z == 1 + previousLayer[x].SouthernmostPoint.Z
/// These goals are the main reason the <see cref="Backstop"/> class exists.
/// Also, we can create a starter backstop to generate the first layer
/// (which will have no previous layer).
///
/// NOTE TO FUTURE SELF - If you want to address the fact that the backstop naturally flattens over time,
/// the correct approach would probably be something like:
/// * before each layer is generated
///   - check if backstop is too flat
///   - if so, create "shims" as needed and add them to some list for later
///   - rebuild the backstop, including the shims
///     * be careful that the new backstop doesn't have any problematic alcoves!
///   - generate the layer using the possibly-updated backstop
/// * when constructing the final array, be sure to include data from all layers and all shims
///
/// The reason the backstop tends toward flatness is due to <see cref="FillAlcoves"/>.
/// (And alcoves would fill naturally even without this method due to cornering.)
/// </remarks>
public sealed class ShinBasicHill
{
	private const int minSeparation = 1;
	private const int maxSeparation = 4;
	const int runLengthMin = 2;
	const int runLengthRand = 4;

	public record struct Item(int y, int layerId);

	record struct Point(XZ xz, int y);

	/// <summary>
	/// When dealing with corners, we will need to populate 2 Z coords at a given X coord.
	/// For example, here is how a run of width 5 with a corner at `run[2]` would look
	///    _ _ c x x
	///    x x x _ _
	///
	/// In that example, the cell at `run[2]` will have
	/// * bool corner: true
	/// * a Z coordinate matching the cells to its left
	/// * a special <see cref="NorthernmostPoint"/> for the "c" spot in the diagram above.
	/// </summary>
	record struct Cell(Point point, bool corner, int layerId)
	{
		private Point CornerPoint() => new Point(point.xz.Add(0, -1), point.y + 1);

		public IEnumerable<Point> Points()
		{
			yield return point;
			if (corner)
			{
				yield return CornerPoint();
			}
		}

		public Point SouthernmostPoint => point;

		public Point NorthernmostPoint => corner ? CornerPoint() : point;

		public Cell ConvertToCorner()
		{
			if (corner)
			{
				throw new Exception("assert fail - already a corner");
			}
			var xz = this.point.xz.Add(0, 1);
			return new Cell(new Point(xz, this.point.y - 1), true, layerId);
		}
	}

	/// <summary>
	/// "Generate next layer" is almost* a function of the backstop.
	/// (* "almost", because we also use the shared PRNG and the width constant.)
	/// </summary>
	sealed class Backstop
	{
		private readonly IReadOnlyList<Point> points;

		public Backstop(IReadOnlyList<Point> points)
		{
			this.points = points;
		}

		private bool Rejects(Point point)
		{
			var north = this.points[point.xz.X];
			if (point.xz.Z != north.xz.Z + 1)
			{
				throw new ArgumentException("given point is not immediately south of backstop");
			}
			int separation = north.y - point.y;
			return separation < minSeparation || separation > maxSeparation;
		}

		/// <summary>
		/// Don't end a run where the Z coordinate changes for aesthetic reasons.
		/// (And maybe for correctness too??)
		/// </summary>
		public bool CanEndRunAt(int xEnd)
		{
			if (xEnd == points.Count)
			{
				return true;
			}
			if (xEnd > points.Count)
			{
				return false;
			}
			return points[xEnd].xz.Z == points[xEnd - 1].xz.Z;
		}

		/// <summary>
		/// Returns all possible Y values that the first cell of the next layer could start at
		/// </summary>
		public IEnumerable<int> InitialYChoices()
		{
			var anchor = this.points[0];
			int y = anchor.y;
			var xz = anchor.xz.Add(0, 1);

			int yMin = anchor.y - maxSeparation;
			while (y >= yMin)
			{
				var test = new Point(xz, y);
				if (!Rejects(test))
				{
					yield return y;
				}
				y--;
			}
		}

		public bool GetCellForNextLayer(int x, ref int y, int layerId, out Cell cell)
		{
			var north = points[x];

			bool drop = false;
			bool raise = false;

			if (x < points.Count - 1)
			{
				var nextNorth = points[x + 1];
				if (nextNorth.xz.Z > north.xz.Z)
				{
					drop = true; // step out and drop Y
				}
			}

			if (x > 0)
			{
				var prevNorth = points[x - 1];
				if (prevNorth.xz.Z > north.xz.Z)
				{
					raise = true; // step in and raise Y
				}
			}

			if (drop && raise)
			{
				throw new Exception("assert fail - cannot drop and raise!");
			}

			// Corner cells expect to be given the southern of the two points.
			// So whether we are stepping in or out, we need to increase Z by 2.
			var xz = (drop || raise) ? north.xz.Add(0, 2) : north.xz.Add(0, 1);
			if (drop)
			{
				y--;
				cell = new Cell(new Point(xz, y), true, layerId);
			}
			else if (raise)
			{
				int thisY = y; // Cell wants the southern point, before we increment Y
				y++;
				cell = new Cell(new Point(xz, thisY), true, layerId);
			}
			else
			{
				cell = new Cell(new Point(xz, y), false, layerId);
			}

			return !Rejects(cell.NorthernmostPoint);
		}
	}

	sealed class Layer
	{
		public required int LayerId { get; init; }
		private readonly IReadOnlyList<Cell> cells;

		public Layer(IReadOnlyList<Cell> cells)
		{
			this.cells = cells;
		}

		public bool HasData => cells.Any(cell => cell.point.y > 0);

		public Backstop ToBackstop()
		{
			var points = this.cells.Select(cell => cell.SouthernmostPoint).ToList();
			return new Backstop(points);
		}

		public int MaxZ => cells.Select(cell => cell.SouthernmostPoint.xz.Z).Max();

		public IEnumerable<Point> Points => cells.SelectMany(cell => cell.Points());
	}

	private readonly PRNG prng;
	private readonly int width;

	private ShinBasicHill(PRNG prng, int width)
	{
		this.prng = prng;
		this.width = width;
	}

	public static I2DSampler<Item> Generate(PRNG prng, int width, int height)
	{
		var hill = new ShinBasicHill(prng, width);
		var layers = hill.BuildLayers(height);
		int zEnd = layers.Last().MaxZ + 1;

		var box = new BoundingBox(new XZ(0, 0), new XZ(width, zEnd));
		var array = new MutableArray2D<Item>(box, new Item(-1, -1));

		foreach (var layer in layers)
		{
			foreach (var point in layer.Points)
			{
				array.Put(point.xz, new Item(point.y, layer.LayerId));
			}
		}

		return array;
	}

	private List<Layer> BuildLayers(int height)
	{
		var layers = new List<Layer>();

		var backstop = GenerateInitialBackstop(height + minSeparation);
		var layer = GenerateLayer(backstop, layers.Count);

		while (layer.HasData)
		{
			layers.Add(layer);
			backstop = layer.ToBackstop();
			layer = GenerateLayer(backstop, layers.Count);
		}

		return layers;
	}

	private Backstop GenerateInitialBackstop(int y)
	{
		// This backstop is not part of the hill.
		// It is only used to constrain the first layer.
		// So we build this backstop at Z=-1 so that the first layer will end up at Z=0.
		const int SHIFT = -1;

		const int minZ = 0 + SHIFT; // inclusive
		const int maxZ = 4 + SHIFT; // inclusive
		int z = prng.NextInt32(maxZ + 1);
		int x = 0;

		var points = new List<Point>();
		while (x < width)
		{
			int xEnd = x + runLengthMin + prng.NextInt32(runLengthRand);
			xEnd = Math.Min(xEnd, width);

			for (; x < xEnd; x++)
			{
				points.Add(new Point(new XZ(x, z), y));
			}

			int dz;
			if (z == minZ)
			{
				dz = 1;
			}
			else if (z == maxZ)
			{
				dz = -1;
			}
			else
			{
				dz = prng.RandomChoice(-1, 1);
			}

			z += dz;
		}

		if (points.Count != width)
		{
			throw new Exception("assert fail");
		}

		return new Backstop(points);
	}

	private Layer GenerateLayer(Backstop backstop, int layerId)
	{
		var buffer = new Cell[width];
		var shared = new Shared(buffer)
		{
			backstop = backstop,
			LayerId = layerId,
			prng = prng,
			legalRunLengths = Enumerable.Range(runLengthMin, runLengthRand).ToImmutableSortedSet(),
		};

		var yChoices = backstop.InitialYChoices().OrderBy(x => prng.NextDouble()).ToList();

		foreach (int y in yChoices)
		{
			bool okay = new LayerGenerator(shared, 0, y).Execute();
			if (okay)
			{
				FillAlcoves(buffer);
				return new Layer(buffer)
				{
					LayerId = layerId
				};
			}
		}

		throw new Exception($"failed to generate layer! tried y=[{string.Join(',', yChoices)}]");
	}

	/// <summary>
	/// An "alcove" is an inset gap.
	/// For example, here is what an alcove having <paramref name="alcoveWidth"/> 2 looks like:
	///    _ _ _ _ c x x c _ _ _ _
	///    x x x x x _ _ x x x x x
	///
	/// It is *essential* that we fill alcoves having width 2 or less.
	/// Otherwise we might get stuck later on.
	/// We will replace alcove cells with corner cells.
	/// So the example above would get replaced by this:
	///    _ _ _ _ c c c c _ _ _ _
	///    x x x x x x x x x x x x
	/// </summary>
	private static bool IsAlcove(IReadOnlyList<Cell> cells, int start, int alcoveWidth)
	{
		int zStart = cells[start].SouthernmostPoint.xz.Z;

		int end = start + alcoveWidth + 1;
		if (end >= cells.Count)
		{
			return false;
		}

		int zEnd = cells[end].SouthernmostPoint.xz.Z;
		if (zStart != zEnd)
		{
			return false;
		}

		for (int i = 1; i <= alcoveWidth; i++)
		{
			if (cells[start + i].SouthernmostPoint.xz.Z >= zStart)
			{
				return false;
			}
		}

		return true;
	}

	private static void FillAlcoves(Cell[] buffer)
	{
		for (int x = 0; x < buffer.Length; x++)
		{
			for (int alcoveWidth = 2; alcoveWidth >= 1; alcoveWidth--)
			{
				if (IsAlcove(buffer, x, alcoveWidth))
				{
					for (int i = 1; i <= alcoveWidth; i++)
					{
						buffer[x + i] = buffer[x + i].ConvertToCorner();
					}
				}
			}
		}
	}

	sealed class Shared
	{
		public required PRNG prng { get; init; }
		public required Backstop backstop { get; init; }
		public required IImmutableSet<int> legalRunLengths { get; init; }
		public required int LayerId { get; init; }
		public int Width => Buffer.Length;

		private readonly Cell[] Buffer;
		public Shared(Cell[] buffer)
		{
			this.Buffer = buffer;
		}

		public Span<Cell> GetWritableBuffer(int xStart, int runLength)
		{
			return Buffer.AsSpan().Slice(xStart, runLength);
		}
	}

	/// <summary>
	/// Holds the state needed to generate a single run.
	/// If we succeed, we recurse.
	/// If we cannot, we backtrack to an earlier state and rely on the mutable state
	/// of the shared PRNG to explore other possible recursions.
	/// </summary>
	record struct LayerGenerator(Shared shared, int xStart, int y)
	{
		public bool Execute()
		{
			if (xStart >= shared.Width)
			{
				return true;
			}

			var runLengths = shared.legalRunLengths;

			for (int i = 0; i < 5; i++)
			{
				if (runLengths.Count == 0)
				{
					return false; // no valid run lengths
				}

				int runLength = shared.prng.RandomChoice(runLengths.ToArray());
				if (!shared.backstop.CanEndRunAt(xStart + runLength))
				{
					// Don't count this as a retry.
					// Remove the failed runLength from the pool and try again.
					runLengths = runLengths.Remove(runLength);
					i--;
					continue;
				}

				if (GenerateRunRecursive(runLength))
				{
					return true;
				}
			}

			return false;
		}

		private bool GenerateRunRecursive(int runLength)
		{
			int y = this.y;
			if (xStart > 0)
			{
				y += shared.prng.RandomChoice(-1, 1);
			}

			var buffer = shared.GetWritableBuffer(xStart, runLength);
			for (int i = 0; i < runLength; i++)
			{
				int x = xStart + i;
				if (shared.backstop.GetCellForNextLayer(x, ref y, shared.LayerId, out var cell))
				{
					buffer[i] = cell;
				}
				else
				{
					return false;
				}
			}

			var next = new LayerGenerator(shared, xStart + runLength, y);
			return next.Execute();
		}
	}
}
