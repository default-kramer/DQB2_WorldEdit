using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Text;
using Godot;
using Microsoft.VisualBasic;
using EyeOfRubiss.Info;

namespace EyeOfRubiss
{
    /// <summary> Class used for handling CMNDAT.BIN files, which hold DQB2 player, progress, and resident data, among other things. </summary>
    public class CommonData : SaveData
    {
        /// <summary> Length of the file header, in bytes. </summary>
        private const int HeaderLength = 0x2A444;

        /// <summary> Address of the file's thumbnail data, which is stored in the Header. </summary>
        private const int ThumbnailAddress = 0x10D;
        /// <summary> Size, in bytes, of the file's thumbnail data. </summary>
        private const int ThumbnailSize = 320 * 180 * 3;

        /// <summary> Starting address for hotbar items. </summary>
        private const int HotbarItemAddress = 0x55B28D;
        /// <summary> The maximum amount of items that can be stored in the player's hotbar. </summary>
        private const int HotbarItemCount = 15;
        /// <summary> Starting address for items in the player's bag. </summary>
        private const int BagItemAddress = 0x55B2C9;
        /// <summary> The maximum amount of items that can be stored in the player's bag. </summary>
        private const int BagItemCount = 420;

        /// <summary> Starting address for important resident data. Also the starting address for resident data in general. </summary>
        private const int ImportantResidentAddress = 0x6ACC8;
        /// <summary> Starting address for generic resident data. </summary>
        private const int ResidentAddress = 0x102A68;
        /// <summary> The amount of data slots available for important residents. </summary>
        private const int ImportantResidentCount = 1023;
        /// <summary> The amount of data slots available for generic residents. </summary>
        private const int ResidentCount = 238;
        /// <summary> Size, in bytes, of a Resident's data. </summary>
        private const int ResidentSize = 608;

        /// <summary> The main active CommonData instance. </summary>
        public static CommonData Instance { get; private set; }
        /// <returns> True if Instance is not null and Instance is loaded. </returns>
        public static bool HasInstance() => Instance is not null && Instance.IsLoaded;

        /// <returns> True if the CommonData instance calling this method is the main active CommonData instance. </returns>
        public bool IsInstance() => this == Instance;

        /// <summary> The date and time when this file was last saved. </summary>
        public DateTime LastSaveTime { get => DateTime.FromFileTime(GetInt64(0x2A40D, header: true)); set => SetInt64(0x2A40D, value.ToFileTime(), header: true); }

        /// <summary> The byte index of the island the Builder is currently sailing from. If the Builder is not sailing, same as ToIsland. </summary>
        public byte FromIsland { get => GetByte(0xC9, header: true); set => SetByte(0xC9, value, header: true); }
        /// <summary> The byte index of the island the Builder is currently on. </summary>
        public byte ToIsland { get => GetByte(0xC8, header: true); set => SetByte(0xC8, value, header: true); }

        /// <summary> The Builder's name. </summary>
        public string PlayerName { get => GetString(0xCD, 12, header: true); set => SetString(0xCD, value, 12, header: true); }
        /// <summary> The Builder's gender. False if female, True if male. </summary>
        public bool PlayerGender { get => GetBit(0xC4, 1, header: true); set => SetBit(0xC4, 1, value, header: true); }
        /// <summary> The Builder's Level. </summary>
        public byte PlayerLevel { get => GetByte(0xCA9CF); set => SetByte(0xCA9CF, value); }
        /// <summary> Amount of experience the Builder has accrued. </summary>
        public short PlayerExperience { get => GetInt16(0x6A9D1); set => SetInt16(0x6A9D1, value); } // TODO test if signed
        /// <summary> The Builder's HP stat. TODO test if signed, also is this current or max? </summary>
        public short PlayerHP { get => GetInt16(0x6A890); set => SetInt16(0x6A890, value); }
        /// <summary> TODO I think this means bonuses from seeds of life, test this please -- also test if signed </summary>
        public short PlayerAdditionalHP { get => GetInt16(0x6A892); set => SetInt16(0x6A892, value); }
        /// <summary> The Builder's current hunger value. </summary>
        public short PlayerHunger { get => GetInt16(0x6A896); set => SetInt16(0x6A896, value); }
        /// <summary> The Builder's maximum stamina. TODO test if signed </summary>
        public short PlayerStamina { get => GetInt16(0x6A8A0); set => SetInt16(0x6A8A0, value); }
        /// <summary> The Builder's Attack stat. TODO test if signed </summary>
        public short PlayerAttack { get => GetInt16(0x6A898); set => SetInt16(0x6A898, value); }
        /// <summary> The Builder's Defence stat. TODO test if signed </summary>
        public short PlayerDefence { get => GetInt16(0x6A89A); set => SetInt16(0x6A89A, value); }

