using Blocksy.Core;
using Blocksy.Core.Generators;
using Blocksy.Core.Generators.BasicHill;
using DQBEdit.Info;
using Godot;
using System;

// TODO delete this
namespace DQBEdit.Scenes
{
	partial class TestGenerator : VoxelGeneratorScript
	{
		private readonly I2DSampler<int> sampler;
		const int Channel = (int)VoxelBuffer.ChannelId.ChannelType;
		const ulong BLOCK_SEAFLOOR = 8;
		private readonly ulong voxelId;

		public TestGenerator()
		{
			var prng = PRNG.Create(new Random());
			//prng.NextDouble();
			//GD.Print("seed is: " + prng.Serialize());
			//var prng = new PRNG(PRNG.State.Deserialize("3648141951-3107574069-4240370761-2437842710-244421380-2819314752"));

			//sampler = BasicHillGenerator.Create(PRNG.Create(new Random()));
			//sampler = BasicHill2.Create(prng, width: 150);

			//var sh = StripedHill.Create(prng, width: 150, new StripedHill.Config());
			//sh.Smooth(2);
			//sampler = sh;

			sampler = Hillish.Create(prng);

			//sampler = new SimpleSlope { Width = 100, Elevation = 50 };
			sampler = sampler.Translate(new XZ(900, 900));

			voxelId = BlockInfo.Get(3).VoxelID; // grassy earth
		}

		public override int _GetUsedChannelsMask()
		{
			return 1 << Channel;
		}

		public override void _GenerateBlock(VoxelBuffer outBuffer, Vector3I originInVoxels, int lod)
		{
			if (originInVoxels.Y < 0)
			{
				outBuffer.Fill(BLOCK_SEAFLOOR);
			}

			Vector3I bufferSize = outBuffer.GetSize();
			var xzOrigin = new XZ(originInVoxels.X, originInVoxels.Z);
			//xzOrigin = xzOrigin.Add(-1024, -1024); // TESTING, seems close to the starting point

			/*
			for (int x = 0; x < Math.Min(bufferSize.X, originInVoxels.X / 32); x++)
			{
				outBuffer.SetVoxel(4, x, 0, 0, Channel);
			}
			for (int z = 0; z < Math.Min(bufferSize.Z, originInVoxels.Z / 32); z++)
			{
				outBuffer.SetVoxel(5, 0, 0, z, Channel);
			}
			*/

			for (int x = 0; x < bufferSize.X; x++)
			{
				for (int z = 0; z < bufferSize.Z; z++)
				{
					var xz = xzOrigin.Add(x, z);
					int height = sampler.Sample(xz) - originInVoxels.Y;
					if (height > 0)
					{
						height = Math.Min(height, bufferSize.Y);
						for (int y = 0; y < height; y++)
						{
							outBuffer.SetVoxel(voxelId, x, y, z, Channel);
						}
					}
				}
			}
		}
	}

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
			//_VoxelTerrain.Stream = new VoxelStreamDQB2() { DQB2StageData = stageData };
			_VoxelTerrain.Generator = new TestGenerator();
			GD.Print("LOADED");
		}
		public void UnloadWorld()
		{
			_VoxelTerrain.Stream = null;
			_VoxelTerrain.Generator = null;
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
