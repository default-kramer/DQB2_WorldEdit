using Godot;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using DQBEdit.Nodes;
using DQBEdit.Info;

namespace DQBEdit.Scenes
{
	public partial class Main : Control
	{
		// TODO add backup functionality
		// TODO handle overwriting open files
		// TODO retake male body screenshots
		// TODO monster, animal, fish screenshots
		// TODO CMNDAT and SCSHDAT are CompressionLevel.Fastest, STGDAT is CompressionLevel.Optimal -- See if this has effect?
		//   Hacky-ass solution: if _Header.Length == StageData.HeaderLength
		private WorldEditorScene _WorldEditorScene;

		private FileDialog _FileDialog;

		private Window _UnsavedChanges_Window;

		private PopupMenu _File_PopupMenu;
		private PopupMenu _File_SaveSingleFile_PopupMenu;
		private PopupMenu _File_SaveAsSingleFile_PopupMenu;
		private PopupMenu _File_Export_PopupMenu;
		private PopupMenu _File_Import_PopupMenu;
		private PopupMenu _Edit_PopupMenu;
		private PopupMenu _Settings_PopupMenu;

		private OptionButton _IslandSelectorButton;

		private SpinBox _Gratitude_SpinBox;
		private SpinBox _Time_SpinBox;
		private OptionButton _Weather_OptionButton;

		private Control _Bag_Panel;
		private ButtonSelectorContainer _HotbarInventory_Container;
		private ButtonSelectorContainer _BagInventory_Container;
		private ButtonSelectorContainer _ChooseAnItem_Container;

		private enum FileDialogStateEnum
		{
			Unknown,
			OpenDirectory,
			SaveDirectory,
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
		private FileDialogStateEnum _FileDialogState = FileDialogStateEnum.Unknown;

		private bool _TargetingHotbar = false;
		private int _TargetedInventoryItem = -1;

		public string WorkingDirectory { get; set; } = null;

		private SaveFileInfo[] PreloadedFileData { get; set; } = new SaveFileInfo[3];

		private bool WantsToQuit = false;

		public override void _Ready()
		{
			_OnReadyVariables();

			GetTree().AutoAcceptQuit = false;
			GetTree().Root.CloseRequested += _On_Root_CloseRequested;

			_InitializeFileDialogPath();
			UpdateLoadedData();
			UpdateMenuButtons();

			string[] accounts = GetDQB2SteamAccountPaths();
			if (accounts.Length == 1)
				PreloadDQB2Save(accounts[0]);
	   	}
		private void _OnReadyVariables()
		{
			_WorldEditorScene = GetNode<WorldEditorScene>("Window/Voxel Test");

			_FileDialog = GetNode<FileDialog>("FileDialog");

			_UnsavedChanges_Window = GetNode<Window>("UnsavedChangesWindow");

			_File_PopupMenu = GetNode<PopupMenu>("HBoxContainer/MenuBar/FileMenu");
			_File_SaveSingleFile_PopupMenu = GetNode<PopupMenu>("HBoxContainer/MenuBar/FileMenu/SaveSingleFileMenu");
			_File_SaveAsSingleFile_PopupMenu = GetNode<PopupMenu>("HBoxContainer/MenuBar/FileMenu/SaveAsSingleFileMenu");
			_File_Export_PopupMenu = GetNode<PopupMenu>("HBoxContainer/MenuBar/FileMenu/ExportMenu");
			_File_Import_PopupMenu = GetNode<PopupMenu>("HBoxContainer/MenuBar/FileMenu/ImportMenu");
			//_Edit_PopupMenu = GetNode<PopupMenu>("MenuBar/Debug");

			_Settings_PopupMenu = GetNode<PopupMenu>("HBoxContainer/MenuBar/SettingsMenu");

			_IslandSelectorButton = GetNode<OptionButton>("IslandSelectorButton");

			_Gratitude_SpinBox = GetNode<SpinBox>("GratitudeBox");
			_Time_SpinBox = GetNode<SpinBox>("TimeBox");
			_Weather_OptionButton = GetNode<OptionButton>("ComplexWeatherSelect");

			_Bag_Panel = GetNode<Panel>("Panel");
			//_HotbarInventory_Container = GetNode<ButtonSelectorContainer>("Panel/VBoxContainer/HotbarInventoryContainer");
			//_BagInventory_Container = GetNode<ButtonSelectorContainer>("Panel/VBoxContainer/ScrollContainer/BagInventoryContainer");
			//_ChooseAnItem_Container = GetNode<ButtonSelectorContainer>("Control/ScrollContainer/GridContainer");
		}
		private void _InitializeFileDialogPath()
		{
			// TODO add support for saving this as a preference
			var path = Path.Join(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), "My Games", "DRAGON QUEST BUILDERS II", "Steam");
			if (Directory.GetDirectories(path).Length == 1)
			{
				path = Path.Join(Directory.GetDirectories(path)[0], "SD");
			}
			_FileDialog.SetCurrentDirRecursive(path);
		}