        /// <summary> Flag for whether or not the player has unlocked the Bag. </summary>
        public bool UnlockBag { get => GetBit(0x635, 0); set => SetBit(0x635, 0, value); }
        /// <summary> Flag for whether or not the player has unlocked the Windbraker. </summary>
        public bool UnlockWindbraker { get => GetBit(0x6A8A2, 1); set => SetBit(0x6A8A2, 1, value); }
        /// <summary> TODO: What does this do? </summary>
        public bool UnlockFlipper { get => GetBit(0x6A8A3, 1); set => SetBit(0x6A8A3, 1, value); }
        /// <summary> Flag for whether or not the player has unlocked the Big Bash. </summary>
        public bool UnlockBigBash { get => GetBit(0x506, 1); set => SetBit(0x506, 1, value); }
        /// <summary> Flag for whether or not the player has unlocked the Bigger Bash. </summary>
        public bool UnlockBiggerBash { get => GetBit(0x502, 3); set => SetBit(0x502, 3, value); }
        /// <summary> TODO: What does this do? </summary>
        public bool BottomlessPotUse { get => GetBit(0x504, 2); set => SetBit(0x504, 2, value); }
        /// <summary> TODO: What does this do? </summary>
        public bool BottomlessPot { get => GetBit(0x67D, 1); set => SetBit(0x67D, 1, value); }
        /// <summary> Flag for whether or not the player has unlocked the Buildnoculars. </summary>
        public bool UnlockBuildnoculars { get => GetBit(0x502, 7); set => SetBit(0x502, 7, value); }
        /// <summary> Flag for whether or not the player has unlocked the Buggy Buggy's flight function. </summary>
        public bool CarFly { get => GetBit(0x506, 6); set => SetBit(0x506, 6, value); }
        /// <summary> Flag for whether or not the player has unlocked the Buggy Buggy's laser function. </summary>
        public bool CarBeam { get => GetBit(0x506, 7); set => SetBit(0x506, 7, value); }
        /// <summary> Flag for whether or not the player has unlocked the Buggy Buggy's headlight function. </summary>
        public bool CarLight { get => GetBit(0x506, 5); set => SetBit(0x506, 5, value); }
        /// <summary> TODO: What does this do? </summary>
        public bool Transform { get => GetBit(0x500, 6); set => SetBit(0x500, 6, value); }
        /// <summary> TODO: What does this do? </summary>
        public bool Expression { get => GetBit(0x501, 1); set => SetBit(0x501, 1, value); }

        /// <summary> Amount of mini medals the player has collected. </summary>
        public byte MiniMedals { get => GetByte(0x226E40); set => SetByte(0x226E40, value); }
        /// <summary> Amount of mini medals the player has exchanged with the Hairy Hermit. </summary>
        public byte MiniMedalsDeposited { get => GetByte(0x226E44); set => SetByte(0x226E44, value); }

        /// <summary> Array of InventoryItem instances pertaining to slots in the player's hotbar. </summary>
        public InventoryItem[] HotbarInventory { get; set; }
        /// <summary> Array of InventoryItem instances pertaining to slots in the player's bag. </summary>
        public InventoryItem[] BagInventory { get; set; }

