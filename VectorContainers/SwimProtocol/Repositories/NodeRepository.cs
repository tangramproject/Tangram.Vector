using Core.API.Model;
using LiteDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SwimProtocol.Repositories
{
    public class NodeRepository : IDisposable
    {
        private readonly LiteRepository _repository;

        public NodeRepository(string name)
        {
            _repository = new LiteRepository(Directory($"{name}.db"));
        }

        public bool Upsert(ISwimNode node)
        {
            return _repository.Upsert(node, "nodes");
        }

        public ISwimNode Get(string hostname)
        {
            return _repository.SingleById<ISwimNode>(hostname, "nodes");
        }

        public IEnumerable<ISwimNode> Get()
        {
            return _repository.Query<ISwimNode>("nodes").ToList();
        }

        public bool Delete(ISwimNode node)
        {
            return Delete(node.Hostname);
        }

        public bool Delete(string hostname)
        {
            return _repository.Delete<ISwimNode>(hostname, "nodes");
        }

        public void Dispose()
        {
            if (_repository != null)
            {
                _repository.Dispose();
            }
        }

        private static Stream Directory(string fileName)
        {
            return File.Open(fileName, System.IO.FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
        }
    }
}
