using EyeOfRubiss.Info;
using EyeOfRubiss.Nodes;
using EyeOfRubiss.Resources;
using Godot;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.XPath;

// TODO delete this
namespace EyeOfRubiss.Scenes
{
	public partial class WorldEditorScene : Node3D
	{
		private VoxelTerrain _VoxelTerrain;
		private VoxelTool _VoxelTool;
		private Node3D _ResidentLayer;
		private Node3D _ObjectLayer;

		private CameraController _CameraController;

		private CanvasItem _DebugInfoContainer;
		private FPSLabel _FPSLabel;
		private Label _PointedVoxelLabel;
		private StatusLabel _StatusLabel;

		public bool AutomaticallyGenerateBedrock = true;

		public BrushType BrushPrimary = BrushType.Pencil;
		public BrushType BrushSecondary = BrushType.Erase;
		public BrushType BrushTertiary = BrushType.Eyedropper;
		public ushort BrushBlock = 1;

		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{
			_OnReadyVariables();
		}
		private void _OnReadyVariables()
		{
			_VoxelTerrain = GetNode<VoxelTerrain>("VoxelTerrain");
			_VoxelTool = _VoxelTerrain.GetVoxelTool();
			_VoxelTool.Channel = VoxelBuffer.ChannelId.ChannelType;
			_ResidentLayer = GetNode<Node3D>("ResidentLayer");
			_ObjectLayer = GetNode<Node3D>("ObjectLayer");

			_CameraController = GetNode<CameraController>("Camera3D");

			_DebugInfoContainer = GetNode<CanvasItem>("HUD/DebugInfo");
			_FPSLabel = GetNode<FPSLabel>("HUD/FPSLabel");
			_PointedVoxelLabel = GetNode<Label>("HUD/DebugInfo/PointedVoxelLabel");
			_StatusLabel = GetNode<StatusLabel>("HUD/StatusLabel");
		}

        public override void _Process(double delta)
        {
            
        }
        public override void _PhysicsProcess(double delta)
        {
            UpdatePointedVoxelLabel();
        }
        public override void _Input(InputEvent @event)
        {
			if (@event.IsActionPressed(Constants.Controls.CURSOR_RELEASE))
				ReleaseCursor();

			if (_CameraController.Enabled)
			{
				if (@event.IsPressed() && @event is InputEventMouseButton mouseButtonEvent) // TODO probably change this to action
				{
					if (mouseButtonEvent.ButtonIndex == MouseButton.Left)
						DoBrush(GetPointedVoxel(), BrushPrimary);
					if (mouseButtonEvent.ButtonIndex == MouseButton.Right)
						DoBrush(GetPointedVoxel(), BrushSecondary);
					if (mouseButtonEvent.ButtonIndex == MouseButton.Middle)
						DoBrush(GetPointedVoxel(), BrushTertiary);
				}
			}

			if (@event.IsPressed() && @event is InputEventMouseButton mouseButtonEvent2 && mouseButtonEvent2.ButtonIndex == MouseButton.Left) // TODO probably change this to action
				CaptureCursor();
        }

		public void LoadWorld(StageData stageData)
		{
            _VoxelTerrain.Stream = new VoxelStreamDQB2()
            {
                DQB2StageData = stageData
            };
			
			if (CommonData.Instance is not null && CommonData.Instance.IsLoaded)
				CreateResidents(CommonData.Instance);
		}
		public void UnloadWorld()
		{
			_VoxelTerrain.Stream = null;
			DestroyResidents();
		}

		private void UpdatePointedVoxelLabel()
		{
			if (_PointedVoxelLabel is null)
				return;
			
			VoxelRaycastResult result = GetPointedVoxel();
			if (result is not null)
			{
				Vector3I friendlyPos = result.Position + new Vector3I(1024, 0, 1024);

				//Vector3I indexPos = StageData.Instance.EuclidPosToIndex(result.Position);

				StageData.BlockInstance block = StageData.Instance.GetBlockAtPosition(result.Position);

				_PointedVoxelLabel.Text = 
					$"Targeted block: {(block is not null ? BlockInfo.Get(block.BlockID).Name + $" [{block.BlockID}]" : "UNKNOWN")}\n" +
					$"X: {friendlyPos.X}, Y: {friendlyPos.Y}, Z: {friendlyPos.Z}\n" +
					//$"Chunk: {indexPos.X}, Layer: {indexPos.Y}, Tile: {indexPos.Z}\n" +
					$"Placed by Builder: {block.PlayerPlaced}" +
					$"\nShape: {block.Chisel}";
			}
			else
			{
				_PointedVoxelLabel.Text = "Targeted block: None";
			}
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
			_ResidentLayer.Visible = show;
		}

