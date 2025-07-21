using Godot;
using System;
using DQBEdit.Info;

namespace DQBEdit
{
    public class Item
    {
        private SaveData _SaveData;
        public int Address;

        public ushort ItemID { get => _SaveData.GetUInt16(Address); set => _SaveData.SetUInt16(Address, value); }
        public short Count { get => _SaveData.GetInt16(Address + 2); set => _SaveData.SetInt16(Address + 2, value); } // TODO check signed

        public Item(SaveData saveData, int address)
        {
            _SaveData = saveData;
            Address = address;
        }

        public ItemInfo GetInfo() => ItemInfo.Get(ItemID);
    }
}