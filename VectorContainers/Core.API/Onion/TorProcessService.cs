using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Core.API.Helper;
using System.Security;
using Newtonsoft.Json.Linq;
using System.Threading;
using Newtonsoft.Json;
using DotNetTor.SocksPort;
using Core.API.Models;
using Core.API.Signatures;

namespace Core.API.Onion
{
    public class TorProcessService : HostedService, ITorProcessService
    {
        private static readonly DirectoryInfo tangramDirectory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);

        readonly IConfigurationSection onionSection;
        readonly ILogger logger;
        readonly string socksHost;
        readonly int socksPort;
        readonly string controlHost;
        readonly int controlPort;
        readonly string onionDirectory;
        readonly string torrcPath;
        readonly string controlPortPath;
        readonly string hiddenServicePath;
        readonly string hiddenServicePort;
        readonly string keyDirectoryPath;
        readonly string hostnamePath;
        readonly string publicKeyPath;
        readonly string secretKeyPath;
        readonly int onionEnabled;

        string hashedPassword;
        int torId;

        Process TorProcess { get; set; }

        public bool OnionStarted { get; private set; }

        public TorProcessService(IConfiguration configuration, ILogger logger)
        {
            onionSection = configuration.GetSection(OnionConstants.ConfigSection);

            this.logger = logger;

            socksHost = onionSection.GetValue<string>(OnionConstants.SocksHost);
            socksPort = onionSection.GetValue<int>(OnionConstants.SocksPort);
            controlHost = onionSection.GetValue<string>(OnionConstants.ControlHost);
            controlPort = onionSection.GetValue<int>(OnionConstants.ControlPort);
            hiddenServicePort = onionSection.GetValue<string>(OnionConstants.HiddenServicePort);

            onionDirectory = Path.Combine(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory), OnionConstants.OnionDirectoryName);
            torrcPath = Path.Combine(onionDirectory, OnionConstants.Torrc);
            controlPortPath = Path.Combine(onionDirectory, OnionConstants.ControlPortFileName);
            hiddenServicePath = Path.Combine(onionDirectory, OnionConstants.HiddenServiceDirectoryName);
            keyDirectoryPath = Path.Combine(onionDirectory, OnionConstants.KeysDirectoryName);
            onionEnabled = onionSection.GetValue<int>(OnionConstants.OnionEnabled);

