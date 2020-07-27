using System.IO;
using Akka.Configuration;

namespace TGMCore.Helper
{
    public static class ConfigurationLoader
    {
        public static Config Load(string configFile) => LoadConfig(configFile);

        private static Config LoadConfig(string configFile)
        {
            if (File.Exists(configFile))
            {
                string config = File.ReadAllText(configFile);
                return ConfigurationFactory.ParseString(config);
            }

            return Config.Empty;
        }
    }
}
