// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using TGMCore.Providers;
using TGMCore.Messages;
using TGMCore.Model;

namespace TGMCore.Actors
{
    public class InterpretActor<TAttach> : ReceiveActor
    {
        protected readonly IUnitOfWork unitOfWork;
        protected readonly ISigningActorProvider signingActorProvider;
        protected readonly ILoggingAdapter logger;

        public InterpretActor(IUnitOfWork unitOfWork, ISigningActorProvider signingActorProvider)
        {
            this.unitOfWork = unitOfWork;
            this.signingActorProvider = signingActorProvider;

            logger = Context.GetLogger();

            ReceiveAsync<InterpretMessage<TAttach>>(async msg => Sender.Tell(await Interpret(msg)));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public virtual Task<bool> Interpret(InterpretMessage<TAttach> message)
        {
            throw new NotImplementedException();
        }
    }
}
