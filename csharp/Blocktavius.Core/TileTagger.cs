using Blocktavius.Core.Generators.Cliffs;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Blocktavius.Core;

sealed record Edge
{
	public required XZ Start { get; init; }
	public required CardinalDirection StepDirection { get; init; }
	public required CardinalDirection InsideDirection { get; init; }
	public required int Length { get; init; }

	public XZ End => Start.Add(Direction.Parse(StepDirection).Step.Scale(Length));

	internal Edge Scale(XZ scale)
	{
		int lengthScale = 1;
		if (StepDirection == CardinalDirection.East || StepDirection == CardinalDirection.West)
		{
			lengthScale = scale.X;
		}
		else if (StepDirection == CardinalDirection.North || StepDirection == CardinalDirection.South)
		{
			lengthScale = scale.Z;
		}

		return new Edge()
		{
			Start = this.Start.Scale(scale),
			InsideDirection = this.InsideDirection,
			StepDirection = this.StepDirection,
			Length = this.Length * lengthScale,
		};
	}
}

sealed record Region
{
	private readonly IReadOnlySet<XZ> unscaledTiles;
	private readonly XZ scale;

	public Region(IReadOnlySet<XZ> unscaledTiles, XZ scale)
	{
		this.unscaledTiles = unscaledTiles;
		this.scale = scale;
	}

	public required IReadOnlyList<Edge> Edges { get; init; }
	public required Rect Bounds { get; init; }

	public bool Contains(XZ xz) => unscaledTiles.Contains(xz.Unscale(scale));
}

sealed class TileTagger<TTag> where TTag : notnull
{
	public XZ UnscaledSize { get; }
	public XZ Scale { get; }
	private MutableArray2D<IImmutableSet<TTag>> array;

	public TileTagger(XZ unscaledSize, XZ scale)
	{
		if (scale.X < 2 || scale.Z < 2)
		{
			throw new ArgumentException($"scale must be at least 2: {scale}");
		}
		UnscaledSize = unscaledSize;
		Scale = scale;
		array = new MutableArray2D<IImmutableSet<TTag>>(new Rect(XZ.Zero, UnscaledSize), ImmutableSortedSet<TTag>.Empty);
	}

	public void AddTag(XZ loc, TTag tag)
	{
		array[loc] = array[loc].Add(tag);
	}

	public IReadOnlyList<Region> GetRegions(TTag tag)
	{
		var regions = FindRegionTiles(array, tag);
		return regions.Select(r => BuildRegion(r, Scale)).ToList();
	}

	/// <summary>
	/// Output is unscaled.
	/// Each hashset contains the unscaled coordinates of tiles which belong in the same region.
	/// </summary>
	private static List<HashSet<XZ>> FindRegionTiles(I2DSampler<IImmutableSet<TTag>> sampler, TTag tag)
	{
		var bounds = sampler.Bounds;
		if (bounds.start != XZ.Zero)
		{
			throw new Exception("Assert fail");
		}

		var regions = new List<HashSet<XZ>>();
		var visited = new MutableArray2D<bool>(bounds, false);

		foreach (var xz in sampler.Bounds.Enumerate())
		{
			if (visited[xz])
			{
				continue;
			}
			visited[xz] = true;

			if (!sampler.Sample(xz).Contains(tag))
			{
				continue;
			}

			var queue = new Queue<XZ>();
			queue.Enqueue(xz);
			var regionTiles = new HashSet<XZ>();

			while (queue.Count > 0)
			{
				var loc = queue.Dequeue();
				regionTiles.Add(loc);

				foreach (var neighbor in loc.CardinalNeighbors())
				{
					if (!bounds.Contains(neighbor) || visited[neighbor])
					{
						continue;
					}
					visited[neighbor] = true;

					if (sampler.Sample(neighbor).Contains(tag))
					{
						queue.Enqueue(neighbor);
					}
				}
			}

			regions.Add(regionTiles);
		}

		return regions;
	}

	/// <summary>
	/// Input is unscaled; output is scaled.
	/// </summary>
	private static Region BuildRegion(IReadOnlySet<XZ> unscaledTiles, XZ scale)
	{
		var unscaledSegments = GetEdgeSegments(unscaledTiles);
		var scaledEdges = CombineEdges(unscaledSegments)
			.Select(edge => edge.Scale(scale))
			.ToList();
		var scaledBounds = Rect.GetBounds(scaledEdges.Select(e => e.End).Concat(scaledEdges.Select(e => e.Start)));

		return new Region(unscaledTiles, scale)
		{
			Bounds = scaledBounds,
			Edges = scaledEdges,
		};
	}

