extends SaveData
class_name CommonData

const HeaderLength: int = 0x2A444

const ThumbnailAddress: int = 0x10D
const ThumbnailSize: int = 320 * 180 * 3

const HotbarItemAddress: int = 0x55B28D
const HotbarItemCount: int = 15
const BagItemAddress: int = 0x55B2C9
const BagItemCount: int = 420

const ImportantResidentAddress: int = 0x6ACC8
const ResidentAddress: int = 0x102A68
const ImportantResidentCount: int = 1023
const ResidentCount: int = 238
const ResidentSize: int = 608

static var Instance: CommonData
static func has_instance() -> bool:
	return Instance != null and Instance.IsLoaded
func is_instance() -> bool:
	return Instance == self

var LastSaveTime: int:
	get:
		return CSharpExtensions.filetime_to_unix_time(Header.decode_s64(0x2A40D))

var FromIsland: int:
	get:
		return Header.decode_u8(0xC9)
	set(value):
		Header.encode_u8(0xC9, value)
var ToIsland: int:
	get:
		return Header.decode_u8(0xC8)
	set(value):
		Header.encode_u8(0xC8, value)

var PlayerName: String:
	get:
		return get_string(0xCD, 12, true)
	set(value):
		set_string(0xCD, 12, value, true)
var PlayerSex: bool:
	get:
		return Header.decode_u8(0xC4)
	set(value):
		Header.encode_u8(0xC4, value)
var PlayerLevel: int:
	get:
		return Buffer.decode_u8(0xCA9CF)
	set(value):
		Buffer.encode_u8(0xCA9CF, value)
var PlayerExperience: int: #CHECKME if signed
	get:
		return Buffer.decode_s16(0x6A9D1)
	set(value):
		Buffer.encode_s16(0x6A9D1, value)
var PlayerHP: int: #CHECKME if signed
	get:
		return Buffer.decode_s16(0x6A890)
	set(value):
		Buffer.encode_s16(0x6A890, value)
var PlayerAdditionalHP: int: #CHECKME if signed
	get:
		return Buffer.decode_s16(0x6A892)
	set(value):
		Buffer.encode_s16(0x6A892, value)
var PlayerTotalHP: int:
	get:
		return PlayerHP + PlayerAdditionalHP
var PlayerHunger: int: #CHECKME if signed
	get:
		return Buffer.decode_s16(0x6A896)
	set(value):
		Buffer.encode_s16(0x6A896, value)
var PlayerStamina: int: #CHECKME if signed
	get:
		return Buffer.decode_s16(0x6A8A0)
	set(value):
		Buffer.encode_s16(0x6A8A0, value)
var PlayerAttack: int: #CHECKME if signed
	get:
		return Buffer.decode_s16(0x6A898)
	set(value):
		Buffer.encode_s16(0x6A898, value)
var PlayerDefence: int: #CHECKME if signed
	get:
		return Buffer.decode_s16(0x6A89A)
	set(value):
		Buffer.encode_s16(0x6A89A, value)

var UnlockBag: bool:
	get:
		return get_bit(0x635, 0)
	set(value):
		set_bit(0x635, 0, value)
var UnlockWindbraker: bool:
	get:
		return get_bit(0x6A8A2, 1)
	set(value):
		set_bit(0x6A8A2, 1, value)
var UnlockFlipper: bool: #CHECKME
	get:
		return get_bit(0x6A8A3, 1)
	set(value):
		set_bit(0x6A8A3, 1, value)
var UnlockBigBash: bool:
	get:
		return get_bit(0x506, 1)
	set(value):
		set_bit(0x506, 1, value)
var UnlockBiggerBash: bool:
	get:
		return get_bit(0x502, 3)
	set(value):
		set_bit(0x502, 3, value)
var BottomlessPotUse: bool: #CHECKME
	get:
		return get_bit(0x504, 2)
	set(value):
		set_bit(0x504, 2, value)
var BottomlessPot: bool: #CHECKME
	get:
		return get_bit(0x67D, 1)
	set(value):
		set_bit(0x67D, 1, value)
var UnlockBuildnoculars: bool: #CHECKME
	get:
		return get_bit(0x502, 7)
	set(value):
		set_bit(0x502, 7, value)

var CarFly: bool:
	get:
		return get_bit(0x506, 6)
	set(value):
		set_bit(0x506, 6, value)
