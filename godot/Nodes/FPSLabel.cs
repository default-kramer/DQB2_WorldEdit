using Godot;
using System;

namespace DQBEdit.Nodes
{
    public partial class FPSLabel : Label
    {
        public override void _Ready()
        {
            Text = "FPS: 0";
        }
        public override void _Process(double delta)
        {
            Text = $"FPS: {Engine.GetFramesPerSecond()}";
        }
    }
}
