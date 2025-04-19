class_name DQB2Item

var _SaveData: SaveData
var Address: int

var ItemID: int:
	get:
		return _SaveData.Buffer.decode_u16(Address)
	set(value):
		_SaveData.Buffer.encode_u16(Address, value)
var Count: int: #CHECKME if signed
	get:
		return _SaveData.Buffer.decode_s16(Address + 2)
	set(value):
		_SaveData.Buffer.encode_s16(Address + 2, value)

func _init(save_data: SaveData, address: int) -> void:
	_SaveData = save_data
	Address = address

func get_info() -> ItemInfo:
	return ItemInfo.get_info(ItemID)