	/// <summary>
	/// Output is unscaled.
	/// Returns one "edge segment" for each side of each tile
	/// when the tile lacks a neighbor on that side.
	/// </summary>
	private static HashSet<Edge> GetEdgeSegments(IReadOnlySet<XZ> unscaledTiles)
	{
		var segments = new HashSet<Edge>();
		foreach (var tile in unscaledTiles)
		{
			void TestAndAdd(Direction direction, Edge edge)
			{
				if (!unscaledTiles.Contains(tile.Add(direction.Step)))
				{
					segments.Add(edge);
				}
			}

			var cornerNW = tile;

			// North empty? Then go NW -> NE
			TestAndAdd(Direction.North, new Edge()
			{
				InsideDirection = CardinalDirection.South,
				Start = cornerNW,
				StepDirection = CardinalDirection.East,
				Length = 1,
			});

			// South empty? Then go SW -> SE
			TestAndAdd(Direction.South, new Edge()
			{
				InsideDirection = CardinalDirection.North,
				Start = cornerNW.Step(Direction.South),
				StepDirection = CardinalDirection.East,
				Length = 1,
			});

			// West empty? Then go NW -> SW
			TestAndAdd(Direction.West, new Edge()
			{
				InsideDirection = CardinalDirection.East,
				Start = cornerNW,
				StepDirection = CardinalDirection.South,
				Length = 1,
			});

			// East empty? Then go NE -> SE
			TestAndAdd(Direction.East, new Edge()
			{
				InsideDirection = CardinalDirection.West,
				Start = cornerNW.Step(Direction.East),
				StepDirection = CardinalDirection.South,
				Length = 1,
			});
		}

		return segments;
	}

	/// <summary>
	/// This method should work correctly whether or not the input is scaled.
	///
	/// Combines edge segments into edges.
	/// For example, if one segment goes from (0,0) to (10,0) and another segment
	/// goes from (10,0) to (20,0) they would get combined into a
	/// single edge that goes from (0,0) to (20,0)
	///
	/// WARNING - The implementation assumes a "normalized" <see cref="Edge.StepDirection"/>.
	/// The input can use North or South but not both (same for East/West).
	/// </summary>
	private static List<Edge> CombineEdges(HashSet<Edge> segments)
	{
		var edges = new List<Edge>();
		while (segments.Count > 0)
		{
			var seg = segments.First();
			segments.Remove(seg);

			var start = seg.Start;
			var end = seg.End;
			int totalLength = seg.Length;

			bool done = false;
			while (!done)
			{
				var connection = segments
					.Where(s => s.StepDirection == seg.StepDirection)
					.Where(s => s.Start == end || s.End == start)
					.FirstOrDefault();

				if (connection == null)
				{
					done = true;
					break;
				}

				segments.Remove(connection);
				totalLength += connection.Length;

				if (connection.Start == end)
				{
					end = connection.End;
				}
				else if (connection.End == start)
				{
					start = connection.Start;
				}
				else
				{
					throw new Exception("assert fail");
				}
			}

			edges.Add(new Edge()
			{
				InsideDirection = seg.InsideDirection,
				StepDirection = seg.StepDirection,
				Start = start,
				Length = totalLength,
			});
		}

		return edges;
	}
}

public sealed class TODO
{
	public static I2DSampler<int> GenerateRandomHills(int scale, PRNG prng)
	{
		const int onlyTag = 42; // any value is fine

		bool inset = false;

		var unscaledSize = new XZ(72 / scale, 24 / scale);
		var tileTagger = new TileTagger<int>(unscaledSize, new XZ(scale, scale));

		for (int z = 0; z < unscaledSize.Z; z++)
		{
			tileTagger.AddTag(new XZ(0, z), onlyTag);
			tileTagger.AddTag(new XZ(unscaledSize.X - 1, z), onlyTag);
		}
		for (int x = 0; x < unscaledSize.X; x++)
		{
			tileTagger.AddTag(new XZ(x, 0), onlyTag);
		}

		var regions = tileTagger.GetRegions(onlyTag);

		const int maxElevation = 20;

		var cliffData = new List<(I2DSampler<QuaintCliff.Item> sampler, Edge edge)>();

		var allEdges = regions.SelectMany(r => r.Edges);
		foreach (var region in regions)
		{
			foreach (var edge in region.Edges)
			{
				var cliff = QuaintCliff.Generate(prng, edge.Length, maxElevation);
				var thickness = cliff.Bounds.Size.Z;

				if (edge.InsideDirection == CardinalDirection.North)
				{
					if (inset)
					{
						cliff = cliff.Rotate(0).Translate(edge.Start.Add(0, -thickness));
					}
					else
					{
						cliff = cliff.Rotate(0).Translate(edge.Start);
					}
				}
				else if (edge.InsideDirection == CardinalDirection.South)
				{
					if (inset)
					{
						cliff = cliff.Rotate(180).Translate(edge.Start); // inset handled naturally via rotation
					}
					else
					{
						cliff = cliff.Rotate(180).Translate(edge.Start.Add(0, -thickness));
					}
				}
				else if (edge.InsideDirection == CardinalDirection.East)
				{
					if (inset)
					{
						cliff = cliff.Rotate(90).Translate(edge.Start); // inset handled naturally via rotation
					}
					else
					{
						cliff = cliff.Rotate(90).Translate(edge.Start.Add(-thickness, 0));
					}
				}
				else if (edge.InsideDirection == CardinalDirection.West)
				{
					if (inset)
					{
						cliff = cliff.Rotate(270).Translate(edge.Start.Add(-thickness, 0));
					}
					else
					{
						cliff = cliff.Rotate(270).Translate(edge.Start);
					}
				}

				cliffData.Add((cliff, edge));
			}
		}

		IReadOnlyList<I2DSampler<Elevation>> corners = new List<I2DSampler<Elevation>>();
		if (!inset)
		{
			corners = BuildCorners(cliffData);
		}

		var cliffs = cliffData.Select(c => c.sampler);
		var bounds = Rect.Union(regions.Select(r => r.Bounds)
			.Concat(cliffs.Select(c => c.Bounds))
			.Concat(corners.Select(c => c.Bounds)));

		const int empty = -1;
		const int cliffMin = 0;
		var elevations = new MutableArray2D<int>(bounds, empty);

		foreach (var cliff in cliffs)
		{
			foreach (var xz in cliff.Bounds.Enumerate())
			{
				var sample = Math.Max(cliffMin, cliff.Sample(xz).y);
				var exist = elevations.Sample(xz);
				bool cliffsOverlap = exist > empty;
				if (cliffsOverlap)
				{
					if (inset)
					{
						sample = Math.Min(sample, exist);
					}
					else
					{
						sample = Math.Max(sample, exist);
					}
				}
				elevations.Put(xz, sample);
			}
		}

		foreach (var corner in corners)
		{
			foreach (var xz in corner.Bounds.Enumerate())
			{
				var sample = corner.Sample(xz);
				elevations.Put(xz, sample.Y);
			}
		}

		foreach (var region in regions)
		{
			foreach (var xz in region.Bounds.Enumerate())
			{
				// don't overwrite anything that came from a cliff
				if (region.Contains(xz) && elevations.Sample(xz) == empty)
				{
					elevations.Put(xz, maxElevation);
				}
			}
		}

		return elevations;
	}

