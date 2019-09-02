using System;
using System.Linq;
using System.Text;
using Core.API.LibSodium;

namespace Core.API.Merkle
{
    public class Hash
    {
        const int HASH_LENGTH = 32;

        public byte[] Value { get; protected set; }

        public static Hash Create(byte[] buffer)
        {
            Hash hash = new Hash();
            hash.ComputeHash(buffer);

            return hash;
        }

        public static Hash Create(string buffer)
        {
            return Create(Encoding.UTF8.GetBytes(buffer));
        }

        public static Hash Create(Hash left, Hash right)
        {
            return Create(left.Value.Concat(right.Value).ToArray());
        }

        public static bool operator ==(Hash h1, Hash h2)
        {
            return h1.Equals(h2);
        }

        public static bool operator !=(Hash h1, Hash h2)
        {
            return !h1.Equals(h2);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            Tree.Contract(() => obj is Hash, "rvalue is not a Hash");
            return Equals((Hash)obj);
        }

        public override string ToString()
        {
            return BitConverter.ToString(Value).Replace("-", "");
        }

        public void ComputeHash(byte[] buffer)
        {
            // Swap SHA for BLAKE2b
            SetHash(Cryptography.GenericHashNoKey(Encoding.UTF8.GetString(buffer)));
        }

        public void SetHash(byte[] hash)
        {
            Tree.Contract(() => hash.Length == HASH_LENGTH, "Unexpected hash length.");
            Value = hash;
        }

        public bool Equals(byte[] hash)
        {
            return Value.SequenceEqual(hash);
        }

        public bool Equals(Hash hash)
        {
            bool ret = false;

            if (((object)hash) != null)
            {
                ret = Value.SequenceEqual(hash.Value);
            }

            return ret;
        }
    }
}
