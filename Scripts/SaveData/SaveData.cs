using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq.Expressions;
using System.Net.Http;
using System.IO;
using System.Linq;
using Godot;
using System.Threading;
using System.Text;
using System.Runtime.CompilerServices;

namespace EyeOfRubiss
{
    /// <summary> Base class for loading DQB2 .BIN files. </summary>
    public class SaveData
    {
        /// <summary> The expected values for the start of a DQB2 .BIN file. Used for checking file validity. </summary>
        private const string ExpectedFileHeader = "aerC";

        /// <summary> The path to the file from which this SaveData instance is loaded. </summary>
        public string Filename { get; set; }

        /// <summary> The bytes of the file's header section. </summary>
        protected byte[] _Header { get; set; }
        /// <summary> The decompressed bytes of the file after the header section. </summary>
        protected byte[] _Buffer { get; set; }

        /// <summary> A boolean value defining whether or not the SaveData has been loaded from a file. </summary>
        public bool IsLoaded { get; set; } = false;

        /// <summary> A boolean value which gets set to true whenever any edits are made to the SaveData's Header or Buffer, and which gets set to false when the file is saved. </summary>
        public bool UnsavedChanges { get; set; } = false;

        /// <summary>
        /// Try to load a SaveData instance from the specified path.
        /// </summary>
        /// <param name="path">The path of the file from which to load.</param>
        /// <param name="headerLength"> Length of the file's header. Used when separating Header and Buffer. </param>
        /// <returns>The newly created SaveData instance; otherwise, null.</returns>
        protected bool _TryLoad(string path, int headerLength)
        {
            Filename = path;

            try
            {
                byte[] fileBytes = Godot.FileAccess.GetFileAsBytes(path);

                if (ExpectedFileHeader != Encoding.UTF8.GetString(fileBytes[..4]))
                    return false;

                _Header = fileBytes[..headerLength];
                _Buffer = Decompress(fileBytes[headerLength..]);
            }
            catch
            {
                return false;
            }

            UnsavedChanges = false;
            return IsLoaded = true;
        }
        
        /// <summary>
        /// Try to load a SaveData instance from the specified path, without decompressing the buffer.
        /// </summary>
        /// <param name="path">The path of the file from which to load.</param>
        /// <param name="headerLength"> Length of the file's header. Used when separating Header and Buffer. </param>
        /// <returns>The newly created CommonData instance; otherwise, null.</returns>
        protected bool _QuickLoad(string path, int headerLength)
        {
            Filename = path;

            try
            {
                byte[] fileBytes = Godot.FileAccess.GetFileAsBytes(path);

                if (ExpectedFileHeader != Encoding.UTF8.GetString(fileBytes[..4]))
                    return false;

                _Header = fileBytes[..headerLength];
                _Buffer = fileBytes[headerLength..];
            }
            catch
            {
                return false;
            }

            UnsavedChanges = false;
            return IsLoaded = true;
        }

        /// <summary> Concatenates the Header and the compressed Buffer, then saves it to a file at the specified path. </summary>
        /// <param name="path"> Path for the file destination. If null, uses the SaveData's Filename value. </param>
        public virtual void Save(string path = null)
        {
            path ??= Filename;

            byte[] data = [.. _Header, .. Compress(_Buffer, System.IO.Compression.CompressionLevel.Fastest)];
            byte[] size = BitConverter.GetBytes(data.Length);
            Array.Copy(size, 0, data, 0x10, size.Length);

            using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Write);
            file.StoreBuffer(data);

