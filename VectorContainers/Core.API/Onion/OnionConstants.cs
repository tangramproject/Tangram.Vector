using System;
using System.Collections.Generic;
using System.Text;

namespace Core.API.Onion
{
    internal sealed class OnionConstants
    {
        public const string ConfigSection = "onion";
        public const string Torrc = "torrc";
        public const string SocksHost = "onion_socks_host";
        public const string SocksPort = "onion_socks_port";
        public const string ControlHost = "onion_control_host";
        public const string ControlPort = "onion_control_port";
        public const string HashedControlPassword = "onion_hashed_control_password";
        public const string HiddenServicePort = "onion_hidden_service_port";
        public const string OnionDirectoryName = "onion";
        public const string ControlPortFileName = "control-port";
        public const string HiddenServiceDirectoryName = "hidden_service";
        public const string KeysDirectoryName = "keys";
        public const string OnionEnabled = "onion_enabled";
        public const string HostnameFileName = "hostname";
        public const string PublicKeyFileName = "hs_ed25519_public_key";
        public const string SecretKeyFileName = "hs_ed25519_secret_key";
        public const string OnionServiceAddress = "onion_service_address";
    }
}