	private static IReadOnlyList<I2DSampler<Elevation>> BuildCorners(IReadOnlyList<(I2DSampler<QuaintCliff.Item> sampler, Edge edge)> cliffData)
	{
		var corners = new List<I2DSampler<Elevation>>();

		foreach (var cliff in cliffData)
		{
			if (cliff.edge.InsideDirection == CardinalDirection.North)
			{
				var found = cliffData.FirstOrDefault(x => x.edge.InsideDirection == CardinalDirection.West && x.edge.End == cliff.edge.End);
				if (found.sampler != null)
				{
					var corner = BuildCorner(cliff.sampler, found.sampler);
					corner = corner.Translate(new XZ(cliff.sampler.Bounds.end.X, found.sampler.Bounds.end.Z));
					corners.Add(corner);
				}

				found = cliffData.FirstOrDefault(x => x.edge.InsideDirection == CardinalDirection.East && x.edge.End == cliff.edge.Start);
				if (found.sampler != null)
				{
					var corner = BuildCorner(cliff.sampler.SwapEastWest(), found.sampler.SwapEastWest());
					corner = corner.SwapEastWest().Translate(cliff.edge.Start.Add(-corner.Bounds.Size.X, 0));
					corners.Add(corner);
				}
			}
			else if (cliff.edge.InsideDirection == CardinalDirection.South)
			{
				var found = cliffData.FirstOrDefault(x => x.edge.InsideDirection == CardinalDirection.East && x.edge.Start == cliff.edge.Start);
				if (found.sampler != null)
				{
					var corner = BuildCorner(cliff.sampler.Rotate(180), found.sampler.Rotate(180));
					corner = corner.Rotate(180).Translate(cliff.edge.Start.Subtract(corner.Bounds.Size));
					corners.Add(corner);
				}

				found = cliffData.FirstOrDefault(x => x.edge.InsideDirection == CardinalDirection.West && x.edge.Start == cliff.edge.End);
				if (found.sampler != null)
				{
					var corner = BuildCorner(cliff.sampler.Rotate(180).SwapEastWest(), found.sampler.Rotate(180).SwapEastWest());
					corner = corner.Rotate(180).SwapEastWest().Translate(cliff.edge.End.Add(0, -corner.Bounds.Size.Z));
					corners.Add(corner);
				}
			}
		}

		return corners;
	}

	private static I2DSampler<Elevation> BuildCorner<T>(I2DSampler<T> cliffWest, I2DSampler<T> cliffNorth) where T : IHaveElevation
	{
		var size = new XZ(cliffNorth.Bounds.Size.X, cliffWest.Bounds.Size.Z);
		var array = new MutableArray2D<Elevation>(new Rect(XZ.Zero, size), new Elevation(-1));

		for (int x = 0; x < size.X; x++)
		{
			for (int z = 0; z < size.Z; z++)
			{
				var westSample = cliffWest.Sample(new XZ(cliffWest.Bounds.end.X - 1, cliffWest.Bounds.start.Z + z));
				var northSample = cliffNorth.Sample(new XZ(cliffNorth.Bounds.start.X + x, cliffNorth.Bounds.end.Z - 1));
				int minY = Math.Min(westSample.Y, northSample.Y);
				array.Put(new XZ(x, z), new Elevation(minY));
			}
		}

		return array;
	}
}