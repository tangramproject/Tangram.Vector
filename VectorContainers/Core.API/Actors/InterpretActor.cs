using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using Core.API.Actors.Providers;
using Core.API.Messages;
using Core.API.Model;

namespace Core.API.Actors
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
