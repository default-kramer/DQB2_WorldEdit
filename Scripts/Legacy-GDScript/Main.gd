extends Control

@onready var _FileDialog: FileDialog = $FileDialog

@onready var _UnsavedChanges_Window: Window = $UnsavedChangesWindow

@onready var _File_PopupMenu: PopupMenu = $MenuBar/File
@onready var _File_SaveSingleFile_PopupMenu: PopupMenu = $MenuBar/File/SaveSingleFileMenu
@onready var _File_SaveAsSingleFile_PopupMenu: PopupMenu = $MenuBar/File/SaveAsSingleFileMenu
@onready var _File_Export_PopupMenu: PopupMenu = $MenuBar/File/ExportMenu
@onready var _File_Import_PopupMenu: PopupMenu = $MenuBar/File/ImportMenu
#@onready var _Edit_PopupMenu = 
#@onready var _Settings_PopupMenu = 

@onready var _Gratitude_SpinBox: SpinBox = $GratitudeBox
@onready var _Time_SpinBox: SpinBox = $TimeBox
@onready var _Weather_OptionButton: OptionButton = $ComplexWeatherSelect

@onready var _Bag_Panel: Panel = $Panel
@onready var _HotbarInventory_Container: ButtonSelectorContainer = $Panel/VBoxContainer/HotbarInventoryContainer
@onready var _BagInventory_Container: ButtonSelectorContainer = $Panel/VBoxContainer/ScrollContainer/BagInventoryContainer
@onready var _ChooseAnItem_Container: ButtonSelectorContainer = $Control/ScrollContainer/GridContainer

enum FileDialogStateEnum
{
	Unknown,
	SaveDirectory,
	OpenDirectory,
	OpenFile,
	SaveCMNDAT,
	SaveSTGDAT,
	SaveSCSHDAT,
	ExportCMNDAT,
	ExportSTGDAT,
	ExportSCSHDAT,
	ImportCMNDAT,
	ImportSTGDAT,
	ImportSCSHDAT
}
var _FileDialogState := FileDialogStateEnum.Unknown

var _TargetingHotbar := false
var _TargetedInventoryItem: int = -1

var WorkingDirectory: String = ""

var PreloadedFileInfo: Array[SaveFileInfo]

var _WantsToQuit := false

func _ready() -> void:
	get_tree().auto_accept_quit = false
	get_tree().root.close_requested.connect(_on_root_close_requested)
	
	_initialize_file_dialog_path()
	update_loaded_data()
	update_menu_buttons()

	
	var accounts: PackedStringArray = get_dqb2_steam_account_paths()
	if len(accounts) == 1:
		preload_dqb2_save(accounts[0])
	else: #TEST
		preload_dqb2_save("C:/Users/walke/Documents/My Games/DRAGON QUEST BUILDERS II/Steam/76561198437040801")
func _initialize_file_dialog_path() -> void:
	var account_paths: PackedStringArray = get_dqb2_steam_account_paths()
	if len(account_paths) == 1:
		var path: String = Util.get_deepest_path((account_paths[0].path_join("SD")))
		_FileDialog.current_dir = path

func update_loaded_data() -> void:
	if StageData.has_instance():
		_Gratitude_SpinBox.set_value_no_signal(StageData.Instance.Gratitude)
		_Gratitude_SpinBox.editable = true
		_Time_SpinBox.editable = true
		_Weather_OptionButton.select(StageData.Instance.Weather)
		_Weather_OptionButton.disabled = false
	else:
		_Gratitude_SpinBox.set_value_no_signal(0)
		_Gratitude_SpinBox.editable = false
		_Time_SpinBox.set_value_no_signal(0)
		_Time_SpinBox.editable = false
		_Weather_OptionButton.select(0)
		_Weather_OptionButton.disabled = true