            hostnamePath = Path.Combine(hiddenServicePath, OnionConstants.HostnameFileName);
            publicKeyPath = Path.Combine(hiddenServicePath, OnionConstants.PublicKeyFileName);
            secretKeyPath = Path.Combine(hiddenServicePath, OnionConstants.SecretKeyFileName);
        }

        public void ChangeCircuit(SecureString password)
        {
            if (password == null)
            {
                throw new ArgumentNullException(nameof(password));
            }

            try
            {
                using (var insecurePassword = password.Insecure())
                {
                    var controlPortClient = new DotNetTor.ControlPort.Client(controlHost, controlPort, insecurePassword.Value);
                    controlPortClient.ChangeCircuitAsync().Wait();
                }
            }
            catch (DotNetTor.TorException ex)
            {
                logger.LogError(ex.Message);
            }
        }

        public void GenerateHashPassword(SecureString password)
        {
            using (var insecurePassword = password.Insecure())
            {
                var torProcessStartInfo = new ProcessStartInfo(GetTorFileName())
                {
                    Arguments = $"--hash-password {password}",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                };

                try
                {
                    TorProcess = Process.Start(torProcessStartInfo);

                    var sOut = TorProcess.StandardOutput;
                    var raw = sOut.ReadToEnd();
                    var lines = raw.Split(Environment.NewLine);
                    string result = string.Empty;


                    logger.LogInformation(GetTorFileName());


                    //  If it's multi-line use the last non-empty line.
                    //  We don't want to pull in potential warnings.
                    if (lines.Length > 1)
                    {
                        var rlines = lines.Reverse();
                        foreach (var line in rlines)
                        {
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                result = Regex.Replace(line, Environment.NewLine, string.Empty);
                                logger.LogInformation($"Hopefully password line: {line}");
                                break;
                            }
                        }
                    }

                    if (!TorProcess.HasExited)
                    {
                        TorProcess.Kill();
                    }

                    sOut.Close();
                    TorProcess.Close();
                    TorProcess = null;

                    hashedPassword = Regex.Match(result, "16:[0-9A-F]+")?.Value ?? string.Empty;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex.Message);
                }
            }
        }

        public void SendCommands(string command, SecureString password)
        {
            if (string.IsNullOrEmpty(command))
            {
                throw new ArgumentException("Command cannot be null or empty!", nameof(command));
            }

            if (password == null)
            {
                throw new ArgumentNullException(nameof(password));
            }

            try
            {
                using (var insecurePassword = password.Insecure())
                {
                    var controlPortClient = new DotNetTor.ControlPort.Client(controlHost, controlPort, insecurePassword.Value);
                    var result = controlPortClient.SendCommandAsync(command).GetAwaiter().GetResult();
                }
            }
            catch (DotNetTor.TorException ex)
            {
                logger.LogError(ex.Message);
            }
        }

        public async Task<JObject> ClientPostAsync<T>(T payload, Uri baseAddress, string path, CancellationToken cancellationToken)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }

            if (baseAddress == null)
            {
                throw new ArgumentNullException(nameof(baseAddress));
            }

            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("Path is missing!", nameof(path));
            }

            using (var client = new HttpClient(new SocksPortHandler(socksHost, socksPort)))
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                client.BaseAddress = baseAddress;
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                using (var request = new HttpRequestMessage(HttpMethod.Post, path))
                {
                    var content = JsonConvert.SerializeObject(payload, Formatting.Indented);
                    var buffer = Encoding.UTF8.GetBytes(content);

                    request.Content = new StringContent(content, Encoding.UTF8, "application/json");

                    using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                    {
                        var stream = await response.Content.ReadAsStreamAsync();

                        if (response.IsSuccessStatusCode)
                        {
                            var result = Util.DeserializeJsonFromStream<JObject>(stream);
                            return Task.FromResult(result).Result;
                        }

                        var contentResult = await Util.StreamToStringAsync(stream);
                        throw new ApiException
                        {
                            StatusCode = (int)response.StatusCode,
                            Content = contentResult
                        };
                    }
                }
            }
        }

        public void StartOnion()
        {
            OnionStarted = false;

            CreateTorrc();
            StartTorProcess().GetAwaiter();
        }

        static string GetTorFileName()
        {
            var directory = tangramDirectory.ToString();
            var binary = "tor";

            if (Util.GetOSPlatform() == OSPlatform.Windows)
                return Path.Combine(directory, $"{binary}.exe");

            return Path.Combine(directory, $"{binary}");
        }

        void CreateTorrc()
        {
            if (string.IsNullOrEmpty(hashedPassword))
            {
                throw new ArgumentException("Hashed control password is not set.", nameof(hashedPassword));
            }

            if (!Directory.Exists(onionDirectory))
            {
                try
                {
                    Directory.CreateDirectory(onionDirectory);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.Message);
                    throw new Exception(ex.Message);
                }
            }

            if (File.Exists(torrcPath))
                return;

            var torrcContent = new string[] {
                "AvoidDiskWrites 1",
                string.Format("HashedControlPassword {0}", hashedPassword),
                "SocksPort auto IPv6Traffic PreferIPv6 KeepAliveIsolateSOCKSAuth",
                "ControlPort auto",
                "CookieAuthentication 1",
                $"HiddenServiceDir {hiddenServicePath}",
                $"HiddenServicePort {hiddenServicePort}",
                $"KeyDirectory {keyDirectoryPath}",
                "HiddenServiceVersion 3",
                "CircuitBuildTimeout 10",
                "KeepalivePeriod 60",
                "NumEntryGuards 8",
                $"ControlPort {controlPort}",
                $"SocksPort {socksPort}",
                "Log notice stdout",
                $"DataDirectory {onionDirectory}",
                $"ControlPortWriteToFile {controlPortPath}"
            };

            try
            {
                using (StreamWriter outputFile = new StreamWriter(torrcPath))
                {
                    foreach (string content in torrcContent)
                        outputFile.WriteLine(content);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                throw new Exception(ex.Message);
            }

            logger.LogInformation($"Created torrc file: {torrcPath}");
        }

        int ReadControlPort()
        {
            int port = 0;

            if (File.Exists(controlPortPath))
            {
                try
                {
                    int.TryParse(Util.Pop(File.ReadAllText(controlPortPath, Encoding.UTF8), ":"), out port);
                }
                catch (Exception e)
                {
                    logger.LogInformation(e.ToString());
                }
            }

            return port == 0 ? controlPort : port;
        }

        async Task StartTorProcess()
        {
            if (torId.Equals(0))
            {
                TorProcess = new Process();
                TorProcess.StartInfo.FileName = GetTorFileName();
                TorProcess.StartInfo.Arguments = $"-f \"{torrcPath}\"";
                TorProcess.StartInfo.UseShellExecute = false;
                TorProcess.StartInfo.CreateNoWindow = true;
                TorProcess.StartInfo.RedirectStandardOutput = true;
                TorProcess.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        if (e.Data.Contains("Bootstrapped 100%: Done"))
                        {
                            OnionStarted = true;
                            logger.LogInformation("tor Started!");
                        }

                        logger.LogInformation(e.Data);
                    }
                };

                TorProcess.Start();

                torId = TorProcess.Id;

                await Task.Run(() => TorProcess.BeginOutputReadLine());
            }
        }

        public override void Dispose()
        {
            try
            {
                foreach (Process tor in Process.GetProcesses())
                {
                    if (tor.Id == torId)
                    {
                        tor.Kill();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex.Message);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            if (onionEnabled == 1)
            {
                logger.LogInformation("Starting Onion Service");

                await Task.Run(() => {
                    GenerateHashPassword("ILoveTangram".ToSecureString());
                    StartOnion();
                });
            }
        }

        public async Task<byte[]> GetHiddenServicePrivateKey()
        {
            byte[] secretKey = new byte[64];
            var secretKeyFileBytes = await File.ReadAllBytesAsync(secretKeyPath);

            Array.ConstrainedCopy(secretKeyFileBytes, 32, secretKey, 0, 64);

            return secretKey;
        }

        public async Task<HiddenServiceDetails> GetHiddenServiceDetailsAsync()
        {
            byte[] publicKey = new byte[32];
            byte[] secretKey = new byte[64];

            var hostname = (await File.ReadAllTextAsync(hostnamePath)).Trim();
            var publicKeyFileBytes = await File.ReadAllBytesAsync(publicKeyPath);
            var secretKeyFileBytes = await File.ReadAllBytesAsync(secretKeyPath);

            Array.ConstrainedCopy(publicKeyFileBytes, 32, publicKey, 0, 32);
            Array.ConstrainedCopy(secretKeyFileBytes, 32, secretKey, 0, 64);

            return new HiddenServiceDetails
            {
                Hostname = hostname,
                PublicKey = publicKey
            };
        }

        public async Task<SignedHashResponse> SignedHashAsync(byte[] hash)
        {
            var hsd = await GetHiddenServiceDetailsAsync();
            var hiddenServicePrivateKey = await GetHiddenServicePrivateKey();

            var signature = Ed25519.Sign(hash, hsd.PublicKey, hiddenServicePrivateKey);

            return new SignedHashResponse
            {
                PublicKey = hsd.PublicKey,
                Signature = signature
            };
        }
    }
}