            UnsavedChanges = false;
        }
        /// <summary> Exports the uncompressed Buffer to the specified file path. </summary>
        /// <param name="path"> Path of the destination file to which to export. </param>
        public void Export(string path)
        {
            using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Write);
            file.StoreBuffer(_Buffer);
        }
        /// <summary> Imports uncompressed binary data from a file and sets the SaveData instance's Buffer to the loaded data. </summary>
        /// <param name="path"> Path to the file from which to load. </param>
        public void Import(string path)
        {
            // TODO: Error handling maybe
            byte[] fileBytes = Godot.FileAccess.GetFileAsBytes(path);
            _Buffer = fileBytes;
        }

        /// <summary> Decompresses the input binary data using ZLib compression. </summary>
        /// <param name="data"> An array of bytes, the data to be decompressed. </param>
        /// <returns> A byte array containing the decompressed data. </returns>
        public static byte[] Decompress(byte[] data)
        {
            using var input = new MemoryStream(data);
            using var zlib = new System.IO.Compression.ZLibStream(input, System.IO.Compression.CompressionMode.Decompress);
            using var output = new MemoryStream();
            zlib.CopyTo(output);

            output.Flush();
            return output.ToArray();
        }
        /// <summary> Compresses the input binary data using ZLib compression. </summary>
        /// <param name="data"> An array of bytes, the data to be compressed. </param>
        /// <param name="compressionLevel"> The desired compression level. </param>
        /// <returns> An array of bytes containing the compressed data. </returns>
        public static byte[] Compress(byte[] data, System.IO.Compression.CompressionLevel compressionLevel)
        {
            using var input = new MemoryStream(data);
            using var output = new MemoryStream();
            using (var zlib = new System.IO.Compression.ZLibStream(output, compressionLevel))
            {
                input.CopyTo(zlib);
                zlib.Flush();
            }
            return output.ToArray();
        }

        /// <summary> Sets the Buffer to a new value. </summary>
        /// <param name="newBuffer">The new value for the Buffer.</param>
        public void SetBuffer(byte[] newBuffer) => _Buffer = newBuffer;

        /// <returns>The length of the Buffer.</returns>
        public int GetBufferSize() => _Buffer.Length;

        /// <summary> Gets a byte value from either the Header or Buffer at the specified address. </summary>
        /// <param name="address"> Address of the desired value. </param>
        /// <param name="header"> If true, returns a value from the Header; otherwise, returns the value from the Buffer. False by default. </param>
        /// <returns> The byte value at the specified address. </returns>
        public byte GetByte(int address, bool header = false) => (header ? _Header : _Buffer)[address];
        /// <summary> Sets the byte from either the Header or Buffer at the specified address to the specified value. </summary>
        /// <param name="address"> Address of the byte to set. </param>
        /// <param name="value"> The value to which to set the specified byte. </param>
        /// <param name="header"> If true, sets a byte in the Header; otherwise, sets a byte in the Buffer. False by default. </param>
        public void SetByte(int address, byte value, bool header = false)
        {
            (header ? _Header : _Buffer)[address] = value;
            UnsavedChanges = true;
        }
        /// <summary> Gets a short value from either the Header or Buffer at the specified address. </summary>
        /// <param name="address"> Address of the desired value. </param>
        /// <param name="header"> If true, returns a value from the Header; otherwise, returns the value from the Buffer. False by default. </param>
        /// <returns> The short value starting at the specified address. </returns>
        public short GetInt16(int address, bool header = false) => BitConverter.ToInt16(header ? _Header : _Buffer, address);
        /// <summary> Sets two bytes from either the Header or Buffer at the specified address to the specified short value. </summary>
        /// <param name="address"> Starting address of the bytes to set. </param>
        /// <param name="value"> The short value to which to set the specified bytes. </param>
        /// <param name="header"> If true, sets bytes in the Header; otherwise, sets bytes in the Buffer. False by default. </param>
        public void SetInt16(int address, short value, bool header = false)
        {
            Array.Copy(BitConverter.GetBytes(value), 0, header ? _Header : _Buffer, address, 2);
            UnsavedChanges = true;
        }
        /// <summary> Gets a ushort value from either the Header or Buffer starting at the specified address. </summary>
        /// <param name="address"> Address of the desired value. </param>
        /// <param name="header"> If true, returns a value from the Header; otherwise, returns the value from the Buffer. False by default. </param>
        /// <returns> The ushort value starting at the specified address. </returns>
        public ushort GetUInt16(int address, bool header = false) => BitConverter.ToUInt16(header ? _Header : _Buffer, address);
        /// <summary> Sets two bytes from either the Header or Buffer at the specified address to the specified ushort value. </summary>
        /// <param name="address"> Starting address of the bytes to set. </param>
        /// <param name="value"> The ushort value to which to set the specified bytes. </param>
        /// <param name="header"> If true, sets bytes in the Header; otherwise, sets bytes in the Buffer. False by default. </param>
        public void SetUInt16(int address, ushort value, bool header = false)
        {
            Array.Copy(BitConverter.GetBytes(value), 0, header ? _Header : _Buffer, address, 2);
            UnsavedChanges = true;
        }
        /// <summary> Gets an int value from either the Header or Buffer starting at the specified address. </summary>
        /// <param name="address"> Address of the desired value. </param>
        /// <param name="header"> If true, returns a value from the Header; otherwise, returns the value from the Buffer. False by default. </param>
        /// <returns> The int value starting at the specified address. </returns>
        public int GetInt32(int address, bool header = false) => BitConverter.ToInt32(header ? _Header : _Buffer, address);
        /// <summary> Sets four bytes from either the Header or Buffer at the specified address to the specified int value. </summary>
        /// <param name="address"> Starting address of the bytes to set. </param>
        /// <param name="value"> The int value to which to set the specified bytes. </param>
        /// <param name="header"> If true, sets bytes in the Header; otherwise, sets bytes in the Buffer. False by default. </param>
        public void SetInt32(int address, int value, bool header = false)
        {
            Array.Copy(BitConverter.GetBytes(value), 0, header ? _Header : _Buffer, address, 4);
            UnsavedChanges = true;
        }
        /// <summary> Gets a uint value from either the Header or Buffer starting at the specified address. </summary>
        /// <param name="address"> Address of the desired value. </param>
        /// <param name="header"> If true, returns a value from the Header; otherwise, returns the value from the Buffer. False by default. </param>
        /// <returns> The uint value starting at the specified address. </returns>
        public uint GetUInt32(int address, bool header = false) => BitConverter.ToUInt32(header ? _Header : _Buffer, address);
        /// <summary> Sets four bytes from either the Header or Buffer at the specified address to the specified uint value. </summary>
        /// <param name="address"> Starting address of the bytes to set. </param>
        /// <param name="value"> The uint value to which to set the specified bytes. </param>
        /// <param name="header"> If true, sets bytes in the Header; otherwise, sets bytes in the Buffer. False by default. </param>
        public void SetUInt32(int address, uint value, bool header = false)
        {
            Array.Copy(BitConverter.GetBytes(value), 0, header ? _Header : _Buffer, address, 4);
            UnsavedChanges = true;
        }
        /// <summary> Gets a long value from either the Header or Buffer starting at the specified address. </summary>
        /// <param name="address"> Address of the desired value. </param>
        /// <param name="header"> If true, returns a value from the Header; otherwise, returns the value from the Buffer. False by default. </param>
        /// <returns> The long value starting at the specified address. </returns>
        public long GetInt64(int address, bool header = false) => BitConverter.ToInt64(header ? _Header : _Buffer, address);
        /// <summary> Sets eight bytes from either the Header or Buffer at the specified address to the specified long value. </summary>
        /// <param name="address"> Starting address of the bytes to set. </param>
        /// <param name="value"> The long value to which to set the specified bytes. </param>
        /// <param name="header"> If true, sets bytes in the Header; otherwise, sets bytes in the Buffer. False by default. </param>
        public void SetInt64(int address, long value, bool header = false)
        {
            Array.Copy(BitConverter.GetBytes(value), 0, header ? _Header : _Buffer, address, 8);
            UnsavedChanges = true;
        }
        /// <summary> Gets a ulong value from either the Header or Buffer starting at the specified address. </summary>
        /// <param name="address"> Address of the desired value. </param>
        /// <param name="header"> If true, returns a value from the Header; otherwise, returns the value from the Buffer. False by default. </param>
        /// <returns> The ulong value starting at the specified address. </returns>
        public ulong GetUInt64(int address, bool header = false) => BitConverter.ToUInt64(header ? _Header : _Buffer, address);
        /// <summary> Sets eight bytes from either the Header or Buffer at the specified address to the specified ulong value. </summary>
        /// <param name="address"> Starting address of the bytes to set. </param>
        /// <param name="value"> The ulong value to which to set the specified bytes. </param>
        /// <param name="header"> If true, sets bytes in the Header; otherwise, sets bytes in the Buffer. False by default. </param>
        public void SetUInt64(int address, ulong value, bool header = false)
        {
            Array.Copy(BitConverter.GetBytes(value), 0, header ? _Header : _Buffer, address, 8);
            UnsavedChanges = true;
        }
        /// <summary> Gets a float value from either the Header or Buffer starting at the specified address. </summary>
        /// <param name="address"> Address of the desired value. </param>
        /// <param name="header"> If true, returns a value from the Header; otherwise, returns the value from the Buffer. False by default. </param>
        /// <returns> The float value starting at the specified address. </returns>
        public float GetSingle(int address, bool header = false) => BitConverter.ToSingle(header ? _Header : _Buffer, address);
        /// <summary> Sets four bytes from either the Header or Buffer at the specified address to the specified float value. </summary>
        /// <param name="address"> Starting address of the bytes to set. </param>
        /// <param name="value"> The float value to which to set the specified bytes. </param>
        /// <param name="header"> If true, sets bytes in the Header; otherwise, sets bytes in the Buffer. False by default. </param>
        public void SetSingle(int address, float value, bool header = false)
        {
            Array.Copy(BitConverter.GetBytes(value), 0, header ? _Header : _Buffer, address, 4);
            UnsavedChanges = true;
        }
        /// <summary> Gets a double value from either the Header or Buffer starting at the specified address. </summary>
        /// <param name="address"> Address of the desired value. </param>
        /// <param name="header"> If true, returns a value from the Header; otherwise, returns the value from the Buffer. False by default. </param>
        /// <returns> The double value starting at the specified address. </returns>
        public double GetDouble(int address, bool header = false) => BitConverter.ToDouble(header ? _Header : _Buffer, address);
        /// <summary> Sets eight bytes from either the Header or Buffer at the specified address to the specified double value. </summary>
        /// <param name="address"> Starting address of the bytes to set. </param>
        /// <param name="value"> The double value to which to set the specified bytes. </param>
        /// <param name="header"> If true, sets bytes in the Header; otherwise, sets bytes in the Buffer. False by default. </param>
        public void SetDouble(int address, double value, bool header = false)
        {
            Array.Copy(BitConverter.GetBytes(value), 0, header ? _Header : _Buffer, address, 8);
            UnsavedChanges = true;
        }
        /// <summary> Gets a string value from either the Header or Buffer starting at the specified address, with the specified length (in bytes). </summary>
        /// <param name="address"> Starting address of the desired value. </param>
        /// <param name="length"> Length of the data to be converted to string, in bytes. </param>
        /// <param name="header"> If true, returns a value from the Header; otherwise, returns the value from the Buffer. False by default. </param>
        /// <param name="encoding"> The desired encoding to use for the string. Null by default; if null, UTF8 is used. </param>
        /// <returns> The values starting from the address to the length, encoded to string using the specified encoding, or UTF8 by default. </returns>
        public string GetString(int address, int length, bool header = false, Encoding encoding = null)
        {
            encoding ??= Encoding.UTF8;
            return encoding.GetString(header ? _Header : _Buffer, address, length);
        }
        /// <summary> Encodes a string value into an amount of bytes equal to the length equal to the length and copies those values to the specified address of either the Header or Buffer. </summary>
        /// <param name="address"> Starting address of the bytes to set. </param>
        /// <param name="value"> The string value to encode. </param>
        /// <param name="length"> The amount of bytes to set starting at the specified address. String data that exceeds the length will be truncated. If the encoded string is shorter than the specified length, the remaining bytes are set to 0. </param>
        /// <param name="header"> If true, sets bytes in the Header; otherwise, sets bytes in the Buffer. False by default. </param>
        /// <param name="encoding"> The desired encoding to use for the string. Null by default; if null, UTF8 is used. </param>
        public void SetString(int address, string value, int? length = null, bool header = false, Encoding encoding = null)
        {
            encoding ??= Encoding.UTF8;
            byte[] stringBytes = encoding.GetBytes(value);
            length ??= stringBytes.Length;

            Fill(0, address, length, header);

            if (stringBytes.Length < length)
                length = stringBytes.Length;

            Array.Copy(stringBytes, 0, header ? _Header : _Buffer, address, (int)length);
            UnsavedChanges = true;
        }
        /// <summary> Gets a boolean value from either the Header or Buffer from the specified bit of the byte at the specified address. </summary>
        /// <param name="address"> Address of the byte containing the desired value. </param>
        /// <param name="bit"> The desired bit index, starting with 0 as the least significant bit and ending with 7 as the most. </param>
        /// <param name="header"> If true, returns a value from the Header; otherwise, returns the value from the Buffer. False by default. </param>
        /// <returns> The specified bit of the specified byte as a boolean value. </returns>
        public bool GetBit(int address, int bit, bool header = false)
        {
            if (bit > 7)
                throw new ArgumentOutOfRangeException();
            if (bit < 0)
                throw new ArgumentOutOfRangeException();

            return ((header ? _Header : _Buffer)[address] & (1 << bit)) != 0;
        }
        /// <summary> Sets the specified bit of the byte at the specified address of either the Header or the Buffer to the specified boolean value. </summary>
        /// <param name="address"> Address of the byte containing the bit to set. </param>
        /// <param name="bit"> The index of the bit to set. An integer between 0 and 7, where 0 is the least significant bit and 7 is the most. </param>
        /// <param name="value"> The boolean value to set. </param>
        /// <param name="header"> If true, sets a bit in the Header; otherwise, sets a bit in the Buffer. False by default. </param>
        public void SetBit(int address, int bit, bool value, bool header = false)
        {
            if (bit > 7)
                throw new ArgumentOutOfRangeException();
            if (bit < 0)
                throw new ArgumentOutOfRangeException();

            byte[] bytes = header ? _Header : _Buffer;
            int left = (value ? 1 : 0) << bit;
            int right = bytes[address] & ((1 << bit) ^ 0b11111111);
            bytes[address] = (byte)(left | right);
            UnsavedChanges = true;
        }
        /// <summary> Gets an array of bytes within the specified range from either the Header or Buffer. </summary>
        /// <param name="range"> Range of values to select. If null, the entire array is returned. </param>
        /// <param name="header"> If true, returns an array from the Header; otherwise, returns the array from the Buffer. False by default. </param>
        /// <returns> The desired array of bytes in the specified range. </returns>
        public Span<byte> GetBytes(System.Range? range = null, bool header = false)
        {
            range ??= ..;
            return header ? _Header.AsSpan((System.Range)range) : _Buffer.AsSpan((System.Range)range);
        }
        /// <summary> Gets an array of bytes starting at the specified address with the specified length from either the Header or Buffer. </summary>
        /// <param name="address"> The starting address of the bytes to select. </param>
        /// <param name="length"> The length of the desired array. If null, or if longer than the specified array, simply returns the rest of the array starting at the address. </param>
        /// <param name="header"> If true, returns an array from the Header; otherwise, returns the array from the Buffer. False by default. </param>
        /// <returns> The desired array of bytes in the specified range. </returns>
        public Span<byte> GetBytes(int address, int? length = null, bool header = false)
        {
            length ??= (header ? _Header.Length : _Buffer.Length) - address;
            return header ? _Header.AsSpan(address, (int)length) : _Buffer.AsSpan(address, (int)length);
        }
        /// <summary> Sets the bytes of either the Header or Buffer starting at the specified address and ending after the specified length to the values in the input byte array. </summary>
        /// <param name="address"> The starting address at which to set bytes. </param>
        /// <param name="bytes"> An array of bytes to which to set the values. </param>
        /// <param name="length"> The amount of byte values to set. If not specified, uses the entire input array. </param>
        /// <param name="header"> If true, sets bytes in the Header; otherwise, sets bytes in the Buffer. False by default. </param>
        public void SetBytes(int address, byte[] bytes, int? length = null, bool header = false)
        {
            length ??= bytes.Length;
            Array.Copy(bytes, 0, header ? _Header : _Buffer, address, (int)length);
            UnsavedChanges = true;
        }

        /// <summary> Gets a uint value from an arbitrary number of bits starting at a specified address and bit from either the Header or the Buffer. </summary>
        /// <param name="address"> Address of the byte wherein the value starts. </param>
        /// <param name="bit"> The bit at which the value starts. 0 to 7, where 0 is the least significant bit and 7 is the most. </param>
        /// <param name="bitCount"> Length in bits of the value. </param>
        /// <param name="header"> If true, returns a value from the Header; otherwise, returns the value from the Buffer. False by default. </param>
        /// <returns> The desired value as a uint. </returns>
        public uint GetNumberBitwise(int address, int bit, int bitCount, bool header = false)
        {
            if (bit > 31 || bitCount > 31)
                throw new Exception("Integers larger than 32 bits are not supported.");
            if (bit < 0 || bitCount < 0)
                throw new Exception("Negative numbers are not allowed.");

            uint left = BitConverter.ToUInt32(header ? _Header : _Buffer, address) >> bit;
            uint right = (uint)((1 << bitCount) - 1);
            return left & right;
        }
        /// <summary> Sets a value of an arbitrary number of bits starting at a specified address and bit from either the Header or the Buffer to the specified value. </summary>
        /// <param name="address"> Address of the byte wherein the value starts. </param>
        /// <param name="bit"> The bit at which the value starts. 0 to 7, where 0 is the least significant bit and 7 is the most. </param>
        /// <param name="bitCount"> Length in bits of the value. </param>
        /// <param name="value"> The value to set. Bits larger than the specified bit count will be ignored. </param>
        /// <param name="header"> If true, sets a value in the Header; otherwise, sets a value from in Buffer. False by default. </param>
        public void SetNumberBitwise(int address, int bit, int bitCount, uint value, bool header = false)
        {
            if (bit > 31 || bitCount > 31)
                throw new Exception("Integers larger than 32 bits are not supported.");
            if (bit < 0 || bitCount < 0)
                throw new Exception("Negative numbers are not allowed.");

            byte[] bytes = header ? _Header : _Buffer;
            uint bitMask = (uint)((1 << bitCount) - 1);
            uint newValue = (value & bitMask) << bit;
            uint oldValue = BitConverter.ToUInt32(bytes, address) & ((bitMask << bit) ^ 0b_11111111_11111111_11111111_11111111);
            Array.Copy(BitConverter.GetBytes(newValue | oldValue), 0, bytes, address, 4);
            UnsavedChanges = true;
            /*Array.Copy(BitConverter.GetBytes(
                (ushort)(
                    (ushort)(BitConverter.ToUInt16(_Buffer, DataAddress + 9) & 0b1111110000011111) |
                    (ushort)((value & 0b00011111) << 5)
                )
            ), 0, _Buffer, ListAddress, 2);*/
        }

        /// <summary> Extends the length of the Buffer to the specified length. If the specified length is smaller than the length of the array, nothing happens. </summary>
        /// <param name="length"> The desired length of the array. If smaller than the array's current length, nothing happens. </param>
        public void Extend(int length)
        {
            if (length <= GetBufferSize())
            {
                GD.Print("For some reason the length was considered smaller");
                return;
            }

            byte[] extension = new byte[length - GetBufferSize()];
            _Buffer = [.. _Buffer, .. extension];
        }
        /// <summary> Fills all bytes of the Header or Buffer starting at the specified address to the specified length with the specified value. </summary>
        /// <param name="value"> The byte value with which to fill the specified portion of the array. </param>
        /// <param name="address"> The starting address from which to fill. 0 by default. </param>
        /// <param name="length"> The amount of bytes to fill with the specified value. If null, fills the rest of the array starting at the specified address. </param>
        /// <param name="header"> If true, fills the specified portion of the Header; otherwise, fills the specified portion of the Buffer. False by default. </param>
        public void Fill(byte value, int address = 0, int? length = null, bool header = false)
        {
            byte[] bytes = header ? _Header : _Buffer;
            length ??= bytes.Length - address;
            Array.Fill(bytes, value, (int)address, (int)length);
            UnsavedChanges = true;
        }
    }
}