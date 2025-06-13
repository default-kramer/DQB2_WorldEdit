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

namespace EyeOfRubiss
{
    /// <summary> Class used for handling STGDAT*.BIN files, which hold DQB2 island data. </summary>
    public class StageData : SaveData
    {
        /*
            Here follows a general map of the buffer's layout.
    
            Chunk Grid: 0x24C7C1 - 0x24E7C0
            ???: 0x24E7C1 - 0x24E7D0
            Props: 0x24E7D1 - 0x150E7D0
            Blocks: 0x183FEF0 - end?
        */

        /// <summary> Length of the file header, in bytes. </summary>
        private const int HeaderLength = 0x110;

        /*
            The chunk grid is a 64x64 grid of chunk IDs. Each chunk ID is a 16-bit integer. The grid is laid out from left to right, and then from top to bottom.
            Each integer ID starting with 0 points to the position of the chunk's blocks in the data. So, the formula to find out the position of a chunk's block data is as follows:
                BlockAddress + (ChunkSize * {ID})
            For chunks without block data, their ID is instead set to 0xFFFF.
        */
        /// <summary> Starting address of the chunk grid. </summary>
        private const int ChunkGridAddress = 0x24C7C1;
        /// <summary> The amount of entries in the chunk grid. Each entry is 2 bytes long, so the byte length is actually twice this value. </summary>
        private const int ChunkGridSize = 0x1000;

        /*
            Blocks are a series of 16-bit values laid out in order, broken up by chunk.
            Within a single chunk, blocks are laid out first from west to east, then from north to south, and finally from bottom to top.
            Each chunk of blocks is 32x32x96 blocks in size (96 blocks high).
        */
        /// <summary> Starting address of block data. </summary>
        private const int BlockAddress = 0x183FEF0;
        /// <summary> The size, in bytes, of a full chunk of blocks. </summary>
        private const int ChunkSize = 0x30000;
        /// <summary> The size, in bytes, of a full layer of blocks. </summary>
        private const int LayerSize = 0x800;

        /*
            The "prop list" is a list of prop IDs and the chunk IDs to which they belong. Each entry is a 32-bit value.
            The first 12 bits of the value point to the chunk ID, and the remaining 20 bits point to the prop ID.
            So, if the value was 0xDEADBEEF, then the prop with the ID of 0xDBEEF would belong to chunk ID 0xDEA.
        */
        /// <summary> Starting address of the Prop list. </summary>
        private const int PropListAddress = 0x150E7D1;
        /// <summary> The size of the Prop list. Each entry is 4 bytes long, so the length in bytes is four times this value. </summary>
        private const int PropListSize = 0xC8000;
        /// <summary> Starting address of Prop data. </summary>
        private const int PropAddress = 0x24E7D1;
        /// <summary> The size, in bytes, of data for a single Prop. </summary>
        private const int PropSize = 24;

        /// <summary> The main active StageData instance. </summary>
        public static StageData Instance { get; private set; }
        /// <returns> True if Instance is not null and Instance is loaded. </returns>
        public static bool HasInstance() => Instance is not null && Instance.IsLoaded;

        /// <summary> The ID of the island. </summary>
        public byte IslandID { get => GetByte(0xC0ED6); set => SetByte(0xC0ED6, value); }
        /// <summary> Count of used chunks in the data. Must be updated after the amount of used chunks changes. </summary>
        public ushort ChunkCount { get => GetUInt16(0x1451AF); set => SetUInt16(0x1451AF, value); }
        /// <summary> Amount of gratitude points the player has accrued on this island. Caps out at 99999. </summary>
        public int Gratitude { get => GetInt32(0xC0ECC); set => SetInt32(0xC0ECC, value); }
        /// <summary> The time on the island as a floating point number. TODO: What exactly do these values mean? </summary>
        public float Time { get => GetSingle(0xC0F50); set => SetSingle(0xC0F50, value); }
        /// <summary> Byte value representing the ID for the type of weather currently on the island. </summary>
        public byte Weather { get => GetByte(0xC0F54); set => SetByte(0xC0F54, value); }

