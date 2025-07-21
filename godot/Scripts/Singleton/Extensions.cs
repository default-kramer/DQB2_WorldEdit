using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Godot;

namespace DQBEdit
{
    public static class Extensions
    {
        public static void SetCurrentDirRecursive(this FileDialog dialog, string path)
        {
            string[] directories = path.Split(new[]{ Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar });
            while (directories.Length > 0 && !Directory.Exists(Path.Join(directories)))
            {
                directories = directories[..(directories.Length - 1)];
            }
    
            if (directories.Length > 0)
                dialog.CurrentDir = Path.Join(directories);
        }
        public static void SetFilter(this FileDialog dialog, string filter, string description = "")
        {
            dialog.ClearFilters();
            dialog.AddFilter(filter, description);
        }
    
        public static bool ToggleVisible(this CanvasItem node)
        {
            return node.Visible = !node.Visible;
        }
    }
}
