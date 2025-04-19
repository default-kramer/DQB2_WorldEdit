extends SaveData
class_name StageData

# ------------------------------------------------------------------------------
# Here follows a general map of the buffer's layout.
# 
# Chunk Grid: 0x24C7C1 - 0x24E7C0
# ???: 0x24E7C1 - 0x24E7D0
# Decorations: 0x24E7D1 - 0x150E7D0
# Blocks: 0x183FEF0 - end?
# ------------------------------------------------------------------------------

const HeaderLength: int = 0x110

# Starting address of the chunk grid
# (an array of ushorts pointing to chunk indices and where they are positionally in the world).
const ChunkGridAddress: int = 0x24C7C1
# Size of chunk grid (each entry is 2 bytes).
const ChunkGridSize: int = 0x1000

# Starting address of block data.
# Block data is an array of ushorts broken up into chunks, then layers, then blocks.
const BlockAddress: int = 0x183FEF0
# Size of a full chunk of blocks.
const ChunkSize: int = 0x30000
# Size of a full layer of blocks.
const LayerSize: int = 0x800

# Starting address of the decoration list - 
# a list of decoration IDs and the chunk indices to which they belong.
const DecorationListAddress: int = 0x150E7D1
# Size of decoration list (Each entry is 4 bytes).
const DecorationListSize: int = 0xC8000
# Start of decoration data.
const DecorationAddress: int = 0x24E7D1
const DecorationSize: int = 24

static var Instance: StageData
static func has_instance() -> bool:
	return Instance != null and Instance.IsLoaded

var IslandID: int:
	get:
		return Buffer.decode_u8(0xC0ED6)
	set(value):
		Buffer.encode_u8(0xC0ED6, value)
var ChunkCount: int:
	get:
		return Buffer.decode_u16(0x1451AF)
	set(value):
		Buffer.encode_u16(0x1451AF, value)
var Gratitude: int:
	get:
		return Buffer.decode_s32(0xC0ECC)
	set(value):
		Buffer.encode_s32(0xC0ECC, value)
var CurrentTime: float:
	get:
		return Buffer.decode_float(0xC0F50)
	set(value):
		Buffer.encode_float(0xC0F50, value)
var Weather: int:
	get:
		return Buffer.decode_u8(0xC0F54)
	set(value):
		Buffer.encode_u8(0xC0F54, value)

var Chunks: Array[Chunk] = _create_chunks()
var Decorations: Array[Decoration] = _create_decorations()

static func try_load_and_set(path: String) -> StageData:
	var stage_data = try_load(path)
	if stage_data != null:
		Instance = stage_data
		return stage_data
	return null
static func try_load(path: String) -> StageData:
	var stage_data: StageData = StageData.new()
	if stage_data._try_load(path, HeaderLength) == Error.OK:
		return stage_data
	return null
func _create_chunks() -> Array[Chunk]:
	var chunks: Array[Chunk] = []
	chunks.resize(ChunkGridSize)
	for i in range(len(chunks)):
		chunks[i] = Chunk.new(self, i)
	return chunks
func _create_decorations() -> Array[Decoration]:
	var decorations: Array[Decoration] = []
	decorations.resize(DecorationListSize)
	for i in range(len(decorations)):
		decorations[i] = Decoration.new(self, i)
	return decorations

static func close() -> void:
	Instance = null

func make_instance() -> void:
	Instance = self

func get_chunk_id_by_index(index: int) -> int:
	return Buffer.decode_u16(ChunkGridAddress + index * 2)
func get_used_chunks() -> Array[Chunk]:
	return Chunks.filter(func(chunk): return chunk.is_used)
func get_chunk_by_id(id: int) -> Chunk:
	for chunk in Chunks:
		if chunk.ID == id:
			return chunk
	return null
func get_chunk_by_index(index: int) -> Chunk:
	return Chunks[index]

func get_block_at_position(position: Vector3i) -> int:
	# FIXME
	if (position.x == 0xFFFF or position.y < 0 or position.y > 96):
		return 0xFFFF
	var index: int = BlockAddress + (position.x * ChunkSize) + (position.y * LayerSize) + (position.z * 2)
	return Buffer.decode_u16(index)
func get_block_at_position_euclid(position: Vector3i) -> int:
	# X is east-west
	# Z is north-south
	return get_block_at_position(euclid_pos_to_index(position))

