using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Text;
using Godot;
using Microsoft.VisualBasic;
using DQBEdit.Info;

namespace DQBEdit
{
    public class CommonData : SaveData
    {
    	private const int HeaderLength = 0x2A444;

        private const int ThumbnailAddress = 0x10D;
        private const int ThumbnailSize = 320 * 180 * 3;

        private const int HotbarItemAddress = 0x55B28D;
        private const int HotbarItemCount = 15;
        private const int BagItemAddress = 0x55B2C9;
        private const int BagItemCount = 420;

        private const int ImportantResidentAddress = 0x6ACC8;
    	private const int ResidentAddress = 0x102A68;
        private const int ImportantResidentCount = 1023;
        private const int ResidentCount = 238;
        private const int ResidentSize = 608;

        public static CommonData Instance { get; private set; }
        public static bool HasInstance => Instance is not null && Instance.IsLoaded;
        public bool IsInstance => this == Instance;

        public DateTime LastSaveTime { get => DateTime.FromFileTime(GetInt64(0x2A40D, header: true)); set => SetInt64(0x2A40D, value.ToFileTime(), header: true); }

        public byte FromIsland { get => GetByte(0xC9, header: true); set => SetByte(0xC9, value, header: true); }
        public byte ToIsland { get => GetByte(0xC8, header: true); set => SetByte(0xC8, value, header: true); }

        public string PlayerName { get => GetString(0xCD, 12, header: true); set => SetString(0xCD, value, 12, header: true); }
        public bool PlayerGender { get => GetBit(0xC4, 1, header: true); set => SetBit(0xC4, 1, value, header: true); } // false: female, true: male
        public byte PlayerLevel { get => GetByte(0xCA9CF); set => SetByte(0xCA9CF, value); }
        public short PlayerExperience { get => GetInt16(0x6A9D1); set => SetInt16(0x6A9D1, value); } // TODO test if signed
        public short PlayerHP { get => GetInt16(0x6A890); set => SetInt16(0x6A890, value); } // TODO test if signed
        public short PlayerAdditionalHP { get => GetInt16(0x6A892); set => SetInt16(0x6A892, value); } // TODO test if signed
        public short PlayerHunger { get => GetInt16(0x6A896); set => SetInt16(0x6A896, value); }
        public short PlayerStamina { get => GetInt16(0x6A8A0); set => SetInt16(0x6A8A0, value); } // TODO test if signed
        public short PlayerAttack { get => GetInt16(0x6A898); set => SetInt16(0x6A898, value); } // TODO test if signed
        public short PlayerDefence { get => GetInt16(0x6A89A); set => SetInt16(0x6A89A, value); } // TODO test if signed

        public bool UnlockBag { get => GetBit(0x635, 0); set => SetBit(0x635, 0, value); }
        public bool UnlockWindbraker { get => GetBit(0x6A8A2, 1); set => SetBit(0x6A8A2, 1, value); }
        public bool UnlockFlipper { get => GetBit(0x6A8A3, 1); set => SetBit(0x6A8A3, 1, value); }
        public bool UnlockBigBash { get => GetBit(0x506, 1); set => SetBit(0x506, 1, value); }
        public bool UnlockBiggerBash { get => GetBit(0x502, 3); set => SetBit(0x502, 3, value); }
        public bool BottomlessPotUse { get => GetBit(0x504, 2); set => SetBit(0x504, 2, value); } // TODO what does this do
        public bool BottomlessPot { get => GetBit(0x67D, 1); set => SetBit(0x67D, 1, value); } // TODO what does this do
        public bool UnlockBuildnoculars { get => GetBit(0x502, 7); set => SetBit(0x502, 7, value); }
        public bool CarFly { get => GetBit(0x506, 6); set => SetBit(0x506, 6, value); }
        public bool CarBeam { get => GetBit(0x506, 7); set => SetBit(0x506, 7, value); }
        public bool CarLight { get => GetBit(0x506, 5); set => SetBit(0x506, 5, value); }
        public bool Transform { get => GetBit(0x500, 6); set => SetBit(0x500, 6, value); } // TODO what does this do
        public bool Expression { get => GetBit(0x501, 1); set => SetBit(0x501, 1, value); } // TODO what does this do

        public byte MiniMedals { get => GetByte(0x226E40); set => SetByte(0x226E40, value); }
        public byte MiniMedalsDeposited { get => GetByte(0x226E44); set => SetByte(0x226E44, value); }

        public Item[] HotbarInventory { get; set; }
        public Item[] BagInventory { get; set; }

        public Item PlayerWeapon { get; set; }
        public Item PlayerArmour { get; set; }
        public Item PlayerShield { get; set; }
        public Item PlayerHammer { get; set; }

        public Resident[] ImportantResidents { get; set; }
        public Resident[] Residents { get; set; }

