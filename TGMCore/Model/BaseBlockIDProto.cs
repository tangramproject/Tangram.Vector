﻿// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System.Text;
using Newtonsoft.Json;
using ProtoBuf;

namespace TGMCore.Model
{
    [ProtoContract]
    public class BaseBlockIDProto<TAttach> : IBaseBlockIDProto<TAttach>
    {
        private const string hexUpper = "0123456789ABCDEF";

        [ProtoMember(1)]
        public string Hash { get; set; }
        [ProtoMember(2)]
        public ulong Node { get; set; }
        [ProtoMember(3)]
        public ulong Round { get; set; }
        [ProtoMember(4)]
        public BaseBlockProto<TAttach> SignedBlock { get; set; }
        [ProtoMember(5)]
        public string PreviousHash { get; set; }

        public override string ToString()
        {
            var v = new StringBuilder();
            v.Append(Node.ToString());
            v.Append(" | ");
            v.Append(Round.ToString());
            if (!string.IsNullOrEmpty(Hash))
            {
                v.Append(" | ");
                for (int i = 6; i < 12; i++)
                {
                    var c = Hash[i];
                    v.Append(new char[] { hexUpper[c >> 4], hexUpper[c & 0x0f] });
                }
            }
            return v.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <returns></returns>
        public T Cast<T>()
        {
            var json = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}
