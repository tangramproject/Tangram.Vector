using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Dawn;
using ProtoBuf;

namespace Core.API.Model
{
    [ProtoContract]
    public class CoinDto
    {
        [ProtoMember(1)]
        public EnvelopeDto Envelope { get; set; }
        [ProtoMember(2)]
        public byte[] Hash { get; set; }
        [ProtoMember(3)]
        public byte[] Hint { get; set; }
        [ProtoMember(4)]
        public byte[] Keeper { get; set; }
        [ProtoMember(5)]
        public byte[] Principle { get; set; }
        [ProtoMember(6)]
        public byte[] Stamp { get; set; }
        [ProtoMember(7)]
        public byte[] Network { get; set; }
        [ProtoMember(8)]
        public int Version { get; set; }

        public IEnumerable<ValidationResult> Validate()
        {
            var results = new List<ValidationResult>();

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

            if (Version < 0)
            {
                results.Add(new ValidationResult("Invalid number", new[] { "Version" }));
            }

            if (Version > int.MaxValue)
            {
                results.Add(new ValidationResult("Invalid number", new[] { "Version" }));
            }

            if (Principle == null)
            {
                results.Add(new ValidationResult("Argument is null", new[] { "Principle" }));
            }
            if (Principle.Length > 64)
            {
                results.Add(new ValidationResult("Range exeption", new[] { "Principle" }));
            }

            if (Stamp == null)
            {
                results.Add(new ValidationResult("Argument is null", new[] { "Stamp" }));
            }
            if (Stamp.Length > 64)
            {
                results.Add(new ValidationResult("Range exeption", new[] { "Stamp" }));
            }


            return results;
        }
    }

    [ProtoContract]
    public class CoinProtoList
    {
        [ProtoMember(1)]
        public string Stamp { get; set; }
        [ProtoMember(2)]
        public List<CoinDto> Coins { get; set; }
    }
}
