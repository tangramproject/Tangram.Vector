using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace Core.API.Model
{
    public class DataProtectionKeyRepository : IXmlRepository
    {
        private readonly IDbContext dbContext;
        private readonly ILogger logger;

        public DataProtectionKeyRepository(IDbContext dbContext, ILogger logger)
        {
            this.dbContext = dbContext;
            this.logger = logger;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IReadOnlyCollection<XElement> GetAllElements()
        {
            List<XElement> elements = null;

            try
            {
                using var session = dbContext.Document.OpenSession();
                elements  = session.Query<DataProtectionKeyProto>().Select(k => XElement.Parse(k.XmlData)).ToList();
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< DataProtectionKeyRepository.GetAllElements >>>: {ex}");
            }

            return new ReadOnlyCollection<XElement>(elements);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="element"></param>
        /// <param name="friendlyName"></param>
        public void StoreElement(XElement element, string friendlyName)
        {
            try
            {
                using var session = dbContext.Document.OpenSession();

                var entity = session.Query<DataProtectionKeyProto>().SingleOrDefault(k => k.FriendlyName == friendlyName);
                if (null != entity)
                {
                    entity.XmlData = element.ToString();

                    session.Store(entity, null, entity.Id);
                    session.SaveChanges();
                }
                else
                {
                    session.Store(new DataProtectionKeyProto { FriendlyName = friendlyName, XmlData = element.ToString() });
                    session.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< DataProtectionKeyRepository.StoreElement >>>: {ex}");
            }

        }
    }
}
