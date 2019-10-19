using System;
using System.Threading.Tasks;
using Core.API.Helper;
using Core.API.Model;
using Core.API.Onion;
using Microsoft.Extensions.Logging;
using System.Linq;
using Core.API.Membership;

namespace MessagePool.API.Services
{
    public class MessagePoolService : IMessagePoolService
    {
        readonly IOnionServiceClient onionServiceClient;
        readonly ILogger logger;
        private readonly IUnitOfWork unitOfWork;
        private readonly ITorClient torClient;
        private readonly IMembershipServiceClient membershipServiceClient;

        public MessagePoolService(IOnionServiceClient onionServiceClient, IUnitOfWork unitOfWork, ITorClient torClient,
            IMembershipServiceClient membershipServiceClient, ILogger<MessagePoolService> logger)
        {
            this.onionServiceClient = onionServiceClient;
            this.unitOfWork = unitOfWork;
            this.torClient = torClient;
            this.membershipServiceClient = membershipServiceClient;
            this.logger = logger;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<byte[]> AddMessage(byte[] message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            try
            {
                var messageProto = Util.DeserializeProto<MessageProto>(message);
                var msg = await unitOfWork.Message.StoreOrUpdate(messageProto);

                if (msg != null)
                {
                    //var hash = Core.API.LibSodium.Cryptography.GenericHashNoKey(message);
                    //var signed = await onionServiceClient.SignHashAsync(hash);

                    //Broadcast(message);

                    //return Util.SerializeProto(new MessageSignedBlockProto
                    //{
                    //    Hash = hash.ToHex(),
                    //    PublicKey = signed.PublicKey.ToHex(),
                    //    Signature = signed.Signature.ToHex()
                    //});

                    return message;
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< MessagePoolService.AddMessage >>>: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task<byte[]> GetMessages(string key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            byte[] result = null;

            try
            {
                var messages = await unitOfWork.Message.GetMany(key);
                if (messages?.Any() == true)
                {
                    result = Util.SerializeProto(messages);
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< MessagePoolService.GetMessages >>>: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <returns></returns>
        public async Task<byte[]> GetMessages(string key, int skip, int take)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            byte[] result = null;

            try
            {
                var messages = await unitOfWork.Message.GetMany(key);
                if (messages?.Any() == true)
                {
                    result = Util.SerializeProto(messages.Skip(skip).Take(take));
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< MessagePoolService.GetMessages >>>: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task<int> Count(string key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            int count = 0;

            try
            {
                count = await unitOfWork.Message.Count(key);
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< MessagePoolService.Count >>>: {ex.Message}");
            }

            return count;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private void Broadcast(byte[] message)
        {
            _ = Task.Factory.StartNew(async () =>
            {
                var members = await membershipServiceClient.GetMembersAsync().ConfigureAwait(false);
                foreach (var member in members)
                {
                    _ = Task.Factory.StartNew(async () =>
                    {
                        var uri = new Uri(new Uri(member.Endpoint), "message");
                        _ = await torClient.PostAsJsonAsync(uri, message);
                    });
                }
            });
        }
    }
}