func update_menu_buttons() -> void:
	_File_PopupMenu.set_item_disabled(3,  not any_is_loaded()) # Save All
	_File_PopupMenu.set_item_disabled(4,  not any_is_loaded()) # Save All As...
	_File_PopupMenu.set_item_disabled(5,  not any_is_loaded()) # Save File
	_File_PopupMenu.set_item_disabled(6,  not any_is_loaded()) # Save File As
	_File_PopupMenu.set_item_disabled(8,  not any_is_loaded()) # Export
	_File_PopupMenu.set_item_disabled(9,  not any_is_loaded()) # Import
	_File_PopupMenu.set_item_disabled(11, not any_is_loaded()) # Close
	
	_File_SaveSingleFile_PopupMenu  .set_item_disabled(0, not CommonData.has_instance())
	_File_SaveAsSingleFile_PopupMenu.set_item_disabled(0, not CommonData.has_instance())
	_File_Export_PopupMenu          .set_item_disabled(0, not CommonData.has_instance())
	_File_Import_PopupMenu          .set_item_disabled(0, not CommonData.has_instance())
	
	_File_SaveSingleFile_PopupMenu  .set_item_disabled(1, not StageData.has_instance())
	_File_SaveAsSingleFile_PopupMenu.set_item_disabled(1, not StageData.has_instance())
	_File_Export_PopupMenu          .set_item_disabled(1, not StageData.has_instance())
	_File_Import_PopupMenu          .set_item_disabled(1, not StageData.has_instance())
	
	_File_SaveSingleFile_PopupMenu  .set_item_disabled(2, not ScreenshotData.has_instance())
	_File_SaveAsSingleFile_PopupMenu.set_item_disabled(2, not ScreenshotData.has_instance())
	_File_Export_PopupMenu          .set_item_disabled(2, not ScreenshotData.has_instance())
	_File_Import_PopupMenu          .set_item_disabled(2, not ScreenshotData.has_instance())

func save_all() -> void:
	if CommonData.has_instance():
		CommonData.Instance.save()
	if StageData.has_instance():
		StageData.Instance.save()
	if ScreenshotData.has_instance():
		ScreenshotData.Instance.save()
func close_all() -> void:
	CommonData.close()
	StageData.close()
	ScreenshotData.close()

func try_close_file() -> bool:
	if (
		(CommonData.has_instance() and CommonData.Instance.UnsavedChanges) or
		(StageData.has_instance() and StageData.Instance.UnsavedChanges) or 
		(ScreenshotData.has_instance() and ScreenshotData.Instance.UnsavedChanges)
	):
		_UnsavedChanges_Window.popup_centered()
		return false
	else:
		return true

func try_load_folder(path: String) -> bool:
	close_all() #TODO handle
	if CommonData.try_load_and_set(path.path_join("CMNDAT.BIN")) == null:
		return false
	
	ScreenshotData.try_load_and_set(path.path_join("SCSHDAT.BIN"))
	
	WorkingDirectory = path
	
	update_loaded_data()
	update_menu_buttons()
	
	return true
func try_load_file(path: String) -> bool:
	if CommonData.try_load_and_set(path) != null:
		update_loaded_data()
		update_menu_buttons()
		return true
	if StageData.try_load_and_set(path) != null:
		update_loaded_data()
		update_menu_buttons()
		return true
	if ScreenshotData.try_load_and_set(path) != null:
		update_loaded_data()
		update_menu_buttons()
		return true
	
	return false
func try_save_folder(path: String) -> void:
	pass #TODO

static func get_dqb2_path() -> String:
	return OS.get_system_dir(OS.SYSTEM_DIR_DOCUMENTS).path_join("My Games").path_join("DRAGON QUEST BUILDERS II").path_join("Steam")
static func get_dqb2_steam_account_paths() -> PackedStringArray:
	var dqb2_path: String = get_dqb2_path()
	if DirAccess.dir_exists_absolute(dqb2_path):
		var paths: PackedStringArray = []
		for path in DirAccess.get_directories_at(dqb2_path):
			paths.append(dqb2_path.path_join(path))
		return paths
	return []

static func any_is_loaded() -> bool:
	return CommonData.has_instance() or StageData.has_instance() or ScreenshotData.has_instance()

func preload_dqb2_save(path: String) -> void:
	path = path.path_join("SD")
	if (not DirAccess.dir_exists_absolute(path)):
		return
	
	PreloadedFileInfo = []
	PreloadedFileInfo.resize(3)
	for i in range(len(PreloadedFileInfo)):
		var common_data = CommonData.quick_load(path.path_join("B0" + str(i)).path_join("CMNDAT.BIN"))
		if common_data != null:
			PreloadedFileInfo[i] = SaveFileInfo.new(common_data)
			print("Preloaded file data for " + PreloadedFileInfo[i].PlayerName + " in slot " + str(i+1) + ".")

