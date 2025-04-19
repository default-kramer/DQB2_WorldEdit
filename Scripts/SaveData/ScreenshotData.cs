using System;
using System.Linq;
using System.IO.Enumeration;
using System.Collections.Generic;
using System.Text;
using Godot;
using System.Runtime.CompilerServices;

namespace DQBEdit
{
    public class ScreenshotData : SaveData
    {
        private const string ExpectedFileHeader = "aerC";
    
        private const int HeaderLength = 0x40;
    
        private const int ImageAddress = 0x69E90;
        private const int ImageSize = 0x64000;
    
        public static ScreenshotData Instance { get; private set; }
        public static bool HasInstance => Instance is not null && Instance.IsLoaded;
    
        public static ScreenshotData TryLoadAndSet(string path)
        {
            if (TryLoad(path) is ScreenshotData screenshotData)
            {
                return Instance = screenshotData;
            }
            else return null;
        }
        public static ScreenshotData TryLoad(string path)
        {
            ScreenshotData screenshotData = new();
            if (screenshotData._TryLoad(path, HeaderLength))
                return screenshotData;
            else return null;
        }
    
        public static void Close()
        {
            Instance = null;
        }
    
        public Image GetImage(int index)
        {
            Image image = new();
            image.LoadJpgFromBuffer(GetBytes(ImageAddress + ImageSize * index, ImageSize).ToArray());
            return image;
        }
        public void SetImage(int index, string filename)
        {
            Image image = new();
            image.Load(filename);
    
            byte[] imageData = image.GetData();
    
            if (!(imageData[0] == 0xFF && imageData[1] == 0xD8))
                return;
            if (imageData.Length > ImageSize)
                return;
            
            Fill(0, ImageAddress + ImageSize * index, ImageSize);
    
            SetBytes(ImageAddress + ImageSize * index, imageData);
        }
    }
}