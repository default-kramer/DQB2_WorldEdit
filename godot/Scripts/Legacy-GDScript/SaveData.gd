extends Object
class_name SaveData

const ExpectedFileHeader := "aerC"

var Filename: String

var Header: PackedByteArray
var Buffer: PackedByteArray

var IsLoaded := false
var UnsavedChanges := false

func _try_load(path: String, header_length: int) -> Error:
	var file_bytes = FileAccess.get_file_as_bytes(path)
	
	if (file_bytes.slice(0,4).get_string_from_utf8() != ExpectedFileHeader):
		return Error.FAILED
	
	var header := file_bytes.slice(0, header_length)
	var buffer := file_bytes.slice(header_length)

	if len(buffer) > 0:
		buffer = decompress(buffer)
		if len(buffer) <= 0:
			return Error.FAILED
	
	Filename = path
	Header = header
	Buffer = buffer
	UnsavedChanges = false
	IsLoaded = true
	return Error.OK
func _quick_load(path: String, header_length: int) -> Error:
	Filename = path
	
	var file_bytes = FileAccess.get_file_as_bytes(path)
	
	if (file_bytes.slice(0,4).get_string_from_utf8() != ExpectedFileHeader):
		return Error.FAILED
	
	Header = file_bytes.slice(0, header_length)
	Buffer = file_bytes.slice(header_length)
	
	return Error.OK

func save(path: String = "") -> void:
	path = Filename if path.is_empty() else path
	
	var data := Header
	data.append_array(compress(Buffer))
	var size: int = len(data)
	data.encode_u32(0x10, size)
	
	var file = FileAccess.open(path, FileAccess.WRITE)
	file.store_buffer(data)
	
	UnsavedChanges = false
func export(path: String) -> void:
	var file = FileAccess.open(path, FileAccess.WRITE)
	file.store_buffer(Buffer)
func import(path: String) -> void:
	Buffer = FileAccess.get_file_as_bytes(path)

static func decompress(data: PackedByteArray) -> PackedByteArray:
	return CSharpExtensions.decompress_zlib(data)
static func compress(data: PackedByteArray) -> PackedByteArray:
	return CSharpExtensions.compress_zlib(data)

func get_bit(address: int, bit: int, header: bool = false) -> bool:
	if (bit > 7):
		push_error("Too big")
	if (bit < 0):
		push_error("Too small")
	
	return ((Header if header else Buffer)[address] & (1 << bit)) != 0
func set_bit(address: int, bit: int, value: bool, header: bool = false) -> void:
	if (bit > 7):
		push_error("Too big")
	if (bit < 0):
		push_error("Too small")
	
	var bytes: PackedByteArray = Header if header else Buffer
	var left: int = (1 if value else 0) << bit
	var right: int = bytes[address] & ((1 << bit) ^ 0b11111111)
	bytes.encode_u8(address, left | right)
	UnsavedChanges = true
func get_number_bitwise(address: int, bit: int, bit_count: int, header: bool = false) -> int:
	if (bit > 31 || bit_count > 31):
		push_error("Integers larger than 32 bits are not supported.")
	if (bit < 0 || bit_count < 0):
		push_error("Negative numbers are not allowed.")
	
	var left: int = (Header if header else Buffer).decode_u32(address) >> bit
	var right: int = (1 << bit_count) - 1
	return left & right
func set_number_bitwise(address: int, bit: int, bit_count: int, value: int, header: bool = false) -> void:
	if (bit > 31 || bit_count > 31):
		push_error("Integers larger than 32 bits are not supported.")
	if (bit < 0 || bit_count < 0):
		push_error("Negative numbers are not allowed.")
	
	var bytes := (Header if header else Buffer)
	var bit_mask: int = (1 << bit_count) - 1
	var new_value: int = (value & bit_mask) << bit
	var old_value: int = bytes.decode_u32(address) & ((bit_mask << bit) ^ 0b11111111_11111111_11111111_11111111)
	bytes.encode_u32(address, old_value | new_value)
	UnsavedChanges = true

func get_string(address: int, length: int, header: bool = false) -> String:
	return (Header if header else Buffer).slice(address, address + length).get_string_from_utf8().rstrip("\u0000")
func set_string(address: int, length: int, value: String, header: bool = false) -> void:
	var string_bytes: PackedByteArray = value.to_utf8_buffer()
	var target: PackedByteArray = Header if header else Buffer
	for i in range(length):
		if i < len(string_bytes):
			target[i + address] = string_bytes[i]
		else:
			target[i + address] = 0

func fill(value: int, address: int = -1, length: int = -1, header: bool = false) -> void:
	var bytes := (Header if header else Buffer)
	address = address if address >= 0 else 0
	length = length if length >= 0 else len(bytes) - address
	for i in range(length):
		bytes[address + i] = value
	UnsavedChanges = true