# Callback methods
func _on_file_popupmenu_id_pressed(id: int) -> void:
	match id:
		0: # Open Folder...
			_FileDialog.set_file_mode(FileDialog.FILE_MODE_OPEN_DIR)
			_FileDialogState = FileDialogStateEnum.OpenDirectory
			_FileDialog.set_title("Open a folder")
			_FileDialog.popup_centered()
		1: # Open File...
			_FileDialog.set_file_mode(FileDialog.FILE_MODE_OPEN_FILE)
			_FileDialogState = FileDialogStateEnum.OpenFile
			_FileDialog.set_title("Open a file")
			_FileDialog.clear_filters()
			_FileDialog.add_filter("*.bin")
			_FileDialog.popup_centered()
		2: # Save All
			save_all()
		3: #Save All As...
			_FileDialog.set_file_mode(FileDialog.FILE_MODE_OPEN_DIR)
			_FileDialogState = FileDialogStateEnum.SaveDirectory
			_FileDialog.set_title("Choose a folder to which to save")
			_FileDialog.popup_centered()
		8: # Close
			try_close_file()
		9: # Quit
			_on_root_close_requested()
func _on_savesinglefile_popupmenu_id_pressed(id: int) -> void:
	match id:
		0: # CMNDAT
			if CommonData.has_instance():
				CommonData.Instance.save()
		1: # STGDAT
			if StageData.has_instance():
				StageData.Instance.save()
		2: # SCSHDAT
			if ScreenshotData.has_instance():
				ScreenshotData.Instance.save()
func _on_saveassinglefile_popupmenu_id_pressed(id: int) -> void:
	_FileDialog.set_file_mode(FileDialog.FILE_MODE_SAVE_FILE)
	_FileDialog.clear_filters()
	_FileDialog.add_filter("*.bin")
	match id:
		0: # CMNDAT
			_FileDialogState = FileDialogStateEnum.SaveCMNDAT
			_FileDialog.set_title("Save CMNDAT...")
		1: # STGDAT
			_FileDialogState = FileDialogStateEnum.SaveSTGDAT
			_FileDialog.set_title("Save STGDAT...")
		2:
			_FileDialogState = FileDialogStateEnum.SaveSCSHDAT
			_FileDialog.set_title("Save SCSHDAT...")
	_FileDialog.popup_centered()
func _on_export_popupmenu_id_pressed(id: int) -> void:
	_FileDialog.set_file_mode(FileDialog.FILE_MODE_SAVE_FILE)
	_FileDialog.clear_filters()
	_FileDialog.add_filter("*", "All Files")
	match id:
		0: # CMNDAT
			_FileDialogState = FileDialogStateEnum.ExportCMNDAT
			_FileDialog.set_title("Export CMNDAT...")
		1: # STGDAT
			_FileDialogState = FileDialogStateEnum.ExportSTGDAT
			_FileDialog.set_title("Export STGDAT...")
		2: # SCSHDAT
			_FileDialogState = FileDialogStateEnum.ExportSCSHDAT
			_FileDialog.set_title("Export SCSHDAT")
	_FileDialog.popup_centered()
func _on_import_popupmenu_id_pressed(id: int) -> void:
	_FileDialog.set_file_mode(FileDialog.FILE_MODE_OPEN_FILE)
	_FileDialog.clear_filters()
	_FileDialog.add_filter("*", "All Files")
	match id:
		0: # CMNDAT
			_FileDialogState = FileDialogStateEnum.ImportCMNDAT
			_FileDialog.set_title("Import CMNDAT...")
		1: # STGDAT
			_FileDialogState = FileDialogStateEnum.ImportSTGDAT
			_FileDialog.set_title("Import STGDAT...")
		2: # SCSHDAT
			_FileDialogState = FileDialogStateEnum.ImportSCSHDAT
			_FileDialog.set_title("Import SCSHDAT...")
	_FileDialog.popup_centered()

