using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Core.API.Model
{
    public class InterpretedRepository : Repository<InterpretedProto>, IInterpretedRepository
    {
        private readonly IDbContext dbContext;
        private readonly ILogger logger;

        public InterpretedRepository(IDbContext dbContext, ILogger logger)
            : base(dbContext, logger)
        {
            this.dbContext = dbContext;
            this.logger = logger;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Task<InterpretedProto> Get()
        {
            InterpretedProto interpreted = null;

            try
            {
                using var session = dbContext.Document.OpenSession();
                interpreted = session.Query<InterpretedProto>().FirstOrDefault();
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< InterpretedRepository.Get >>>: {ex.ToString()}");
            }

            return Task.FromResult(interpreted);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Task<ulong> GetRound()
        {
            ulong round = 0;

            try
            {
                using var session = dbContext.Document.OpenSession();

                var interpreted = session.Query<InterpretedProto>().FirstOrDefault();
                if (interpreted != null)
                {
                    round = interpreted.Round;
                    round = round > 0 ? round - 1 : round;
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< InterpretedRepository.GetRound >>>: {ex.ToString()}");
            }

            return Task.FromResult(round);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="consumed"></param>
        /// <param name="round"></param>
        public void Store(ulong consumed, ulong round)
        {
            if (consumed < 0)
                throw new ArgumentOutOfRangeException(nameof(consumed));

            if (round < 0)
                throw new ArgumentNullException(nameof(round));

            try
            {
                using var session = dbContext.Document.OpenSession();

                var interpretedProto = session.Query<InterpretedProto>().FirstOrDefault();
                if (interpretedProto == null)
                {
                    session.Store(new InterpretedProto { Consumed = consumed, Round = round });
                }
                else
                {
                    interpretedProto.Consumed = consumed;
                    interpretedProto.Round = round;

                    session.Store(interpretedProto, interpretedProto.Id);
                }

                session.SaveChanges();
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< InterpretedRepository.Store >>>: {ex.ToString()}");
            }
        }
    }
}
