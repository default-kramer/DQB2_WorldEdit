using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace DQBEdit.Nodes
{
	public partial class ItemSelectPanel : PanelContainer
	{
		public class PanelItem
		{
			public int ID { get; set; }
			public string Name { get; set; } = "";
			public int ImageID { get; set; } = -1;
			public string[] Tags { get; set; } = Array.Empty<string>();
			public float SortIndex { get; set; } = float.MaxValue;
			public int GridMapTile { get; set; } = -1;
	
			public Dictionary<string, int> Variants { get; set; }
			public int BaseVariant { get; set; } = -1;
	
			public Button LinkedButton { get; set; } = null;
		}
	
		[Export] Texture2D ItemsTexture;
		private ButtonGroup _ButtonGroup = new();
		private List<PanelItem> Items { get; set; } = new List<PanelItem>();
	
		private GridContainer ButtonsContainer;
	
		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{
			_OnReadyVariables();
			//TestAddButtons();
		}
		private void _OnReadyVariables()
		{
			ButtonsContainer = GetNode<GridContainer>("ScrollContainer/MarginContainer/GridContainer");
		}
	
		// Called every frame. 'delta' is the elapsed time since the previous frame.
		public override void _Process(double delta)
		{
		}
	
		public void TestAddButtons()
		{
			var items = JsonSerializer.Deserialize<PanelItem[]>(Godot.FileAccess.GetFileAsString("res://Tiles.json"));
			foreach (var item in items)
			{
				AddButton(item);
			}
			SortAndFilter();
		}
		public void AddButton(PanelItem item)
		{
			int iconX = item.ImageID % (int)Math.Floor((double)(ItemsTexture.GetWidth() / 112));
			int iconY = item.ImageID / (int)Math.Floor((double)(ItemsTexture.GetWidth() / 112));
	
			item.LinkedButton = new Button
			{
				ToggleMode = true,
				ButtonGroup = _ButtonGroup,
				TooltipText = item.Name,
				Icon = new AtlasTexture
				{
					Atlas = ItemsTexture,
					Region = new Rect2(112 * iconX, 112 * iconY, 112, 112)
				}
			};
			Items.Add(item);
	
			ButtonsContainer.AddChild(item.LinkedButton);
		}
		public void SortAndFilter()
		{
			foreach (Node child in ButtonsContainer.GetChildren())
			{
				ButtonsContainer.RemoveChild(child);
			}
	
			foreach (PanelItem item in Items.OrderBy(item => item.SortIndex))
			{
				ButtonsContainer.AddChild(item.LinkedButton);
				if (item.Tags.Contains("noeditor"))
					item.LinkedButton.Hide();
				if (item.Tags.Contains("liquid"))
					item.LinkedButton.Hide();
				if (item.BaseVariant >= 0)
					item.LinkedButton.Hide();
			}
		}
	}
}