        /// <summary> TODO </summary>
        public int SeaLevel { get; set; } = 30;

        /// <summary> Array of Chunk instances representing chunk data. </summary>
        public Chunk[] Chunks { get; set; }
        /// <summary> Array of Prop instances representing prop data. </summary>
        public Prop[] Props { get; set; }

        /// <summary>
        /// Try to load a StageData instance from the specified path. If successful, sets the current Instance to that new StageData instance.
        /// </summary>
        /// <param name="path">The path of the file from which to load.</param>
        /// <returns>The newly created StageData instance; otherwise, null.</returns>
        public static StageData TryLoadAndSet(string path)
        {
            if (TryLoad(path) is StageData stageData)
            {
                return Instance = stageData;
            }
            else return null;
        }
        /// <summary>
        /// Try to load a StageData instance from the specified path.
        /// </summary>
        /// <param name="path">The path of the file from which to load.</param>
        /// <returns>The newly created StageData instance; otherwise, null.</returns>
        public static StageData TryLoad(string path)
        {
            StageData stageData = new();
            if (stageData._TryLoad(path, HeaderLength))
            {
                stageData.SetupChunks();
                stageData.SetupProps();
                return stageData;
            }
            else return null;
        }
        /// <summary> Initialises the Chunks array by filling it with Chunk instances. Called when loading from file. </summary>
        private void SetupChunks()
        {
            Chunks = new Chunk[ChunkGridSize];
            for (int i = 0; i < ChunkGridSize; i++)
            {
                Chunks[i] = new Chunk(this, i);
            }
        }
        /// <summary> Initialises the Props array by filling it with Prop instances. Called when loading from file. </summary>
        private void SetupProps()
        {
            Props = new Prop[PropListSize];
            for (int i = 0; i < PropListSize; i++)
            {
                Props[i] = new Prop(this, i);
            }
        }

        public override void Save(string path = null)
        {
            DefragmentChunks();
            base.Save(path);
        }

        /// <summary> Sets the current active Instance to null. </summary>
        public static void Close()
        {
            Instance = null;
        }

        /// <summary> Get the chunk ID at the specified address in the chunk grid. </summary>
        /// <param name="index"> The index in the chunk grid to look up. </param>
        /// <returns> The ID of the chunk at the address in the chunk grid. </returns>
        public ushort GetChunkIdAtIndex(int index)
        {
            if (index < 0 || index >= Chunks.Length)
                return 0xFFFF;
            else
                return GetUInt16(ChunkGridAddress + index * 2);
        }
        /// <summary> Get the Chunk instance at the specified index in the Chunks array. </summary>
        /// <param name="index"> Index of chunk to get. </param>
        /// <returns> The Chunk instance at the specified index in the Chunks array. </returns>
        public Chunk GetChunkAtIndex(int index)
        {
            return Chunks[index];
        }
        /// <summary> Get the Chunk instance with the specified ID. </summary>
        /// <param name="id"> The chunk ID to look up. </param>
        /// <returns> The first Chunk instance with the specified ID; if there are none, null. </returns>
        public Chunk GetChunkById(ushort id)
        {
            return Chunks.FirstOrDefault(chunk => chunk.ID == id);
        }

