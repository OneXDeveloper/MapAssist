using System.ComponentModel;
using System.Threading.Tasks;

namespace MapAssist.Types
{
    public class MapSeed
    {
        private static readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();

        private BackgroundWorker BackgroundCalculator;
        private uint GameSeedXor { get; set; } = 0;

        public bool IsReady => BackgroundCalculator != null && GameSeedXor != 0;

        public uint Get(UnitPlayer player)
        {
            if (GameSeedXor != 0)
            {
                return (uint)(player.InitSeedHash ^ GameSeedXor);
            }
            else if (BackgroundCalculator == null)
            {
                var InitSeedHash = player.InitSeedHash;
                var EndSeedHash = player.EndSeedHash;

                BackgroundCalculator = new BackgroundWorker();

                BackgroundCalculator.DoWork += (sender, args) =>
                {
                    uint magic = 0x6AC690C5;
                    uint offset = 666;

                    uint divisor = 2 << 16 - 1;

                    uint trySeed = 0;
                    uint incr = 1;
                    for (; trySeed < uint.MaxValue; trySeed += incr)
                    {
                        var seedResult = ((uint)trySeed * magic + offset) & 0xFFFFFFFF;

                        if (seedResult == EndSeedHash)
                        {
                            GameSeedXor = (uint)InitSeedHash ^ trySeed;
                            break;
                        }

                        if (incr == 1 && (seedResult % divisor) == (EndSeedHash % divisor))
                        {
                            incr = divisor;
                        }
                    }

                    BackgroundCalculator.Dispose();

                    if (GameSeedXor == 0)
                    {
                        _log.Info("Failed to brute force map seed");
                        BackgroundCalculator = null;
                    }
                };

                BackgroundCalculator.RunWorkerAsync();
            }

            return 0;
        }
    }
}