        /// <summary> InventoryItem instance pertaining to the slot which holds the Builder's equipped weapon. </summary>
        public InventoryItem PlayerWeapon { get; set; }
        /// <summary> InventoryItem instance pertaining to the slot which holds the Builder's equipped armour. </summary>
        public InventoryItem PlayerArmour { get; set; }
        /// <summary> InventoryItem instance pertaining to the slot which holds the Builder's equipped shield. </summary>
        public InventoryItem PlayerShield { get; set; }
        /// <summary> InventoryItem instance pertaining to the slot which holds the Builder's equipped hammer. </summary>
        public InventoryItem PlayerHammer { get; set; }

        /// <summary> Array of Resident instances of important residents. </summary>
        public Resident[] ImportantResidents { get; set; }
        /// <summary> Array of Resident instances of generic residents. </summary>
        public Resident[] Residents { get; set; }

        /// <summary>
        /// Try to load a CommonData instance from the specified path. If successful, sets the current Instance to that new CommonData instance.
        /// </summary>
        /// <param name="path">The path of the file from which to load.</param>
        /// <returns>The newly created CommonData instance; otherwise, null.</returns>
        public static CommonData TryLoadAndSet(string path)
        {
            if (TryLoad(path) is CommonData commonData)
            {
                return Instance = commonData;
            }
            else return null;
        }
        /// <summary>
        /// Try to load a CommonData instance from the specified path.
        /// </summary>
        /// <param name="path">The path of the file from which to load.</param>
        /// <returns>The newly created CommonData instance; otherwise, null.</returns>
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
        /// <summary>
        /// Try to load a CommonData instance from the specified path, without decompressing the buffer.
        /// </summary>
        /// <param name="path">The path of the file from which to load.</param>
        /// <returns>The newly created CommonData instance; otherwise, null.</returns>
        public static CommonData QuickLoad(string path)
        {
            CommonData commonData = new();
            if (commonData._QuickLoad(path, HeaderLength))
            {
                return commonData;
            }
            else return null;
        }

        /// <summary> Initialises the InventoryItems to be used by this instance. Called when loading from file. </summary>
        private void CreateItems()
        {
            HotbarInventory = new InventoryItem[HotbarItemCount];
            for (int i = 0; i < HotbarItemCount; i++)
            {
                HotbarInventory[i] = new InventoryItem(this, HotbarItemAddress + i * 4);
            }
            BagInventory = new InventoryItem[BagItemCount];
            for (int i = 0; i < BagItemCount; i++)
            {
                BagInventory[i] = new InventoryItem(this, BagItemAddress + i * 4);
            }

            PlayerWeapon = new InventoryItem(this, 0x55B959);
            PlayerArmour = new InventoryItem(this, 0x55B989);
            PlayerShield = new InventoryItem(this, 0x55B985);
            PlayerHammer = new InventoryItem(this, 0x55B95D);
        }
        /// <summary> Initialises the Residents to be used by this instance. Called when loading from file. </summary>
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

        /// <summary> Sets the current active Instance to null. </summary>
        public static void Close()
        {
            Instance = null;
        }

        /// <returns> The thumbnail for this save file as an Image. </returns>
        public Image GetThumbnail()
        {
            Image image = Image.CreateEmpty(320, 180, false, Image.Format.Rgb8);
            var xspan = GetBytes(ThumbnailAddress, ThumbnailSize, header: true);
            for (int y = 0; y < 180; y++)
            {
                for (int x = 0; x < 320; x++)
                {
                    image.SetPixel(x, y, new Color()
                    {
                        B8 = xspan[(y * 320 * 3) + (x * 3)],
                        G8 = xspan[(y * 320 * 3) + (x * 3) + 1],
                        R8 = xspan[(y * 320 * 3) + (x * 3) + 2]
                    });
                }
            }
            return image;
        }

