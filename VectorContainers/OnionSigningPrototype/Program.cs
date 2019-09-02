using Newtonsoft.Json;
using SimpleBase;
using System;
using System.IO;
using System.Text;

namespace OnionSigningPrototype
{
    class Program
    {
        static void Main(string[] args)
        {
            var address = "5tzzzhbuyoxmf6zsrjlv5c7lwy5v5c363u7w76bqbczoyuewnb4vrfqd.onion";

            var privateKeyBytes = new byte[] { 0x28, 0x41, 0x0d, 0x32, 0x58 ,0x0b ,0xfb ,0x7d
                                              ,0x3e ,0xb6 ,0x2b ,0x5b ,0xc7 ,0xbf ,0x30 ,0x4c
                                              ,0xa6 ,0xf9 ,0xe3 ,0x31 ,0x4e ,0xb9 ,0x01 ,0xfc
                                              ,0xca ,0x73 ,0x31 ,0xff ,0x37 ,0x9f ,0xfe ,0x74
                                              ,0x22 ,0xa2 ,0xb8 ,0x87 ,0x66 ,0x4e ,0xc0 ,0x50
                                              ,0xd6 ,0xde ,0x91 ,0x27 ,0xb7 ,0xee ,0xcb ,0x45
                                              ,0x53 ,0x4e ,0x93 ,0x85 ,0xf5 ,0x72 ,0xd8 ,0xfb
                                              ,0x60 ,0xf2 ,0x94 ,0xf0 ,0x0c ,0x5e ,0xf2 ,0xd1 };

            var publicKeyBytes = Core.API.Onion.Utilities.ConvertV3OnionHostnameToEd25518PublicKey(address);

            var byts = JsonConvert.SerializeObject(publicKeyBytes);

            byte[] si = new byte[64];

            var message = "test message";
            var message_bytes = Encoding.Default.GetBytes(message);

            Ed25519.Sign(si, message_bytes, message_bytes.Length, publicKeyBytes, privateKeyBytes);

            var verified = Ed25519.Verify(si, message_bytes, message_bytes.Length, publicKeyBytes);
        }
    }
}
