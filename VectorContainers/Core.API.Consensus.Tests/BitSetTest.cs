using System;
using Xunit;

namespace Core.API.Consensus.Tests
{
    public class BitSetFixture : IDisposable
    {
        public BitSet Bitset;

        public BitSetFixture()
        {
            Bitset = new BitSet();
        }

        public void Dispose() { }
    }

    public class BitSetTest : IClassFixture<BitSetFixture>
    {
        readonly BitSetFixture fixture;

        public BitSetTest(BitSetFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public void Clone()
        {
            var b = fixture.Bitset;
            BitSet cloned;
            b.Commits = new ulong[] { 1, 2, 3 };
            b.Prepares = new ulong[] { 4, 5 };
            cloned = b.Clone();
            Assert.Equal(b.Commits, cloned.Commits);
            Assert.Equal(b.Prepares, cloned.Prepares);
        }

        [Fact]
        public void CommitCount()
        {
            var b = fixture.Bitset;
            b.Commits = new ulong[] { 1, 2 };
            var actual = b.CommitCount();
            var expected = 2;
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void PrepareCount()
        {
            var b = fixture.Bitset;
            b.Prepares = new ulong[] { 1, 2 };
            var actual = b.PrepareCount();
            var expected = 2;
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void SetCommit()
        {
            var b = fixture.Bitset;
            b.Commits = new ulong[] { 1 };
            b.SetCommit(1);
            var actual = b.Commits;
            var expected = new ulong[] { 3 };
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void SetPrepare()
        {
            var b = fixture.Bitset;
            b.Prepares = new ulong[] { 1 };
            b.SetPrepare(1);
            var actual = b.Prepares;
            var expected = new ulong[] { 3 };
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void NewBitSet()
        {
            BitSet newed = new BitSet(423);
            Assert.Equal(newed.Commits, new ulong[] { 0, 0, 0, 0, 0, 0, 0 });
            Assert.Equal(newed.Prepares, new ulong[] { 0, 0, 0, 0, 0, 0, 0 });
        }

        [Fact]
        public void HasCommit()
        {
            var b = fixture.Bitset;
            b.Commits = new ulong[] { 1, 2, 3, 4 };
            var actual = b.HasCommit(0);
            var expected = true;
            Assert.Equal(expected, actual);

            b.Commits = new ulong[] { 1, 2, 3, 4 };
            actual = b.HasCommit(1);
            expected = false;
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void HasPrepare()
        {
            var b = fixture.Bitset;
            b.Prepares = new ulong[] { 1, 2, 3, 4 };
            var actual = b.HasPrepare(0);
            var expected = true;
            Assert.Equal(expected, actual);

            b.Prepares = new ulong[] { 1, 2, 3, 4 };
            actual = b.HasPrepare(1);
            expected = false;
            Assert.Equal(expected, actual);
        }
    }
}
