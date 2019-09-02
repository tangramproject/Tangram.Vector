using System;
using System.Collections.Generic;
using Xunit;

namespace Core.API.Consensus.Tests
{
    public class GraphTest
    {
        [Fact]
        public void OneNodeTest()
        {
            var nodes = new ulong[] { 1 };
            var cfg = new Config(nodes, 1);

            void cb(Interpreted x)
            {
                Console.WriteLine($"Interpreted round is {x.Round}");
            }

            var graph = new Graph(cfg, cb);

            var block1 = new BlockGraph(new BlockID("foofoofoofoo", 1, 1));
            var block2 = new BlockGraph(new BlockID("foofoofoofoo", 1, 2), block1.Block);
            var block3 = new BlockGraph(new BlockID("foofoofoofoo", 1, 3), block2.Block);
            var block4 = new BlockGraph(new BlockID("foofoofoofoo", 1, 4), block3.Block);

            graph.Add(block1);
            graph.Add(block2);
            graph.Add(block3);
            graph.Add(block4);
        }

        [Fact]
        public void TwoNodeTest()
        {
            var nodes = new ulong[] { 1, 2 };
            var cfg = new Config(nodes, 1);

            void cb(Interpreted x)
            {
                Console.WriteLine($"Interpreted round is {x.Round}");
            }

            var graph = new Graph(cfg, cb);

            var block1 = new BlockGraph(new BlockID("foofoofoofoo", 1, 1));
            var block2 = new BlockGraph(new BlockID("foofoofoofoo", 1, 2), new List<Dep> {
                new Dep(new BlockID("foofoofoofoo", 2, 1))
            }, block1.Block);
            var block3 = new BlockGraph(new BlockID("foofoofoofoo", 1, 3), block2.Block);
            var block4 = new BlockGraph(new BlockID("foofoofoofoo", 1, 4), new List<Dep> {
                new Dep(
                    new BlockID("foofoofoofoo", 2, 2),
                    new List<BlockID> {
                        new BlockID("foofoofoofoo", 1, 1),
                        new BlockID("foofoofoofoo", 1, 2)
                    },
                    new BlockID("foofoofoofoo", 2, 1)
                )
            }, block3.Prev);

            graph.Add(block1);
            graph.Add(block2);
            graph.Add(block3);
            graph.Add(block4);
        }

        [Fact]
        public void ThreeNodeTest()
        {
            var nodes = new ulong[] { 1, 2, 3, 4 };
            var cfg = new Config(nodes, 1);
            void cb(Interpreted x)
            {
                Console.WriteLine($"Interpreted round is {x.Round}");
            }
            var graph = new Graph(cfg, cb);

            var block1 = new BlockGraph(new BlockID("foofoofoofoo", 1, 1));
            var block2 = new BlockGraph(new BlockID("foofoofoofoo", 1, 2), new List<Dep> {
                new Dep(new BlockID("foofoofoofoo", 2, 1)),
                new Dep(new BlockID("foofoofoofoo", 3, 1))
            }, block1.Block);
            var block3 = new BlockGraph(new BlockID("foofoofoofoo", 1, 3), new List<Dep> {
                new Dep(
                    new BlockID("foofoofoofoo", 2, 2),
                    new List<BlockID> {
                        new BlockID("foofoofoofoo", 3, 1)
                    },
                    new BlockID("foofoofoofoo", 2, 1)
                )
            }, block2.Block);
            var block4 = new BlockGraph(new BlockID("foofoofoofoo", 1, 4), new List<Dep> {
                new Dep(
                    new BlockID("foofoofoofoo", 2, 3),
                    new List<BlockID> {
                        new BlockID("foofoofoofoo", 1, 1),
                        new BlockID("foofoofoofoo", 1, 2)
                    },
                    new BlockID("foofoofoofoo", 2, 2)
                ),
                new Dep(
                    new BlockID("foofoofoofoo", 3, 2),
                    new List<BlockID> {
                        new BlockID("foofoofoofoo", 1, 1),
                        new BlockID("foofoofoofoo", 2, 1),
                        new BlockID("foofoofoofoo", 1, 2),
                        new BlockID("foofoofoofoo", 2, 2)
                    },
                    new BlockID("foofoofoofoo", 3, 1)
                )
            }, block3.Block);

            graph.Add(block1);
            System.Threading.Thread.Sleep(1000);
            graph.Add(block2);
            System.Threading.Thread.Sleep(1000);
            graph.Add(block3);
            System.Threading.Thread.Sleep(1000);
            graph.Add(block4);
        }

        [Fact]
        public void FourNodeTest()
        {
            var nodes = new ulong[] { 1, 2, 3, 4 };
            var cfg = new Config(nodes, 1);
            void cb(Interpreted x)
            {
                Console.WriteLine($"Interpreted round is {x.Round}");
            }
            var graph = new Graph(cfg, cb);

            var block1 = new BlockGraph(new BlockID("foofoofoofoo", 1, 1));
            var block2 = new BlockGraph(new BlockID("foofoofoofoo", 1, 2), new List<Dep> { new Dep(new BlockID("foofoofoofoo", 2, 1)) }, block1.Block);
            var block3 = new BlockGraph(new BlockID("foofoofoofoo", 1, 3), new List<Dep> { new Dep(new BlockID("foofoofoofoo", 3, 1)) }, block2.Block);
            var block4 = new BlockGraph(new BlockID("foofoofoofoo", 1, 4), new List<Dep> {
                new Dep(
                    new BlockID("foofoofoofoo", 2, 2),
                    new List<BlockID> {
                        new BlockID("foofoofoofoo", 1, 1),
                        new BlockID("foofoofoofoo", 1, 2),
                        new BlockID("foofoofoofoo", 3, 1)
                    },
                    new BlockID("foofoofoofoo", 2, 1)
                ),
                new Dep(new BlockID("foofoofoofoo", 4, 1))
            }, block3.Block);
            var block5 = new BlockGraph(new BlockID("foofoofoofoo", 1, 5), new List<Dep> {
                new Dep(
                    new BlockID("foofoofoofoo", 3, 2),
                    new List<BlockID> {
                        new BlockID("foofoofoofoo", 4, 1),
                        new BlockID("foofoofoofoo", 2, 1)
                    },
                    new BlockID("foofoofoofoo", 3, 1)
                ),
                new Dep(
                    new BlockID("foofoofoofoo", 2, 3),
                    new List<BlockID> {
                        new BlockID("foofoofoofoo", 1, 3),
                        new BlockID("foofoofoofoo", 4, 1)
                    },
                    new BlockID("foofoofoofoo", 2, 2)
                )
            }, block4.Block);

            graph.Add(block1);
            System.Threading.Thread.Sleep(1000);
            graph.Add(block2);
            System.Threading.Thread.Sleep(1000);
            graph.Add(block3);
            System.Threading.Thread.Sleep(1000);
            graph.Add(block4);
            System.Threading.Thread.Sleep(1000);
            graph.Add(block5);
        }
    }
}
