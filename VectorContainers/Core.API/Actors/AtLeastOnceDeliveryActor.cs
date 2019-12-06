using System;
using Akka.Actor;
using Akka.Event;
using Akka.Persistence;
using Core.API.Messages;

namespace Core.API.Actors
{
    public class AtLeastOnceDeliveryActor: AtLeastOnceDeliveryReceiveActor
    {
        public override string PersistenceId => Context.Self.Path.Name;

        private ICancelable recurringMessageSend;
        private ICancelable recurringSnapshotCleanup;
        private readonly IActorRef targetActor;
        private readonly string hash;
        private readonly ILoggingAdapter logger;

        private class DoSend { }
        private class CleanSnapshots { }

        public AtLeastOnceDeliveryActor(IActorRef targetActor, string hash)
        {
            this.targetActor = targetActor;
            this.hash = hash;

            logger = Context.GetLogger();

            Recover<SnapshotOffer>(offer => offer.Snapshot is AtLeastOnceDeliverySnapshot, offer =>
            {
                var snapshot = offer.Snapshot as AtLeastOnceDeliverySnapshot;
                SetDeliverySnapshot(snapshot);
            });

            Command<DoSend>(send =>
            {
                Self.Tell(new WriteMessage(hash));
            });

            Command<WriteMessage>(write =>
            {
                Deliver(targetActor.Path, messageId => new ReliableDeliveryEnvelopeMessage<WriteMessage>(write, messageId));
                SaveSnapshot(GetDeliverySnapshot());
            });

            Command<ReliableDeliveryAckMessage>(ack =>
            {
                ConfirmDelivery(ack.MessageId);
            });

            Command<CleanSnapshots>(clean =>
            {
                SaveSnapshot(GetDeliverySnapshot());
            });

            Command<SaveSnapshotSuccess>(saved =>
            {
                var seqNo = saved.Metadata.SequenceNr;
                DeleteSnapshots(new SnapshotSelectionCriteria(seqNo, saved.Metadata.Timestamp.AddMilliseconds(-1)));
            });

            Command<SaveSnapshotFailure>(failure =>
            {
                logger.Warning(failure.ToString());
            });
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void PreStart()
        {
            recurringMessageSend = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(10), Self, new DoSend(), Self);

            recurringSnapshotCleanup =
                Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(TimeSpan.FromSeconds(10),
                    TimeSpan.FromSeconds(10), Self, new CleanSnapshots(), ActorRefs.NoSender);

            base.PreStart();
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void PostStop()
        {
            recurringSnapshotCleanup?.Cancel();
            recurringMessageSend?.Cancel();

            base.PostStop();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="targetActor"></param>
        /// <param name="hash"></param>
        /// <returns></returns>
        public static Props Create(IActorRef targetActor, string hash) =>
            Akka.Actor.Props.Create(() => new AtLeastOnceDeliveryActor(targetActor, hash));
    }
}
