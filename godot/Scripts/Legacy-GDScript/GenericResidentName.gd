extends Object
class_name GenericResidentName

const DATABASE_PATH_MALE = "res://Info/MaleNames.txt"
const DATABASE_PATH_FEMALE = "res://Info/FemaleNames.txt"

static var _MaleNames: PackedStringArray
static var _FemaleNames: PackedStringArray

static func _load_database(force_reload: bool = false) -> void:
    if force_reload or _MaleNames == null or _MaleNames.is_empty():
        _MaleNames = FileAccess.get_file_as_string(DATABASE_PATH_MALE).split("\n")
    if force_reload or _FemaleNames == null or _FemaleNames.is_empty():
        _FemaleNames = FileAccess.get_file_as_string(DATABASE_PATH_FEMALE).split("\n")

static func get_name(id: int, gender: int) -> String:
    _load_database()

    if gender == 1:
        if id > len(_MaleNames):
            return ""
        else:
            return _MaleNames[id]
    else:
        if id > len(_FemaleNames):
            return ""
        else:
            return _FemaleNames[id]