// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;
using TGMCore.Consensus.Messages;
using TGMCore.Consensus.States;
using System.Diagnostics;

namespace TGMCore.Consensus
{
    using StateKV = Dictionary<StateData, object>;

    public class Entry
    {
        public BlockID Block;
        public BlockID[] Deps;
        public BlockID Prev;

        public Entry(BlockID prev)
        {
            Prev = prev;
        }

        public Entry(BlockID block, BlockID prev)
        {
            Block = block;
            Prev = prev;
        }

        public Entry(BlockID block, BlockID[] deps, BlockID prev)
        {
            Block = block;
            Deps = deps;
            Prev = prev;
        }
    }

    public class BlockInfo
    {
        public ulong Max;
        public BlockGraph Data;

        public BlockInfo(BlockGraph data, ulong max)
        {
            Data = data;
            Max = max;
        }
    }

    public class Consensus
    {
        public string Hash { get; }
        public ulong Node { get; }
        public ulong Round { get; }

        public Consensus(string hash, ulong node, ulong round)
        {
            Hash = hash;
            Node = node;
            Round = round;
        }
    }

    public class Config
    {
        public ulong LastInterpreted;
        public ulong[] Nodes;
        public ulong SelfID;
        public ulong TotalNodes;

        public Config(ulong[] nodes, ulong id)
        {
            Nodes = nodes;
            SelfID = id;
            TotalNodes = (ulong)nodes.Length;
        }

        public Config(ulong lastInterpreted, ulong[] nodes, ulong id, ulong totalNodes)
        {
            LastInterpreted = lastInterpreted;
            Nodes = nodes;
            SelfID = id;
            TotalNodes = totalNodes;
        }
    }

    public class Graph
    {
        private readonly Mutex GraphMutex;
        private readonly Channel<BlockGraph> Entries;

        protected virtual void OnBlockmaniaInterpreted(Interpreted e)
        {
            BlockmaniaInterpreted?.Invoke(this, e);
        }

        public event EventHandler<Interpreted> BlockmaniaInterpreted;

        public List<BlockInfo> Blocks;
        public Func<Task<Interpreted>> action;
        public Dictionary<BlockID, ulong> Max;
        public int NodeCount;
        public ulong[] Nodes;
        public int Quorumf1;
        public int Quorum2f;
        public int Quorum2f1;
        public Dictionary<ulong, Dictionary<ulong, string>> Resolved;
        public ulong Round;
        public ulong Self;
        public Dictionary<BlockID, State> Statess;
        public ulong TotalNodes;
        public List<Consensus> Consensus;

        public Graph()
        {
            Blocks = new List<BlockInfo>();
            Max = new Dictionary<BlockID, ulong>();
            Resolved = new Dictionary<ulong, Dictionary<ulong, string>>();
            Statess = new Dictionary<BlockID, State>();
            GraphMutex = new Mutex();
            Consensus = new List<Consensus>();
        }

        public Graph(Config cfg)
        {
            var f = (cfg.Nodes.Length - 1) / 3;
            Blocks = new List<BlockInfo>();
            Max = new Dictionary<BlockID, ulong>();
            NodeCount = cfg.Nodes.Length;
            Nodes = cfg.Nodes;
            Quorumf1 = f + 1;
            Quorum2f = 2 * f;
            Quorum2f1 = 2 * f + 1;
            Resolved = new Dictionary<ulong, Dictionary<ulong, string>>();
            Round = cfg.LastInterpreted + 1;
            Self = cfg.SelfID;
            Statess = new Dictionary<BlockID, State>();
            TotalNodes = cfg.TotalNodes;
            GraphMutex = new Mutex();
            Entries = Channel.CreateBounded<BlockGraph>(10000);
            Consensus = new List<Consensus>();

            _ = Task.Factory.StartNew(async () =>
            {
                await Run(Entries.Reader);
            });
        }

