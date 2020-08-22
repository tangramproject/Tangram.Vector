// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using Akka.Actor;
using Akka.Event;

namespace TGMCore.Actors
{
    public class DeadLetterMonitorActor: ReceiveActor
    {
        public DeadLetterMonitorActor()
        {
            Receive<DeadLetter>(x => Handle(x));
        }

        private void Handle(DeadLetter deadLetter)
        {

        }
    }
}
