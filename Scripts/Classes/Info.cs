using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace DQBEdit.Info
{
    public class ItemInfo
    {
        private const string DATABASE_PATH = "res://Info/Items.json";

        private static ItemInfo[] _Database { get; set; }

    	public ushort ID { get; set; }
    	public string Name { get; set; } = "";
    	public int ImageID { get; set; } = -1;
        public bool Connecting { get; set; } = false;
        public int Rarity { get; set; } = 0;
        public ItemCategory Category { get; set; }
        public bool ShowInEditor { get; set; } = true;
        public bool ShowAdvanced { get; set; } = false;
        public float SortIndex { get; set; } = float.MaxValue;

        public bool Unknown { get; set; } = false;

        [JsonConstructor]
        private ItemInfo(){}
        private ItemInfo(ushort id, bool unknown = false)
        {
            ID = id;
            Unknown = unknown;
            if (unknown)
                Name = "Unknown";
        }

        public static void LoadDatabase(bool forceReload = false)
        {
            if (forceReload || _Database is null)
                _Database = JsonSerializer.Deserialize<ItemInfo[]>(FileAccess.GetFileAsString(DATABASE_PATH));
        }

        public static ItemInfo Get(ushort id)
        {
            if (_Database is null)
                LoadDatabase();

            return _Database.FirstOrDefault(i => i.ID == id) ?? new ItemInfo(id, true);
        }
        public static ItemInfo[] GetAll()
        {
            if (_Database is null)
                LoadDatabase();
            return _Database;
        }

        public string GetNameRich()
        {
    		string itemText = Name;
    		itemText = itemText.Replace("{White}", $"[color={Constants.COLOUR_WHITE}]■[/color]");
    		itemText = itemText.Replace("{Black}", $"[color={Constants.COLOUR_BLACK}]■[/color]");
    		itemText = itemText.Replace("{Purple}", $"[color={Constants.COLOUR_PURPLE}]■[/color]");
    		itemText = itemText.Replace("{Pink}", $"[color={Constants.COLOUR_PINK}]■[/color]");
    		itemText = itemText.Replace("{Red}", $"[color={Constants.COLOUR_RED}]■[/color]");
    		itemText = itemText.Replace("{Green}", $"[color={Constants.COLOUR_GREEN}]■[/color]");
    		itemText = itemText.Replace("{Yellow}", $"[color={Constants.COLOUR_YELLOW}]■[/color]");
    		itemText = itemText.Replace("{Blue}", $"[color={Constants.COLOUR_BLUE}]■[/color]");
            return itemText;
        }

        public AtlasTexture GetIcon() => Util.GetItemIcon(ImageID);

        public enum ItemCategory
        {
            Consumable = 0,
            Food = 1,
            Blueprint,
            BuildingBlock,
            Furniture,
            DecorativeItem,
            WallHanging,
            Debug,
            Unused,
            Unobtainable,
            Fixture,
            FarmingEquipment,
            Material,
            Lighting,
            Machinery,
            CraftingStation,
            Fish
        }
    }
    public class BlockInfo
    {
        private const string DATABASE_PATH = "res://Info/Blocks.json";

        private static BlockInfo[] _Database { get; set; }

    	public ushort ID { get; set; }
    	public string Name { get; set; } = "";
    	public int ImageID { get; set; } = -1;
    	public string[] Tags { get; set; } = Array.Empty<string>();
    	public float SortIndex { get; set; } = float.MaxValue;
    	public ulong VoxelID { get; set; } = 0;

        public bool Unknown { get; set; } = false;

    	public Dictionary<string, int> Variants { get; set; }
    	public int BaseVariant { get; set; } = -1;

        [JsonConstructor]
        private BlockInfo(){}
        private BlockInfo(ushort id, bool unknown = false)
        {
            ID = id;
            Unknown = unknown;
            if (unknown)
                Name = "Unknown";
        }

        public static void LoadDatabase(bool forceReload = false)
        {
            if (forceReload || _Database is null)
                _Database = JsonSerializer.Deserialize<BlockInfo[]>(FileAccess.GetFileAsString(DATABASE_PATH));
        }

        public static BlockInfo Get(ushort id)
        {
            if (_Database is null)
                LoadDatabase();

            return _Database.FirstOrDefault(i => i.ID == id) ?? new BlockInfo(id, true);
        }
        public static BlockInfo[] GetAll()
        {
            return _Database;
        }

        public AtlasTexture GetIcon() => Util.GetItemIcon(ImageID);
    }
    public class DecorationInfo
    {
        private const string DATABASE_PATH = "res://Info/Decorations.json";

        private static DecorationInfo[] _Database { get; set; }

        public ushort ID { get; set; }
        public string Name { get; set; }

        public bool Unknown { get; set; } = false;

        public DecorationInfo(){}
        private DecorationInfo(ushort id, bool unknown = false)
        {
            ID = id;
            Unknown = unknown;
            if (unknown)
                Name = "Unknown";
        }

        public static void LoadDatabase(bool forceReload = false)
        {
            if (forceReload || _Database is null)
            {
                _Database = JsonSerializer.Deserialize<DecorationInfo[]>(FileAccess.GetFileAsString(DATABASE_PATH));
            }
        }

        public static DecorationInfo Get(ushort id)
        {
            if (_Database is null)
                LoadDatabase();

            return _Database.FirstOrDefault(i => i.ID == id) ?? new DecorationInfo(id, true);
        }
        public static DecorationInfo[] GetAll()
        {
            return _Database;
        }
    }
    public class WeatherInfo
    {
        private const string DATABASE_PATH = "res://Info/Weather.json";

        private static WeatherInfo[] _Database { get; set; }

        public ushort ID { get; set; }
        public string Name { get; set; }

        public bool Unknown { get; set; } = false;

        public WeatherInfo(){}
        private WeatherInfo(byte id, bool unknown = false)
        {
            ID = id;
            Unknown = unknown;
            if (unknown)
                Name = "Unknown";
        }

        public static void LoadDatabase(bool forceReload = false)
        {
            if (forceReload || _Database is null)
            {
                _Database = JsonSerializer.Deserialize<WeatherInfo[]>(FileAccess.GetFileAsString(DATABASE_PATH));
            }
        }

        public static WeatherInfo Get(byte id)
        {
            if (_Database is null)
                LoadDatabase();

            return _Database.FirstOrDefault(i => i.ID == id) ?? new WeatherInfo(id, true);
        }
    }

    public static class IslandName
    {
        private const string DATABASE_PATH = "res://Info/IslandName.txt";
        private static string[] _Database { get; set; }

        public static void LoadDatabase(bool forceReload = false)
        {
            if (forceReload || _Database is null)
                _Database = FileAccess.GetFileAsString(DATABASE_PATH).Split("\n");
        }

        public static string Get(byte id)
        {
            if (_Database is null)
                LoadDatabase();

            if (id > _Database.Length)
                return "Undefined Island";
            else
                return _Database[id];
        }
    }
    public static class ImportantResidentName
    {
        private const string DATABASE_PATH = "res://Info/StoryPeopleNames.txt";
        private static string[] _Database { get; set; }

        public static void LoadDatabase(bool forceReload = false)
        {
            if (forceReload || _Database is null)
                _Database = FileAccess.GetFileAsString(DATABASE_PATH).Split("\n");
        }

        public static string Get(int id)
        {
            if (_Database is null)
                LoadDatabase();

            if (id > _Database.Length || id < 0)
                return "";
            else
                return _Database[id];
        }
    }
    public static class GenericName
    {
        private const string DATABASE_PATH_MALE = "res://Info/MaleNames.txt";
        private const string DATABASE_PATH_FEMALE = "res://Info/FemaleNames.txt";
        private static string[] _MaleNames { get; set; }
        private static string[] _FemaleNames { get; set; }

        public static void LoadDatabase(bool forceReload = false)
        {
            if (forceReload || _MaleNames is null)
                _MaleNames = FileAccess.GetFileAsString(DATABASE_PATH_MALE).Split("\n");
            if (forceReload || _FemaleNames is null)
                _FemaleNames = FileAccess.GetFileAsString(DATABASE_PATH_FEMALE).Split("\n");
        }

        public static string Get(int id, byte gender)
        {
            LoadDatabase();

            if (gender == 1)
            {
                if (id > _MaleNames.Length || id < 0)
                    return "";
                else
                    return _MaleNames[id];
            }
            else
            {
                if (id > _FemaleNames.Length || id < 0)
                    return "";
                else
                    return _FemaleNames[id];
            }
        }
    }
}