        private void Deliver(ulong node, ulong round, string hash)
        {
            if (round < Round)
            {
                return;
            }
            var hashes = new Dictionary<ulong, string>();
            if (Resolved.ContainsKey(round))
            {
                hashes = Resolved[round];
                if (hashes.ContainsKey(node))
                {
                    var curHash = hashes[node];
                    if (curHash != hash)
                    {
                        Debug.WriteLine($"Mismatching block hash for delivery, node={node} round={round}");
                    }
                }
                else
                {
                    Console.WriteLine($"Consensus achieved hash={hash} node={node} round={round}");
                    Consensus.Add(new Consensus(hash, node, round));
                    hashes[node] = hash;
                }
            }
            else
            {
                hashes = new Dictionary<ulong, string>
                {
                    {node, hash}
                };

                Resolved[round] = hashes;
            }
            if (round != Round)
            {
                return;
            }
            if (hashes.Count == NodeCount)
            {
                DeliverRound(round, hashes);
            }
        }

        private void DeliverRound(ulong round, Dictionary<ulong, string> hashes)
        {
            var blocks = new List<BlockID>();
            foreach (KeyValuePair<ulong, string> item in hashes)
            {
                if (string.IsNullOrEmpty(item.Value))
                {
                    continue;
                }
                blocks.Add(new BlockID(item.Value, item.Key, round));
            }
            blocks.Sort((x, y) => string.Compare(x.Hash, y.Hash, StringComparison.Ordinal));
            Resolved.Remove(round);
            GraphMutex.WaitOne();
            var consumed = Blocks[0].Data.Block.Round - 1;
            var idx = 0;
            for (var i = 0; i < Blocks.Count; i++)
            {
                var info = Blocks[i];
                if (info.Max > round)
                {
                    break;
                }
                Max.Remove(info.Data.Block);
                Statess.Remove(info.Data.Block);
                foreach (var dep in info.Data.Deps)
                {
                    Max.Remove(dep.Block);
                    Statess.Remove(dep.Block);
                }
                consumed++;
                idx = i + 1;
            }
            if (idx > 0)
            {
                Blocks = Blocks.GetRange(idx, Blocks.Count - idx);
            }
            Round++;
            Debug.WriteLine($"Mem usage: g.max={Max.Count} g.statess={Statess.Count} g.blocks={Blocks.Count}");
            GraphMutex.ReleaseMutex();
            OnBlockmaniaInterpreted(new Interpreted(blocks, consumed, round));
            if (Resolved.ContainsKey(round + 1) && hashes.Count == NodeCount)
            {
                hashes = Resolved[round + 1];
                DeliverRound(round + 1, hashes);
            }
        }

        private State FindOrCreateState(Entry e)
        {
            State stat;
            if (e.Prev.Valid())
            {
                if (!Statess.ContainsKey(e.Prev))
                {
                    stat = new State(new Dictionary<ulong, List<Timeout>>()).Clone(Round);
                }
                else
                {
                    stat = Statess[e.Prev].Clone(Round);
                }
            }
            else
            {
                stat = new State(new Dictionary<ulong, List<Timeout>>());
            }
            return stat;
        }

