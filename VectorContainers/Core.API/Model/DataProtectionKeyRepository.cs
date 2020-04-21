using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;
using Microsoft.AspNetCore.DataProtection.Repositories;
using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Core.API.Model
{
    public interface IDataProtectionKeyRepository : IXmlRepository
    {

    }

    public class DataProtectionKeyRepository : IDataProtectionKeyRepository
    {
        private readonly IDbContext dbContext;
        private readonly ILogger<DataProtectionKeyRepository> logger;

        public DataProtectionKeyRepository(IDbContext dbContext)
        {
            this.dbContext = dbContext;
            logger = NullLogger<DataProtectionKeyRepository>.Instance;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IReadOnlyCollection<XElement> GetAllElements()
        {
            List<XElement> elements = new List<XElement>();

            try
            {
                using var session = dbContext.Document.OpenSession();

                var results = session.Query<DataProtectionKeyProto>().Select(k => k.XmlData).ToList();
                foreach (var item in results)
                {
                    elements.Add(XElement.Parse(item));
                }
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
