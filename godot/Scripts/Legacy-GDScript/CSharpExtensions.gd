class_name CSharpExtensions

const CSharp_Script: CSharpScript = preload("res://Scripts/CSharp/CSharpExtensions.cs")
static var CSharp_Instance: Object = CSharp_Script.new()

static func decompress_zlib(data: PackedByteArray) -> PackedByteArray:
	return CSharp_Instance.DecompressZLib(data)
static func compress_zlib(data: PackedByteArray) -> PackedByteArray:
	return CSharp_Instance.CompressZLib(data)

static func filetime_to_unix_time(filetime: int) -> int:
	return CSharp_Instance.FileTimeToUnixTime(filetime)
