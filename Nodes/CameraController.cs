using DQBEdit.Info;
using Godot;
using System;

namespace DQBEdit.Nodes
{
	public partial class CameraController : Camera3D
	{
		[Export] float Speed = 1;
		[Export] float MaxSpeedMultiplier = 2;
		[Export] float MinSpeedMultiplier = 0.25f;
		[Export] float SpeedMultiplierStep = 0.25f;
		[Export] float MouseSensitivity = 0.4f;
		[Export] float MaxAngle = 90;
		[Export] float MinAngle = -90;

		private float SpeedMultiplier = 1;

		[Export] VoxelTerrain _VoxelTerrain;
		private VoxelTool _VoxelTool;

		[Export] Label PointedVoxelLabel { get; set; }

		private bool _CursorCaptured;

        public override void _Ready()
        {
            _OnReadyVariables();
        }
		private void _OnReadyVariables()
		{
			if (_VoxelTerrain is not null)
				_VoxelTool = _VoxelTerrain.GetVoxelTool();
				_VoxelTool.Channel = VoxelBuffer.ChannelId.ChannelType;
		}

        // Called every frame. 'delta' is the elapsed time since the previous frame.
        public override void _Process(double delta)
		{
			if (!_CursorCaptured)
				return;

			Vector3 positionChangeVector = Vector3.Zero;

			if (Input.IsActionPressed("camera_left"))
				positionChangeVector += Vector3.Left;
			if (Input.IsActionPressed("camera_right"))
				positionChangeVector += Vector3.Right;
			if (Input.IsActionPressed("camera_forward"))
				positionChangeVector += Vector3.Forward;
			if (Input.IsActionPressed("camera_back"))
				positionChangeVector += Vector3.Back;
			if (Input.IsActionPressed("camera_up"))
				positionChangeVector += Vector3.Up;
			if (Input.IsActionPressed("camera_down"))
				positionChangeVector += Vector3.Down;

			positionChangeVector = positionChangeVector.Normalized().Rotated(Vector3.Up, Rotation.Y);
			Position += positionChangeVector * (float)delta * Speed * SpeedMultiplier;

		}
        public override void _PhysicsProcess(double delta)
        {
			if (PointedVoxelLabel is not null)
			{
				VoxelRaycastResult result = GetPointedVoxel();
				if (result is not null)// && result.Position.Y >= 0)
				{
					Vector3I friendlyPos = result.Position + new Vector3I(1024, 0, 1024);
					Vector3I indexPos = StageData.Instance.EuclidPosToIndex(result.Position);
					StageData.BlockInstance block = StageData.Instance.GetBlockAtIndex(indexPos);
					PointedVoxelLabel.Text = 
						$"Targeted block: {(block is not null ? BlockInfo.Get(block.BlockID).Name + $" [{block.BlockID}]" : "UNKNOWN")}\n" +
						$"X: {friendlyPos.X}, Y: {friendlyPos.Y}, Z: {friendlyPos.Z}\n" +
						$"Chunk: {indexPos.X}, Layer: {indexPos.Y}, Tile: {indexPos.Z}\n" +
						$"Placed by Builder: {block.PlayerPlaced}" +
						$"\nShape: {block.Chisel}";
				}
				else
				{
					PointedVoxelLabel.Text = "Targeted block: None";
				}
			}

			return;

			if (Input.IsActionJustPressed("ui_accept"))
			{
				var spaceState = GetWorld3D().DirectSpaceState;
				var cam = this;
				var mousePos = GetViewport().GetMousePosition();

				var origin = cam.ProjectRayOrigin(mousePos);
				var end = origin + cam.ProjectRayNormal(mousePos);
				var query = PhysicsRayQueryParameters3D.Create(origin, end);


				//var hit = _VoxelTool.Raycast(GlobalTransform.Origin, -Transform.Basis.Z.Normalized(), 100);
				//var hit = _VoxelTool.Raycast(origin, -end, 100);
				var hit = _VoxelTool.Raycast(query.From, query.To, 100);
				if (hit is not null)
				{
					GD.Print(hit.Position);
					Vector3I lastPosition = hit.PreviousPosition;
					_VoxelTool.Mode = VoxelTool.ModeEnum.Set;
					_VoxelTool.Value = _VoxelTool.GetVoxel(lastPosition) + 1;
					//_VoxelTool.DoPoint(lastPosition);
				}
				else
					GD.Print("No block in sight");
			}
        }

        public override void _Input(InputEvent @event)
        {
			if (@event is InputEventMouseButton inputEventMouseButton && inputEventMouseButton.IsPressed() && inputEventMouseButton.ButtonMask == MouseButtonMask.Left)
			{
				CaptureCursor();
			}
			if (@event.IsActionPressed("cursor_release"))
			{
				ReleaseCursor();
			}

			if (!_CursorCaptured)
				return;
			
            if (@event is InputEventMouseMotion mouseMotion)
			{
				var motion = mouseMotion.Relative;

				float x = Rotation.Y - Mathf.DegToRad(motion.X) * MouseSensitivity;
				float y = Rotation.X - Mathf.DegToRad(motion.Y) * MouseSensitivity;

				y = (float)Mathf.Clamp(y, Mathf.DegToRad(MinAngle + 0.001), Mathf.DegToRad(MaxAngle - 0.001));

				Rotation = new Vector3(y, x, Rotation.Z);
			}
			
			if (@event.IsActionPressed("camera_speed_up"))
			{
				SpeedMultiplier += SpeedMultiplierStep;
				if (SpeedMultiplier > MaxSpeedMultiplier)
					SpeedMultiplier = MaxSpeedMultiplier;
			}
			if (@event.IsActionPressed("camera_speed_down"))
			{
				SpeedMultiplier -= SpeedMultiplierStep;
				if (SpeedMultiplier < MinSpeedMultiplier)
					SpeedMultiplier = MinSpeedMultiplier;
			}
        }

		public VoxelRaycastResult GetPointedVoxel()
		{
			Vector3 origin = GlobalTransform.Origin;
			Vector3 forward = -Transform.Basis.Z.Normalized();
			VoxelRaycastResult hit = _VoxelTool.Raycast(origin, forward, 4096);
			return hit;
		}

		public void CaptureCursor()
		{
			Input.MouseMode = Input.MouseModeEnum.Captured;
			_CursorCaptured = true;
		}
		public void ReleaseCursor()
		{
			Input.MouseMode = Input.MouseModeEnum.Visible;
			_CursorCaptured = false;
		}
    }
}