		public void UpdateLoadedData()
		{
			if (!string.IsNullOrEmpty(WorkingDirectory))
			{
				_IslandSelectorButton.Disabled = false;
				for (int idx = 1; idx < _IslandSelectorButton.ItemCount; idx++)
				{
					int id = _IslandSelectorButton.GetItemId(idx);
					_IslandSelectorButton.SetItemDisabled(idx, !File.Exists(Path.Combine(WorkingDirectory, $"STGDAT{id:D2}.BIN")));
				}
			}
			else
			{
				_IslandSelectorButton.Select(0);
				_IslandSelectorButton.Disabled = true;
			}

			if (StageData.HasInstance)
			{
				_Gratitude_SpinBox.SetValueNoSignal(StageData.Instance.Gratitude);
				_Gratitude_SpinBox.Editable = true;
				_Time_SpinBox.SetValueNoSignal(StageData.Instance.Time);
				_Time_SpinBox.Editable = true;
				_Weather_OptionButton.Select((int)StageData.Instance.Weather);
				_Weather_OptionButton.Disabled = false;
			}
			else
			{
				_Gratitude_SpinBox.SetValueNoSignal(0);
				_Gratitude_SpinBox.Editable = false;
				_Time_SpinBox.SetValueNoSignal(0);
				_Time_SpinBox.Editable = false;
				_Weather_OptionButton.Select(0);
				_Weather_OptionButton.Disabled = true;
			}
		}
		public void UpdateMenuButtons()
		{
			_File_PopupMenu.SetItemDisabled(3, !AnyIsLoaded()); // Save All
			_File_PopupMenu.SetItemDisabled(4, !AnyIsLoaded()); // Save All As...
			_File_PopupMenu.SetItemDisabled(5, !AnyIsLoaded()); // Save File
			_File_PopupMenu.SetItemDisabled(6, !AnyIsLoaded()); // Save File As
			_File_PopupMenu.SetItemDisabled(8, !AnyIsLoaded()); // Export
			_File_PopupMenu.SetItemDisabled(9, !AnyIsLoaded()); // Import
			_File_PopupMenu.SetItemDisabled(11, !AnyIsLoaded()); // Close

			_File_SaveSingleFile_PopupMenu.SetItemDisabled(0, !CommonData.HasInstance);
			_File_SaveSingleFile_PopupMenu.SetItemDisabled(1, !StageData.HasInstance);
			_File_SaveSingleFile_PopupMenu.SetItemDisabled(2, !ScreenshotData.HasInstance);

			_File_SaveAsSingleFile_PopupMenu.SetItemDisabled(0, !CommonData.HasInstance);
			_File_SaveAsSingleFile_PopupMenu.SetItemDisabled(1, !StageData.HasInstance);
			_File_SaveAsSingleFile_PopupMenu.SetItemDisabled(2, !ScreenshotData.HasInstance);

			_File_Export_PopupMenu.SetItemDisabled(0, !CommonData.HasInstance);
			_File_Export_PopupMenu.SetItemDisabled(1, !StageData.HasInstance);
			_File_Export_PopupMenu.SetItemDisabled(2, !ScreenshotData.HasInstance);

			_File_Import_PopupMenu.SetItemDisabled(0, !CommonData.HasInstance);
			_File_Import_PopupMenu.SetItemDisabled(1, !StageData.HasInstance);
			_File_Import_PopupMenu.SetItemDisabled(2, !ScreenshotData.HasInstance);
		}

		public static void SaveAll()
		{
			CommonData.Instance?.Save();
			StageData.Instance?.Save();
			ScreenshotData.Instance?.Save();
		}
		public void CloseAll()
		{
			CommonData.Close();
			StageData.Close();
			ScreenshotData.Close();
			_WorldEditorScene.UnloadWorld();
		}

