using System;
using Xunit;
using Xunit.Abstractions;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using NUlid;
using System.IO;
using Microsoft.Extensions.Logging;

namespace SwimProtocol.Tests
{
    public class XunitLoggerProvider : ILoggerProvider
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public XunitLoggerProvider(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        public ILogger CreateLogger(string categoryName)
            => new XunitLogger(_testOutputHelper, categoryName);

        public void Dispose()
        { }
    }

    public class XunitLogger : ILogger
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly string _categoryName;

        public XunitLogger(ITestOutputHelper testOutputHelper, string categoryName)
        {
            _testOutputHelper = testOutputHelper;
            _categoryName = categoryName;
        }

        public IDisposable BeginScope<TState>(TState state)
            => NoopDisposable.Instance;

        public bool IsEnabled(LogLevel logLevel)
            => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            _testOutputHelper.WriteLine($"{formatter(state, exception)}");
            Debug.WriteLine($"{formatter(state, exception)}");

            if (exception != null)
            {
                _testOutputHelper.WriteLine(exception.ToString());
                Debug.WriteLine(exception.ToString());
            }
        }

        private class NoopDisposable : IDisposable
        {
            public static NoopDisposable Instance = new NoopDisposable();
            public void Dispose()
            { }
        }
    }

    public class FailureDetectionTests
    {
        private readonly ITestOutputHelper _output;
        private readonly IConfiguration _configuration;
        private readonly LoggerFactory _loggerFactory;

        private readonly ILogger<FailureDetectionProvider> _logger;


        public void Init()
        {
        }

        public FailureDetectionTests(ITestOutputHelper output)
        {
            _output = output;
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var loggerFactory = new LoggerFactory();
            loggerFactory.AddProvider(new XunitLoggerProvider(output));
            _logger = loggerFactory.CreateLogger<FailureDetectionProvider>();
        }

        [Fact]
        public void HappyPath()
        {
            ISwimProtocolProvider protocolProvider = new SwimProtocolProvider(null, _output);

            var loggerFactory = new LoggerFactory();
            loggerFactory.AddProvider(new XunitLoggerProvider(_output));

            var swimProtocol = new FailureDetectionProvider(protocolProvider, _configuration, loggerFactory.CreateLogger<FailureDetectionProvider>());

            swimProtocol.OnTransitioned((action) =>
            {
                _output.WriteLine($"{action.Source} => {action.Trigger} => {action.Destination}");
            });

            swimProtocol.Fire(SwimFailureDetectionTrigger.Ping);
            Assert.Equal(SwimFailureDetectionState.Pinged, swimProtocol.State);

            swimProtocol.Fire(SwimFailureDetectionTrigger.PingExpireLive);
            Assert.Equal(SwimFailureDetectionState.Alive, swimProtocol.State);

            swimProtocol.Fire(SwimFailureDetectionTrigger.ProtocolExpireLive);
            Assert.Equal(SwimFailureDetectionState.Expired, swimProtocol.State);

            swimProtocol.Fire(SwimFailureDetectionTrigger.Reset);
            Assert.Equal(SwimFailureDetectionState.Idle, swimProtocol.State);
        }

        [Fact]
        public void SemiHappyPath()
        {
            ISwimProtocolProvider protocolProvider = new SwimProtocolProvider(null, _output);

            var loggerFactory = new LoggerFactory();
            loggerFactory.AddProvider(new XunitLoggerProvider(_output));

            var swimProtocol = new FailureDetectionProvider(protocolProvider, _configuration, loggerFactory.CreateLogger<FailureDetectionProvider>());

            swimProtocol.OnTransitioned((action) =>
            {
                _output.WriteLine($"{action.Source} => {action.Trigger} => {action.Destination}");
            });

            swimProtocol.Fire(SwimFailureDetectionTrigger.Ping);
            Assert.Equal(SwimFailureDetectionState.Pinged, swimProtocol.State);

            swimProtocol.Fire(SwimFailureDetectionTrigger.PingExpireNoResponse);
            Assert.Equal(SwimFailureDetectionState.PrePingReq, swimProtocol.State);

            swimProtocol.Fire(SwimFailureDetectionTrigger.PingReq);
            Assert.Equal(SwimFailureDetectionState.PingReqed, swimProtocol.State);

            swimProtocol.Fire(SwimFailureDetectionTrigger.ProtocolExpireLive);
            Assert.Equal(SwimFailureDetectionState.Expired, swimProtocol.State);

            swimProtocol.Fire(SwimFailureDetectionTrigger.Reset);
            Assert.Equal(SwimFailureDetectionState.Idle, swimProtocol.State);
        }

        [Fact]
        public void DeadPath()
        {
            ISwimProtocolProvider protocolProvider = new SwimProtocolProvider(null, _output);

            var loggerFactory = new LoggerFactory();
            loggerFactory.AddProvider(new XunitLoggerProvider(_output));

            var swimProtocol = new FailureDetectionProvider(protocolProvider, _configuration, loggerFactory.CreateLogger<FailureDetectionProvider>());

            swimProtocol.OnTransitioned((action) =>
            {
                _output.WriteLine($"{action.Source} => {action.Trigger} => {action.Destination}");
            });

            swimProtocol.Fire(SwimFailureDetectionTrigger.Ping);
            Assert.Equal(SwimFailureDetectionState.Pinged, swimProtocol.State);

            swimProtocol.Fire(SwimFailureDetectionTrigger.PingExpireNoResponse);
            Assert.Equal(SwimFailureDetectionState.PrePingReq, swimProtocol.State);

            swimProtocol.Fire(SwimFailureDetectionTrigger.PingReq);
            Assert.Equal(SwimFailureDetectionState.PingReqed, swimProtocol.State);

            swimProtocol.Fire(SwimFailureDetectionTrigger.ProtocolExpireDead);
            Assert.Equal(SwimFailureDetectionState.Expired, swimProtocol.State);

            swimProtocol.Fire(SwimFailureDetectionTrigger.Reset);
            Assert.Equal(SwimFailureDetectionState.Idle, swimProtocol.State);
        }

        public static SwimNode GenerateNode(string onionAddress)
        {
            return new SwimNode($"http://{onionAddress}");
        }

        public static SwimProtocolProvider GenerateProtocolProvider(SwimNode node, ITestOutputHelper _output)
        {
            return new SwimProtocolProvider(node, _output);
        }

        public static FailureDetectionProvider GenerateFailureDetection(SwimProtocolProvider provider, IConfiguration configuration, ILogger<FailureDetectionProvider> logger)
        {
            return new FailureDetectionProvider(provider, configuration, logger);
        }

        [Fact]
        public void ThreeNodesPingSuccess()
        {
            var pairs = new List<Tuple<string, byte[]>>();

            pairs.Add(
                Tuple.Create("tuo3aav6qk36ahmrw2ujgrjpios6n3wzupndvhxoq35ainmbmslx3rad.onion", new byte[] 
                {
                    0x88 ,0x3C ,0x5E ,0x30 ,0xC0 ,0x6D ,0xEB ,0x99 ,
                    0xB6 ,0x14 ,0x03 ,0xD9 ,0x14 ,0xDF ,0x7D ,0x53 ,
                    0xFF ,0x6B ,0x3B ,0x21 ,0x46 ,0x82 ,0x44 ,0x81 ,
                    0xAA ,0x5F ,0xB5 ,0x55 ,0xF4 ,0xAC ,0xE1 ,0x78 ,
                    0xD9 ,0xAA ,0x34 ,0xB8 ,0xC8 ,0xA8 ,0xDB ,0x01 ,
                    0x02 ,0x21 ,0xB5 ,0xAA ,0x6C ,0xCC ,0xBD ,0xA5 ,
                    0x17 ,0x92 ,0x16 ,0xE2 ,0x09 ,0x89 ,0x9A ,0xFE ,
                    0xBB ,0x80 ,0xA9 ,0x1F ,0x81 ,0xAF ,0x54 ,0x18
                })
            );

            pairs.Add(
                Tuple.Create("5tzzzhbuyoxmf6zsrjlv5c7lwy5v5c363u7w76bqbczoyuewnb4vrfqd.onion", new byte[]
                {
                    0x28, 0x41, 0x0d, 0x32, 0x58 ,0x0b ,0xfb ,0x7d
                   ,0x3e ,0xb6 ,0x2b ,0x5b ,0xc7 ,0xbf ,0x30 ,0x4c
                   ,0xa6 ,0xf9 ,0xe3 ,0x31 ,0x4e ,0xb9 ,0x01 ,0xfc
                   ,0xca ,0x73 ,0x31 ,0xff ,0x37 ,0x9f ,0xfe ,0x74
                   ,0x22 ,0xa2 ,0xb8 ,0x87 ,0x66 ,0x4e ,0xc0 ,0x50
                   ,0xd6 ,0xde ,0x91 ,0x27 ,0xb7 ,0xee ,0xcb ,0x45
                   ,0x53 ,0x4e ,0x93 ,0x85 ,0xf5 ,0x72 ,0xd8 ,0xfb
                   ,0x60 ,0xf2 ,0x94 ,0xf0 ,0x0c ,0x5e ,0xf2 ,0xd1
                })
            );

            pairs.Add(
                Tuple.Create("2dqecntuii6zenpg24qonkyylvoetbdzju5jg4l7hvukv2fawvfkp3qd.onion", new byte[]
                {
                    0x50 ,0x55 ,0x0A ,0x27 ,0xDD ,0x8C ,0x18 ,0x75 ,
                    0x82 ,0x89 ,0xDC ,0xA7 ,0x6B ,0xC3 ,0xC2 ,0xF5 ,
                    0x5E ,0xE3 ,0x5A ,0x3C ,0x6D ,0xD9 ,0xE5 ,0xFC ,
                    0x30 ,0x61 ,0x8F ,0x64 ,0x05 ,0xCF ,0x95 ,0x43 ,
                    0x49 ,0x87 ,0x39 ,0x79 ,0x00 ,0x26 ,0xC7 ,0x6A ,
                    0x46 ,0x5B ,0xD4 ,0x7C ,0xEE ,0x99 ,0xDE ,0xC0 ,
                    0x16 ,0x59 ,0xE6 ,0x09 ,0x92 ,0xC5 ,0x43 ,0x6B ,
                    0x67 ,0x71 ,0x71 ,0x73 ,0x6A ,0x15 ,0x53 ,0xB8
                })
            );

            var node1 = GenerateNode(pairs[0].Item1);
            var node2 = GenerateNode(pairs[1].Item1);
            var node3 = GenerateNode(pairs[2].Item1);

            var protocolProvider1 = GenerateProtocolProvider(node1, _output);
            protocolProvider1.SecretKey = pairs[0].Item2;

            var protocolProvider2 = GenerateProtocolProvider(node2, _output);
            protocolProvider2.SecretKey = pairs[1].Item2;

            var protocolProvider3 = GenerateProtocolProvider(node3, _output);
            protocolProvider3.SecretKey = pairs[2].Item2;

            var fd1 = GenerateFailureDetection(protocolProvider1, _configuration, _logger);
            var fd2 = GenerateFailureDetection(protocolProvider2, _configuration, _logger);
            var fd3 = GenerateFailureDetection(protocolProvider3, _configuration, _logger);

            var listen1 = protocolProvider1.Listen();

            Thread.Sleep(5000);

            var listen2 = protocolProvider2.Listen();

            Thread.Sleep(5000);

            var listen3 = protocolProvider3.Listen();

            fd1.Start();
            fd2.Start();
            fd3.Start();

            protocolProvider1.SendMessage(node2, new AliveMessage(Ulid.NewUlid(), node1, node1));
            protocolProvider1.SendMessage(node3, new AliveMessage(Ulid.NewUlid(), node1, node1));

            while (true) { }
        }

        [Fact]
        public void TwoNodesPingSuccess()
        {
            var pairs = new List<Tuple<string, byte[]>>();

            pairs.Add(
                Tuple.Create("tuo3aav6qk36ahmrw2ujgrjpios6n3wzupndvhxoq35ainmbmslx3rad.onion", new byte[]
                {
                    0x88 ,0x3C ,0x5E ,0x30 ,0xC0 ,0x6D ,0xEB ,0x99 ,
                    0xB6 ,0x14 ,0x03 ,0xD9 ,0x14 ,0xDF ,0x7D ,0x53 ,
                    0xFF ,0x6B ,0x3B ,0x21 ,0x46 ,0x82 ,0x44 ,0x81 ,
                    0xAA ,0x5F ,0xB5 ,0x55 ,0xF4 ,0xAC ,0xE1 ,0x78 ,
                    0xD9 ,0xAA ,0x34 ,0xB8 ,0xC8 ,0xA8 ,0xDB ,0x01 ,
                    0x02 ,0x21 ,0xB5 ,0xAA ,0x6C ,0xCC ,0xBD ,0xA5 ,
                    0x17 ,0x92 ,0x16 ,0xE2 ,0x09 ,0x89 ,0x9A ,0xFE ,
                    0xBB ,0x80 ,0xA9 ,0x1F ,0x81 ,0xAF ,0x54 ,0x18
                })
            );

            pairs.Add(
                Tuple.Create("5tzzzhbuyoxmf6zsrjlv5c7lwy5v5c363u7w76bqbczoyuewnb4vrfqd.onion", new byte[]
                {
                    0x28, 0x41, 0x0d, 0x32, 0x58 ,0x0b ,0xfb ,0x7d
                   ,0x3e ,0xb6 ,0x2b ,0x5b ,0xc7 ,0xbf ,0x30 ,0x4c
                   ,0xa6 ,0xf9 ,0xe3 ,0x31 ,0x4e ,0xb9 ,0x01 ,0xfc
                   ,0xca ,0x73 ,0x31 ,0xff ,0x37 ,0x9f ,0xfe ,0x74
                   ,0x22 ,0xa2 ,0xb8 ,0x87 ,0x66 ,0x4e ,0xc0 ,0x50
                   ,0xd6 ,0xde ,0x91 ,0x27 ,0xb7 ,0xee ,0xcb ,0x45
                   ,0x53 ,0x4e ,0x93 ,0x85 ,0xf5 ,0x72 ,0xd8 ,0xfb
                   ,0x60 ,0xf2 ,0x94 ,0xf0 ,0x0c ,0x5e ,0xf2 ,0xd1
                })
            );

            var node1 = GenerateNode(pairs[0].Item1);
            var node2 = GenerateNode(pairs[1].Item1);

            var protocolProvider1 = GenerateProtocolProvider(node1, _output);
            protocolProvider1.SecretKey = pairs[0].Item2;

            var protocolProvider2 = GenerateProtocolProvider(node2, _output);
            protocolProvider2.SecretKey = pairs[1].Item2;

            var fd1 = GenerateFailureDetection(protocolProvider1, _configuration, _logger);
            var fd2 = GenerateFailureDetection(protocolProvider2, _configuration, _logger);

            var listen1 = protocolProvider1.Listen();

            Thread.Sleep(5000);

            var listen2 = protocolProvider2.Listen();

            fd1.Start();
            fd2.Start();

            while (true) { }
        }
    }
}
