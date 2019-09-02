using Core.API.LibSodium;
using Core.API.Signatures;
using System.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Core.API.Models;
using Core.API.Onion;
using System.Threading.Tasks;

namespace SwimProtocol.Messages
{
    public class SignedSwimMessage : SignedMessageBase
    {
        public MessageBase Message { get; set; }

        public static SignedSwimMessage Create(MessageBase message, byte[] pk, byte[] secret)
        {
            var serialized = JsonConvert.SerializeObject(message);
            var hash = Cryptography.GenericHashNoKey(serialized);

            var signature = Ed25519.Sign(hash, pk, secret);

            return new SignedSwimMessage
            { 
                Hash = hash,
                Message = message,
                Signature = signature,
                PublicKey = pk
            };
        }

        public static async Task<SignedSwimMessage> CreateAsync(MessageBase message, IOnionServiceClient client)
        {
            var serialized = JsonConvert.SerializeObject(message);
            var hash = Cryptography.GenericHashNoKey(serialized);

            var signatureResponse = await client.SignHashAsync(hash);

            return new SignedSwimMessage
            {
                Hash = hash,
                Message = message,
                Signature = signatureResponse.Signature,
                PublicKey = signatureResponse.PublicKey
            };
        }

        public override bool IsValid()
        {
            return IsValid(JsonConvert.SerializeObject(Message));
        }
    }
}