		public bool TryCloseFile()
		{
			if ((CommonData.HasInstance && CommonData.Instance.UnsavedChanges) || (StageData.HasInstance && StageData.Instance.UnsavedChanges) || (ScreenshotData.HasInstance && ScreenshotData.Instance.UnsavedChanges))
			{
				_UnsavedChanges_Window.PopupCentered();
				return false;
			}
			else
			{
				CloseAll();
				return true;
			}
		}

		public bool TryLoadFolder(string path)
		{
			CloseAll(); // TODO handle
			if (CommonData.TryLoadAndSet(Path.Join(path, "CMNDAT.BIN")) is null)
				return false;

			ScreenshotData.TryLoadAndSet(Path.Join(path, "SCSHDAT.BIN"));

			WorkingDirectory = path;

			UpdateLoadedData();
			UpdateMenuButtons();

			return true;
			// TODO
		}
		public bool TryLoadFile(string path)
		{
			if (CommonData.TryLoadAndSet(path) is not null)
			{
				UpdateLoadedData();
				UpdateMenuButtons();
				_WorldEditorScene.CreateNPCSprites(CommonData.Instance);
				return true;
			}
			if (StageData.TryLoadAndSet(path) is not null)
			{
				UpdateLoadedData();
				UpdateMenuButtons();
				_WorldEditorScene.UnloadWorld();
				_WorldEditorScene.LoadWorld(StageData.Instance);
				return true;
			}
			if (ScreenshotData.TryLoadAndSet(path) is not null)
			{
				UpdateLoadedData();
				UpdateMenuButtons();
				return true;
			}

			return false;
		}
		public void TrySaveFolder(string path)
		{
			// TODO
		}

		public static string GetDQB2Path()
		{
			return Path.Join(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), "My Games", "DRAGON QUEST BUILDERS II", "Steam");
		}
		public static string[] GetDQB2SteamAccountPaths()
		{
			return Directory.GetDirectories(GetDQB2Path());
		}
		public static bool AnyIsLoaded()
		{
			return CommonData.HasInstance || StageData.HasInstance || ScreenshotData.HasInstance;
		}

		public void PreloadDQB2Save(string path)
		{
			path = Path.Join(path, "SD");
			if (!Directory.Exists(path))
				return;

			for (int i = 0; i < PreloadedFileData.Length; i++)
			{
				CommonData commonData = CommonData.QuickLoad(Path.Combine(path, $"B{i:D2}", "CMNDAT.BIN"));
				if (commonData is not null)
					PreloadedFileData[i] = new SaveFileInfo(commonData);
			}
		}