        /// <returns> An IEnumerable containing every Chunk instance in the Chunks array where the ID is not 0xFFFF. </returns>
        public IEnumerable<Chunk> GetUsedChunks()
        {
            return Chunks.Where(chunk => chunk.IsUsed());
        }
        /// <summary> Changes the chunk IDs so that they are in order in the chunk grid. </summary>
        public void DefragmentChunks()
        {
            IOrderedEnumerable<Chunk> chunks = GetUsedChunks().OrderBy(chunk => chunk.Index);

            // Check to see if the enumerable is already sorted
            bool sorted = true;
            for (int i = 0; i < chunks.Count() - 1; i++)
            {
                Chunk previous = chunks.ElementAt(i);
                Chunk next = chunks.ElementAt(i + 1);

                if (previous.ID > next.ID)
                {
                    sorted = false;
                    break;
                }
            }
            if (sorted)
                return;

            byte[] chunkData = [];

            ushort index = 0; // TODO: Can this be done inside the sort function?
            foreach (Chunk chunk in chunks)
            {
                GD.Print($"Shifting chunk {chunk.ID} to {index}.");

                chunkData = [.. chunkData, .. chunk.GetData().ToArray()];
                chunk.ID = index;

                index++;
            }

            Fill(0, BlockAddress); // Zeroes out the block data
            SetBytes(BlockAddress, chunkData); // Copies the reserved chunk data

            return; /*

            List<byte> _buffer = [.. _Buffer[..BlockAddress]];

            IOrderedEnumerable<Chunk> chunks = GetUsedChunks().OrderBy(chunk => chunk.Index);
            List<byte[]> chunkData = [];
            ushort index = 0;
            foreach (Chunk chunk in chunks)
            {
                _buffer = [.. _buffer, .. chunk.GetData().ToArray()];
                GD.Print($"Shifting chunk {chunk.ID} to {index}.");
                chunk.ID = index;
                index++;
            }

            SetBuffer([.. _buffer]); // FIXME*/
        }
        /// <summary> Sets the chunk at the specified index to the new ID, resizing the Buffer if necessary. If no ID is specified, the lowest unused ID is used instead. </summary>
        /// <param name="index"> The index at which to add a chunk. </param>
        /// <param name="id"> The new ID to set. If not specified, the lowest unused ID is used instead. </param>
        /// <returns> The chunk at the specified index. If out of bounds, or if the chunk is already used, returns null. </returns>
        public Chunk AddChunk(int index, ushort? id = null)
        {
            if (index >= ChunkGridSize || index < 0)
                return null;

            Chunk chunk = GetChunkAtIndex(index);

            if (chunk.IsUsed())
                return null;

            chunk.ID = id ?? GetLowestUnusedChunkId();

            if (BlockAddress + ChunkSize * (chunk.ID + 1) > GetBufferSize())
            {
                Extend(BlockAddress + ChunkSize * (chunk.ID + 1));
            }

            GD.Print($"Created a new chunk {chunk.ID} at index {index}.");
            return chunk;
            // FIXME ~ 699 chunks may be the max?
        }
        /// <summary> Clears the chunk at the specified index and sets its ID to 0xFFFF. </summary>
        /// <param name="index"> The index of the chunk to remove. </param>
        public void RemoveChunk(int index)
        {
            if (index >= ChunkGridSize || index < 0)
                return;

            Chunk chunk = GetChunkAtIndex(index);

            chunk.Clear();
            chunk.ID = 0xFFFF;
        }

        /// <returns> The lowest ushort number not used as any Chunk ID. </returns>
        private ushort GetLowestUnusedChunkId()
        {
            IOrderedEnumerable<ushort> ordered = Chunks.Select(chunk => chunk.ID).Order();

            if (!ordered.Any() || ordered.First() > 0)
                return 0;

            for (int i = 0; i < ordered.Count() - 1; i++)
            {
                ushort a = ordered.ElementAt(i);
                ushort b = ordered.ElementAt(i + 1);

                if (b - a > 1)
                    return (ushort)(a + 1);
                if (b == 0xFFFF)
                    return (ushort)(a + 1);
            }

            return (ushort)(ordered.Last() + 1);
        }

        /// <summary> Converts a position in 3D space to a position in block data. </summary>
        /// <param name="position"> A Vector3I of the position in 3D space to convert. </param>
        /// <returns> A Vector3I with its X position set to the chunk grid index, its Y position set to its layer, and its Z position set to the tile index within the layer. </returns>
        public static Vector3I PositionToBlockPosition(Vector3I position)
        {
            int chunkIndex = (position.X / 32) + (position.Z / 32 * 64);
            int layer = position.Y;
            int tile = position.X % 32 + (position.Z % 32 * 32);

            return new Vector3I(chunkIndex, layer, tile);
        }

