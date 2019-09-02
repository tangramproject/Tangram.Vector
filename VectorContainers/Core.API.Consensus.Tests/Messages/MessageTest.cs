using System;
using System.Collections.Generic;
using Xunit;
using Core.API.Consensus.Messages;

namespace Core.API.Consensus.Tests.Messages
{
    public class MessageFixture : IDisposable
    {
        public Commit c;
        public NewView nv;
        public Prepare p;
        public PrePrepare pp;
        public ViewChange vc;

        public MessageFixture()
        {
            c = new Commit();
            nv = new NewView();
            p = new Prepare();
            pp = new PrePrepare();
            vc = new ViewChange();
        }

        public void Dispose() { }
    }

    public class MessageTest : IClassFixture<MessageFixture>
    {
        readonly MessageFixture fixture;

        public MessageTest(MessageFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public void FmtHashBlank()
        {
            var actual = Util.FmtHash("");
            var expected = "";
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void FmtHashString()
        {
            var actual = Util.FmtHash("lotsoffoobar");
            var expected = "666F6F626172";
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CheckKind()
        {
            Assert.Equal(MessageKind.CommitMsg, fixture.c.Kind());
            Assert.Equal(MessageKind.NewViewMsg, fixture.nv.Kind());
            Assert.Equal(MessageKind.PrepareMsg, fixture.p.Kind());
            Assert.Equal(MessageKind.PrePrepareMsg, fixture.pp.Kind());
            Assert.Equal(MessageKind.ViewChangedMsg, fixture.vc.Kind());
        }

        [Fact]
        public void NodeRound()
        {
            var c = fixture.c;
            c.Node = 2;
            c.Round = 1;
            (var node, var round) = c.NodeRound();
            Assert.Equal(c.Node, node);
            Assert.Equal(c.Round, round);

            var nv = fixture.nv;
            nv.Node = 2;
            nv.Round = 1;
            (node, round) = nv.NodeRound();
            Assert.Equal(nv.Node, node);
            Assert.Equal(nv.Round, round);

            var p = fixture.p;
            p.Node = 2;
            p.Round = 1;
            (node, round) = p.NodeRound();
            Assert.Equal(p.Node, node);
            Assert.Equal(p.Round, round);

            var pp = fixture.pp;
            pp.Node = 2;
            pp.Round = 1;
            (node, round) = pp.NodeRound();
            Assert.Equal(pp.Node, node);
            Assert.Equal(pp.Round, round);

            var vc = fixture.vc;
            vc.Node = 2;
            vc.Round = 1;
            (node, round) = vc.NodeRound();
            Assert.Equal(vc.Node, node);
            Assert.Equal(vc.Round, round);
        }

        [Fact]
        public void Pre()
        {
            var c = fixture.c;
            c.Hash = "foofoofoofoo";
            c.Node = 2;
            c.Round = 1;
            c.View = 1;
            Assert.Equal(new PrePrepare(c.Hash, c.Node, c.Round, c.View), c.Pre());

            var p = fixture.p;
            p.Hash = "foofoofoofoo";
            p.Node = 2;
            p.Round = 1;
            p.View = 1;
            Assert.Equal(new PrePrepare(p.Hash, p.Node, p.Round, p.View), p.Pre());
        }

        [Fact]
        public void String()
        {
            var c = fixture.c;
            c.Hash = "foofoofoofoo";
            c.Node = 2;
            c.Round = 1;
            c.Sender = 3;
            c.View = 0;
            Assert.Equal("commit{node: 2, round: 1, view: 0, hash: '666F6F666F6F', sender: 3}", c.ToString());

            var nv = fixture.nv;
            nv.Hash = "foofoofoofoo";
            nv.Node = 2;
            nv.Round = 1;
            nv.Sender = 3;
            Assert.Equal("new-view{node: 2, round: 1, view: 0, hash: '666F6F666F6F', sender: 3}", nv.ToString());

            var p = fixture.p;
            p.Hash = "foofoofoofoo";
            p.Node = 2;
            p.Round = 1;
            p.Sender = 3;
            p.View = 0;
            Assert.Equal("prepare{node: 2, round: 1, view: 0, hash: '666F6F666F6F', sender: 3}", p.ToString());

            var pp = fixture.pp;
            pp.Hash = "foofoofoofoo";
            pp.Node = 2;
            pp.Round = 1;
            Assert.Equal("pre-prepare{node: 2, round: 1, view: 0, hash: '666F6F666F6F'}", pp.ToString());

            var vc = fixture.vc;
            vc.Hash = "foofoofoofoo";
            vc.Node = 2;
            vc.Round = 1;
            vc.Sender = 3;
            Assert.Equal("view-change{node: 2, round: 1, view: 0, hash: '666F6F666F6F', sender: 3}", vc.ToString());
        }

        [Fact]
        public void MessageKindTest()
        {
            var messageKindStringTests = new Dictionary<MessageKind, string>() {
                    { MessageKind.CommitMsg, "commit" },
                    { MessageKind.NewViewMsg, "new-view" },
                    { MessageKind.PrePrepareMsg, "pre-prepare" },
                    { MessageKind.PrepareMsg, "prepare" },
                    { MessageKind.UnknownMsg, "unknown" },
                    { MessageKind.ViewChangedMsg, "view-change" }
                };

            foreach (KeyValuePair<MessageKind, string> item in messageKindStringTests)
            {
                var actual = Util.GetMessageKindString(item.Key);
                var expected = item.Value;
                Assert.Equal(expected, actual);
            }

            var mkt = 1000;
            Assert.Throws<Exception>(() => Util.GetMessageKindString((MessageKind)mkt));
        }
    }
}
