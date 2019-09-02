using Swim.Messages;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Logging;
using Swim.Collections;
using NUlid;

namespace Swim
{
    public class SwimClient : ISwimClient
    {
        private static TimeSpan MedianRTT = new TimeSpan(0, 0, 0, 0, 650);
        private TimeSpan ProtocolPeriod { get; } = new TimeSpan(0, 0, 0, 7, 500);

        private ConcurrentQueue<SwimNode> Nodes { get; set; } = new ConcurrentQueue<SwimNode>();
        private object _nodesLock = new object();

        private SwimNode ActiveNode { get; set; }

        private ConcurrentQueue<MessageBase> Messages { get; set; } = new ConcurrentQueue<MessageBase>();

        private ConcurrentBag<BroadcastableItem> BroadcastQueue { get; set; } = new ConcurrentBag<BroadcastableItem>();
        private object _broadcastQueueLock = new object();

        private ConcurrentDictionaryEx<Ulid, MessageBase> CorrelatedMessages { get; set; } = new ConcurrentDictionaryEx<Ulid, MessageBase>(200);

        private int InitialNodeCount { get; set; }
        private int ProtocolPeriodsComplete { get; set; } = 0;
        private int Lambda = 3;
        private Timer ProtocolTimer { get; set; }
        private bool ProtocolTimerRunning { get; set; }

        private Timer PingTimer { get; set; }
        private bool PingTimerRunning { get; set; }

        private ISwimProtocolProvider ProtocolProvider { get; set; }
        private ILogger Logger { get; set; }

        private bool ReceivedAck { get; set; }
        private object _receivedAckLock = new object();

        private Ulid? PingCorrelationId { get; set; }

        public IEnumerable<SwimNode> Members
        {
            get
            {
                lock (_nodesLock)
                {
                    return Nodes.ToList();
                }
            }
        }

        public IEnumerable<SwimNode> GetRandomMembers(int size)
        {
            var members = Members.ToList();
            members.Shuffle();

            return members.Take(size).ToList();
        }

        public SwimClient(ISwimProtocolProvider protocolProvider, ILogger logger)
        {
            ProtocolProvider = protocolProvider;
            Logger = logger;

            ProtocolProvider.ReceivedMessage += ProtocolProvider_ReceivedMessage;
            ProtocolTimer = new Timer(ProtocolPeriod.TotalMilliseconds);
            PingTimer = new Timer(MedianRTT.TotalMilliseconds * 2);
            PingCorrelationId = null;

            ProtocolTimer.Elapsed += ProtocolTimer_Elapsed;
            PingTimer.Elapsed += PingTimer_Elapsed;
        }

        private void ProtocolProvider_ReceivedMessage(object sender, ReceivedMessageEventArgs e)
        {
            if (e.Message != null)
            {
                if (!e.Message.IsValid)
                {
                    Logger.LogDebug($"Received and Rejected Message: {e.Message}");
                }

                Logger.LogInformation($"Received {e.Message.MessageType.ToString()} Message: {e.Message}");
                Messages.Enqueue(e.Message);
            }
        }

        private IEnumerable<MessageBase> GetBroadcastMessages(int num = 10)
        {
            lock (_broadcastQueueLock)
            {
                //  Prioritize new items and remove items that have hit the Broadcast threshold
                lock (_nodesLock)
                {
                    BroadcastQueue = new ConcurrentBag<BroadcastableItem>(BroadcastQueue.OrderBy(x => x.BroadcastCount).Where(x => x.BroadcastCount < Lambda * Math.Log(Nodes.Count + 1)));
                }

                IEnumerable<BroadcastableItem> bis = null;

                bis = BroadcastQueue.Count < num ? BroadcastQueue.Take(BroadcastQueue.Count) : BroadcastQueue.Take(num);

                foreach (var bi in bis)
                {
                    bi.BroadcastCount++;
                }

                return bis.Select(x => x.Message);
            }
        }

        private void AddBroadcastMessage(MessageBase message)
        {
            lock (_broadcastQueueLock)
            {
                BroadcastQueue.Add(new BroadcastableItem(message));
            }
        }

