using System;
using System.Collections.Generic;
using System.Text;

namespace Core.API.Onion
{
    public interface IOnionServiceClientConfiguration
    {
        string SocksHost { get; }
        int SocksPort { get; }
        string ControlHost { get; }
        int ControlPort { get; }
        string HiddenServicePort { get; }
        string OnionServiceAddress { get; }

        string GetHiddenServiceDetailsRoute { get; }
        string IsTorStartedRoute { get; }

        string SignMessageRoute { get; }

        TimeSpan ClientTimeout { get; }
    }
}
