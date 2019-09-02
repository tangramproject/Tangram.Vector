using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Core.API.Helper;
using Core.API.Model;
using Dawn;

namespace MessagePool.API.Services
{
    public class MessagePoolService : IMessagePoolService
    {
        readonly IUnitOfWork unitOfWork;

        public MessagePoolService(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
        }

        public async Task<MessageProto> AddMessage(MessageProto messageProto)
        {
            var link = await unitOfWork.MessageLink.Get(messageProto.Address.ToBytes());

            if (link == null)
            {
                var messageLink = new MessageProtoList
                {
                    Address = messageProto.Address,
                    Keys = new List<Guid> { Guid.NewGuid() }
                };

                await unitOfWork.MessageLink.Put(messageProto.Address.ToBytes(), messageLink);
                await unitOfWork.Message.Put(messageLink.Keys.First().ToString().ToBytes(), messageProto);
            }
            else
            {
                Monitor.Enter(link);

                try
                {
                    var guid = Guid.NewGuid();
                    link.Keys.Add(guid);
                    await unitOfWork.MessageLink.Put(messageProto.Address.ToBytes(), link);
                    await unitOfWork.Message.Put(guid.ToString().ToBytes(), messageProto);
                }
                finally
                {
                    Monitor.Exit(link);
                }
            }

            return messageProto;
        }


        public async Task<List<MessageProto>> GetMessages(string address, int skip, int take)
        {
            List<MessageProto> messageProtos = null;

            var link = await unitOfWork.MessageLink.Get(address.ToBytes());

            if (link != null)
            {
                messageProtos = new List<MessageProto>();

                foreach (var key in link.Keys)
                {
                    var msg = await unitOfWork.Message.Get(key.ToString().ToBytes());

                    if (msg != null)
                    {
                        msg.Address = Convert.ToBase64String(Encoding.UTF8.GetBytes(msg.Address));
                        msg.Body = Convert.ToBase64String(Encoding.UTF8.GetBytes(msg.Body));
                        messageProtos.Add(msg);
                    }

                }

                messageProtos = messageProtos.Select(m => m).Skip(skip).Take(take).ToList();
            }

            return messageProtos;
        }

        public async Task<int> Count(string address)
        {
            var link = await unitOfWork.MessageLink.Get(address.ToBytes());
            return link != null ? link.Keys.Count() : 0;
        }
    }
}
