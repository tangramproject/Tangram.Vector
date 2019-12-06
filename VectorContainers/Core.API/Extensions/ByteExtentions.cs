using System.Text;
using Sodium;

namespace Core.API.Extentions
{
    public static class ByteExtentions
    {
        public static byte[] ToBytes<T>(this T arg) => Encoding.UTF8.GetBytes(arg.ToString());

        public static string ToHex(this byte[] data) => Utilities.BinaryToHex(data);

        public static string ToStr(this byte[] data) => Encoding.UTF8.GetString(data);

        public static byte[] HexToBinary<T>(this T hex) => Utilities.HexToBinary(hex.ToString());
    }
}