        public async Task BeginProtocolPeriodAsync()
        {
            try
            {
                Debug.WriteLine("Beginning Protocol Period");
                Logger.LogInformation("Beginning Protocol Period");

                ProtocolTimer.Start();

                ProtocolTimerRunning = true;

                lock (_receivedAckLock)
                {
                    ReceivedAck = false;

                    SwimNode node = null;

                    lock (_nodesLock)
                    {
                        InitialNodeCount = Nodes.Count;

                        if (Nodes.TryDequeue(out node))
                        {
                            ActiveNode = node;

                            Debug.WriteLine(ActiveNode.Endpoint);

                            PingNode(ActiveNode);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"{e.ToString()}: {e.StackTrace}");
                Logger.LogError(e, string.Empty);
            }
        }

        private void ProtocolTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            EndProtocolPeriod();
        }

        private void PingTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (_receivedAckLock)
            {
                if (!ReceivedAck && ActiveNode != null)
                {
                    Debug.WriteLine($"No direct response from Node {ActiveNode.Endpoint}");
                    Logger.LogInformation($"No direct response from Node {ActiveNode.Endpoint}");

                    lock (_nodesLock)
                    {
                        if (Nodes.Count >= 1)
                        {
                            //  Select k num of nodes.
                            RNGCryptoServiceProvider provider = new RNGCryptoServiceProvider();
                            // Get four random bytes.
                            byte[] four_bytes = new byte[4];
                            provider.GetBytes(four_bytes);

                            // Convert that into an uint.
                            int num = BitConverter.ToInt32(four_bytes, 0);

                            num = num < 0 ? num * -1 : num;

                            int k = 1 + (num % Math.Min(Nodes.Count, 8));

                            //  Create list of node candidates, remove active node.
                            var candidates = Nodes.ToList();
                            candidates.Remove(ActiveNode);

                            //  Shuffle candidates
                            candidates.Shuffle();

                            //  Select k nodes
                            var nodesToPingReq = candidates.Take(k);

                            if (!nodesToPingReq.Any())
                            {
                                Debug.WriteLine("No nodes to select from");
                                Logger.LogInformation("No nodes to select from");
                            }

                            foreach (var node in nodesToPingReq)
                            {
                                Debug.WriteLine($"Sending pingreq for {ActiveNode.Endpoint} to {node.Endpoint}");
                                Logger.LogInformation($"Sending pingreq for {ActiveNode.Endpoint} to {node.Endpoint}");

                                ProtocolProvider.SendMessage(node, new PingReqMessage(PingCorrelationId.Value, ActiveNode, ProtocolProvider.Node));
                            }
                        }
                    }
                }
                else if (ReceivedAck && ActiveNode != null)
                {
                    Debug.WriteLine($"Node {ActiveNode.Endpoint} responded with Ack, marking as alive");
                    Logger.LogInformation($"Node {ActiveNode.Endpoint} responded with Ack, marking as alive");
                }
            }

            PingTimer.Stop();
        }

        private void PingNode(SwimNode node)
        {
            //  Ping node directly

            PingCorrelationId = Ulid.NewUlid();

            Debug.WriteLine($"Pinging node {node.Endpoint}.");
            Logger.LogInformation($"Pinging node {node.Endpoint}.");

            ProtocolProvider.SendMessage(node, new PingMessage(PingCorrelationId.Value) { SourceNode = ProtocolProvider.Node });

            PingTimer.Start();
        }

        public void RemoveNode(SwimNode node)
        {
            //  This is a main reason why we need to lock the nodes collection,
            //  when nodes die we need to remove them from our queue.
            //  TODO: Implement a more robust collection.
            lock (_nodesLock)
            {
                var nodes_filtered = Nodes.Where(x => x.Endpoint != node.Endpoint);
                Nodes = new ConcurrentQueue<SwimNode>(nodes_filtered);
            }
        }

        public void AddNode(SwimNode node)
        {
            lock (_nodesLock)
            {
                if (!Nodes.Any(x => x.Endpoint == node.Endpoint))
                {
                    Nodes.Enqueue(node);
                }
            }
        }

        public bool TryDequeueNode(out SwimNode node)
        {
            lock (_nodesLock)
            {
                return Nodes.TryDequeue(out node);
            }
        }

        public void ShuffleNodes()
        {
            //  This a reason why we need to lock the nodes collection.
            //  TODO: Implement a more robust collection.

            lock (_nodesLock)
            {
                var nodes = new List<SwimNode>(Nodes.ToArray());

                Nodes = new ConcurrentQueue<SwimNode>();

                nodes.Shuffle();

                foreach (var node in nodes)
                {
                    Nodes.Enqueue(node);
                }
            }
        }

