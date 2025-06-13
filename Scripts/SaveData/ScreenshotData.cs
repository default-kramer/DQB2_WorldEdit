using System;
using System.Linq;
using System.IO.Enumeration;
using System.Collections.Generic;
using System.Text;
using Godot;
using System.Runtime.CompilerServices;

namespace EyeOfRubiss
{
    /// <summary> Class used for handling SCSHDAT.BIN files, which hold DQB2 screenshot data. </summary>
    public class ScreenshotData : SaveData
    {
        /// <summary> Length of the file header, in bytes. </summary>
        private const int HeaderLength = 0x40;

        /// <summary> Address at which image data begins. </summary>
        private const int ImageAddress = 0x69E90;
        /// <summary> Size, in bytes, of a single image of data. </summary>
        private const int ImageSize = 0x64000;

        /// <summary> The main active ScreenshotData instance. </summary>
        public static ScreenshotData Instance { get; private set; }
        /// <returns> True if Instance is not null and Instance is loaded. </returns>
        public static bool HasInstance() => Instance is not null && Instance.IsLoaded;

        /// <summary>
        /// Try to load a ScreenshotData instance from the specified path. If successful, sets the current Instance to that new ScreenshotData instance.
        /// </summary>
        /// <param name="path">The path of the file from which to load.</param>
        /// <returns>The newly created ScreenshotData instance; otherwise, null.</returns>
        public static ScreenshotData TryLoadAndSet(string path)
        {
            if (TryLoad(path) is ScreenshotData screenshotData)
            {
                return Instance = screenshotData;
            }
            else return null;
        }
        /// <summary>
        /// Try to load a ScreenshotData instance from the specified path.
        /// </summary>
        /// <param name="path">The path of the file from which to load.</param>
        /// <returns>The newly created ScreenshotData instance; otherwise, null.</returns>
        public static ScreenshotData TryLoad(string path)
        {
            ScreenshotData screenshotData = new();
            if (screenshotData._TryLoad(path, HeaderLength))
                return screenshotData;
            else return null;
        }

        /// <summary> Sets the current active Instance to null. </summary>
        public static void Close()
        {
            Instance = null;
        }

        /// <summary> Loads JPG data from the image index in the buffer. </summary>
        /// <param name="index"> The index of the screenshot to load. </param>
        /// <returns> The screenshot as an Image. </returns>
        public Image GetImage(int index)
        {
            Image image = new();
            image.LoadJpgFromBuffer(GetBytes(ImageAddress + ImageSize * index, ImageSize).ToArray());
            return image;
        }
        /// <summary> Loads the image from the specified filename and stores the data in the Buffer at the specified index. TODO: test. </summary>
        /// <param name="index"> The screenshot index to overwrite. </param>
        /// <param name="filename"> The path to the image file to load. </param>
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