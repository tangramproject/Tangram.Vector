using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ProtoBuf;

namespace Coin.API.Model
{
    [ProtoContract]
    public class CoinProto
    {
        [ProtoMember(1)]
        public string Commitment { get; set; }
        [ProtoMember(2)]
        public string Hash { get; set; }
        [ProtoMember(3)]
        public string Hint { get; set; }
        [ProtoMember(4)]
        public string Keeper { get; set; }
        [ProtoMember(5)]
        public string Principle { get; set; }
        [ProtoMember(6)]
        public string RangeProof { get; set; }
        [ProtoMember(7)]
        public string Stamp { get; set; }
        [ProtoMember(8)]
        public string Network { get; set; }
        [ProtoMember(9)]
        public int Version { get; set; }

        public IEnumerable<ValidationResult> Validate()
        {
            var results = new List<ValidationResult>();
            if (Commitment == null)
            {
                results.Add(new ValidationResult("Argument is null", new[] { "Commitment" }));
            }
            if (Commitment.Length > 66)
            {
                results.Add(new ValidationResult("Range exeption", new[] { "Commitment" }));
            }

            if (Hint == null)
            {
                results.Add(new ValidationResult("Argument is null", new[] { "Hint" }));
            }
            if (Hint.Length > 64)
            {
                results.Add(new ValidationResult("Range exeption", new[] { "Hint" }));
            }

            if (Keeper == null)
            {
                results.Add(new ValidationResult("Argument is null", new[] { "Keeper" }));
            }
            if (Keeper.Length > 64)
            {
                results.Add(new ValidationResult("Range exeption", new[] { "Keeper" }));
            }

            if (Principle == null)
            {
                results.Add(new ValidationResult("Argument is null", new[] { "Principle" }));
            }
            if (Principle.Length > 64)
            {
                results.Add(new ValidationResult("Range exeption", new[] { "Principle" }));
            }

            if (RangeProof == null)
            {
                results.Add(new ValidationResult("Argument is null", new[] { "RangeProof" }));
            }
            if (RangeProof.Length > 1350)
            {
                results.Add(new ValidationResult("Range exeption", new[] { "RangeProof" }));
            }

            if (Stamp == null)
            {
                results.Add(new ValidationResult("Argument is null", new[] { "Stamp" }));
            }
            if (Stamp.Length > 64)
            {
                results.Add(new ValidationResult("Range exeption", new[] { "Stamp" }));
            }

            if (Version < 0)
            {
                results.Add(new ValidationResult("Invalid number", new[] { "Version" }));
            }

            if (Version > int.MaxValue)
            {
                results.Add(new ValidationResult("Invalid number", new[] { "Version" }));
            }

            return results;
        }
    }
}