        /// <summary> Sets the Count and ID of all items in the hotbar to 0. </summary>
        public void ClearHotbar()
        {
            foreach (InventoryItem item in HotbarInventory)
            {
                item.ItemID = 0;
                item.Count = 0;
            }
        }
        /// <summary> Sets the Count and ID of all items in the bag to 0. </summary>
        public void ClearBag()
        {
            foreach (InventoryItem item in BagInventory)
            {
                item.ItemID = 0;
                item.Count = 0;
            }
        }

        /// <summary> Class representing the data of a Resident within a CommonData instance. </summary>
        /// <param name="saveData"> The CommonData instance to which this Resident belongs. </param>
        /// <param name="id"> The ID of the resident. Used as the index at which this Resident's data is stored in the CommonData's Buffer. </param>
        public class Resident(CommonData saveData, int id)
        {
            /// <summary> The CommonData instance to which this Resident belongs. </summary>
            public CommonData SaveData { get; set; } = saveData;
            /// <summary> The ID of the resident. Used as the index at which this Resident's data is stored in the CommonData's Buffer. </summary>
            public int ID { get; set; } = id;
            /// <summary> The address in the Buffer of the SaveData at which this Resident's data is stored. </summary>
            public int Address => (ID - 1) * ResidentSize + ImportantResidentAddress;

            /// <returns> Whether or not this Resident is considered an important NPC. True if the Resident's ID (minus 1) falls at or below the ImportantResidentCount. </returns>
            public bool IsImportant() => (ID - 1) <= ImportantResidentCount;

            /// <summary> The (custom) name set for this Resident. Overrides their default name if UseCustomName is true. </summary>
            public string Name { get => SaveData.GetString(Address, 30).Replace("\0", ""); set => SaveData.SetString(Address, value, 30); }
            /// <summary> If true, this Resident uses the custom name specified in the Name field instead of their default name. </summary>
            public bool UseCustomName { get => SaveData.GetBit(Address + 301, 7); set => SaveData.SetBit(Address + 301, 7, value); }
            /// <summary> A byte ID associated with this Resident's generic name (used if they are a generic NPC and the UseCustomName flag is false). </summary>
            public byte GenericName { get => SaveData.GetByte(Address + 274); set => SaveData.SetByte(Address + 274, value); }
            /// <summary> A byte value representing the Resident's sex. 01 if male, 00 if female. Any other value is also considered female. </summary>
            public byte Sex { get => SaveData.GetByte(Address + 258); set => SaveData.SetByte(Address + 258, value); }
            /// <summary> The Resident's HP stat. TODO: Maximum or current?? </summary>
            public short HP { get => SaveData.GetInt16(Address + 146); set => SaveData.SetInt16(Address + 146, value); }
            /// <summary> A byte value representing the Resident's job. </summary>
            public byte Job { get => SaveData.GetByte(Address + 271); set => SaveData.SetByte(Address + 271, value); }
            /// <summary> A byte value representing the "type" of resident this Resident functions as. </summary>
            public ushort Type { get => SaveData.GetUInt16(Address + 144); set => SaveData.SetUInt16(Address + 144, value); }
            /// <summary> A boolean value that is true if the Resident is capable of being equipped with weapons. </summary>
            public bool CanEquip { get => SaveData.GetBit(Address + 307, 1); set => SaveData.SetBit(Address + 307, 1, value); }
            /// <summary> A boolean value that is true if the Resident will participate in battle. </summary>
            public bool CanBattle { get => SaveData.GetBit(Address + 259, 1); set => SaveData.SetBit(Address + 259, 1, value); }
            /// <summary> A byte value pertaining to the ID of this Resident's Home Island. </summary>
            public byte HomeIsland { get => SaveData.GetByte(Address + 275); set => SaveData.SetByte(Address + 275, value); }
            /// <summary> A byte value pertaining to the ID of the island that the Resident is currently on. </summary>
            public byte CurrentIsland { get => SaveData.GetByte(Address + 223); set => SaveData.SetByte(Address + 223, value); }
            /// <summary> A byte value pertaining to the section of the Isle of Awakening in which this Resident lives (Green Gardens, Scarlet Sands, etc. Should be set to 0 if CurrentIsland is not the Isle of Awakening). </summary>
            public byte IslandSection { get => SaveData.GetByte(Address + 324); set => SaveData.SetByte(Address + 324, value); }