		// Callback methods
		public void _On_File_PopupMenu_IdPressed(int id)
		{
			switch(id)
			{
				case 0: // Open Folder...
					_FileDialog.FileMode = FileDialog.FileModeEnum.OpenDir;
					_FileDialogState = FileDialogStateEnum.OpenDirectory;
					_FileDialog.Title = "Open a folder";
					_FileDialog.PopupCentered();
					break;
				case 1: // Open File...
					_FileDialog.FileMode = FileDialog.FileModeEnum.OpenFile;
					_FileDialogState = FileDialogStateEnum.OpenFile;
					_FileDialog.Title = "Open a file";
					_FileDialog.SetFilter("*.bin");
					_FileDialog.PopupCentered();
					break;
				case 2: // Save All
					SaveAll();
					break;
				case 3: // Save All As...
					_FileDialog.FileMode = FileDialog.FileModeEnum.OpenDir;
					_FileDialogState = FileDialogStateEnum.SaveDirectory;
					_FileDialog.Title = "Choose a folder to save";
					_FileDialog.PopupCentered();
					break;
				case 8: // Close
					TryCloseFile();
					break;
				case 9: // Quit
					_On_Root_CloseRequested();
					break;
				default:
					break;
			}
		}
		public void _On_SaveSingleFile_PopupMenu_IdPressed(int id)
		{
			//_FileDialog.FileMode = FileDialog.FileModeEnum.SaveFile;
			switch (id)
			{
				case 0: // CMNDAT
					CommonData.Instance.Save();
					break;
				case 1: // STGDAT
					StageData.Instance.Save();
					break;
				case 2: // SCSHDAT
					ScreenshotData.Instance.Save();
					break;
				default:
					break;
			}
			//_FileDialog.PopupCentered();
		}
		public void _On_SaveAsSingleFile_PopupMenu_IdPressed(int id)
		{
			_FileDialog.FileMode = FileDialog.FileModeEnum.SaveFile;
			switch (id)
			{
				case 0: // CMNDAT
					_FileDialogState = FileDialogStateEnum.SaveCMNDAT;
					_FileDialog.Title = "Save CMNDAT...";
					_FileDialog.SetFilter("*.bin");
					break;
				case 1: // STGDAT
					_FileDialogState = FileDialogStateEnum.SaveSTGDAT;
					_FileDialog.Title = "Save STGDAT...";
					_FileDialog.SetFilter("*.bin");
					break;
				case 2: // SCSHDAT
					_FileDialogState = FileDialogStateEnum.SaveSCSHDAT;
					_FileDialog.Title = "Save SCSHDAT...";
					_FileDialog.SetFilter("*.bin");
					break;
				default:
					break;
			}
			_FileDialog.PopupCentered();
		}
		public void _On_Export_PopupMenu_IdPressed(int id)
		{
			_FileDialog.FileMode = FileDialog.FileModeEnum.SaveFile;
			_FileDialog.SetFilter("*", "All Files");
			switch (id)
			{
				case 0: // CMNDAT
					_FileDialogState = FileDialogStateEnum.ExportCMNDAT;
					_FileDialog.Title = "Export CMNDAT...";
					break;
				case 1: // STGDAT
					_FileDialogState = FileDialogStateEnum.ExportSTGDAT;
					_FileDialog.Title = "Export STGDAT...";
					break;
				case 2: // SCSHDAT
					_FileDialogState = FileDialogStateEnum.ExportSCSHDAT;
					_FileDialog.Title = "Export SCSHDAT...";
					break;
				default:
					break;
			}
			_FileDialog.PopupCentered();
		}
		public void _On_Import_PopupMenu_IdPressed(int id)
		{
			_FileDialog.FileMode = FileDialog.FileModeEnum.OpenFile;
			_FileDialog.SetFilter("*", "All Files");
			switch (id)
			{
				case 0: // CMNDAT
					_FileDialogState = FileDialogStateEnum.ImportCMNDAT;
					_FileDialog.Title = "Import CMNDAT...";
					break;
				case 1: // STGDAT
					_FileDialogState = FileDialogStateEnum.ImportSTGDAT;
					_FileDialog.Title = "Import STGDAT...";
					break;
				case 2: // SCSHDAT
					_FileDialogState = FileDialogStateEnum.ImportSCSHDAT;
					_FileDialog.Title = "Import SCSHDAT...";
					break;
				default:
					break;
			}
			_FileDialog.PopupCentered();
		}

		public void _On_Settings_PopupMenu_IdPressed(int id)
		{
			switch (id)
			{
				case 0: // Advanced Mode
					break;
				case 1: // Show FPS
					bool showFps = !_Settings_PopupMenu.IsItemChecked(1);
					_Settings_PopupMenu.SetItemChecked(1, showFps);
					_WorldEditorScene.ChangeFPSDisplay(showFps);
					break;
				case 2: // Show Debug Info
					bool showDebugInfo = !_Settings_PopupMenu.IsItemChecked(2);
					_Settings_PopupMenu.SetItemChecked(2, showDebugInfo);
					_WorldEditorScene.ChangeDebugInfoDisplay(showDebugInfo);
					break;
			}
		}

		public void _On_FileDialog_FileSelected(string path)
		{
			switch (_FileDialogState)
			{
				case FileDialogStateEnum.OpenDirectory:
					TryLoadFolder(path);
					break;
				case FileDialogStateEnum.SaveDirectory:
					TrySaveFolder(path);
					break;
				case FileDialogStateEnum.OpenFile:
					TryLoadFile(path);
					break;
				case FileDialogStateEnum.SaveCMNDAT:
					CommonData.Instance?.Save(path);
					break;
				case FileDialogStateEnum.SaveSTGDAT:
					StageData.Instance?.Save(path);
					break;
				case FileDialogStateEnum.SaveSCSHDAT:
					ScreenshotData.Instance?.Save(path);
					break;
				case FileDialogStateEnum.ExportCMNDAT:
					CommonData.Instance?.Export(path);
					break;
				case FileDialogStateEnum.ExportSTGDAT:
					StageData.Instance?.Export(path);
					break;
				case FileDialogStateEnum.ExportSCSHDAT:
					ScreenshotData.Instance?.Export(path);
					break;
				case FileDialogStateEnum.ImportCMNDAT:
					CommonData.Instance?.Import(path);
					break;
				case FileDialogStateEnum.ImportSTGDAT:
					StageData.Instance?.Import(path);
					break;
				case FileDialogStateEnum.ImportSCSHDAT:
					ScreenshotData.Instance?.Import(path);
					break;
				default:
					break;
			}
		}
		