        private void Process(Entry ntry)
        {
            Debug.WriteLine($"Interpreting block block.id={ntry.Block}");

            var state = FindOrCreateState(ntry);
            var node = ntry.Block.Node;
            var round = ntry.Block.Round;
            var hash = ntry.Block.Hash;
            var out_ = new List<IMessage>
            {
                new PrePrepare(hash, node, round, 0)
            };
            if (ntry.Deps.Length > 0)
            {
                if (state.Delay == null)
                {
                    state.Delay = new Dictionary<ulong, ulong>();
                }
                foreach (var dep in ntry.Deps)
                {
                    state.Delay[dep.Node] = Util.Diff(round, dep.Round) * 10;
                }
            }
            var tval = 10UL;

            if (state.Delay.Count > Quorum2f1)
            {
                var vals = new ulong[state.Delay.Count];
                var i = 0;
                foreach (KeyValuePair<ulong, ulong> item in state.Delay)
                {
                    vals[i] = item.Value;
                    i++;
                }
                Array.Sort(vals);

                var xval = vals[Quorum2f];
                if (xval > tval)
                {
                    tval = xval;
                }
            }
            else
            {
                foreach (KeyValuePair<ulong, ulong> item in state.Delay)
                {
                    var val = item.Value;
                    if (val > tval)
                    {
                        tval = val;
                    }
                }
            }
            state.Timeout = tval;
            var tround = round + tval;
            foreach (var xnode in Nodes)
            {
                if (!state.Timeouts.ContainsKey(tround) || state.Timeouts[tround] == null)
                {
                    state.Timeouts[tround] = new List<Timeout>();
                }
                state.Timeouts[tround].Add(new Timeout(xnode, round, 0));
            }

            if (state.Timeouts.ContainsKey(round))
            {
                foreach (var tmout in state.Timeouts[round])
                {
                    if (state.Data.ContainsKey(new Final(tmout.Node, tmout.Round)))
                    {
                        continue;
                    }
                    uint v = 0;
                    var skey = new View(tmout.Node, tmout.Round);
                    if (state.Data.ContainsKey(skey))
                    {
                        v = (uint)state.Data[skey];
                    }
                    if (v > tmout.View)
                    {
                        continue;
                    }
                    var hval = "";
                    var skey2 = new Prepared(tmout.Node, tmout.Round, tmout.View);
                    if (state.Data.ContainsKey(skey2))
                    {
                        hval = (string)state.Data[skey2];
                    }
                    if (state.Data == null)
                    {
                        state.Data = new StateKV { { skey, v + 1 } };
                    }
                    else
                    {
                        state.Data[skey] = v + 1;
                    }
                    out_.Add(new ViewChange(hval, tmout.Node, tmout.Round, node, tmout.View + 1));
                }
            }

            var idx = out_.Count;
            var processed = new Dictionary<IMessage, bool>();
            out_.AddRange(ProcessMessages(state, processed, node, node, ntry.Block, out_.GetRange(0, idx)));
            foreach (var dep in ntry.Deps)
            {
                Debug.WriteLine($"Processing block dep block.id={dep}");

                List<IMessage> output = new List<IMessage>();
                if (Statess.ContainsKey(dep) && Statess[dep] != null)
                {
                    output = Statess[dep].GetOutPut();
                }
                out_.AddRange(ProcessMessages(state, processed, dep.Node, node, ntry.Block, output));
            }
            state.Out = out_;
            Statess[ntry.Block] = state;
        }

