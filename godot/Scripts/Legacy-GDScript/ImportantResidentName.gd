extends Object
class_name ImportantResidentName

const DATABASE_PATH: String = "res://Info/StoryPeopleNames.txt"

static var _Database: PackedStringArray

static func _load_database(force_reload: bool = false) -> void:
    if force_reload or _Database == null or _Database.is_empty():
        _Database = FileAccess.get_file_as_string(DATABASE_PATH).split("\n")

static func get_name(id: int) -> String:
    _load_database()

    if id > len(_Database):
        return ""
    else:
        return _Database[id]