        /// <param name="position"> The position in 3D space (same as the position in the voxel terrain) to search. </param>
        /// <returns> A BlockInstance pertaining to the block at the specified position in 3D space. </returns>
        public BlockInstance GetBlockAtPosition(Vector3I position)
        {
            if (!PositionIsInBounds(position))
                return null;

            Vector3I blockPosition = PositionToBlockPosition(position);

            Chunk chunk = GetChunkAtIndex(blockPosition.X);

            if (chunk is null || !chunk.IsUsed())
                return null;

            return chunk.GetBlock(blockPosition.Y, blockPosition.Z);
        }
        /// <summary> Sets the block at the specified position in 3D space. </summary>
        /// <param name="position"> The position in 3D space (same as the position in the voxel terrain) to set. </param>
        /// <param name="blockId"> The ID of the block to set at the specified position. </param>
        /// <param name="chisel"> The ChiselType of the block to set at the specified position. If not set, the chisel type of the block at the position is not changed. </param>
        /// <param name="playerPlaced"> Whether or not the set block should bear the "PlayerPlaced" flag. If not set, the flag of the block at the position is not changed. </param>
        /// <param name="createChunk"> If true, automatically creates a chunk at the block's position if none exists. False by default. </param>
        /// <returns> True if the block was successfully updated; otherwise, false. </returns>
        public bool SetBlockAtPosition(Vector3I position, ushort blockId, BlockInstance.ChiselType? chisel = null, bool? playerPlaced = null, bool createChunk = false)
        {
            if (!PositionIsInBounds(position))
                return false;

            Chunk chunk = GetChunkAtPosition(position);

            if (chunk is null || !chunk.IsUsed())
            {
                if (createChunk)
                {
                    AddChunk(chunk.Index);
                }
                else return false;
            }

            BlockInstance block = GetBlockAtPosition(position);
            block.BlockID = blockId;
            if (chisel is BlockInstance.ChiselType _chisel)
                block.Chisel = _chisel;
            if (playerPlaced is bool _playerPlaced)
                block.PlayerPlaced = _playerPlaced;

            return true;
        }

        /// <param name="position"> The position in 3D space to query. </param>
        /// <returns> True if the position is within the bounds of what can be represented by the chunk grid; otherwise, returns false. </returns>
        public static bool PositionIsInBounds(Vector3I position)
        {
            if (position.Y < 0 || position.Y >= 96)
                return false;
            if (position.Z < 0 || position.Z >= 2048 || position.X < 0 || position.X >= 2048)
                return false;

            return true;
        }
        /// <param name="position"> The position in 3D space to query. </param>
        /// <returns> The Chunk which contains the block at the specified position. </returns>
        public Chunk GetChunkAtPosition(Vector3I position)
        {
            int x = position.X / 32;
            int z = position.Z / 32;
            return GetChunkAtIndex(x + (z * 64));
        }

        /// <summary> TODO </summary>
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

        /// <summary> Class representing the data of a chunk within the chunk grid and its respective block data. </summary>
        /// <param name="saveData"> The StageData instance to which this Chunk belongs. </param>
        /// <param name="index"> The index of this Chunk within the chunk grid. </param>
        public class Chunk(StageData saveData, int index)
        {
            /// <summary> The StageData instance to which this Chunk belongs. </summary>
            public StageData SaveData { get; set; } = saveData;
            /// <summary> The index of this Chunk within the chunk grid. </summary>
            public int Index { get; set; } = index;
            /// <summary> The ID of this chunk: The 16-bit value stored at the Index within the chunk grid. </summary>
            public ushort ID { get => SaveData.GetUInt16(ChunkGridAddress + Index * 2); set => SaveData.SetUInt16(ChunkGridAddress + Index * 2, value); }// => SaveData.GetChunkIdByIndex(Index);