		public void CreateResident(CommonData.Resident resident)
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
			npcSprite.Rotation = Vector3.Up * resident.Rotation;
			_ResidentLayer.AddChild(npcSprite);
		}
		public void CreateResidents(CommonData commonData)
		{
			if (StageData.Instance is null || !StageData.Instance.IsLoaded)
				return;

			DestroyResidents();
			foreach (CommonData.Resident resident in commonData.Residents)
			{
				if (resident.CurrentIsland == StageData.Instance.IslandID)
				{
					CreateResident(resident);
				}
			}
			foreach (CommonData.Resident resident in commonData.ImportantResidents)
			{
				if (resident.CurrentIsland == StageData.Instance.IslandID)
				{
					CreateResident(resident);
				}
			}
		}
		public void DestroyResidents()
		{
			_ResidentLayer.QueueFreeAllChildren();
		}

		public VoxelRaycastResult GetPointedVoxel()
		{
			Vector3 origin = _CameraController.GlobalTransform.Origin;
			Vector3 forward = -_CameraController.Transform.Basis.Z.Normalized();
			VoxelRaycastResult hit = _VoxelTool.Raycast(origin, forward, 4096);
			return hit;
		}
		
		public void CaptureCursor()
		{
			Input.MouseMode = Input.MouseModeEnum.Captured;
			_CameraController.Enabled = true;
		}
		public void ReleaseCursor()
		{
			Input.MouseMode = Input.MouseModeEnum.Visible;
			_CameraController.Enabled = false;
		}

		public void SetBlock(Vector3I position, ushort blockId, StageData.BlockInstance.ChiselType? chiselType = null, bool? playerPlaced = null)
		{
			if (StageData.Instance is null || !StageData.Instance.IsLoaded)
				return;
			// TODO

			bool success = StageData.Instance.SetBlockAtPosition(position, blockId, chiselType, playerPlaced, true);
			if (success)
			{
				ulong voxelId = BlockInfo.Get(blockId).VoxelID;
				_VoxelTerrain.GetVoxelTool().SetVoxel(position, voxelId);

				if (AutomaticallyGenerateBedrock && blockId != Constants.BLOCK_AIR && position.Y > 0)
				{
					SetBlock(new Vector3I(position.X, 0, position.Z), Constants.BLOCK_BEDROCK, chiselType, playerPlaced);
				}
			}
			else
			{
				_StatusLabel.PrintMessage("Cannot place blocks out of bounds.");
			}
		}
		public void Builderize(Vector3I position)
		{
			if (StageData.Instance is null || !StageData.Instance.IsLoaded)
				return;

			StageData.BlockInstance blockInstance = StageData.Instance.GetBlockAtPosition(position);
			blockInstance.PlayerPlaced = true;
		}
		public void DoEyedropper(Vector3I position)
		{
			if (StageData.Instance.GetBlockAtPosition(position) is StageData.BlockInstance block)
			{
				SetBrushBlock(block.BlockID);
				GD.Print($"Set brush block to {BlockInfo.Get(block.BlockID).Name} ({block.BlockID})");
			}
		}

		public void SetBrushPrimary(BrushType brush)
		{
			BrushPrimary = brush;
		}
		public void SetBrushPrimary(int brush)
		{
			SetBrushPrimary((BrushType) brush);
		}
		public void SetBrushBlock(ushort block)
		{
			BrushBlock = block;
		}

		public void DoBrush(VoxelRaycastResult result, BrushType brush)
		{
			if (result is null)
				return;

			switch (brush)
			{
				case BrushType.Erase:
					SetBlock(result.Position, Constants.BLOCK_AIR);
					break;
				case BrushType.Pencil:
					SetBlock(result.PreviousPosition, BrushBlock);
					break;
				case BrushType.Swap:
					SetBlock(result.Position, BrushBlock);
					break;
				case BrushType.Builderize:
					Builderize(result.Position);
					break;
				case BrushType.Eyedropper:
					DoEyedropper(result.Position);
					break;
			}
		}

		public void Refresh()
		{
			var x = _VoxelTerrain.Stream;
			_VoxelTerrain.Stream = _VoxelTerrain.Stream;
			_VoxelTerrain.Stream = x;
		}

		public enum BrushType : int
		{
			Erase = 0,
			Pencil = 1,
			Fill = 2,
			Swap = 3,
			Builderize = 4,
			Eyedropper = 5
		}
	}
}
