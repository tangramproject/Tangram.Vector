using System;
using Core.API.Consensus.States;
using Xunit;

namespace Core.API.Consensus.Tests.States
{
    public class StateFixture : IDisposable
    {
        public Final f;
        public Hnv h;
        public Prepared pd;
        public PrePrepared ppd;
        public View v;
        public ViewChanged vcd;

        public StateFixture()
        {
            f = new Final();
            h = new Hnv();
            pd = new Prepared();
            ppd = new PrePrepared();
            vcd = new ViewChanged();
            v = new View();
        }

        public void Dispose() { }
    }

    public class StateTest : IClassFixture<StateFixture>
    {
        readonly StateFixture fixture;

        public StateTest(StateFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public void GetRound()
        {
            var f = fixture.f;
            f.Round = 1;
            Assert.Equal(f.Round, f.GetRound());

            var h = fixture.h;
            h.Round = 1;
            Assert.Equal(h.Round, h.GetRound());

            var pd = fixture.pd;
            pd.Round = 1;
            Assert.Equal(pd.Round, pd.GetRound());

            var ppd = fixture.ppd;
            ppd.Round = 1;
            Assert.Equal(ppd.Round, ppd.GetRound());

            var v = fixture.v;
            v.Round = 1;
            Assert.Equal(v.Round, v.GetRound());

            var vcd = fixture.vcd;
            vcd.Round = 1;
            Assert.Equal(vcd.Round, vcd.GetRound());
        }

        [Fact]
        public void SdKind()
        {
            Assert.Equal(StateDataKind.FinalState, fixture.f.SdKind());
            Assert.Equal(StateDataKind.HNVState, fixture.h.SdKind());
            Assert.Equal(StateDataKind.PreparedState, fixture.pd.SdKind());
            Assert.Equal(StateDataKind.PrePreparedState, fixture.ppd.SdKind());
            Assert.Equal(StateDataKind.ViewState, fixture.v.SdKind());
            Assert.Equal(StateDataKind.ViewChangedState, fixture.vcd.SdKind());
        }
    }
}
