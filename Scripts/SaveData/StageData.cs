using System;
using System.Linq;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Godot;
using System.Dynamic;
using System.Reflection;
using System.Collections.Generic;
using System.Data;

namespace DQBEdit
{
    public class StageData : SaveData
    {
        /*
            Here follows a general map of the buffer's layout.
    
            Chunk Grid: 0x24C7C1 - 0x24E7C0
            ???: 0x24E7C1 - 0x24E7D0
            Decorations: 0x24E7D1 - 0x150E7D0
            Blocks: 0x183FEF0 - end?
        */
    
        private const int HeaderLength = 0x110;
    
        /*
            The chunk grid is a 64x64 grid of chunk IDs. Each chunk ID is a 16-bit integer. The grid is laid out from left to right, and then from top to bottom.
            Each integer ID starting with 0 points to the position of the chunk's blocks in the data. So, the formula to find out the position of a chunk's block data is as follows:
                BlockAddress + (ChunkSize * {ID})
            For chunks without block data, their ID is instead set to 0xFFFF.
        */
        private const int ChunkGridAddress = 0x24C7C1; // Starting address of the chunk grid
        private const int ChunkGridSize = 0x1000; // Size of chunk grid (each entry is 2 bytes).

        /*
            Blocks are a series of unsigned 16-bit integers laid out in order, broken up by chunk.
            Within a single chink, blocks are laid out first from west to east, then from north to south, and finally from bottom to top.
            Each chunk of blocks is 32x32x96 blocks in size (96 blocks high).
        */    
        private const int BlockAddress = 0x183FEF0; // Starting address of block data. Block data is an array of ushorts broken up into chunks, then layers, then blocks.
        private const int ChunkSize = 0x30000; // Size of a full chunk of blocks.
        private const int LayerSize = 0x800; // Size of a full layer of blocks.
    
        /*
            The "decoration list" is a list of decoration IDs and the chunk IDs to which they belong. Each entry is a 32-bit value.
            The first 12 bits of the value point to the chunk ID, and the remaining 20 bits point to the decoration ID.
            So, if the value was 0xDEADBEEF, then the decoration with the ID of 0xDBEEF would belong to chunk ID 0xDEA.
        */
        private const int DecorationListAddress = 0x150E7D1; // Starting address of the decoration list
        private const int DecorationListSize = 0xC8000; // Size of decoration list (Each entry is 4 bytes).
        private const int DecorationAddress = 0x24E7D1; // Start of decoration data.
        private const int DecorationSize = 24;
    
        public static StageData Instance { get; private set; }
        public static bool HasInstance => Instance is not null && Instance.IsLoaded;
    
        public byte IslandID { get => GetByte(0xC0ED6); set => SetByte(0xC0ED6, value); }
        public ushort ChunkCount { get => GetUInt16(0x1451AF); set => SetUInt16(0x1451AF, value); }
        public int Gratitude { get => GetInt32(0xC0ECC); set => SetInt32(0xC0ECC, value); }
        public float Time { get => GetSingle(0xC0F50); set => SetSingle(0xC0F50, value); }
        public byte Weather { get => GetByte(0xC0F54); set => SetByte(0xC0F54, value); }

        public int SeaLevel { get; set; } = 30;
    
        public Chunk[] Chunks { get; set; }
        public Decoration[] Decorations { get; set; }
    
        public static StageData TryLoadAndSet(string path)
        {
            if (TryLoad(path) is StageData stageData)
            {
                return Instance = stageData;
            }
            else return null;
        }
        public static StageData TryLoad(string path)
        {
            StageData stageData = new();
            if (stageData._TryLoad(path, HeaderLength))
            {
                stageData.SetupChunks();
                stageData.SetupDecorations();
                return stageData;
            }
            else return null;
        }
        private void SetupChunks()
        {
            Chunks = new Chunk[ChunkGridSize];
            for (int i = 0; i < ChunkGridSize; i++)
            {
                Chunks[i] = new Chunk(this, i);
            }
        }
        private void SetupDecorations()
        {
            Decorations = new Decoration[DecorationListSize];
            for (int i = 0; i < DecorationListSize; i++)
            {
                Decorations[i] = new Decoration(this, i);
            }
        }
    
        public static void Close()
        {
            Instance = null;
        }
    
        public ushort GetChunkIdByIndex(int index)
        {
            if (index < 0 || index >= Chunks.Length)
                return 0xFFFF;
            else
                return GetUInt16(ChunkGridAddress + index * 2);
        }
        public Chunk GetChunkByIndex(int index)
        {
            return Chunks[index];
        }
        public Chunk GetChunkById(ushort id)
        {
            return Chunks.FirstOrDefault(chunk => chunk.ID == id);
        }

