using Godot;
using System;
using System.ComponentModel;

namespace EyeOfRubiss.Nodes
{
    public partial class StatusLabel : Label
    {
        private AnimationPlayer _AnimationPlayer;
        const string FADEOUT_ANIMATION = "fadeout";

        public override void _Ready()
        {
            _OnReadyVariables();
            Text = "";
        }
        private void _OnReadyVariables()
        {
            _AnimationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
        }

        public void PrintMessage(string message, bool console = true)
        {
            if (console)
                GD.Print(message);
            Text = message;
            _AnimationPlayer?.Stop();
            _AnimationPlayer?.Play(FADEOUT_ANIMATION);
        }
    }
}
