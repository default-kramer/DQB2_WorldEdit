using DQBEdit.Info;
using Godot;
using System;
using System.Runtime.ExceptionServices;

namespace DQBEdit
{
    public partial class VoxelStreamDQB2 : VoxelStreamScript
    {
        const ulong BLOCK_SEAFLOOR = 8;

        public StageData DQB2StageData { get; set; }

        const int Channel = (int)VoxelBuffer.ChannelId.ChannelType;

        public override int _GetUsedChannelsMask()
        {
            return 1 << Channel;
        }

        public override int _LoadVoxelBlock(VoxelBuffer outBuffer, Vector3I positionInBlocks, int lod)
        {
            if (DQB2StageData is null || !DQB2StageData.IsLoaded)
                return (int)ResultCode.BlockNotFound;
            
            if (positionInBlocks.Y < 0)
            {
                outBuffer.Fill(BLOCK_SEAFLOOR);
                return (int)ResultCode.BlockFound;
            }
            Vector3I bufferSize = outBuffer.GetSize();

            //Step 1: check if inbounds
            if (!StageData.PositionIsInBounds(positionInBlocks * bufferSize))
                return (int)ResultCode.BlockNotFound;
            
            // Step 2: check if chunk exists
            StageData.Chunk chunk = DQB2StageData.GetChunkAtPosition(positionInBlocks * bufferSize);

            for (int x = 0; x < bufferSize.X; x++)
            {
                for (int y = 0; y < bufferSize.Y; y++)
                {
                    for (int z = 0; z < bufferSize.Z; z++)
                    {
                        Vector3I coords = (positionInBlocks * bufferSize) + new Vector3I(x, y, z);
                        int tile = coords.X % 32 + (coords.Z % 32 * 32);
                        StageData.BlockInstance block = chunk.GetBlock(coords.Y, tile);
                        if (block is not null)
                        {
                            ulong voxelId = BlockInfo.Get(block.BlockID).VoxelID;
                            outBuffer.SetVoxel(voxelId, x, y, z, Channel);
                        }
                    }
                }
            }

            return (int)ResultCode.BlockFound;
        }
        public override void _SaveVoxelBlock(VoxelBuffer buffer, Vector3I positionInBlocks, int lod)
        {
            return;
        }
    }
}