        public IEnumerable<Chunk> GetUsedChunks()
        {
            return Chunks.Where(chunk => chunk.IsUsed);
        }
    


        public BlockInstance GetBlockAtIndex(Vector3I position) // Chunk ID, layer, block index
        {
            if (position.X < 0 || position.X >= ushort.MaxValue || position.Y < 0 || position.Y >= 96 || position.Z < 0 || position.Z >= LayerSize)
                return null;

            Chunk chunk = GetChunkById((ushort)position.X);
            if (chunk is null || !chunk.IsUsed)
                return null;
            
            return chunk.GetBlock(position.Y, position.Z);
        }
        public BlockInstance GetBlockAtPos(Vector3I position)
        {
            // X is east-west
            // Z is north-south
            return GetBlockAtIndex(EuclidPosToIndex(position));
        }
        
        public Vector3I EuclidPosToIndex(Vector3I position)
        {
            position += new Vector3I(1024, 0, 1024); // Centers the grid at 0,0,0. Consider removing if problems arise.
            if (position.X < 0 || position.Y < 0 || position.Z < 0)
                return -Vector3I.One;

            // GetChunkAt?
            int x = position.X / 32;
            int z = position.Z / 32;
            int chunkId = GetChunkIdByIndex(x + (z * 64));
            int tile = position.X % 32 + (position.Z % 32 * 32);

            return new Vector3I(chunkId, position.Y, tile);

            // FIXME
            // the negative numbers are fake! point zero is an illusion!
            //int chunkGridIndex = (int)(Math.Floor((double)(position.X + 1024) / 32) + Math.Floor((double)(position.Z + 1024) / 32) * 64);
            //int chunkNumber = GetChunkIdByIndex(chunkGridIndex);
            //int tile = (position.X + 1024) % 32 + ((position.Z + 1024) % 32 * 32);
            //return new Vector3I(chunkNumber,position.Y,tile);
        }
        public static bool PositionIsInBounds(Vector3I position)
        {
            if (position.Y < 0 || position.Y >= 96)
                return false;
            if (position.Z < 0 || position.Z >= 2048 || position.X < 0 || position.X >= 2048)
                return false;
            
            return true;
        }
        public Chunk GetChunkAtPosition(Vector3I position)
        {
            int x = position.X / 32;
            int z = position.Z / 32;
            return GetChunkByIndex(x + (z * 64));
        }
        
        public void MakeSuperflat(List<ushort> layers)
        {
            foreach (Chunk chunk in GetUsedChunks())
            {
                chunk.Clear();
                for (int i = 0; i < layers.Count; i++)
                {
                    chunk.SetLayer(i, layers[i]);
                }
                // BIG NOTE: if there's a block with additional data (like a lore book generated with the world that u can read) removing it will break the superflat
                // FIXME
                if (chunk.ID >= 163)
                    break;
            }
        }
        
        public class Chunk
        {
            public StageData SaveData { get; set; }
            public int Index { get; set; } = -1;
            public ushort ID => SaveData.GetChunkIdByIndex(Index);
    
            public Vector3I EuclidOrigin => new Vector3I((Index % 64 * 32) - 1024, 0, (Index / 64 * 32) - 1024);
    
            public Chunk(StageData saveData, int index)
            {
                SaveData = saveData;
                Index = index;
            }
    
            public bool IsUsed => ID != ushort.MaxValue;
    
            public int BlockAddress => StageData.BlockAddress + ChunkSize * ID;
    
            public BlockInstance[] GetBlocks()
            {
                if (!IsUsed)
                    return null;
                
                BlockInstance[] blocks = new BlockInstance[ChunkSize / 2];
                for (int i = 0; i < ChunkSize / 2; i++)
                {
                    blocks[i] = new BlockInstance(SaveData, BlockAddress + i * 2);
                }
                return blocks;
            }
            public BlockInstance GetBlock(int layer, int index)
            {
                return IsUsed ? new BlockInstance(SaveData, BlockAddress + layer * LayerSize + index * 2) : null;
            }
            /*public List<Tuple<Vector3I, ushort>> GetBlocksAndEuclidPos()
            {
                List<Tuple<Vector3I, ushort>> blocks = new();
                int x = 0;
                int y = 0;
                int z = 0;
                Vector3I origin = EuclidOrigin;
                foreach (ushort block in GetBlocks())
                {
                    blocks.Add(new Tuple<Vector3I, ushort>(new Vector3I(x,y,z) + origin, block));
    
                    x++;
                    if (x >= 32)
                    {
                        x = 0;
                        z++;
                    }
                    if (z >= 32)
                    {
                        z = 0;
                        y++;
                    }
                }
                return blocks;
            }*/
            public void SetBlock(int layer, int index, ushort blockId, bool? playerPlaced = null, byte? chisel = null)
            {
                if (!IsUsed)
                    return;
                
                BlockInstance block = GetBlock(layer, index);
                block.BlockID = blockId;
                if (playerPlaced is not null)
                    block.PlayerPlaced = (bool)playerPlaced;
                if (chisel is not null)
                    block.Chisel = (byte)chisel;
            }
            public void SetLayer(int layer, ushort block)
            {
                for (int i = 0; i < LayerSize / 2; i++)
                {
                    SetBlock(layer, i, block);
                }
            }
    
