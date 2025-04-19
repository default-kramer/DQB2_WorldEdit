using Godot;
using System;

namespace DQBEdit
{
    public partial class VoxelTerrainEditor : VoxelTerrain
    {
        [Export] public Node3D Camera;

        private VoxelTool _VoxelTool;

        public override void _Ready()
        {
            OnReadyVariables();
        }
        private void OnReadyVariables()
        {
            _VoxelTool = GetVoxelTool();
        }

        public override void _Input(InputEvent @event)
        {
            
        }
    }
}
