using System;
using DBreeze;
using DBreeze.Utils;
using LightningDB;

namespace Core.API.Model
{
    public sealed class DbContext : IDisposable
    {
        static readonly Lazy<DbContext> lazy = new Lazy<DbContext>(() => new DbContext());

        public static DbContext Instance { get { return lazy.Value; } }

        public LightningEnvironment LightningEnvironment { get; set; }

        DbContext() { }

        public void SetEngine(string name, string filePath)
        {
            if (LightningEnvironment == null)
            {
                LightningEnvironment = new LightningEnvironment(filePath)
                {
                    MaxDatabases = 4,
                    MapSize = int.MaxValue,
                    MaxReaders = 126
                };

                LightningEnvironment.Open();
            }
        }

        public void Dispose()
        {
            LightningEnvironment.Dispose();
        }
    }
}


