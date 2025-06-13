using EyeOfRubiss.Info;
using Godot;
using System;

/// <summary> TODO </summary>
namespace EyeOfRubiss.Nodes
{
    public partial class BlockSelector : Control
    {
        [Signal] delegate void BlockSelectedEventHandler(ushort block);
    
        private ButtonGroup _ButtonGroup;
    
        public override void _Ready()
        {
            _ButtonGroup = new ButtonGroup();
    
            Populate();
        }
    
        public void Populate()
        {
            BlockInfo[] data = BlockInfo.GetAll();
    
            foreach (BlockInfo blockInfo in data)
            {
                Button button = new()
                {
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                    Text = blockInfo.Name,
                    ToggleMode = true,
                    ButtonGroup = _ButtonGroup
                };
                button.Pressed += () => _On_Button_Pressed(blockInfo.ID);
    
                AddChild(button);
            }
        }
    
        public void _On_Button_Pressed(ushort button)
        {
            EmitSignal(SignalName.BlockSelected, button);
        }
    }
}