            /// <returns> The origin of the chunk's position in 3D space as a Vector3I. </returns>
            public Vector3I GetOrigin() => new((Index % 64 * 32) - 1024, 0, (Index / 64 * 32) - 1024);

            /// <summary> Whether or not this Chunk is used, within the chunk grid. </summary>
            /// <returns> False if the Chunk's ID is 0xFFFF; otherwise, true. </returns>
            public bool IsUsed() => ID != ushort.MaxValue;

            /// <returns> The byte address at which this Chunk's block data begins. </returns>
            public int GetBlockAddress() => BlockAddress + ChunkSize * ID;

            /// <returns> A span of bytes containing the entirety of this Chunk's block data. </returns>
            public Span<byte> GetData() => SaveData.GetBytes(GetBlockAddress(), ChunkSize);

            /// <param name="layer"> The layer, 0 to 95, of the requested block. </param>
            /// <param name="tile"> The index within the specified layer of the requested block. 0 through 1023. </param>
            /// <returns> A BlockInstance pointing to the block at the specified tile index of the specified layer of this Chunk. </returns>
            public BlockInstance GetBlock(int layer, int tile)
            {
                return IsUsed() ? new BlockInstance(SaveData, GetBlockAddress() + layer * LayerSize + tile * 2) : null;
            }
            /// <summary> Sets the block at the specified layer and tile index of this Chunk. </summary>
            /// <param name="layer"> The layer, 0 to 95, at which to set the block. </param>
            /// <param name="tile"> The index within the specified layer of the block to set. 0 through 1023. </param>
            /// <param name="blockId"> The ID of the block to set at the specified position. </param>
            /// <param name="playerPlaced"> Whether or not the set block should bear the "PlayerPlaced" flag. If not set, the flag of the block at the position is not changed. </param>
            /// <param name="chisel"> The ChiselType of the block to set at the specified position. If not set, the chisel type of the block at the position is not changed. </param>
            public void SetBlock(int layer, int tile, ushort blockId, bool? playerPlaced = null, BlockInstance.ChiselType? chisel = null)
            {
                if (!IsUsed())
                    return;

                BlockInstance block = GetBlock(layer, tile);
                block.BlockID = blockId;
                if (playerPlaced is not null)
                    block.PlayerPlaced = (bool)playerPlaced;
                if (chisel is not null)
                    block.Chisel = (BlockInstance.ChiselType)chisel;
            }
            /// <summary> Sets a full layer of blocks within this chunk to the specified block ID. </summary>
            /// <param name="layer"> The layer index of blocks to replace. </param>
            /// <param name="block"> The ID of the block with which to fill the layer. </param>
            public void SetLayer(int layer, ushort block)
            {
                for (int i = 0; i < LayerSize / 2; i++)
                {
                    SetBlock(layer, i, block);
                }
            }

            /// <summary> Zeroes out all of this Chunk's block data. </summary>
            public void Clear()
            {
                for (int i = GetBlockAddress(); i < GetBlockAddress() + ChunkSize; i++)
                {
                    SaveData.SetByte(i, 0); // TODO replace with fill
                }
            }
        }
        /// <summary> A class representing a single block of data at the designated address. </summary>
        /// <param name="saveData"> The StageData instance to which this BlockInstance belongs. </param>
        /// <param name="address"> The address, in bytes, of the associated data. </param>
        public class BlockInstance(StageData saveData, int address)
        {
            /// <summary> The StageData instance to which this BlockInstance belongs. </summary>
            public StageData SaveData { get; set; } = saveData;
            /// <summary> The address, in bytes, of the associated data. </summary>
            public int Address { get; set; } = address;

