using Godot;
using System;

namespace DQBEdit.Nodes
{
    public partial class NPCSprite : Node3D
    {
        [Export] public int _Icon;
        [Export] public string _Name;

        private Sprite3D _Sprite;
        private Label3D _Label;

        public override void _Ready()
        {
            _OnReadyVariables();
            UpdateNPC();
        }
        private void _OnReadyVariables()
        {
            _Sprite = GetNode<Sprite3D>("Sprite3D");
            _Label = GetNode<Label3D>("Label3D");
        }

        public void SetNPC(CommonData.Resident resident)
        {
            _Name = resident.GetDisplayName();
            _Icon = resident.Type;
            UpdateNPC();
        }
        private void UpdateNPC()
        {
            if (_Sprite is not null)
            {
                _Sprite.Frame = _Icon - 1;
            }
            if (_Label is not null)
            {
                _Label.Text = _Name;
            }
        }
    }
}
