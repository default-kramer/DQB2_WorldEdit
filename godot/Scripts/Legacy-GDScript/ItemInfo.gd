class_name ItemInfo

const DATABASE_PATH := "res://Info/Items.json"

static var _Database: Array[ItemInfo]

var ID: int
var Name: String = ""
var ImageID: int = -1
var Connecting: bool = false
var Rarity: int = 0
var Category: ItemCategory
var ShowInEditor: bool = true
var ShowAdvanced: bool = false
var SortIndex: float = 1.79769e308

var Unknown: bool = false

func _init(id: int = -1, unknown: bool = false) -> void:
	print("New ItemInfo " + str(id) + ", " + str(unknown))
	ID = id
	Unknown = unknown
	if (unknown):
		Name = "Unknown"

static func _load_database(force_reload: bool = false) -> void:
	if (force_reload or _Database == null or _Database.is_empty()):
		_Database = []
		Util.load_json_object_array(DATABASE_PATH, "res://Scripts/Info/ItemInfo.gd", _Database)
		

static func get_info(id: int) -> ItemInfo:
	_load_database()
	
	for item in _Database:
		if item.ID == id:
			return item
	return ItemInfo.new(id, true)
static func get_all() -> Array[ItemInfo]:
	_load_database()
	return _Database

func get_name_rich() -> String:
	var item_text: String = Name
	item_text = item_text.replace("{White}",  "[color=" + Constants.COLOUR_WHITE  + "]■[/color]")
	item_text = item_text.replace("{Black}",  "[color=" + Constants.COLOUR_BLACK  + "]■[/color]")
	item_text = item_text.replace("{Purple}", "[color=" + Constants.COLOUR_PURPLE + "]■[/color]")
	item_text = item_text.replace("{Pink}",   "[color=" + Constants.COLOUR_PINK   + "]■[/color]")
	item_text = item_text.replace("{Red}",    "[color=" + Constants.COLOUR_RED    + "]■[/color]")
	item_text = item_text.replace("{Green}",  "[color=" + Constants.COLOUR_GREEN  + "]■[/color]")
	item_text = item_text.replace("{Yellow}", "[color=" + Constants.COLOUR_YELLOW + "]■[/color]")
	item_text = item_text.replace("{Blue}",   "[color=" + Constants.COLOUR_BLUE   + "]■[/color]")
	return item_text

func get_icon() -> AtlasTexture:
	return Util.get_item_icon(ImageID)

enum ItemCategory
{
	CONSUMABLE = 0,
	FOOD = 1,
	BLUEPRINT,
	BUILDING_BLOCK,
	FURNITURE,
	DECORATIVE_ITEM,
	WALL_HANGING,
	DEBUG,
	UNUSED,
	UNOBTAINABLE,
	FIXTURE,
	FARMING_EQUIPMENT,
	MATERIAL,
	LIGHTING,
	MACHINERY,
	CRAFTING_STATION,
	FISH
}
