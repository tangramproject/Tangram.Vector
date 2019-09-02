using System;
using System.Text;
using Sodium;

namespace Core.API.LibSodium
{
    public static class Cryptography
    {
        /// <summary>
        /// Box seal.
        /// </summary>
        /// <returns>The seal.</returns>
        /// <param name="message">Message.</param>
        /// <param name="pk">Pk.</param>
        public static byte[] BoxSeal(string message, byte[] pk)
        {
            if (string.IsNullOrEmpty(message))
                throw new ArgumentException("message", nameof(message));

            if (pk == null)
                throw new ArgumentNullException(nameof(pk));

            var encrypted = SealedPublicKeyBox.Create(Encoding.UTF8.GetBytes(message), pk);
            return encrypted;
        }

        /// <summary>
        /// Generics hash no key.
        /// </summary>
        /// <returns>The hash no key.</returns>
        /// <param name="message">Message.</param>
        /// <param name="bytes">Bytes.</param>
        public static byte[] GenericHashNoKey(string message, int bytes = 32)
        {
            if (string.IsNullOrEmpty(message))
                throw new ArgumentException("Message cannot be null or empty!", nameof(message));

            return GenericHash.Hash(Encoding.UTF8.GetBytes(message), null, bytes);
        }

        /// <summary>
        /// Generics hash no key.
        /// </summary>
        /// <returns>The hash no key.</returns>
        /// <param name="message">Message.</param>
        /// <param name="bytes">Bytes.</param>
        public static byte[] GenericHashNoKey(byte[] message, int bytes = 32)
        {
            if (message == null)
                throw new ArgumentException("Message cannot be null or empty!", nameof(message));

            return GenericHash.Hash(message, null, bytes);
        }

        /// <summary>
        /// Generics hash with key.
        /// </summary>
        /// <returns>The hash with key.</returns>
        /// <param name="message">Message.</param>
        /// <param name="key">Key.</param>
        /// <param name="bytes">Bytes.</param>
        public static byte[] GenericHashWithKey(string message, byte[] key, int bytes = 32)
        {
            if (string.IsNullOrEmpty(message))
                throw new ArgumentException("Message cannot be null or empty!", nameof(message));

            return GenericHash.Hash(Encoding.UTF8.GetBytes(message), key, bytes);
        }

        /// <summary>
        /// Generics hash with key.
        /// </summary>
        /// <returns>The hash with key.</returns>
        /// <param name="message">Message.</param>
        /// <param name="key">Key.</param>
        /// <param name="bytes">Bytes.</param>
        public static byte[] GenericHashWithKey(byte[] message, byte[] key, int bytes = 32)
        {
            if (message == null)
                throw new ArgumentException("Message cannot be null or empty!", nameof(message));

            return GenericHash.Hash(message, key, bytes);
        }

        /// <summary>
        /// Argon2 Hash password.
        /// </summary>
        /// <returns>The pwd.</returns>
        /// <param name="pwd">Pwd.</param>
        public static byte[] HashPwd(string pwd)
        {
            if (string.IsNullOrEmpty(pwd))
                throw new ArgumentException("Password cannot be null or empty!", nameof(pwd));

            const long OPS_LIMIT = 4;
            const int MEM_LIMIT = 33554432;

            var hash = PasswordHash.ArgonHashString(pwd, OPS_LIMIT, MEM_LIMIT);

            return Encoding.UTF8.GetBytes(hash);
        }

        /// <summary>
        /// Generates a KeyPair
        /// </summary>
        /// <returns>The pair.</returns>
        public static KeyPairDto KeyPair()
        {
            var kp = PublicKeyBox.GenerateKeyPair();
            return new KeyPairDto() { PublicKey = kp.PublicKey, SecretKey = kp.PrivateKey };
        }

        /// <summary>
        ///  Generates a KeyPair.
        /// </summary>
        /// <returns>The pair auth.</returns>
        /// <param name="seed">Seed.</param>
        public static KeyPairDto KeyPairAuth(byte[] seed = null)
        {
            var kp = PublicKeyAuth.GenerateKeyPair(seed);
            return new KeyPairDto() { PublicKey = kp.PublicKey, SecretKey = kp.PrivateKey };
        }

