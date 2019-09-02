using System;
using System.Collections.Generic;
using Xunit;

namespace Core.API.Consensus.Tests
{
    public class ViewChangeTest
    {
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
            var hash = "foofoofoofoo";

            var blocks = new List<BlockGraph> {
                new BlockGraph(new BlockID(hash, 1, 1)),
                new BlockGraph(new BlockID(hash, 1, 2), new List<Dep> {new Dep(new BlockID(hash, 2, 1))}, new BlockID(hash, 1, 1)),
                new BlockGraph(new BlockID(hash, 1, 3), new List<Dep> {new Dep(new BlockID(hash, 3, 1)), new Dep(new BlockID(hash, 1, 1))}, new BlockID(hash, 1, 2)),
                new BlockGraph(new BlockID(hash, 1, 4), new List<Dep> {new Dep(new BlockID(hash, 2, 2), new List<BlockID> {new BlockID(hash, 1, 1), new BlockID(hash, 1, 2), new BlockID(hash, 3, 1)}, new BlockID(hash, 2, 1))}, new BlockID(hash, 1, 3)),
                new BlockGraph(new BlockID(hash, 1, 5), new List<Dep> {}, new BlockID(hash, 1, 4)),
                new BlockGraph(
                    new BlockID(hash, 1, 6),
                    new List<Dep>{
                        new Dep(new BlockID(hash, 2, 3), new List<BlockID> {new BlockID(hash, 1, 3)}, new BlockID(hash, 2, 2)),
                        new Dep(new BlockID(hash, 3, 2), new List<BlockID> {new BlockID(hash, 2, 1), new BlockID(hash, 1, 2), new BlockID(hash, 1, 3), new BlockID(hash, 2, 2)}, new BlockID(hash, 3, 1)),
                        new Dep(new BlockID(hash, 2, 4), new List<BlockID> {new BlockID(hash, 3, 2)}, new BlockID(hash, 2, 3)),
                        new Dep(new BlockID(hash, 3, 3), new List<BlockID> {new BlockID(hash, 2, 3)}, new BlockID(hash, 3, 2)),
                        new Dep(new BlockID(hash, 2, 5), new List<BlockID> {new BlockID(hash, 3, 3)}, new BlockID(hash, 2, 4)),
                        new Dep(new BlockID(hash, 3, 4), new List<BlockID> {new BlockID(hash, 2, 4)}, new BlockID(hash, 3, 3)),
                        new Dep(new BlockID(hash, 2, 6), new List<BlockID> {new BlockID(hash, 3, 4), new BlockID(hash, 1, 4)}, new BlockID(hash, 2, 5)),
                        new Dep(new BlockID(hash, 3, 5), new List<BlockID> {new BlockID(hash, 2, 5), new BlockID(hash, 1, 4)}, new BlockID(hash, 3, 4)),
                        new Dep(new BlockID(hash, 2, 7), new List<BlockID> {new BlockID(hash, 3, 5)}, new BlockID(hash, 2, 6)),
                        new Dep(new BlockID(hash, 3, 6), new List<BlockID> {new BlockID(hash, 2, 6)}, new BlockID(hash, 3, 5))
                    },
                    new BlockID(hash, 1, 5)
                ),
                new BlockGraph(
                    new BlockID(hash, 1, 7),
                    new List<Dep>{
                        new Dep(new BlockID(hash, 2, 8), new List<BlockID> {new BlockID(hash, 1, 5), new BlockID(hash, 3, 6)}, new BlockID(hash, 2, 7)),
                        new Dep(new BlockID(hash, 3, 7), new List<BlockID> {new BlockID(hash, 1, 5), new BlockID(hash, 2, 7)}, new BlockID(hash, 3, 6))
                    },
                    new BlockID(hash, 1, 6)
                ),
                new BlockGraph(
                    new BlockID(hash, 1, 8),
                    new List<Dep>{
                        new Dep(new BlockID(hash, 3, 8), new List<BlockID> {new BlockID(hash, 2, 8), new BlockID(hash, 1, 6)}, new BlockID(hash, 3, 7)),
                        new Dep(new BlockID(hash, 2, 9), new List<BlockID> {new BlockID(hash, 3, 7), new BlockID(hash, 1, 6)}, new BlockID(hash, 2, 8))
                    },
                    new BlockID(hash, 1, 7)
                ),
                new BlockGraph(
                    new BlockID(hash, 1, 9),
                    new List<Dep>{
                        new Dep(new BlockID(hash, 2, 10), new List<BlockID> {new BlockID(hash, 3, 8), new BlockID(hash, 1, 7)}, new BlockID(hash, 2, 9)),
                        new Dep(new BlockID(hash, 3, 9), new List<BlockID> {new BlockID(hash, 2, 9), new BlockID(hash, 1, 7)}, new BlockID(hash, 3, 8))
                    },
                    new BlockID(hash, 1, 8)
                ),
                new BlockGraph(
                    new BlockID(hash, 1, 10),
                    new List<Dep>{
                        new Dep(new BlockID(hash, 2, 11), new List<BlockID> {new BlockID(hash, 3, 9), new BlockID(hash, 1, 8)}, new BlockID(hash, 2, 10)),
                        new Dep(new BlockID(hash, 3, 10), new List<BlockID> {new BlockID(hash, 1, 8), new BlockID(hash, 2, 10)}, new BlockID(hash, 3, 9))
                    },
                    new BlockID(hash, 1, 9)
                ),
                new BlockGraph(
                    new BlockID(hash, 1, 11),
                    new List<Dep>{
                        new Dep(new BlockID(hash, 3, 11), new List<BlockID> {new BlockID(hash, 1, 9), new BlockID(hash, 2, 11)}, new BlockID(hash, 3, 10)),
                        new Dep(new BlockID(hash, 2, 12), new List<BlockID> {new BlockID(hash, 1, 9), new BlockID(hash, 3, 10)}, new BlockID(hash, 2, 11))
                    },
                    new BlockID(hash, 1, 10)
                ),
                new BlockGraph(
                    new BlockID(hash, 1, 12),
                    new List<Dep>{
                        new Dep(new BlockID(hash, 2, 13), new List<BlockID> {new BlockID(hash, 3, 11), new BlockID(hash, 1, 10)}, new BlockID(hash, 2, 12)),
                        new Dep(new BlockID(hash, 3, 12), new List<BlockID> {new BlockID(hash, 1, 10), new BlockID(hash, 2, 12)}, new BlockID(hash, 3, 11))
                    },
                    new BlockID(hash, 1, 11)
                ),
                new BlockGraph(
                    new BlockID(hash, 1, 13),
                    new List<Dep>{
                        new Dep(new BlockID(hash, 2, 14), new List<BlockID> {new BlockID(hash, 1, 11), new BlockID(hash, 3, 12)}, new BlockID(hash, 2, 13)),
                        new Dep(new BlockID(hash, 3, 13), new List<BlockID> {new BlockID(hash, 1, 11), new BlockID(hash, 2, 13)}, new BlockID(hash, 3, 12))
                    },
                    new BlockID(hash, 1, 12)
                )
            };

            for (var i = 0; i < blocks.Count; i++)
            {
                graph.Add(blocks[i]);
                System.Threading.Thread.Sleep(1000);
            }
        }
    }
}
