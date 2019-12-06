using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace Core.API.Onion
{
    public class OnionServiceClientConfiguration : IOnionServiceClientConfiguration
    {
        public string SocksHost { get; }

        public int SocksPort { get; }

        public string ControlHost { get; }

        public int ControlPort { get; }

        public string HiddenServicePort { get; }

        public string OnionServiceAddress { get; }

        public string GetHiddenServiceDetailsRoute { get; }

        public string SignMessageRoute { get; }

        public string IsTorStartedRoute { get; }

        public TimeSpan ClientTimeout { get; }

        public OnionServiceClientConfiguration(IConfiguration configuration)
        {
            var onionSection = configuration.GetSection(OnionConstants.ConfigSection);

            SocksHost = onionSection.GetValue<string>(OnionConstants.SocksHost);
            SocksPort = onionSection.GetValue<int>(OnionConstants.SocksPort);
            ControlHost = onionSection.GetValue<string>(OnionConstants.ControlHost);
            ControlPort = onionSection.GetValue<int>(OnionConstants.ControlPort);
            HiddenServicePort = onionSection.GetValue<string>(OnionConstants.HiddenServicePort);
            OnionServiceAddress = onionSection.GetValue<string>(OnionConstants.OnionServiceAddress);
            ClientTimeout = TimeSpan.FromSeconds(onionSection.GetValue(OnionConstants.ClientTimeout, 7));

            GetHiddenServiceDetailsRoute = "api/Onion/hsdetails";
            SignMessageRoute = "api/Onion/sign";
            IsTorStartedRoute = "api/Onion/istorstarted";
        }
    }
}
