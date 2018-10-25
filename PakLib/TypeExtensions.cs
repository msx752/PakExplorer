using System;
using System.IO;
using System.Text;

namespace PakLib
{
    public static class BinaryReaderExtensions
    {
        public static string ReadFString(this BinaryReader reader)
        {
            int length = reader.ReadInt32();
            string value = Encoding.UTF8.GetString(reader.ReadBytes(length - 1));
            reader.ReadByte(); // delimiter
            return value;
        }
    }

    public static class StringExtensions
    {
        public static byte[] ConvertHexStringToBytes(this string hexString)
        {
            if (hexString.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase))
                hexString = hexString.Substring(2);

            if (hexString.Length % 2 == 1)
                throw new ArgumentException("All characters must be in pairs", nameof(hexString));

            byte[] data = new byte[hexString.Length / 2];
            for (int i = 0; i < hexString.Length; i += 2)
                data[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);

            return data;
        }
    }
}