            public void Clear()
            {
                for (int i = BlockAddress; i < BlockAddress + ChunkSize; i++)
                {
                    SaveData.SetByte(i, 0); // TODO replace with fill
                }
            }
        }
        public class BlockInstance
        {
            public StageData SaveData { get; set; }
            public int Address { get; set; }

            public ushort BlockID { get => (ushort)SaveData.GetNumberBitwise(Address, 0, 11); set => SaveData.SetNumberBitwise(Address, 0, 11, value); }
            public bool PlayerPlaced { get => SaveData.GetBit(Address + 1, 3); set => SaveData.SetBit(Address + 1, 3, value); }
            public byte Chisel { get => (byte)SaveData.GetNumberBitwise(Address + 1, 4, 4); set => SaveData.SetNumberBitwise(Address + 1, 4, 4, value); }

            public BlockInstance(StageData saveData, int address)
            {
                SaveData = saveData;
                Address = address;
            }

            public enum ChiselType : int
            {
                FullBlock = 0,
                DiagonalNorth = 1,
                DiagonalNorthwest = 2,
                DiagonalWest = 3,
                DiagonalSouthwest = 4,
                DiagonalSouth = 5,
                DiagonalSoutheast = 6,
                DiagonalEast = 7,
                DiagonalNortheast = 8,
                ConcaveNorthwest = 9,
                ConcaveSouthwest = 10,
                ConcaveSoutheast = 11,
                ConcaveNortheast = 12,
                TopHalf = 13,
                BottomHalf = 14
            }
        }

        public class Decoration
        {
            public StageData SaveData { get; set; }
            public int Position { get; set; }
    
            public int ListAddress => DecorationListAddress + Position * 4;
    
            public ushort ChunkIndex
            {
                get
                {
                    return (ushort)SaveData.GetNumberBitwise(ListAddress, 0, 12);
                }
                set
                {
                    SaveData.SetNumberBitwise(ListAddress, 0, 12, value);
                }
            }
            public uint Index
            {
                get
                {
                    return SaveData.GetNumberBitwise(ListAddress, 12, 20);
                }
                set
                {
                    SaveData.SetNumberBitwise(ListAddress, 12, 20, value);
                }
            }
    
            public int DataAddress => DecorationAddress + (int)Index * DecorationSize;
    
            public ushort DecorationID
            {
                get
                {
                    return (byte)SaveData.GetNumberBitwise(DataAddress + 8, 0, 13);
                }
                set
                {
                    SaveData.SetNumberBitwise(DataAddress + 8, 0, 13, value);
                }
            }
    
            public byte X
            {
                get
                {
                    return (byte)SaveData.GetNumberBitwise(DataAddress + 9, 5, 5);
                }
                set
                {
                    SaveData.SetNumberBitwise(DataAddress + 9, 5, 5, value);
                }
            }
            public byte Y
            {
                get
                {
                    return (byte)SaveData.GetNumberBitwise(DataAddress + 10, 2, 7);
                }
                set
                {
                    SaveData.SetNumberBitwise(DataAddress + 10, 2, 7, value);
                }
            }
            public byte Z
            {
                get
                {
                    return (byte)SaveData.GetNumberBitwise(DataAddress + 11, 1, 5);
                }
                set
                {
                    SaveData.SetNumberBitwise(DataAddress + 11, 1, 5, value);
                }
            }
            public byte Rotation
            {
                get
                {
                    return (byte)SaveData.GetNumberBitwise(DataAddress + 11, 6, 2);
                }
                set
                {
                    SaveData.SetNumberBitwise(DataAddress + 11, 6, 2, value);
                }
            }
    
            public Decoration(StageData saveData, int position)
            {
                SaveData = saveData;
                Position = position;
            }
        }
    }
}