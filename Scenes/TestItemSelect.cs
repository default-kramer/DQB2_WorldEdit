using Godot;
using System;
using EyeOfRubiss.Nodes;
using EyeOfRubiss.Info;

// TODO delete this
namespace EyeOfRubiss.Testing.Scenes
{
	public partial class TestItemSelect : Control
	{
		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{
			ButtonSelectorContainer buttonSelectorContainer = GetNode<ButtonSelectorContainer>("ScrollContainer/GridContainer");
			foreach (ItemInfo item in ItemInfo.GetAll())
			{
				Button button = new Button();
				
				button.Text = item.Name;
				buttonSelectorContainer.AddButton(button, item.ID);
			}
		}
	
		public void _On_ButtonSelectorContainer_ButtonPressed(int id)
		{
			GD.PrintRich(ItemInfo.Get((ushort)id).GetNameRich());
		}
	}
}