            /// <summary> ID of the Resident's face model. </summary>
            public ushort Face { get => SaveData.GetUInt16(Address + 229); set => SaveData.SetUInt16(Address + 229, value); }
            /// <summary> ID of the Resident's body model. </summary>
            public ushort Body { get => SaveData.GetUInt16(Address + 233); set => SaveData.SetUInt16(Address + 233, value); }
            /// <summary> ID of the Resident's hair model. </summary>
            public ushort Hair { get => SaveData.GetUInt16(Address + 231); set => SaveData.SetUInt16(Address + 231, value); }
            /// <summary> ID of the Resident's skin colour. </summary>
            public ushort SkinColour { get => SaveData.GetUInt16(Address + 239); set => SaveData.SetUInt16(Address + 239, value); }
            /// <summary> ID of the Resident's hair colour. </summary>
            public ushort HairColour { get => SaveData.GetUInt16(Address + 237); set => SaveData.SetUInt16(Address + 237, value); }
            /// <summary> ID of the Resident's eye colour. </summary>
            public ushort EyeColour { get => SaveData.GetUInt16(Address + 235); set => SaveData.SetUInt16(Address + 235, value); }
            /// <summary> ID of the Resident's voice type. Determines which sound effects are played when the Resident makes noise. Not applicable to non-human Residents. </summary>
            public byte VoiceType { get => SaveData.GetByte(Address + 267); set => SaveData.SetByte(Address + 267, value); }
            /// <summary> ID of the Resident's message type. Determines which set of messages the Resident says when responding to events. </summary>
            public byte MessageType { get => SaveData.GetByte(Address + 266); set => SaveData.SetByte(Address + 266, value); }
            /// <summary> True if the Resident should override its Face, Body, and Hair models with their default values. </summary>
            public bool LockGraphic { get => SaveData.GetBit(Address + 302, 4); set => SaveData.SetBit(Address + 302, 4, value); }

            /// <summary> The Resident's preferred room size. </summary>
            public byte RoomSize { get => SaveData.GetByte(Address + 263); set => SaveData.SetByte(Address + 263, value); }
            /// <summary> The Resident's preferred room fanciness level. </summary>
            public byte RoomFanciness { get => SaveData.GetByte(Address + 264); set => SaveData.SetByte(Address + 264, value); }
            /// <summary> The Resident's preferred room ambience. </summary>
            public byte RoomAmbience { get => SaveData.GetByte(Address + 265); set => SaveData.SetByte(Address + 265, value); }

            /// <summary> The Resident's current X position, where 0 represents the exact centre of the island. </summary>
            public float PositionX { get => SaveData.GetSingle(Address + 0x5C); set => SaveData.SetSingle(Address + 0x5C, value); }
            /// <summary> The Resident's current Y position. </summary>
            public float PositionY { get => SaveData.GetSingle(Address + 0x60); set => SaveData.SetSingle(Address + 0x60, value); }
            /// <summary> The Resident's current Z position, where 0 represents the exact centre of the island. </summary>
            public float PositionZ { get => SaveData.GetSingle(Address + 0x64); set => SaveData.SetSingle(Address + 0x64, value); }
            /// <summary> The Resident's current rotation. 0 is facing directly south, and Pi is facing directly north. </summary>
            public float Rotation { get => SaveData.GetSingle(Address + 0x8C); set => SaveData.SetSingle(Address + 0x8C, value); }

            /// <returns> The name that would be displayed in-game for this Resident. </returns>
            public string GetDisplayName()
            {
                if (!string.IsNullOrEmpty(Name))
                    return Name;

                if (IsImportant())
                    return ImportantResidentName.Get(ID);

                return Name;
            }
        }
    }
}