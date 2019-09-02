using Xunit;

namespace Core.API.Consensus.Tests
{
    public class BlockManiaTest
    {
        [Fact]
        public void BlockID()
        {
            var blockID = new BlockID("foofoofoofoo");
            var actual = blockID.ToString();
            var expected = $"{blockID.Node} | {blockID.Round} | 666F6F666F6F";
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void InvalidBlockID()
        {
            var blockID = new BlockID("");
            var actual = blockID.ToString();
            var expected = $"{blockID.Node} | {blockID.Round}";
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void InvalidHash()
        {
            var blockID = new BlockID("");
            var actual = blockID.Valid();
            var expected = false;
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ValidHash()
        {
            var blockID = new BlockID("foo");
            var actual = blockID.Valid();
            var expected = true;
            Assert.Equal(expected, actual);
        }
    }
}