func euclid_pos_to_index(position: Vector3i) -> Vector3i:
	# FIXME
	# The negative numbers are fake! point zero is an illusion!
	@warning_ignore("integer_division")
	var chunk_grid_index: int = (floor((position.x + 1024) / 32) + floor((position.z + 1024) / 32) * 64)
	var chunk_number: int = get_chunk_id_by_index(chunk_grid_index)
	var tile: int = (position.x + 1024) % 32 + ((position.z + 1024) % 32 * 32)
	return Vector3i(chunk_number,position.y,tile)

func make_superflat(layers: PackedInt32Array) -> void:
	for chunk in get_used_chunks():
		chunk.clear()
		for i in range(len(layers)):
			chunk.set_layer(i, layers[i])
		# BIG NOTE: if there's a block with additional data (like a lore book generated with the world that u can read) removing it will break the superflat

class Chunk:
	var _SaveData: StageData
	var Index: int = -1
	var ID: int:
		get:
			return _SaveData.get_chunk_id_by_index(Index)
	
	var EuclidOrigin: Vector3i:
		get:
			@warning_ignore("integer_division")
			return Vector3i((Index % 64 * 32) - 1024, 0, (Index / 64 * 32) - 1024)
	
	func _init(save_data: StageData, index: int) -> void:
		_SaveData = save_data
		Index = index
	
	var is_used: bool:
		get:
			return ID != 0xFFFF
	
	var BlockAddress: int:
		get:
			return BlockAddress + ChunkSize * ID
	
	func get_block(layer: int, index: int) -> int:
		return _SaveData.decode_u16(BlockAddress + layer * LayerSize + index * 2) if is_used else 0xFFFF
	func get_blocks() -> PackedInt32Array:
		var blocks: PackedInt32Array = []
		@warning_ignore("integer_division")
		blocks.resize(ChunkSize / 2)
		for i in range(len(blocks)):
			blocks[i] = _SaveData.Buffer.decode_u16(BlockAddress + i * 2)
		return blocks
	func get_blocks_and_euclid_pos() -> Array[Array]:
		var blocks: Array[Array] = []
		var x: int = 0
		var y: int = 0
		var z: int = 0
		for block in get_blocks():
			blocks.append([Vector3i(x,y,z), block])

			x += 1
			if x >= 32:
				x = 0
				z += 1
			if z >= 32:
				z = 0
				y += 1
		return blocks
	func set_block(layer: int, index: int, block: int) -> void:
		if not is_used:
			return
		_SaveData.encode_u16(BlockAddress + layer * LayerSize + index * 2, block)
	func set_layer(layer: int, block: int) -> void:
		if not is_used:
			return
		@warning_ignore("integer_division")
		for i in range(LayerSize / 2):
			set_block(layer, i, block)
	
	func clear() -> void:
		for i in range(ChunkSize):
			_SaveData.encode_u8(BlockAddress + i, 0)

class Decoration:
	var _SaveData: SaveData
	var Position: int
	
	var ListAddress: int:
		get:
			return DecorationListAddress + Position * 4
	
	var ChunkIndex: int:
		get:
			return _SaveData.get_number_bitwise(ListAddress, 0, 12)
		set(value):
			_SaveData.set_number_bitwise(ListAddress, 0, 12, value)
	var Index: int:
		get:
			return _SaveData.get_number_bitwise(ListAddress, 12, 20)
		set(value):
			_SaveData.set_number_bitwise(ListAddress, 12, 20, value)
	
	var DataAddress: int:
		get:
			return DecorationAddress + Index * DecorationSize
	
	var DecorationID: int:
		get:
			return _SaveData.get_number_bitwise(DataAddress + 8, 0, 13)
		set(value):
			_SaveData.set_number_bitwise(DataAddress + 8, 0, 13, value)
	
	var X: int:
		get:
			return _SaveData.get_number_bitwise(DataAddress + 9, 5, 5)
		set(value):
			_SaveData.set_number_bitwise(DataAddress + 9, 5, 5, value)
	var Y: int:
		get:
			return _SaveData.get_number_bitwise(DataAddress + 10, 2, 7)
		set(value):
			_SaveData.set_number_bitwise(DataAddress + 10, 2, 7, value)
	var Z: int:
		get:
			return _SaveData.get_number_bitwise(DataAddress + 11, 1, 5)
		set(value):
			_SaveData.set_number_bitwise(DataAddress + 11, 1, 5, value)
	var Rotation: int:
		get:
			return _SaveData.get_number_bitwise(DataAddress + 11, 6, 2)
		set(value):
			_SaveData.set_number_bitwise(DataAddress + 11, 6, 2, value)
	
	func _init(save_data: StageData, position: int) -> void:
		_SaveData = save_data
		Position = position