        public static CommonData TryLoadAndSet(string path)
        {
            if (TryLoad(path) is CommonData commonData)
            {
                return Instance = commonData;
            }
            else return null;
        }
        public static CommonData TryLoad(string path)
        {
            CommonData commonData = new();
            if (commonData._TryLoad(path, HeaderLength))
            {
                commonData.CreateItems();
                commonData.CreateResidents();
                return commonData;
            }
            else return null;
        }
        public static CommonData QuickLoad(string path)
        {
            CommonData commonData = new();
            if (commonData._QuickLoad(path, HeaderLength))
            {
                return commonData;
            }
            else return null;
        }
        private void CreateItems()
        {
            HotbarInventory = new Item[HotbarItemCount];
            for (int i = 0; i < HotbarItemCount; i++)
            {
                HotbarInventory[i] = new Item(this, HotbarItemAddress + i * 4);
            }
            BagInventory = new Item[BagItemCount];
            for (int i = 0; i < BagItemCount; i++)
            {
                BagInventory[i] = new Item(this, BagItemAddress + i * 4);
            }

            PlayerWeapon = new Item(this, 0x55B959);
            PlayerArmour = new Item(this, 0x55B989);
            PlayerShield = new Item(this, 0x55B985);
            PlayerHammer = new Item(this, 0x55B95D);
        }
        private void CreateResidents()
        {
            ImportantResidents = new Resident[ImportantResidentCount];
            for (int i = 0; i < ImportantResidentCount; i++)
            {
                ImportantResidents[i] = new Resident(this, i + 1);
            }
            Residents = new Resident[ResidentCount];
            for (int i = 0; i < ResidentCount; i++)
            {
                Residents[i] = new Resident(this, ImportantResidentCount + i + 1);
            }
        }

        public static void Close()
        {
            Instance = null;
        }

        public Image GetThumbnail()
        {
            Image image = Image.CreateEmpty(320, 180, false, Image.Format.Rgb8);
            var xspan = GetBytes(ThumbnailAddress, ThumbnailSize, header: true);
            for (int y = 0; y < 180; y++)
            {
                for (int x = 0; x < 320; x++)
                {
                    image.SetPixel(x, y, new Color(){
                        B8 = xspan[(y * 320 * 3) + (x * 3)],
                        G8 = xspan[(y * 320 * 3) + (x * 3) + 1],
                        R8 = xspan[(y * 320 * 3) + (x * 3) + 2]
                    });
                }
            }
            return image;
        }

        public void ClearHotbar()
        {
            foreach (Item item in HotbarInventory)
            {
                item.ItemID = 0;
                item.Count = 0;
            }
        }
        public void ClearBag()
        {
            foreach (Item item in BagInventory)
            {
                item.ItemID = 0;
                item.Count = 0;
            }
        }

        public class Resident
        {
            public CommonData SaveData { get; set; }
            public int ID { get; set; }
            public int Address => (ID - 1) * ResidentSize + ImportantResidentAddress;

            public bool IsImportant => (ID - 1) <= ImportantResidentCount;

            public string Name { get => SaveData.GetString(Address, 30).Replace("\0", ""); set => SaveData.SetString(Address, value, 30); }
            public bool UseCustomName { get => SaveData.GetBit(Address + 301, 7); set => SaveData.SetBit(Address + 301, 7, value); }
            public byte GenericName { get => SaveData.GetByte(Address + 274); set => SaveData.SetByte(Address + 274, value); }
            public byte Sex { get => SaveData.GetByte(Address + 258); set => SaveData.SetByte(Address + 258, value); }
            public short HP { get => SaveData.GetInt16(Address + 146); set => SaveData.SetInt16(Address + 146, value); }
            public byte Job { get => SaveData.GetByte(Address + 271); set => SaveData.SetByte(Address + 271, value); }
            public ushort Type { get => SaveData.GetUInt16(Address + 144); set => SaveData.SetUInt16(Address + 144, value); }
            public bool CanEquip { get => SaveData.GetBit(Address + 307, 1); set => SaveData.SetBit(Address + 307, 1, value); }
            public bool CanBattle { get => SaveData.GetBit(Address + 259, 1); set => SaveData.SetBit(Address + 259, 1, value); }
            public byte HomeIsland { get => SaveData.GetByte(Address + 275); set => SaveData.SetByte(Address + 275, value); }
            public byte CurrentIsland { get => SaveData.GetByte(Address + 223); set => SaveData.SetByte(Address + 223, value); }
            public byte IslandSection { get => SaveData.GetByte(Address + 324); set => SaveData.SetByte(Address + 324, value); }
            public ushort Face { get => SaveData.GetUInt16(Address + 229); set => SaveData.SetUInt16(Address + 229, value); }
            public ushort Body { get => SaveData.GetUInt16(Address + 233); set => SaveData.SetUInt16(Address + 233, value); }
            public ushort Hair { get => SaveData.GetUInt16(Address + 231); set => SaveData.SetUInt16(Address + 231, value); }
            public ushort SkinColour { get => SaveData.GetUInt16(Address + 239); set => SaveData.SetUInt16(Address + 239, value); }
            public ushort HairColour { get => SaveData.GetUInt16(Address + 237); set => SaveData.SetUInt16(Address + 237, value); }
            public ushort EyeColour { get => SaveData.GetUInt16(Address + 235); set => SaveData.SetUInt16(Address + 235, value); }
            public byte VoiceType { get => SaveData.GetByte(Address + 267); set => SaveData.SetByte(Address + 267, value); }
            public byte MessageType { get => SaveData.GetByte(Address + 266); set => SaveData.SetByte(Address + 266, value); }
            public bool LockGraphic { get => SaveData.GetBit(Address + 302, 4); set => SaveData.SetBit(Address + 302, 4, value); }
            public byte RoomSize { get => SaveData.GetByte(Address + 263); set => SaveData.SetByte(Address + 263, value); }
            public byte RoomFanciness { get => SaveData.GetByte(Address + 264); set => SaveData.SetByte(Address + 264, value); }
            public byte RoomAmbience { get => SaveData.GetByte(Address + 265); set => SaveData.SetByte(Address + 265, value); }

            public Resident(CommonData saveData, int id)
            {
                SaveData = saveData;
                ID = id;
            }

            public string GetDisplayName()
            {
                if (!string.IsNullOrEmpty(Name))
                    return Name;

                if (IsImportant)
                    return ImportantResidentName.Get(ID);

                return Name;
            }
        }
    }
}