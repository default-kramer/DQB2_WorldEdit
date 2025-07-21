using Godot;
using System;
using System.Collections.Generic;

namespace DQBEdit.Nodes
{
	public partial class ButtonSelectorContainer : Container
	{
		[Signal]
		public delegate void ButtonPressedEventHandler(int id);
	
		[Export] public bool ConnectChildrenOnReady { get; set; } = false;
	
		private Dictionary<int, BaseButton> _Buttons { get; set; } = new Dictionary<int, BaseButton>();
	
	    public override void _Ready()
	    {
	        if (ConnectChildrenOnReady)
			{
				foreach (var child in GetChildren())
				{
					if (child is Button button)
						ConnectButton(button);
				}
			}
	    }
	
	    public void AddButton(BaseButton button, int? id = null)
	    {
			int actualId = id ?? _GetFirstUnusedIndex();
	        AddChild(button);
			_Buttons.Add(actualId, button);
			button.Pressed += () => _On_Button_Pressed(actualId);
	    }
		public void ConnectButton(BaseButton button, int? id = null)
		{
			int actualId = id ?? _GetFirstUnusedIndex();
			_Buttons.Add(actualId, button);
			button.Pressed += () => _On_Button_Pressed(actualId);
		}
	
		public BaseButton GetButton(int id)
		{
			return _Buttons[id];
		}
	
		public void _On_Button_Pressed(int id)
		{
			EmitSignal(SignalName.ButtonPressed, id);
		}
	
		private int _GetFirstUnusedIndex()
		{
			for (int i = 0; i < int.MaxValue; i++)
			{
				if (!_Buttons.ContainsKey(i))
					return i;
			}
	
			throw new IndexOutOfRangeException();
		}
	}
}
