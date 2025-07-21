using Godot;
using System;

namespace DQBEdit.Nodes
{
	public partial class AutoSubmenu : PopupMenu
	{
		[Export] public int SubmenuIndex { get; set; } = -1;
	
		public override void _Ready()
		{
			if (SubmenuIndex < 0)
				return;
			
			if (GetParent() is PopupMenu parentPopupMenu)
			{
				parentPopupMenu.SetItemSubmenuNode(SubmenuIndex, this);
			}
			if (GetParent() is MenuButton parentMenuButton)
			{
				parentMenuButton.GetPopup().SetItemSubmenuNode(SubmenuIndex, this);
			}
		}
	}
}