var CarBeam: bool:
	get:
		return get_bit(0x506, 7)
	set(value):
		set_bit(0x506, 7, value)
var CarLight: bool:
	get:
		return get_bit(0x506, 5)
	set(value):
		set_bit(0x506, 5, value)

var Transform: bool: #CHECKME
	get:
		return get_bit(0x500, 6)
	set(value):
		set_bit(0x500, 6, value)
var PlayerExpression: bool: #CHECKME
	get:
		return get_bit(0x501, 1)
	set(value):
		set_bit(0x501, 1, value)

var MiniMedals: int:
	get:
		return Buffer.decode_u8(0x226E40)
	set(value):
		Buffer.encode_u8(0x226E40, value)
var MiniMedalsDeposited: int:
	get:
		return Buffer.decode_u8(0x226E44)
	set(value):
		Buffer.encode_u8(0x226E44, value)

var HotbarInventory: Array[DQB2Item] = _create_items(HotbarItemAddress, HotbarItemCount)
var BagInventory: Array[DQB2Item] = _create_items(BagItemAddress, BagItemCount)

var PlayerWeapon: DQB2Item = DQB2Item.new(self, 0x55B959)
var PlayerArmour: DQB2Item = DQB2Item.new(self, 0x55B989)
var PlayerShield: DQB2Item = DQB2Item.new(self, 0x55B985)
var PlayerHammer: DQB2Item = DQB2Item.new(self, 0x55B95D)

var ImportantResidents: Array[Resident] = _create_important_residents()
var Residents: Array[Resident] = _create_residents()

static func try_load_and_set(path: String) -> CommonData:
	var common_data = try_load(path)
	if common_data != null:
		Instance = common_data
		return common_data
	return null
static func try_load(path: String) -> CommonData:
	var common_data: CommonData = CommonData.new()
	if (common_data._try_load(path, HeaderLength) == Error.OK):
		return common_data
	return null
static func quick_load(path: String) -> CommonData:
	var common_data: CommonData = CommonData.new()
	if common_data._quick_load(path, HeaderLength) == Error.OK:
		return common_data
	return null

func _create_items(item_start: int, item_count: int) -> Array[DQB2Item]:
	var inv: Array[DQB2Item] = []
	inv.resize(item_count)
	for i in range(len(inv)):
		inv[i] = DQB2Item.new(self, item_start + i * 4)
	return inv
func _create_important_residents() -> Array[Resident]:
	var residents: Array[Resident] = []
	residents.resize(ImportantResidentCount)
	for i in range(len(residents)):
		residents[i] = Resident.new(self, i + 1)
	return residents
func _create_residents() -> Array[Resident]:
	var residents: Array[Resident] = []
	residents.resize(ResidentCount)
	for i in range(len(residents)):
		residents[i] = Resident.new(self, ImportantResidentCount + i + 1)
	return residents

static func close() -> void:
	Instance = null

func make_instance() -> void:
	Instance = self

func get_thumbnail() -> Image:
	var image: Image = Image.create_empty(320, 180, false, Image.FORMAT_RGB8)
	var xspan: PackedByteArray = Header.slice(ThumbnailAddress, ThumbnailAddress + ThumbnailSize)
	for y in range(180):
		for x in range(320):
			var color: Color = Color()
			color.b8 = xspan[(y * 320 * 3) + (x * 3)]
			color.g8 = xspan[(y * 320 * 3) + (x * 3) + 1]
			color.r8 = xspan[(y * 320 * 3) + (x * 3) + 2]
			image.set_pixel(x, y, color)
	return image

func clear_hotbar() -> void:
	for item in HotbarInventory:
		item.ItemID = 0
		item.Count = 0
func clear_bag() -> void:
	for item in BagInventory:
		item.ItemID = 0
		item.Count = 0