		public void _On_UnsavedChanges_Window_Save_Button_Pressed()
		{
			_UnsavedChanges_Window.Hide();
			SaveAll();
			CloseAll();
			if (WantsToQuit)
				GetTree().Quit();
		}
		public void _On_UnsavedChanges_Window_DontSave_Button_Pressed()
		{
			_UnsavedChanges_Window.Hide();
			CloseAll();
			if (WantsToQuit)
				GetTree().Quit();
		}
		public void _On_UnsavedChanges_Window_Cancel_Button_Pressed()
		{
			_UnsavedChanges_Window.Hide();
			WantsToQuit = false;
		}

		public void _On_Gratitude_SpinBox_ValueChanged(float value)
		{
			if (StageData.HasInstance)
				StageData.Instance.Gratitude = (int)value;
		}
		public void _On_Weather_OptionButton_ItemSelected(int index)
		{
			StageData.Instance.Weather = (byte)index;
		}

		public void _On_Bag_Button_Pressed()
		{
			if (_Bag_Panel.Visible)
			{
				_Bag_Panel.Hide();
				return;
			}
			else _Bag_Panel.Show();

			if (CommonData.HasInstance && _HotbarInventory_Container.GetChildCount() == 0 && _BagInventory_Container.GetChildCount() == 0 && _ChooseAnItem_Container.GetChildCount() == 0)
			{
				foreach (Item item in CommonData.Instance.HotbarInventory)
				{
					Button newbutton = new Button();
					newbutton.Text = item.GetInfo().Name + $" (x{item.Count})";
					newbutton.AutowrapMode = TextServer.AutowrapMode.Arbitrary;
					newbutton.SizeFlagsHorizontal = SizeFlags.ExpandFill;
					_HotbarInventory_Container.AddButton(newbutton);
				}
				foreach (Item item in CommonData.Instance.BagInventory)
				{
					Button newbutton = new Button();
					newbutton.Text = item.GetInfo().Name + $" (x{item.Count})";
					newbutton.AutowrapMode = TextServer.AutowrapMode.Arbitrary;
					newbutton.SizeFlagsHorizontal = SizeFlags.ExpandFill;
					_BagInventory_Container.AddButton(newbutton);
				}
				foreach (ItemInfo item in ItemInfo.GetAll())
				{
					Button newbutton = new Button();
					newbutton.Text = item.Name;
					newbutton.AutowrapMode = TextServer.AutowrapMode.Arbitrary;
					newbutton.SizeFlagsHorizontal = SizeFlags.ExpandFill;
					_ChooseAnItem_Container.AddButton(newbutton, item.ID);
				}
			}
		}
		public void _On_BagInventory_Container_ButtonPressed(int id)
		{
			_TargetedInventoryItem = id;
			_TargetingHotbar = false;
		}
		public void _On_ChooseAnItem_Container_ButtonPressed(int id)
		{
			if (_TargetedInventoryItem < 0)
				return;

			if (!_TargetingHotbar)
			{
				Item selectedItem = CommonData.Instance.BagInventory[_TargetedInventoryItem];
				selectedItem.ItemID = (ushort)id;
				Button itemButton = _BagInventory_Container.GetChild<Button>(_TargetedInventoryItem);
				if (id == 0)
					selectedItem.Count = 0;
				else
					selectedItem.Count = 1;
				itemButton.Text = selectedItem.GetInfo().Name + $" (x{selectedItem.Count})";
			}
		}

		public void _On_Root_CloseRequested()
		{
			WantsToQuit = true;
			if (TryCloseFile())
				GetTree().Quit();
		}

		private struct SaveFileInfo
		{
			public string PlayerName { get; set; }
			public bool PlayerGender { get; set; }
			public byte ToIsland { get; set; }
			public DateTime LastSaveTime { get; set; }
			public Image Thumbnail { get; set; }

			public SaveFileInfo(CommonData data)
			{
				PlayerName = data.PlayerName;
				PlayerGender = data.PlayerGender;
				ToIsland = data.ToIsland;
				LastSaveTime = data.LastSaveTime;
				Thumbnail = data.GetThumbnail();
			}
		}
	}
}
