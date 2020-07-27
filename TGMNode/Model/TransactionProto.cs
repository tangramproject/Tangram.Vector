// TGMNode by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ProtoBuf;

namespace TGMNode.Model
{
    [ProtoContract]
    public class TransactionProto
    {
        [ProtoMember(1)]
        public int Version { get; set; }
        [ProtoMember(2)]
        public string PreImage { get; set; }
        [ProtoMember(3)]
        public int Mix { get; set; }
        [ProtoMember(4)]
        public Vin Vin { get; set; }
        [ProtoMember(5)]
        public Vout Vout { get; set; }

        public IEnumerable<ValidationResult> Validate()
        {
            var results = new List<ValidationResult>();
            if (Version != 1)
            {
                results.Add(new ValidationResult("Incorrect number", new[] { "Version" }));
            }
            if (PreImage == null)
            {
                results.Add(new ValidationResult("Argument is null", new[] { "PreImage" }));
            }
            if (PreImage.Length != 64)
            {
                results.Add(new ValidationResult("Range exeption", new[] { "PreImage" }));
            }
            if (Mix < 0)
            {
                results.Add(new ValidationResult("Incorrect number", new[] { "Mix" }));
            }
            if (Mix > 17)
            {
                results.Add(new ValidationResult("Range exeption", new[] { "Mix" }));
            }
            if (Vin == null)
            {
                results.Add(new ValidationResult("Argument is null", new[] { "Vin" }));
            }
            if (Vout == null)
            {
                results.Add(new ValidationResult("Argument is null", new[] { "Vout" }));
            }
            return results;
        }
    }
}