        private IMessage ProcessMessage(State s, ulong sender, ulong receiver, BlockID origin, IMessage msg)
        {
            var (node, round) = msg.NodeRound();
            if (s.Data.ContainsKey(new Final(node, round)))
            {
                return null;
            }
            var v = s.GetView(node, round);

            Debug.WriteLine($"Processing message from block block.id={origin} message={msg}");

            switch (msg)
            {
                case PrePrepare m:
                    if (v != m.View)
                    {
                        return null;
                    }
                    var pp = new PrePrepared(node, round, m.View);
                    if (s.Data.ContainsKey(pp))
                    {
                        return null;
                    }
                    ulong size = sender > receiver ? sender : receiver;
                    // var b = s.GetBitSet(NodeCount, m);
                    var b = s.GetBitSet((int)size, m);
                    b.SetPrepare(sender);
                    b.SetPrepare(receiver);
                    if (s.Data == null)
                    {
                        s.Data = new StateKV() { { pp, m } };
                    }
                    else
                    {
                        s.Data[pp] = m;
                    }
                    return new Prepare(m.Hash, node, round, receiver, m.View);
                case Prepare m:
                    if (v > m.View)
                    {
                        return null;
                    }
                    if (v < m.View)
                    {
                        // b = s.GetBitSet(NodeCount, m.Pre());
                        b = s.GetBitSet((int)sender, m.Pre());
                        b.SetPrepare(m.Sender);
                        return null;
                    }
                    // b = s.GetBitSet(NodeCount, m.Pre());
                    b = s.GetBitSet((int)sender, m.Pre());
                    b.SetPrepare(m.Sender);

                    Debug.WriteLine($"Prepare count == {b.PrepareCount()}");

                    if (b.PrepareCount() != Quorum2f1)
                    {
                        return null;
                    }
                    if (b.HasCommit(receiver))
                    {
                        return null;
                    }
                    b.SetCommit(receiver);
                    var p = new Prepared(node, round, m.View);
                    if (!s.Data.ContainsKey(p))
                    {
                        if (s.Data == null)
                        {
                            s.Data = new StateKV() { { p, m.Hash } };
                        }
                        else
                        {
                            s.Data[p] = m.Hash;
                        }
                    }
                    return new Commit(m.Hash, node, round, receiver, m.View);
                case Commit m:
                    if (v < m.View)
                    {
                        return null;
                    }
                    // b = s.GetBitSet(NodeCount, m.Pre());
                    b = s.GetBitSet((int)sender, m.Pre()); 
                    b.SetCommit(m.Sender);

                    Debug.WriteLine($"Commit count == {b.CommitCount()}");

                    if (b.CommitCount() != Quorum2f1)
                    {
                        return null;
                    }
                    var nr = new NodeRound(node, round);
                    if (s.Final.ContainsKey(nr))
                    {
                        return null;
                    }
                    if (s.Final == null)
                    {
                        s.Final = new Dictionary<NodeRound, string>() { { nr, m.Hash } };
                    }
                    else
                    {
                        s.Final[nr] = m.Hash;
                    }
                    Deliver(node, round, m.Hash);
                    return null;
                case ViewChange m:
                    if (v > m.View)
                    {
                        return null;
                    }
                    Dictionary<ulong, string> vcs;
                    var key = new ViewChanged(node, round, v);
                    if (s.Data.ContainsKey(key))
                    {
                        var val = s.Data[key];
                        vcs = (Dictionary<ulong, string>)val;
                    }
                    else
                    {
                        vcs = new Dictionary<ulong, string>();
                        if (s.Data == null)
                        {
                            s.Data = new StateKV() { { key, vcs } };
                        }
                        else
                        {
                            s.Data[key] = vcs;
                        }
                    }
                    vcs[m.Sender] = m.Hash;
                    if (vcs.Count != Quorum2f1)
                    {
                        return null;
                    }
                    s.Data[new View(node, round)] = m.View;
                    var hash = "";
                    foreach (KeyValuePair<ulong, string> item in vcs)
                    {
                        var hval = item.Value;
                        if (hval != "")
                        {
                            if (hash != "" && hval != hash)
                            {
                                Console.WriteLine($"Got multiple hashes in a view change node.id={node} round={round} hash={hash} hash.alt={hval}");
                            }
                            hash = hval;
                        }
                    }
                    return new NewView(hash, node, round, receiver, m.View);
                case NewView m:
                    if (v > m.View)
                    {
                        return null;
                    }
                    var viewKey = new Hnv(node, round, m.View);
                    if (s.Data.ContainsKey(viewKey))
                    {
                        return null;
                    }
                    if (s.Data == null)
                    {
                        s.Data = new StateKV();
                    }
                    s.Data[new View(node, round)] = m.View;
                    var tval = origin.Round + s.Timeout + 5;
                    if (!s.Timeouts.ContainsKey(tval))
                    {
                        s.Timeouts[tval] = new List<Timeout>();
                    }
                    s.Timeouts[tval].Add(new Timeout(node, round, m.View));
                    s.Data[viewKey] = true;
                    return new PrePrepare(m.Hash, node, round, m.View);
                default:
                    throw new Exception($"blockmania: unknown message kind to process: {msg.Kind()}");
            }
        }