        /// <summary>
        ///  Generates a KeyPair.
        /// </summary>
        /// <returns>The pair box.</returns>
        /// <param name="privateKey">Private key.</param>
        public static KeyPairDto KeyPairBox(byte[] privateKey = null)
        {
            var kp = PublicKeyBox.GenerateKeyPair(privateKey);
            return new KeyPairDto() { PublicKey = kp.PublicKey, SecretKey = kp.PrivateKey };
        }

        /// <summary>
        /// Opens the box seal.
        /// </summary>
        /// <returns>The box seal.</returns>
        /// <param name="cipher">Cipher.</param>
        /// <param name="keyPair">Key pair.</param>
        public static string OpenBoxSeal(byte[] cipher, KeyPair keyPair)
        {
            if (cipher == null)
                throw new ArgumentNullException(nameof(cipher));

            if (keyPair == null)
                throw new ArgumentNullException(nameof(keyPair));

            var decrypted = SealedPublicKeyBox.Open(cipher, keyPair);
            return Encoding.UTF8.GetString(decrypted);
        }

        /// <summary>
        /// Random bytes.
        /// </summary>
        /// <returns>The bytes.</returns>
        /// <param name="bytes">Bytes.</param>
        public static byte[] RandomBytes(int bytes = 32) => SodiumCore.GetRandomBytes(bytes);

        /// <summary>
        /// Random key.
        /// </summary>
        /// <returns>The key.</returns>
        public static byte[] RandomKey() => GenericHash.GenerateKey();

        public static int RandomNumber(int n) => SodiumCore.GetRandomNumber(n);

        /// <summary>
        /// Scalars the mult base.
        /// </summary>
        /// <returns>The mult base.</returns>
        /// <param name="sk">Sk.</param>
        public static byte[] ScalarMultBase(byte[] sk)
        {
            if (sk == null)
                throw new ArgumentNullException(nameof(sk));

            return Sodium.ScalarMult.Base(sk);
        }

        /// <summary>
        /// Scalars mult.
        /// </summary>
        /// <returns>The mult.</returns>
        /// <param name="bobSk">Bob sk.</param>
        /// <param name="alicePk">Alice pk.</param>
        public static byte[] ScalarMult(byte[] bobSk, byte[] alicePk)
        {
            if (bobSk == null)
                throw new ArgumentNullException(nameof(bobSk));

            if (alicePk == null)
                throw new ArgumentNullException(nameof(alicePk));

            return Sodium.ScalarMult.Mult(bobSk, alicePk);
        }

        /// <summary>
        /// Short hash.
        /// </summary>
        /// <returns>The hash.</returns>
        /// <param name="message">Message.</param>
        /// <param name="key">Key.</param>
        public static byte[] ShortHash(string message, byte[] key) => Sodium.ShortHash.Hash(message, key);

        /// <summary>
        /// Sign the specified message.
        /// </summary>
        /// <returns>The sign.</returns>
        /// <param name="message">Message.</param>
        /// <param name="sk">Sk.</param>
        public static byte[] Sign(byte[] message, byte[] sk) => PublicKeyAuth.SignDetached(message, sk);

        /// <summary>
        /// Verifiies the message.
        /// </summary>
        /// <returns><c>true</c>, if sign was verifiyed, <c>false</c> otherwise.</returns>
        /// <param name="signature">Signature.</param>
        /// <param name="message">Message.</param>
        /// <param name="pk">Pk.</param>
        public static bool VerifiySign(byte[] signature, byte[] message, byte[] pk) => PublicKeyAuth.VerifyDetached(signature, message, pk);

        /// <summary>
        /// Verifiies Argon2 hashed password.
        /// </summary>
        /// <returns><c>true</c>, if pwd was verifiyed, <c>false</c> otherwise.</returns>
        /// <param name="hash">Hash.</param>
        /// <param name="pwd">Pwd.</param>
        public static bool VerifiyPwd(byte[] hash, byte[] pwd)
        {
            if (hash == null)
                throw new ArgumentNullException(nameof(hash));

            if (pwd == null)
                throw new ArgumentNullException(nameof(pwd));

            return PasswordHash.ArgonHashStringVerify(hash, pwd);
        }

    }
}