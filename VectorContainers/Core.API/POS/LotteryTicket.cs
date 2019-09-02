using Core.API.LibSodium;
using Newtonsoft.Json;
using Org.BouncyCastle.Math;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Core.API.POS
{
    public class LotteryTicket
    {
        public const ulong TimestampMask = 0x0000000f;
        public static BigInteger PoWTarget = new BigInteger(1,
            new byte[] 
            {
                0x00, 0x00, 0x3f, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff
            }
        );

        public int Nonce { get; }
        public ulong Round { get; }
        public ulong Timestamp { get; }

        public LotteryTicket(int nonce, ulong round, ulong timestamp)
        {
            Nonce = nonce;
            Round = round;
            Timestamp = timestamp;
        }

        public static LotteryTicket Generate(ulong round)
        {
            ulong unixTime = (ulong)((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();

            var timestamp = unixTime & ~TimestampMask;

            var nonce = Cryptography.RandomNumber(int.MaxValue);
            return new LotteryTicket(nonce, round, timestamp);
        }

        public static BigInteger Hash(LotteryTicket lt)
        {
            var serialized = JsonConvert.SerializeObject(lt);
            var hash = Cryptography.GenericHashNoKey(serialized).ToList();
            return new BigInteger(1, hash.ToArray());
        }

        public static LotteryTicket GenerateValidTarget(ulong round)
        {
            LotteryTicket lotteryTicket = null;
            object ticketLock = new object();

            Parallel.For(
                0,
                Environment.ProcessorCount,
                new ParallelOptions {
                    MaxDegreeOfParallelism = Environment.ProcessorCount
                }, 
                (i, state) => {
                    LotteryTicket lt = null;
                    BigInteger hash = BigInteger.Zero;
                    int ct = 1;

                    do
                    {
                        lt = Generate(round);
                        hash = Hash(lt);
                        ct = hash.CompareTo(PoWTarget);
                    } while (ct == 1);

                    lock (ticketLock)
                    {
                        if (lotteryTicket == null)
                        {
                            lotteryTicket = lt;
                        }

                        state.Stop();
                    }
                });

            return lotteryTicket;
        }
    }
}
