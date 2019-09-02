using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ProtoBuf;

namespace Core.API.Model
{
    [ProtoContract]
    public class EnvelopeDto
    {
        [ProtoMember(1)]
        public byte[] Commitment { get; set; }
        [ProtoMember(2)]
        public byte[] PublicKey { get; set; }
        [ProtoMember(3)]
        public byte[] Proof { get; set; }
        [ProtoMember(4)]
        public byte[] Signature { get; set; }
        [ProtoMember(5)]
        public byte[] RangeProof { get; set; }

        public IEnumerable<ValidationResult> Validate()
        {
            var results = new List<ValidationResult>();

            if (Commitment == null)
            {
                results.Add(new ValidationResult("Argument is null", new[] { "Commitment" }));
            }
            if (Commitment.Length > 33)
            {
                results.Add(new ValidationResult("Range exeption", new[] { "Commitment" }));
            }

            if (PublicKey == null)
            {
                results.Add(new ValidationResult("Argument is null", new[] { "PublicKey" }));
            }
            if (PublicKey.Length > 64)
            {
                results.Add(new ValidationResult("Range exeption", new[] { "Keeper" }));
            }

            if (Proof == null)
            {
                results.Add(new ValidationResult("Argument is null", new[] { "Proof" }));
            }
            if (Proof.Length > 32)
            {
                results.Add(new ValidationResult("Range exeption", new[] { "Proof" }));
            }

            if (Signature == null)
            {
                results.Add(new ValidationResult("Argument is null", new[] { "Signature" }));
            }
            if (Signature.Length > 64)
            {
                results.Add(new ValidationResult("Range exeption", new[] { "Signature" }));
            }

            if (RangeProof == null)
            {
                results.Add(new ValidationResult("Argument is null", new[] { "RangeProof" }));
            }
            if (RangeProof.Length > 675)
            {
                results.Add(new ValidationResult("Range exeption", new[] { "RangeProof" }));
            }


            return results;
        }
    }
}
