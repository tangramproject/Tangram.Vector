using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Numerics;
using System.IO.Compression;
using ProtoBuf;
using Core.API.LibSodium;
using Core.API.Model;
using System.Linq;

namespace Core.API.Helper
{
    public static class Util
    {
        public static byte[] GetZeroBytes()
        {
            byte[] bytes = new byte[0];
            if ((bytes[bytes.Length - 1] & 0x80) != 0)
            {
                Array.Resize(ref bytes, bytes.Length + 1);
            }

            return bytes;
        }

        public static string EntryAssemblyPath()
        {
            return Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        }

        public static OSPlatform GetOSPlatform()
        {
            OSPlatform osPlatform = OSPlatform.Create("Other Platform");
            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            osPlatform = isWindows ? OSPlatform.Windows : osPlatform;

            bool isOSX = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            osPlatform = isOSX ? OSPlatform.OSX : osPlatform;

            bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
            osPlatform = isLinux ? OSPlatform.Linux : osPlatform;

            return osPlatform;
        }

        public static string Pop(string value, string delimiter)
        {
            var stack = new Stack<string>(value.Split(new string[] { delimiter }, StringSplitOptions.None));
            return stack.Pop();
        }

        public static byte[] ToArray(this SecureString s)
        {
            if (s == null)
                throw new NullReferenceException();
            if (s.Length == 0)
                return new byte[0];
            var result = new List<byte>();
            IntPtr ptr = SecureStringMarshal.SecureStringToGlobalAllocAnsi(s);
            try
            {
                int i = 0;
                do
                {
                    byte b = Marshal.ReadByte(ptr, i++);
                    if (b == 0)
                        break;
                    result.Add(b);
                } while (true);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocAnsi(ptr);
            }
            return result.ToArray();
        }

        public static T DeserializeJsonFromStream<T>(Stream stream)
        {
            if (stream == null || stream.CanRead == false)
                return default;

            using (var sr = new StreamReader(stream))
            using (var jtr = new JsonTextReader(sr))
            {
                var js = new JsonSerializer();
                var searchResult = js.Deserialize<T>(jtr);
                return searchResult;
            }
        }

        public static async Task<string> StreamToStringAsync(Stream stream)
        {
            string content = null;

            if (stream != null)
                using (var sr = new StreamReader(stream))
                    content = await sr.ReadToEndAsync();

            return content;
        }

        [CLSCompliant(false)]
#pragma warning disable CS3021 // Type or member does not need a CLSCompliant attribute because the assembly does not have a CLSCompliant attribute
        public static InsecureString Insecure(this SecureString secureString) => new InsecureString(secureString);
#pragma warning restore CS3021 // Type or member does not need a CLSCompliant attribute because the assembly does not have a CLSCompliant attribute

        public static BigInteger Mod(BigInteger a, BigInteger n)
        {
            var result = (a % n);
            if ((result < 0 && n > 0) || (result > 0 && n < 0))
            {
                result += n;
            }
            return result;
        }

        public static Org.BouncyCastle.Math.BigInteger SecureRandom(int bytes)
        {
            Org.BouncyCastle.Security.SecureRandom secureRandom = new Org.BouncyCastle.Security.SecureRandom();
            return new Org.BouncyCastle.Math.BigInteger(bytes, secureRandom);
        }

        public static Org.BouncyCastle.Math.BigInteger GeneratePrime(int bytes)
        {
            var c = SecureRandom(bytes);

            for (; ; )
            {
                if (c.IsProbablePrime(1) == true) break;

                c = c.Subtract(new Org.BouncyCastle.Math.BigInteger("1"));
            }

            return c;
        }

        public static BigInteger GetHashNumber(byte[] hash, BigInteger prime, int bytes)
        {
            var intH = new BigInteger(hash);
            var subString = Convert.ToInt32(intH.ToString().Substring(0, bytes));
            var result = Mod(subString, prime);

            return result;
        }

        public static long GetInt64HashCode(byte[] hash)
        {
            long hashCode = 0;
            //32Byte hashText separate
            //hashCodeStart = 0~7  8Byte
            //hashCodeMedium = 8~23  8Byte
            //hashCodeEnd = 24~31  8Byte
            //and Fold
            var hashCodeStart = BitConverter.ToInt64(hash, 0);
            var hashCodeMedium = BitConverter.ToInt64(hash, 8);
            var hashCodeEnd = BitConverter.ToInt64(hash, 24);

            hashCode = hashCodeStart ^ hashCodeMedium ^ hashCodeEnd;

            return hashCode;
        }

        public static byte[] SerializeProto<T>(T data)
        {
            try
            {
                using (var ms = new MemoryStream())
                {
                    Serializer.Serialize(ms, data);
                    return ms.ToArray();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static T DeserializeProto<T>(byte[] data)
        {
            try
            {
                using (var ms = new MemoryStream(data))
                {
                    return Serializer.Deserialize<T>(ms);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static IEnumerable<T> DeserializeListProto<T>(byte[] data) where T : class
        {
            List<T> list = new List<T>();

            try
            {
                using (var ms = new MemoryStream(data))
                {
                    T item;
                    while ((item = Serializer.DeserializeWithLengthPrefix<T>(ms, PrefixStyle.Base128, fieldNumber: 1)) != null)
                    {
                        list.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return list.AsEnumerable();
        }

        public static byte[] Combine(byte[] first, byte[] second)
        {
            byte[] ret = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, ret, 0, first.Length);
            Buffer.BlockCopy(second, 0, ret, first.Length, second.Length);
            return ret;
        }
    }
}
