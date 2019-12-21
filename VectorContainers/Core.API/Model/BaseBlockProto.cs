using Newtonsoft.Json;
using ProtoBuf;

namespace Core.API.Model
{
    [ProtoContract]
    public class BaseBlockProto<TAttach>
    {
        [ProtoMember(1)]
        public string Key { get; set; }
        [ProtoMember(2)]
        public TAttach Attach { get; set; }
        [ProtoMember(3)]
        public string PublicKey { get; set; }
        [ProtoMember(4)]
        public string Signature { get; set; }
        [ProtoMember(5)]
        public long Epoch { get; set; }
        [ProtoMember(6)]
        public long Height { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Cast<T>()
        {
            var json = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}
