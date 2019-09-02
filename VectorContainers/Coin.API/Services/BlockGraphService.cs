using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Core.API.Consensus;
using Core.API.Helper;
using Core.API.LibSodium;
using Core.API.Membership;
using Core.API.Model;
using Core.API.Onion;
using Core.API.Signatures;
using Microsoft.Extensions.Logging;
using Secp256k1_ZKP.Net;

namespace Coin.API.Services
{
    public class BlockGraphService : IBlockGraphService
    {
        private static readonly AsyncLock processBlocksMutex = new AsyncLock();

        private readonly IUnitOfWork unitOfWork;
        private readonly IMembershipServiceClient membershipServiceClient;
        private readonly IOnionServiceClient onionServiceClient;
        private readonly ILogger logger;
        private readonly ITorClient torClient;
        private readonly System.Timers.Timer processBlocksTimer;
        private readonly System.Timers.Timer replayTimer;

        public string Hostname { get; }
        public Graph Graph { get; }
        public Config Config { get; }

        public BlockGraphService(IUnitOfWork unitOfWork, IMembershipServiceClient membershipServiceClient, IOnionServiceClient onionServiceClient, ILogger<BlockGraphService> logger, ITorClient torClient)
        {
            this.unitOfWork = unitOfWork;
            this.membershipServiceClient = membershipServiceClient;
            this.onionServiceClient = onionServiceClient;
            this.logger = logger;
            this.torClient = torClient;

            Hostname = GetHostName();
            Config = new Config(new ulong[Endpoints().Count()], HostNameToInt64(Hostname.ToBytes()));
            Graph = new Graph(Config, BlockmaniaCallback);
            //processBlocksTimer = new System.Timers.Timer
            //{
            //    Interval = 3000
            //};
            replayTimer = new System.Timers.Timer
            {
                Interval = 5000
            };

            //processBlocksTimer.Elapsed += (s, e) => ProcessBlocks(s, e).SwallowException();
            //processBlocksTimer.Start();

            replayTimer.Elapsed += (s, e) => Replay(s, e).SwallowException();
            replayTimer.Start();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="blockGraphProto"></param>
        /// <returns></returns>
        public async Task<BlockGraphProto> AddBlockGraph(BlockGraphProto blockGraphProto)
        {
            if (blockGraphProto == null)
                throw new ArgumentNullException(nameof(blockGraphProto));

            try
            {
                var key = blockGraphProto.Block.SignedBlock.Key;

                var blockId = await unitOfWork.BlockID.Get(key);
                if (blockId != null)
                {
                    logger.LogError($"BlockGraphService: BlockID exists");
                    return null;
                }

                var blockGraphs = await unitOfWork.MemPool.GetMultiple(key);
                if (blockGraphs == null)
                {
                    if (!blockGraphProto.Block.Node.Equals(Graph.Self))
                    {
                        return await BlockIdNotIncluded(blockGraphProto);
                    }

                    var added = await unitOfWork.MemPool.PutMultiple(key, new List<BlockGraphProto> { blockGraphProto });
                    if (added == false)
                    {
                        logger.LogError($"BlockGraphService: Failed to add block graph proto to mem pool");
                        return null;
                    }

                    return blockGraphProto;
                }

                if (blockGraphs != null)
                {
                    if (blockGraphProto.Block.Node.Equals(Graph.Self))
                    {
                        blockGraphProto.Prev = blockGraphs.Last().Block;

                        var list = blockGraphs.ToList();
                        list.Add(blockGraphProto);

                        var added = await unitOfWork.MemPool.PutMultiple(key, list.AsEnumerable());
                        if (added == false)
                        {
                            logger.LogError($"BlockGraphService: Failed to add block graph proto to mem pool");
                            return null;
                        }

                        //return memBlockGraph;
                    }
                }

                //if (!blockGraphProto.Block.Node.Equals(Graph.Self))
                //{
                //    if (memBlockGraph.Deps?.Any() != true)
                //    {
                //        memBlockGraph.Deps = new List<DepProto>();
                //    }

                //    memBlockGraph.Deps.Add(
                //        new DepProto
                //        {
                //            Block = blockGraphProto.Block,
                //            Deps = blockGraphProto.Deps.Select(d => d.Block).ToList(),
                //            Prev = blockGraphProto.Prev ?? null
                //        });

                //    var addedMemBlockGraph = await unitOfWork.MemPool.Put(key, memBlockGraph);
                //    if (addedMemBlockGraph == false)
                //    {
                //        logger.LogError($"BlockGraphService: Failed to add block graph proto to mem pool");
                //        return null;
                //    }

                //    return memBlockGraph;
                //}
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }

           return blockGraphProto;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public async Task<BlockIDProto> GetBlockID(byte[] address)
        {
            BlockIDProto blockId = null;

            try
            {
                blockId = await unitOfWork.BlockID.Get(address);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }

            return blockId;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<List<BlockIDProto>> AllBlockIDs()
        {
            List<BlockIDProto> blockIds = null;

            try
            {
                blockIds = await unitOfWork.BlockID.All();
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }

            return blockIds;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="coin"></param>
        /// <returns></returns>
        public bool ValidateRule(CoinProto coin)
        {
            if (coin == null)
                throw new ArgumentNullException(nameof(coin));

            var coinHasElements = coin.Validate().Any();

            if (!coinHasElements)
            {
                try
                {
                    using (var secp256k1 = new Secp256k1())
                    using (var bulletProof = new BulletProof())
                    {
                        var success = bulletProof.Verify(coin.Commitment, coin.RangeProof, null);
                        if (!success)
                            return false;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.Message);
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="coin"></param>
        /// <returns></returns>
        public async Task<BlockGraphProto> Sign(CoinProto coin, uint round)
        {
            if (coin == null)
                throw new ArgumentNullException(nameof(coin));

            if (round == 0)
                throw new ArgumentOutOfRangeException(nameof(round));

            try
            {
                var blockHash = BlockHash(coin, round, GetPublicKey());
                var coinHash = HashCoin(coin, GetPublicKey());
                var combinedHash = Util.Combine(blockHash, coinHash);
                var signedHash = await onionServiceClient.SignHashAsync(combinedHash);

                return new BlockGraphProto
                {
                    Block = new BlockIDProto
                    {
                        Hash = coin.Stamp.ToStr(),
                        Node = Graph.Self,
                        Round = round,
                        SignedBlock = new BlockProto
                        {
                            Key = coin.Stamp,
                            Coin = coin,
                            PublicKey = signedHash.PublicKey,
                            Signature = signedHash.Signature
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="blockIDProto"></param>
        /// <returns></returns>
        public bool VerifiySignature(BlockIDProto blockIDProto)
        {
            if (blockIDProto == null)
                throw new ArgumentNullException(nameof(blockIDProto));

            bool result = false;

            try
            {
                var signedBlock = blockIDProto.SignedBlock;
                var blockHash = BlockHash(signedBlock.Coin, (uint)blockIDProto.Round, signedBlock.PublicKey);
                var coinHash = HashCoin(signedBlock.Coin, signedBlock.PublicKey);
                var combinedHash = Util.Combine(blockHash, coinHash);

                result = Ed25519.Verify(signedBlock.Signature, combinedHash, signedBlock.PublicKey);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }

            return result;
        }

        /// <summary>
        /// Verifiies the coin chain.
        /// </summary>
        /// <returns><c>true</c>, if coin chain was verifiyed, <c>false</c> otherwise.</returns>
        /// <param name="previous">Previous coin</param>
        /// <param name="next">Next coin.</param>
        public bool VerifiyHashChain(CoinProto previous, CoinProto next)
        {
            if (previous == null)
                throw new ArgumentNullException(nameof(previous));

            if (next == null)
                throw new ArgumentNullException(nameof(next));

            bool validH = false, validK = false;

            try
            {
                var hint = Cryptography.GenericHashNoKey($"{next.Version} {next.Stamp.ToStr()} {next.Principle.ToStr()}").ToHex();
                var keeper = Cryptography.GenericHashNoKey($"{next.Version} {next.Stamp.ToStr()} {next.Hint.ToStr()}").ToHex();

                validH = previous.Hint.ToStr().Equals(hint);
                validK = previous.Keeper.ToStr().Equals(keeper);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }

            return validH && validK;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<BlockIDProto> GetPrevBlockID(byte[] address)
        {
            if (address == null)
                throw new ArgumentNullException(nameof(address));

            BlockIDProto blockIDProto = null;

            try
            {
                var blockIDs = await unitOfWork.BlockID.Search(address);
                if (blockIDs != null)
                {
                    if (blockIDs.LastOrDefault().SignedBlock != null)
                    {
                        var isVerified = VerifiySignature(blockIDs.LastOrDefault());
                        if (!isVerified)
                        {
                            return null;
                        }
                    }
                }

                blockIDProto = blockIDs.LastOrDefault();
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }

            return blockIDProto;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="blockGraphProtos"></param>
        public void Broadcast(IEnumerable<BlockGraphProto> blockGraphProtos)
        {
            if (blockGraphProtos?.Any() != true)
                throw new ArgumentNullException(nameof(blockGraphProtos));

            _ = Task.Factory.StartNew(async () =>
            {
                try
                {
                    var tasks = new List<Task<HttpResponseMessage>>();
                    var batchSize = 100;
                    var numberOfBatches = (int)Math.Ceiling((double)blockGraphProtos.Count() / batchSize);
                    var endPoints = Endpoints().Where(x => !x.Equals(Hostname));

                    for (int ep = 0; ep < endPoints.Count(); ep++)
                    {
                        var uri = new Uri(new Uri(endPoints.ElementAt(ep)), "blockgraph");

                        for (int n = 0; n < numberOfBatches; n++)
                        {
                            var currentBlockGraphs = Util.SerializeProto(blockGraphProtos.Skip(n * batchSize).Take(batchSize));
                            tasks.Add(torClient.PostAsJsonAsync(uri, currentBlockGraphs));
                        }
                    }

                    await Task.WhenAll(tasks);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.Message);
                }
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> Endpoints() => membershipServiceClient.GetMembers().Select(x => x.Endpoint);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public long BlockHeight()
        {
            long blockHeight = 0;

            try
            {
                blockHeight = unitOfWork.BlockID.Count();
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }

            return blockHeight;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="coin"></param>
        /// <param name="round"></param>
        /// <param name="publicKey"></param>
        /// <returns></returns>
        private byte[] BlockHash(CoinProto coin, uint round, byte[] publicKey)
        {
            return Cryptography.GenericHashWithKey($"{coin.Stamp.ToStr()}{Graph.Self}{round}", publicKey);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="coin"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        private byte[] HashCoin(CoinProto coin, byte[] key = null)
        {
            var serialized = Util.SerializeProto(coin);
            var hash = key == null ? Cryptography.GenericHashNoKey(serialized) : Cryptography.GenericHashWithKey(serialized, key);

            return hash;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private async Task Replay(object sender, System.Timers.ElapsedEventArgs e)
        {
            replayTimer.Stop();

            try
            {
                var rangeBlockGraphTasks = new List<Task<IEnumerable<IEnumerable<BlockGraphProto>>>>();
                var addSignedBlockGraphTasks = new List<Task<BlockGraphProto>>();
                var batchSize = 100;
                var numberOfBatches = (int)Math.Ceiling((double)unitOfWork.MemPool.Count() / batchSize);

                for (int n = 0; n < numberOfBatches; n++)
                    rangeBlockGraphTasks.Add(unitOfWork.MemPool.GetRangeMultiple(n * batchSize, batchSize));

                var replayList = new List<BlockGraphProto>();

                var graphs = (await Task.WhenAll(rangeBlockGraphTasks)).SelectMany(x => x).Select(x => x.Where(c => c.Block.Node.Equals(Graph.Self)));

                graphs.ForEach(ghs =>
                {
                    ghs.ForEach(x => {

                        var round = x.Block.Round++;
                        var signedBlockGraph = Sign(x.Block.SignedBlock.Coin, (uint)round).GetAwaiter().GetResult();
                        if (signedBlockGraph == null)
                        {
                            logger.LogError($"BlockGraphService: Could not sign Replay block");
                            return;
                        }

                        //signedBlockGraph.Prev = x.Prev ?? null;

                        addSignedBlockGraphTasks.Add(AddBlockGraph(signedBlockGraph));

                        replayList.Add(signedBlockGraph);
                    });
                });

                Broadcast(replayList);

                await Task.WhenAll(addSignedBlockGraphTasks);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }

            replayTimer.Start();

            return;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="blockGraphProto"></param>
        /// <returns></returns>
        private async Task<BlockGraphProto> BlockIdNotIncluded(BlockGraphProto blockGraphProto)
        {
            if (blockGraphProto == null)
                throw new ArgumentNullException(nameof(blockGraphProto));

            try
            {
                var key = blockGraphProto.Block.SignedBlock.Key;
                var notIncluded = await unitOfWork.MemPool.Get(key);

                if (notIncluded == null)
                {
                    //var coinProto = Util.DeserializeProto<CoinProto>(blockGraphProto.Block.SignedBlock.Coin);
                    var signedBlockGraph = await Sign(blockGraphProto.Block.SignedBlock.Coin, 1);
                    if (signedBlockGraph == null)
                    {
                        logger.LogError($"BlockGraphService: Could not sign NotInclueded block");
                        return null;
                    }

                    if (blockGraphProto.Deps?.Any() != true)
                    {
                        signedBlockGraph.Deps = new List<DepProto>
                    {
                        new DepProto
                        {
                            Block = blockGraphProto.Block,
                            Prev = blockGraphProto.Prev ?? null
                        }
                    };
                    }
                    else
                    {
                        signedBlockGraph.Deps.Add(
                            new DepProto
                            {
                                Block = blockGraphProto.Block,
                                Deps = blockGraphProto.Deps.Select(d => d.Block).ToList(),
                                Prev = blockGraphProto.Prev ?? null
                            });
                    }

                    var addedNotIncluded = await unitOfWork.MemPool.Put(key, signedBlockGraph);
                    if (addedNotIncluded == false)
                    {
                        logger.LogError($"BlockGraphService: Could not add NotInclueded block");
                        return null;
                    }

                    return signedBlockGraph;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }

            return blockGraphProto;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hostname"></param>
        /// <returns></returns>
        private ulong HostNameToInt64(byte[] hostname) => (ulong)BitConverter.ToInt64(hostname, 0);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private string GetHostName()
        {
            var hostname = onionServiceClient.GetHiddenServiceDetailsAsync().Result.Hostname;
            return hostname.Substring(0, hostname.Length - 6);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private byte[] GetPublicKey()
        {
            var pubKey = onionServiceClient.GetHiddenServiceDetailsAsync().Result.PublicKey;
            return pubKey;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="interpreted"></param>
        private void BlockmaniaCallback(Interpreted interpreted)
        {
            Console.WriteLine($"Interpreted round is {interpreted.Round}");

            _ = Task.Factory.StartNew(async () =>
            {
                try
                {
                    var blocks = interpreted.Blocks.GroupBy(x => x.Hash).Select(x => x.First());
                    foreach (var block in blocks)
                    {
                        var blockId = await unitOfWork.BlockID.Get(block.SignedBlock.Key);
                        if (blockId != null)
                        {
                            logger.LogError($"BlockGraphService: BlockID exists");
                            continue;
                        }

                        var blockIdProto = new BlockIDProto { Hash = block.Hash, Node = block.Node, Round = block.Round, SignedBlock = block.SignedBlock };
                        if (!VerifiySignature(blockIdProto))
                        {
                            logger.LogError($"BlockGraphService: unable to verify signature for block {block.Round} from node {block.Node}");
                            continue;
                        }

                        //var coinProto = Util.DeserializeProto<CoinProto>(blockIdProto.SignedBlock.Coin);
                        var coinRule = ValidateRule(blockIdProto.SignedBlock.Coin);
                        if (!coinRule)
                        {
                            logger.LogError($"BlockGraphService: unable to validate coin rule for block {block.Round} from node {block.Node}");
                            continue;
                        }

                        var coins = await unitOfWork.BlockID.Search(blockIdProto.SignedBlock.Key);
                        if (coins?.Any() == true)
                        {
                            var list = coins.ToList();
                            for (int i = 0; i < list.Count; i++)
                            {
                                CoinProto previous;
                                CoinProto next;

                                try
                                {
                                    previous = list[(i - 1) % (list.Count - 1)].SignedBlock.Coin; // Util.DeserializeProto<CoinProto>(list[(i - 1) % (list.Count - 1)].SignedBlock.Coin);
                                }
                                catch (DivideByZeroException)
                                {
                                    previous = list[i].SignedBlock.Coin; // Util.DeserializeProto<CoinProto>(list[i].SignedBlock.Coin);
                                }

                                var previousRule = ValidateRule(previous);
                                if (!previousRule)
                                {
                                    logger.LogError($"BlockGraphService: unable to validate coin rule for block {block.Round} from node {block.Node}");
                                    return;
                                }

                                try
                                {
                                    next = list[(i + 1) % (list.Count - 1)].SignedBlock.Coin; // Util.DeserializeProto<CoinProto>(list[(i + 1) % (list.Count - 1)].SignedBlock.Coin);

                                    var nextRule = ValidateRule(next);
                                    if (!nextRule)
                                    {
                                        logger.LogError($"BlockGraphService: unable to validate coin rule for block {block.Round} from node {block.Node}");
                                        return;
                                    }
                                }
                                catch (DivideByZeroException)
                                {
                                    next = blockIdProto.SignedBlock.Coin; // coinProto;
                                }

                                if (!VerifiyHashChain(previous, next))
                                {
                                    logger.LogError($"BlockGraphService: Could not verifiy hash chain for Interpreted BlockID");
                                    return;
                                }
                            }
                        }

                        var addedBlockId = await unitOfWork.BlockID.Put(blockIdProto.SignedBlock.Key, blockIdProto);
                        if (addedBlockId == false)
                        {
                            logger.LogError($"BlockGraphService: Could not add Interpreted BlockID");
                            continue;
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.Message);
                }
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async Task ProcessBlocks(object sender, System.Timers.ElapsedEventArgs e)
        {
            processBlocksTimer.Stop();

            using (processBlocksMutex.LockAsync().GetAwaiter().GetResult())
            {
                try
                {
                    var rangeBlockGraphTasks = new List<Task<IEnumerable<BlockGraphProto>>>();
                    var batchSize = 100;
                    var numberOfBatches = (int)Math.Ceiling((double)unitOfWork.MemPool.Count() / batchSize);

                    for (int n = 0; n < numberOfBatches; n++)
                        rangeBlockGraphTasks.Add(unitOfWork.MemPool.GetRange(n * batchSize, batchSize));

                    var graphs = (await Task.WhenAll(rangeBlockGraphTasks)).SelectMany(x => x);

                    graphs.ForEach(x =>
                    {
                        if (!VerifiySignature(x.Block))
                        {
                            logger.LogError($"BlockGraphService: unable to verify signature for block {x.Block.Round} from node {x.Block.Node}");
                            return;
                        }

                        if (x.Prev != null && x.Prev.Round != 0)
                        {
                            if (!VerifiySignature(x.Prev))
                            {
                                logger.LogError($"BlockGraphService: unable to verify signature for previous block on block {x.Block.Round} from node {x.Block.Node}");
                                return;
                            }

                            if (x.Prev.Node != x.Block.Node)
                            {
                                logger.LogError($"BlockGraphService: previous block node does not match on block {x.Block.Round} from node {x.Block.Node}");
                                return;
                            }

                            if (x.Prev.Round + 1 != x.Block.Round)
                            {
                                logger.LogError($"BlockGraphService: previous block round is invalid on block {x.Block.Round} from node {x.Block.Node}");
                                return;
                            }
                        }

                        for (int i = 0; i < x.Deps.Count(); i++)
                        {
                            var dep = x.Deps[i];

                            if (!VerifiySignature(dep.Block))
                            {
                                logger.LogError($"BlockGraphService: unable to verify signature for block reference {x.Block.Round} from node {x.Block.Node}");
                                return;
                            }

                            if (dep.Block.Node == x.Block.Node)
                            {
                                logger.LogError($"BlockGraphService: block references includes a block from same node in block reference  {x.Block.Round} from node {x.Block.Node}");
                                return;
                            }
                        }

                        Graph.Add(x.ToBlockGraph());
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.Message);
                }
            }

            processBlocksTimer.Start();
        }
    }
}