        private List<IMessage> ProcessMessages(State s, Dictionary<IMessage, bool> processed, ulong sender, ulong receiver, BlockID origin, List<IMessage> msgs)
        {
            var out_ = new List<IMessage>();
            foreach (var msg in msgs)
            {
                if (processed.ContainsKey(msg) && processed[msg])
                {
                    continue;
                }
                var resp = ProcessMessage(s, sender, receiver, origin, msg);
                processed[msg] = true;
                if (resp != null)
                {
                    out_.Add(resp);
                }
            }
            for (var i = 0; i < out_.Count; i++)
            {
                var msg = out_[i];
                if (processed.ContainsKey(msg) && processed[msg])
                {
                    continue;
                }
                var resp = ProcessMessage(s, sender, receiver, origin, msg);
                processed[msg] = true;
                if (resp != null)
                {
                    out_.Add(resp);
                }
            }
            return out_;
        }

        private async Task Run(ChannelReader<BlockGraph> reader)
        {
            while (await reader.WaitToReadAsync())
            {
                while (reader.TryRead(out var data))
                {
                    var entries = new Entry[data.Deps.Count];
                    GraphMutex.WaitOne();
                    var max = data.Block.Round;
                    var round = Round;
                    for (var i = 0; i < data.Deps.Count; i++)
                    {
                        var dep = data.Deps[i];

                        Debug.WriteLine($"Dep: block.id={dep.Block}");

                        var depMax = dep.Block.Round;
                        var rcheck = false;
                        var e = new Entry(dep.Block, dep.Prev);
                        if (dep.Block.Round != 1)
                        {
                            e.Deps = new BlockID[dep.Deps.Count + 1];
                            e.Deps[0] = dep.Prev;
                            Array.Copy(dep.Deps.ToArray(), 0, e.Deps, 1, dep.Deps.Count);
                            var prevMax = (ulong)0;
                            if (Max.ContainsKey(dep.Prev))
                            {
                                prevMax = Max[dep.Prev];
                                rcheck = true;
                            }
                            else if (prevMax > depMax)
                            {
                                depMax = prevMax;
                            }
                        }
                        else
                        {
                            e.Deps = dep.Deps.ToArray();
                        }
                        entries[i] = e;
                        foreach (var link in dep.Deps)
                        {
                            if (!Max.ContainsKey(link))
                            {
                                rcheck = true;
                            }
                            else
                            {
                                var linkMax = Max[link];
                                if (linkMax > depMax)
                                {
                                    depMax = linkMax;
                                }
                            }
                        }
                        if (rcheck && round > depMax)
                        {
                            depMax = round;
                        }
                        Max[dep.Block] = depMax;
                        if (depMax > max)
                        {
                            max = depMax;
                        }
                    }
                    var rcheck2 = false;
                    if (data.Block.Round != 1)
                    {
                        if (!Max.ContainsKey(data.Prev))
                        {
                            rcheck2 = true;
                        }
                        else
                        {
                            var pmax = Max[data.Prev];
                            if (pmax > max)
                            {
                                max = pmax;
                            }
                        }
                    }
                    if (rcheck2 && round > max)
                    {
                        max = round;
                    }
                    Max[data.Block] = max;
                    Blocks.Add(new BlockInfo(data, max));
                    GraphMutex.ReleaseMutex();
                    foreach (var e in entries)
                    {
                        Process(e);
                    }
                    var self = new Entry(data.Block, data.Prev)
                    {
                        Deps = new BlockID[data.Deps.Count + 1]
                    };
                    self.Deps[0] = data.Prev;
                    for (var i = 0; i < data.Deps.Count; i++)
                    {
                        var dep = data.Deps[i];
                        self.Deps[i + 1] = dep.Block;
                    }
                    Process(self);
                }
            }
        }

        public void Add(BlockGraph data)
        {
            Debug.WriteLine($"Adding block to graph block.id={data.Block}");

            var task = Task.Factory.StartNew(async () =>
            {
                await Entries.Writer.WriteAsync(data);
            });

            task.Wait();
        }
    }
}
