using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.API.Models
{
    public class HiddenServiceDetails
    {
        public string Hostname { get; set; }
        public byte[] PublicKey { get; set; }
    }
}
