extends Object
class_name BlockInfo

const DATABASE_PATH: String = "res://Info/Blocks.json"

static var _Database: Array[BlockInfo]

var ID: int
var Name: String = ""
var ImageID: int = -1
var Tags: PackedStringArray = []
var SortIndex: float = 1.79769e308
var VoxelID: int = -1

var Unknown: bool = false

var Variants: Dictionary
var BaseVariant: int = -1

func _init(id: int, unknown: bool = false) -> void:
    ID = id
    Unknown = unknown
    if unknown:
        Name = "Unknown"

static func _load_database(force_reload: bool = false):
    if force_reload or _Database == null or _Database.is_empty():
        _Database = JSON.parse_string(FileAccess.get_file_as_string(DATABASE_PATH))

static func get_block(id: int) -> BlockInfo:
    _load_database()
        
    for block in _Database:
        if block.ID == id:
            return block
    return BlockInfo.new(id, true)
static func get_all_blocks() -> Array[BlockInfo]:
    return _Database

func get_icon() -> AtlasTexture:
    return Util.get_item_icon(ImageID)