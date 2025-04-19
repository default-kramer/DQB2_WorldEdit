using System;
using System.IO;
using Godot;

public partial class CSharpExtensions : GodotObject
{
	public static byte[] DecompressZLib(byte[] data)
	{
		try
		{
			VoxelTerrain x = null;

			using var input = new MemoryStream(data);
			using var zlib = new System.IO.Compression.ZLibStream(input, System.IO.Compression.CompressionMode.Decompress);
			using var output = new MemoryStream();
			zlib.CopyTo(output);

			output.Flush();
			return output.ToArray();
			var c = new VoxelTerrain();
		}
		catch
		{
			return Array.Empty<byte>();
		}
	}
	public static byte[] CompressZLib(byte[] data)
	{
		try
		{
			System.IO.Compression.CompressionLevel compressionLevel = System.IO.Compression.CompressionLevel.Fastest;
			using var input = new MemoryStream(data);
			using var output = new MemoryStream();
			using (var zlib = new System.IO.Compression.ZLibStream(output, compressionLevel))
			{
				input.CopyTo(zlib);
				zlib.Flush();
			}
			return output.ToArray();
		}
		catch
		{
			return Array.Empty<byte>();
		}
	}

    public static long FileTimeToUnixTime(long filetime)
    {
        return ((DateTimeOffset)DateTime.FromFileTime(filetime)).ToUnixTimeSeconds();
    }
}