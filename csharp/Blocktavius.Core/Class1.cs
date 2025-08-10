namespace Blocktavius.Core;

public record struct XZ(int X, int Z)
{
	public XZ Add(int dx, int dz) => new XZ(X + dx, Z + dz);

	public XZ Add(XZ xz) => Add(xz.X, xz.Z);

	public XZ Subtract(XZ xz) => new XZ(X - xz.X, Z - xz.Z);
}

public record CompassDirection(int dX, int dZ)
{
}

public sealed record CardinalDirection : CompassDirection
{
	private CardinalDirection(int dX, int dZ) : base(dX, dZ) { }

	public static readonly CardinalDirection North = new(0, -1);
	public static readonly CardinalDirection South = new(0, 1);
	public static readonly CardinalDirection East = new(1, 0);
	public static readonly CardinalDirection West = new(-1, 0);
}

public sealed record OrdinalDirection : CompassDirection
{
	private OrdinalDirection(int dX, int dZ) : base(dX, dZ) { }

	public static readonly OrdinalDirection NorthEast = new(1, -1);
	public static readonly OrdinalDirection SouthEast = new(1, 1);
	public static readonly OrdinalDirection SouthWest = new(-1, 1);
	public static readonly OrdinalDirection NorthWest = new(-1, -1);
}

public record Rect(XZ start, XZ end)
{
	public static readonly Rect Zero = new(new XZ(0, 0), new XZ(0, 0));

	public bool Contains(XZ xz) => GetIndex(xz).HasValue;

	/// <summary>
	/// If the given <paramref name="xz"/> is within this box, returns a unique index
	/// for that xz in the inclusive range 0 .. (Width*Height - 1).
	/// </summary>
	public int? GetIndex(XZ xz)
	{
		if (xz.X >= end.X || xz.Z >= end.Z)
		{
			return null;
		}

		int zIndex = xz.Z - start.Z;
		if (zIndex < 0)
		{
			return null;
		}

		int xIndex = xz.X - start.X;
		if (xIndex < 0)
		{
			return null;
		}

		int width = end.X - start.X;
		return zIndex * width + xIndex;
	}

	public XZ Size => new XZ(end.X - start.X, end.Z - start.Z);

	public static Rect Union(IEnumerable<Rect> boxes)
	{
		int minX = int.MaxValue;
		int minZ = int.MaxValue;

		int maxX = int.MinValue;
		int maxZ = int.MinValue;

		bool any = false;

		foreach (var box in boxes)
		{
			any = true;

			var start = box.start;
			var end = box.end;

			minX = Math.Min(minX, start.X);
			minZ = Math.Min(minZ, start.Z);

			maxX = Math.Max(maxX, end.X);
			maxZ = Math.Max(maxZ, end.Z);
		}

		if (!any)
		{
			return Rect.Zero;
		}

		return new Rect(new XZ(minX, minZ), new XZ(maxX, maxZ));
	}

	public Rect Translate(XZ xz) => new Rect(this.start.Add(xz), this.end.Add(xz));
}

public interface I2DSampler<T>
{
	Rect Bounds { get; }
	T Sample(XZ xz);
}

sealed class MutableArray2D<T> : I2DSampler<T>
{
	private readonly T[] array;
	private readonly T defaultValue;
	public Rect Bounds { get; }

	public MutableArray2D(Rect bounds, T defaultValue)
	{
		this.defaultValue = defaultValue;
		Bounds = bounds;
		array = new T[bounds.Size.X * bounds.Size.Z];
		array.AsSpan().Fill(defaultValue);
	}

	public T Sample(XZ xz)
	{
		var index = Bounds.GetIndex(xz);
		if (index.HasValue)
		{
			return array[index.Value];
		}
		return defaultValue;
	}

	public void Put(XZ xz, T value)
	{
		var index = Bounds.GetIndex(xz);
		if (!index.HasValue)
		{
			throw new ArgumentOutOfRangeException(nameof(xz));
		}
		array[index.Value] = value;
	}
}
