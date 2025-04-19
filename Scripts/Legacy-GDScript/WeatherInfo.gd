extends Object
class_name WeatherInfo

const DATABASE_PATH: String = "res://Info/Weather.json"

static var _Database: Array[WeatherInfo]

var ID: int = -1
var Name: String

var Unknown: bool = false

func _init(id: int, unknown: bool = false) -> void:
    ID = id
    Unknown = unknown
    if unknown:
        Name = "Unknown"

static func _load_database(force_reload: bool = false) -> void:
    if force_reload or _Database == null or _Database.is_empty():
        _Database = JSON.parse_string(FileAccess.get_file_as_string(DATABASE_PATH))

static func get_weather(id: int) -> WeatherInfo:
    _load_database()

    for weather in _Database:
        if weather.ID == id:
            return weather
    return WeatherInfo.new(id, true)
static func get_all_weathers() -> Array[WeatherInfo]:
    return _Database