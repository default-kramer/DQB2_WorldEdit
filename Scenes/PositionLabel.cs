using Godot;
using System;

namespace DQBEdit.Nodes
{
    public partial class PositionLabel : Label
    {
        [Export] string DisplayText { get; set; } = "Current position: %s";
        [Export] Node Target { get; set; }
        [Export] bool UseGlobalPosition { get; set;} = false;

        public override void _Process(double delta)
        {
            if (!Visible)
                return;

            if (Target is Node2D target2D)
            {
                Text = DisplayText.Replace("%s", UseGlobalPosition ? target2D.GlobalPosition.ToString() : target2D.Position.ToString());
            }
            else if (Target is Node3D target3D)
            {
                Text = DisplayText.Replace("%s", UseGlobalPosition ? target3D.GlobalPosition.ToString() : target3D.Position.ToString());
            }
            else
            {
                Text = "Error: Target object not found.";
            }
        }
    }
}
