extends Object
class_name DecorationInfo

const DATABASE_PATH: String = "res://Info/Decorations.json"

static var _Database: Array[DecorationInfo]

var ID: int = -1
var Name: String

var Unknown: bool = false

func _init(id: int, unknown: bool = false):
    ID = id
    Unknown = unknown
    if unknown:
        Name = "Unknown"

static func _load_database(force_reload: bool = false) -> void:
    if force_reload or _Database == null or _Database.is_empty():
        _Database = JSON.parse_string(FileAccess.get_file_as_string(DATABASE_PATH))

static func get_decoration(id: int) -> DecorationInfo:
    _load_database()

    for decoration in _Database:
        if decoration.ID == id:
            return decoration
    return DecorationInfo.new(id, true)
static func get_all_decorations() -> Array[DecorationInfo]:
    return _Database