        public void EndProtocolPeriod()
        {
            try
            {
                Debug.WriteLine("Ending Protocol Period");
                Logger.LogInformation("Ending Protocol Period");

                lock (_receivedAckLock)
                {
                    if (ActiveNode != null)
                    {
                        if (!ReceivedAck)
                        {
                            Debug.WriteLine($"No response from Node {ActiveNode.Endpoint}, marking as dead.");
                            Logger.LogInformation($"No response from Node {ActiveNode.Endpoint}, marking as dead.");

                            AddBroadcastMessage(new DeadMessage(ActiveNode));
                        }
                        else
                        {
                            Debug.WriteLine($"Response from Node {ActiveNode.Endpoint}, marking as alive.");
                            Logger.LogInformation($"Response from Node {ActiveNode.Endpoint}, marking as alive.");

                            //  Add the node back into the queue.
                            AddNode(ActiveNode);
                        }

                        ActiveNode = null;

                        ProtocolPeriodsComplete++;

                        if (InitialNodeCount == ProtocolPeriodsComplete)
                        {
                            Debug.WriteLine("Shuffing Nodes");
                            Logger.LogInformation("Shuffing Nodes");

                            ShuffleNodes();

                            lock (_nodesLock)
                            {
                                InitialNodeCount = Nodes.Count;
                            }

                            ProtocolPeriodsComplete = 0;
                        }
                    }

                    ReceivedAck = false;
                    ProtocolTimer.Stop();
                    ProtocolTimerRunning = false;
                    PingTimer.Stop();
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"{e.ToString()}: {e.StackTrace}");
                Logger.LogError(e, string.Empty);
            }
        }

        public async Task ProtocolLoop()
        {
            try
            {
                while (true)
                {
                    if (!ProtocolTimerRunning)
                    {
                        await BeginProtocolPeriodAsync();
                    }

                    while (Messages.Any())
                    {
                        MessageBase message = null;

                        if (Messages.TryDequeue(out message))
                        {
                            HandleMessage(message);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"{e.ToString()}: {e.StackTrace}");
                Logger.LogError(e, string.Empty);
            }
        }

        private void HandleMessage(MessageBase message)
        {
            Debug.WriteLine($"Processing {message.MessageType} Message from {message.SourceNode?.Endpoint ?? "unidentified node"}");
            Logger.LogInformation($"Processing {message.MessageType} Message from {message.SourceNode?.Endpoint ?? "unidentified node"}");

            try
            {
                switch (message.MessageType)
                {
                    case MessageType.Composite:
                        {
                            var cm = message as CompositeMessage;

                            if (cm.Messages != null)
                            {
                                foreach (var em in cm.Messages)
                                {
                                    HandleMessage(em);
                                }
                            }
                        }

                        break;
                    case MessageType.Alive:
                        {
                            lock (_nodesLock)
                            {
                                if (!Nodes.Any(x => x == message.SourceNode))
                                {
                                    AddNode(message.SourceNode);
                                }
                            }

                            AddBroadcastMessage(message);
                        }

                        break;
                    case MessageType.Dead:
                        {
                            lock (_nodesLock)
                            {
                                if (Nodes.Any(x => x == message.SourceNode))
                                {
                                    RemoveNode(message.SourceNode);
                                }
                            }

                            AddBroadcastMessage(message);
                        }

                        break;
                    case MessageType.Ping:
                        {
                            lock (_nodesLock)
                            {
                                if (!Nodes.Any(x => x == message.SourceNode))
                                {
                                    AddNode(message.SourceNode);
                                }
                            }

                            if (message.SourceNode != null && ProtocolProvider?.Node?.Endpoint != null)
                            {
                                ProtocolProvider.SendMessage(message.SourceNode,
                                    new AckMessage(message.CorrelationId, ProtocolProvider.Node));
                            }
                        }

                        break;
                    case MessageType.Ack:
                        {
                            if (message.SourceNode == ActiveNode)
                            {
                                lock (_receivedAckLock)
                                {
                                    ReceivedAck = true;
                                }
                            }

                            if (message.CorrelationId.HasValue)
                            {
                                MessageBase ms = null;

                                //  Send message back to originating node.
                                if (CorrelatedMessages.TryRemove(message.CorrelationId.Value, out ms))
                                {
                                    var cm = ms as PingReqMessage;
                                    ProtocolProvider.SendMessage(cm.SourceNode, message);
                                }
                            }
                        }

                        break;
                    case MessageType.PingReq:
                        {
                            var m = message as PingReqMessage;

                            if (m.CorrelationId.HasValue)
                            {
                                CorrelatedMessages.TryAdd(m.CorrelationId.Value, m);
                            }

                            ProtocolProvider.SendMessage(m.Endpoint, new PingMessage(m.CorrelationId.Value) { SourceNode = ProtocolProvider.Node });
                        }

                        break;
                    default:
                        Debug.WriteLine($"Unknown message type {message.MessageType}, skipping...");
                        Logger.LogWarning($"Unknown message type {message.MessageType}, skipping...");

                        break;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"{e.ToString()}: {e.StackTrace}");
                Logger.LogError(e, string.Empty);
            }
        }
    }
}
