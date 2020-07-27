using TGMCore.Interop;

namespace TGMCore.Signatures
{
    public static class Ed25519
    {
        public static byte[] Sign(byte[] message, byte[] public_key, byte[] private_key)
        {
            var sig = new byte[64];
            Ed25519Dll.Sign(sig, message, message.Length, public_key, private_key);
            return sig;
        }

        public static bool Verify(byte[] signature, byte[] message, byte[] public_key)
        {
            return Ed25519Dll.Verify(signature, message, message.Length, public_key);
        }
    }
}