func _on_filedialog_file_selected(path: String) -> void:
	match _FileDialogState:
		FileDialogStateEnum.OpenDirectory:
			try_load_folder(path)
		FileDialogStateEnum.SaveDirectory:
			try_save_folder(path)
		FileDialogStateEnum.OpenFile:
			try_load_file(path)
		FileDialogStateEnum.SaveCMNDAT:
			if (CommonData.has_instance()):
				CommonData.Instance.save(path)
		FileDialogStateEnum.SaveSTGDAT:
			if (StageData.has_instance()):
				StageData.Instance.save(path)
		FileDialogStateEnum.SaveSCSHDAT:
			if (ScreenshotData.has_instance()):
				ScreenshotData.Instance.save(path)
		FileDialogStateEnum.ExportCMNDAT:
			if (CommonData.has_instance()):
				CommonData.Instance.export(path)
		FileDialogStateEnum.ExportSTGDAT:
			if (StageData.has_instance()):
				StageData.Instance.export(path)
		FileDialogStateEnum.ExportSCSHDAT:
			if (ScreenshotData.has_instance()):
				ScreenshotData.Instance.export(path)
		FileDialogStateEnum.ImportCMNDAT:
			if (CommonData.has_instance()):
				CommonData.Instance.import(path)
		FileDialogStateEnum.ImportSTGDAT:
			if (StageData.has_instance()):
				StageData.Instance.import(path)
		FileDialogStateEnum.ImportSCSHDAT:
			if (ScreenshotData.has_instance()):
				ScreenshotData.Instance.import(path)

func _on_gratitude_spinbox_value_changed(value: float) -> void:
	if (StageData.has_instance()):
		StageData.Instance.Gratitude = roundi(value)
func _on_weather_optionbutton_item_selected(index: int) -> void:
	if (StageData.has_instance()):
		StageData.Instance.Weather = index

func _on_unsavedchanges_window_save_button_pressed():
	_UnsavedChanges_Window.hide()
	save_all()
	close_all()
	if _WantsToQuit:
		get_tree().quit()
func _on_unsavedchanges_window_dontsave_button_pressed():
	_UnsavedChanges_Window.hide()
	close_all()
	if _WantsToQuit:
		get_tree().quit()
func _on_unsavedchanges_window_cancel_button_pressed():
	_UnsavedChanges_Window.hide()
	_WantsToQuit = false

func _on_bag_button_pressed() -> void:
	if _Bag_Panel.visible:
		_Bag_Panel.hide()
		return
	else:
		_Bag_Panel.show()
	
	if CommonData.has_instance() && _HotbarInventory_Container.get_child_count() == 0 && _BagInventory_Container.get_child_count() == 0 && _ChooseAnItem_Container.get_child_count() == 0:
		for item in CommonData.Instance.HotbarInventory:
			var new_button = Button.new()
			new_button.text = item.get_info().Name + " (x" + str(item.Count) + ")"
			new_button.autowrap_mode = TextServer.AUTOWRAP_ARBITRARY
			new_button.size_flags_horizontal = Control.SIZE_EXPAND_FILL
			_HotbarInventory_Container.add_button(new_button)
		for item in CommonData.Instance.BagInventory:
			var new_button = Button.new()
			new_button.text = item.get_info().Name + " (x" + str(item.Count) + ")"
			new_button.autowrap_mode = TextServer.AUTOWRAP_ARBITRARY
			new_button.size_flags_horizontal = Control.SIZE_EXPAND_FILL
			_BagInventory_Container.add_button(new_button)
		for item in ItemInfo.get_all():
			var new_button = Button.new()
			new_button.text = item.Name
			new_button.autowrap_mode = TextServer.AUTOWRAP_ARBITRARY
			new_button.size_flags_horizontal = Control.SIZE_EXPAND_FILL
			_ChooseAnItem_Container.add_button(new_button)
func _on_baginventory_container_button_pressed(id: int) -> void:
	_TargetedInventoryItem = id
	_TargetingHotbar = false
func _on_chooseanitem_container_button_pressed(id: int) -> void:
	if _TargetedInventoryItem < 0:
		return
	
	if not _TargetingHotbar:
		var selected_item = CommonData.Instance.BagInventory[_TargetedInventoryItem]
		selected_item.ItemID = id
		var item_button: Button = _BagInventory_Container.get_child(_TargetedInventoryItem)
		if id == 0:
			selected_item.Count = 0
		else:
			selected_item.Count = 1
		item_button.text = selected_item.get_info().Name + " (x" + str(selected_item.Count) + ")"

func _on_root_close_requested() -> void:
	_WantsToQuit = true
	if (try_close_file()):
		get_tree().quit()

class SaveFileInfo:
	var PlayerName: String
	var PlayerSex: bool
	var ToIsland: int
	var LastSaveTime: int
	var Thumbnail: Image

	func _init(data: CommonData) -> void:
		PlayerName = data.PlayerName
		PlayerSex = data.PlayerSex
		ToIsland = data.ToIsland
		LastSaveTime = data.LastSaveTime
		Thumbnail = data.get_thumbnail()
