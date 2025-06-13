using Godot;
using System;
using System.Threading.Channels;

namespace EyeOfRubiss
{
    public partial class VoxelGeneratorTest : VoxelGeneratorScript
    {
        const int Channel = (int)VoxelBuffer.ChannelId.ChannelType;

        public override int _GetUsedChannelsMask()
        {
            return 1 << Channel;
        }
        public override void _GenerateBlock(VoxelBuffer outBuffer, Vector3I originInVoxels, int lod)
        {
            if (lod != 0)
                return;
            if (originInVoxels.Y < 0)
                outBuffer.Fill(1, Channel);
            if (originInVoxels.X == originInVoxels.Z && originInVoxels.Y < 1)
                outBuffer.Fill(1, Channel);
            
            outBuffer.SetVoxel(2, 0,0,0);
        }
    }
}