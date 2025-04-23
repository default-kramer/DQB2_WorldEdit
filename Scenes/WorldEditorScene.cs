using DQBEdit.Info;
using DQBEdit.Nodes;
using Godot;
using System;

// TODO delete this
namespace DQBEdit.Scenes
{
	public partial class WorldEditorScene : Node3D
	{
		private VoxelTerrain _VoxelTerrain;

		private FPSLabel _FPSLabel;
		private CanvasItem _DebugInfoContainer;
		private Node3D _NPCSpriteLayer;

		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{
			_OnReadyVariables();
		}
		private void _OnReadyVariables()
		{
			_VoxelTerrain = GetNode<VoxelTerrain>("VoxelTerrain");
			_FPSLabel = GetNode<FPSLabel>("HUD/FPSLabel");
			_DebugInfoContainer = GetNode<CanvasItem>("HUD/DebugInfo");
			_NPCSpriteLayer = GetNode<Node3D>("NPCSpriteLayer");
		}

		public void LoadWorld(StageData stageData)
		{
            _VoxelTerrain.Stream = new VoxelStreamDQB2()
            {
                DQB2StageData = stageData
            };
			if (CommonData.Instance is not null && CommonData.Instance.IsLoaded)
				CreateNPCSprites(CommonData.Instance);
		}
		public void UnloadWorld()
		{
			_VoxelTerrain.Stream = null;
			DestroyNPCSprites();
		}

		public void ChangeFPSDisplay(bool show)
		{
			_FPSLabel.Visible = show;
		}
		public void ChangeDebugInfoDisplay(bool show)
		{
			_DebugInfoContainer.Visible = show;
		}
		public void ChangeNPCDisplay(bool show)
		{
			_NPCSpriteLayer.Visible = show;
		}

		public void CreateNPCSprite(CommonData.Resident resident)
		{
			/*
			Sprite3D sprite3D = new Sprite3D();
			GetNode("NPCSpriteLayer").AddChild(sprite3D);
			sprite3D.Texture = ResourceLoader.Load<Texture2D>("res://Graphics/Resident/monster_hammerhood.png");
			sprite3D.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
			sprite3D.FixedSize = true;
			sprite3D.PixelSize = 0.001f;
			sprite3D.Position = new Vector3(resident.PositionX, resident.PositionY + 0.5f, resident.PositionZ);

			Label3D label3D = new Label3D();
			label3D.Text = resident.GetDisplayName();
			sprite3D.AddChild(label3D);
			label3D.Position += Vector3.Up;
			label3D.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
			label3D.FixedSize = true;
			label3D.PixelSize = 0.001f;
			*/
			NPCSprite npcSprite = ResourceLoader.Load<PackedScene>("res://Nodes/NPCSprite.tscn").Instantiate<NPCSprite>();
			npcSprite.SetNPC(resident);
			npcSprite.Position = new Vector3(resident.PositionX, resident.PositionY, resident.PositionZ);
			_NPCSpriteLayer.AddChild(npcSprite);
		}
		public void CreateNPCSprites(CommonData commonData)
		{
			if (StageData.Instance is null || !StageData.Instance.IsLoaded)
				return;

			DestroyNPCSprites();
			foreach (CommonData.Resident resident in commonData.Residents)
			{
				if (resident.CurrentIsland == StageData.Instance.IslandID)
				{
					CreateNPCSprite(resident);
				}
			}
			foreach (CommonData.Resident resident in commonData.ImportantResidents)
			{
				if (resident.CurrentIsland == StageData.Instance.IslandID)
				{
					CreateNPCSprite(resident);
				}
			}
		}
		public void DestroyNPCSprites()
		{
			GetNode("NPCSpriteLayer").QueueFreeAllChildren();
		}
	}
}
