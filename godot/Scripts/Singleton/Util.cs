using System;
using System.IO;
using System.Runtime.CompilerServices;
using Godot;

namespace DQBEdit
{
	public static class Util
	{
	    public static AtlasTexture GetItemIcon(int id)
	    {
	        Texture2D atlas = ResourceLoader.Load<Texture2D>("res://Graphics/Items.png");
	
	        if (id < 0)
	        {
	            return new AtlasTexture
			    {
			    	Atlas = atlas,
			    	Region = new Rect2(0,0,0,0)
			    };
	        }
	
			int iconX = id % (int)Math.Floor((double)(atlas.GetWidth() / 112));
			int iconY = id / (int)Math.Floor((double)(atlas.GetWidth() / 112));
	
	        return new AtlasTexture
			{
				Atlas = atlas,
				Region = new Rect2(112 * iconX, 112 * iconY, 112, 112)
			};
	    }
	}
}