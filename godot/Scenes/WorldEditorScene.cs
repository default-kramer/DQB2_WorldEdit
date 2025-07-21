using DQBEdit.Info;
using Godot;
using System;

// TODO delete this
namespace DQBEdit.Scenes
{
	public partial class WorldEditorScene : Node3D
	{
		private VoxelTerrain _VoxelTerrain;

		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{
			_OnReadyVariables();
		}
		private void _OnReadyVariables()
		{
			_VoxelTerrain = GetNode<VoxelTerrain>("VoxelTerrain");
		}

		public void LoadWorld(StageData stageData)
		{
            _VoxelTerrain.Stream = new VoxelStreamDQB2()
            {
                DQB2StageData = stageData
            };
		}
		public void UnloadWorld()
		{
			_VoxelTerrain.Stream = null;
		}

		/*public void _on_button_pressed()
		{
			GetViewport().GuiReleaseFocus();

			if (!lol)
			{
				GD.Print("Loading stage data...");

				StageData.TryLoadAndSet("C:/Users/walke/Documents/My Games/DRAGON QUEST BUILDERS II/Steam/76561198437040801/SD/B00/STGDAT01.BIN");

                //TestVoxelStreamScript script = ResourceLoader.Load<TestVoxelStreamScript>("res://Resources/new_voxel_stream_script.tres");
                TestVoxelStreamScript script = new()
                {
                    DQB2StageData = StageData.Instance
                };
                GetNode<VoxelTerrain>("VoxelTerrain").Stream = script;

				GD.Print("Stage data loaded.");
			}
			else
			{
				//GetNode<VoxelTerrain>("VoxelTerrain").Stream = null;
				((TestVoxelStreamScript)GetNode<VoxelTerrain>("VoxelTerrain").Stream).DQB2StageData = null;
				GD.Print("Unset voxel terrain stream.");
			}

			lol = !lol;

			//VoxelTerrain voxelTerrain = GetNode<VoxelTerrain>("VoxelTerrain");
			//voxelTerrain.AutomaticLoadingEnabled = false;
			
			//VoxelTool voxelTool = voxelTerrain.GetVoxelTool();
			//voxelTool.SetVoxel(Vector3I.Zero, 3);
			//voxelTool.Channel = VoxelBuffer.ChannelId.ChannelType;

			/*foreach (StageData.Chunk chunk in StageData.Instance.Chunks)
			{
				foreach ((Vector3I vec, ushort block) in chunk.GetBlocksAndEuclidPos())
				{
					BlockInfo blockInfo = BlockInfo.Get(block);
					voxelTool.Value = blockInfo.VoxelID;
					voxelTool.DoPoint(vec);
				}
			}

			return;
		}*/
	}
}