            /// <summary> The ID of the block type at this position. An 11-bit unsigned integer. </summary>
            public ushort BlockID { get => (ushort)SaveData.GetNumberBitwise(Address, 0, 11); set => SaveData.SetNumberBitwise(Address, 0, 11, value); }
            /// <summary> A flag which gets set if this block was placed by the player. (Note: Does not get set for Prop blocks or for blocks placed using the Transform-O-Trowel.) </summary>
            public bool PlayerPlaced { get => SaveData.GetBit(Address + 1, 3); set => SaveData.SetBit(Address + 1, 3, value); }
            /// <summary> The chisel shape of this block. </summary>
            public ChiselType Chisel { get => (ChiselType)SaveData.GetNumberBitwise(Address + 1, 4, 4); set => SaveData.SetNumberBitwise(Address + 1, 4, 4, (byte)value); }

            /// <summary> a 4-bit value which refers to the shape of the block, carved using the chisel. </summary>
            public enum ChiselType : byte
            {
                /// <summary> The default value. A full block, uncarved by the chisel. </summary>
                FullBlock = 0,
                /// <summary> TODO </summary>
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
                BottomHalf = 14,
                UNDEFINED = 15
            }
        }

        /// <summary> A class representing a Prop instance and its associated data within a StageData instance. </summary>
        /// <param name="saveData"> The StageData instance to which this Prop belongs. </param>
        /// <param name="listIndex"> The position of this Prop within the Prop List. </param>
        public class Prop(StageData saveData, int listIndex)
        {
            /// <summary> The StageData instance to which this Prop belongs. </summary>
            public StageData SaveData { get; set; } = saveData;
            /// <summary> The position of this Prop within the Prop List. </summary>
            public int ListIndex { get; set; } = listIndex;

            /// <returns> The address in the StageData's buffer of this Prop's location in the Prop list. </returns>
            public int GetListAddress() => PropListAddress + ListIndex * 4;

            /// <summary> The chunk index (in the chunk grid) of the chunk containing this Prop. </summary>
            public ushort ChunkIndex { get { return (ushort)SaveData.GetNumberBitwise(GetListAddress(), 0, 12); } set { SaveData.SetNumberBitwise(GetListAddress(), 0, 12, value); } }
            /// <summary> The Index of this Prop within the prop data. </summary>
            public uint Index { get { return SaveData.GetNumberBitwise(GetListAddress(), 12, 20); } set { SaveData.SetNumberBitwise(GetListAddress(), 12, 20, value); } }

            /// <returns> The address in the StageData's Buffer wherein this Prop's data is stored. </returns>
            public int GetDataAddress() => PropAddress + (int)Index * PropSize;

            /// <summary> The ID of the type of prop placed in this slot. </summary>
            public ushort PropID { get { return (ushort)SaveData.GetNumberBitwise(GetDataAddress() + 8, 0, 13); } set { SaveData.SetNumberBitwise(GetDataAddress() + 8, 0, 13, value); } }

            /// <summary> The X position of the Prop within its chunk, 0 through 31. </summary>
            public byte X { get { return (byte)SaveData.GetNumberBitwise(GetDataAddress() + 9, 5, 5); } set { SaveData.SetNumberBitwise(GetDataAddress() + 9, 5, 5, value); } }
            /// <summary> The Y position of this Prop, 0 to 95. </summary>
            public byte Y { get { return (byte)SaveData.GetNumberBitwise(GetDataAddress() + 10, 2, 7); } set { SaveData.SetNumberBitwise(GetDataAddress() + 10, 2, 7, value); } }
            /// <summary> The Z position of the Prop within its chunk, 0 through 31. </summary>
            public byte Z { get { return (byte)SaveData.GetNumberBitwise(GetDataAddress() + 11, 1, 5); } set { SaveData.SetNumberBitwise(GetDataAddress() + 11, 1, 5, value); } }
            /// <summary> The rotation of this Prop, where 0 is TODO, 1 is TODO, 2 is TODO, and 3 is TODO. </summary>
            public byte Rotation { get { return (byte)SaveData.GetNumberBitwise(GetDataAddress() + 11, 6, 2); } set { SaveData.SetNumberBitwise(GetDataAddress() + 11, 6, 2, value); } }
        }
    }
}