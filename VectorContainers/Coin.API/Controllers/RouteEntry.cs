using System;
namespace Coin.API.Controllers
{
    public class RouteName
    {
        private readonly string name;
        private readonly int value;

        public static readonly RouteName AddBlock = new RouteName(1, "blockgraph");
        public static readonly RouteName AddBlocks = new RouteName(2, "blockgraphs");
        public static readonly RouteName BlockHeight = new RouteName(3, "height");
        public static readonly RouteName NetworkBlockHeight = new RouteName(4, "networkheight");
        public static readonly RouteName AddCoin = new RouteName(5, "mempool");

        private RouteName(int value, string name)
        {
            this.value = value;
            this.name = name;
        }

        public override string ToString()
        {
            return name;
        }
    }
}