class Resident:
	var _SaveData: CommonData
	var ID: int
	var Address: int:
		get:
			return (ID - 1) * ResidentSize + ImportantResidentAddress
	
	var is_important: bool:
		get:
			return (ID - 1) <= ImportantResidentCount
	
	var Name: String:
		get:
			return _SaveData.get_string(Address, 30)
		set(value):
			_SaveData.set_string(Address, 30, value)
	var UseCustomName: bool:
		get:
			return _SaveData.get_bit(Address + 301, 7)
		set(value):
			_SaveData.set_bit(Address + 301, 7, value)
	var GenericName: int:
		get:
			return _SaveData.Buffer.decode_u8(Address + 274)
		set(value):
			_SaveData.Buffer.encode_u8(Address + 274, value)
	
	var Sex: int:
		get:
			return _SaveData.Buffer.decode_u8(Address + 258)
		set(value):
			_SaveData.Buffer.encode_u8(Address + 258, value)
	var Job: int:
		get:
			return _SaveData.Buffer.decode_u8(Address + 271)
		set(value):
			_SaveData.Buffer.encode_u8(Address + 271, value)
	var Type: int:
		get:
			return _SaveData.Buffer.decode_u16(Address + 144)
		set(value):
			_SaveData.Buffer.encode_u16(Address + 144, value)
	
	var HomeIsland: int:
		get:
			return _SaveData.Buffer.decode_u8(Address + 275)
		set(value):
			_SaveData.Buffer.encode_u8(Address + 275, value)
	var CurrentIsland: int:
		get:
			return _SaveData.Buffer.decode_u8(Address + 223)
		set(value):
			_SaveData.Buffer.encode_u8(Address + 223, value)
	var IslandRegion: int:
		get:
			return _SaveData.Buffer.decode_u8(Address + 324)
		set(value):
			_SaveData.Buffer.encode_u8(Address + 324, value)
	
	var HP: int: #CHECKME if signed
		get:
			return _SaveData.Buffer.decode_s16(Address + 146)
		set(value):
			_SaveData.Buffer.encode_s16(Address + 146, value)
	
	var CanEquip: bool:
		get:
			return _SaveData.get_bit(Address + 307, 1)
		set(value):
			_SaveData.set_bit(Address + 307, 1, value)
	var CanBattle: bool:
		get:
			return _SaveData.get_bit(Address + 259, 1)
		set(value):
			_SaveData.set_bit(Address + 259, 1, value)
	var CanJoin: bool #CHECKME
	
	var LockGraphic: bool:
		get:
			return _SaveData.get_bit(Address + 302, 4)
		set(value):
			_SaveData.set_bit(Address + 302, 4, value)
	var Face: int:
		get:
			return _SaveData.Buffer.decode_u16(Address + 229)
		set(value):
			_SaveData.Buffer.encode_u16(Address + 229, value)
	var Body: int:
		get:
			return _SaveData.Buffer.decode_u16(Address + 233)
		set(value):
			_SaveData.Buffer.encode_u16(Address + 233, value)
	var Hair: int:
		get:
			return _SaveData.Buffer.decode_u16(Address + 231)
		set(value):
			_SaveData.Buffer.encode_u16(Address + 231, value)
	var SkinColour: int:
		get:
			return _SaveData.Buffer.decode_u16(Address + 239)
		set(value):
			_SaveData.Buffer.encode_u16(Address + 239, value)
	var HairColour: int:
		get:
			return _SaveData.Buffer.decode_u16(Address + 237)
		set(value):
			_SaveData.Buffer.encode_u16(Address + 237, value)
	var EyeColour: int:
		get:
			return _SaveData.Buffer.decode_u16(Address + 235)
		set(value):
			_SaveData.Buffer.encode_u16(Address + 235, value)
	
	var VoiceType: int:
		get:
			return _SaveData.Buffer.decode_u8(Address + 267)
		set(value):
			_SaveData.Buffer.encode_u8(Address + 267, value)
	var MessageType: int:
		get:
			return _SaveData.Buffer.decode_u8(Address + 266)
		set(value):
			_SaveData.Buffer.encode_u8(Address + 266, value)
	
	var RoomSize: int:
		get:
			return _SaveData.Buffer.decode_u8(Address + 263)
		set(value):
			_SaveData.Buffer.encode_u8(Address + 263, value)
	var RoomFanciness: int:
		get:
			return _SaveData.Buffer.decode_u8(Address + 264)
		set(value):
			_SaveData.Buffer.encode_u8(Address + 264, value)
	var RoomAmbience: int:
		get:
			return _SaveData.Buffer.decode_u8(Address + 265)
		set(value):
			_SaveData.Buffer.encode_u8(Address + 265, value)
	
	func _init(save_data: CommonData, id: int) -> void:
		_SaveData = save_data
		ID = id
	
	func get_display_name() -> String:
		if (Name.is_empty() or Name == null):
			return Name
		
		if (is_important):
			return ImportantResidentName.get_name(ID)
		else:
			return GenericResidentName.get_name(ID, Sex)
