using System.ComponentModel;
using System.Threading.Tasks;

namespace MapAssist.Types
{
    public class MapSeed
    {
        private static readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();

        private BackgroundWorker BackgroundCalculator;
        private ulong GameSeedXor { get; set; } = 0;

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
                    var isEven = EndSeedHash % 2 == 0;

                    Parallel.For(0, (uint.MaxValue - 1) / 2 + 1, (i, state) =>
                    {
                        var trySeed = (isEven ? 0 : 1) + i;

                        if ((((uint)trySeed * 0x6AC690C5 + 666) & 0xFFFFFFFF) == EndSeedHash)
                        {
                            GameSeedXor = InitSeedHash ^ (uint)trySeed;

                            state.Stop();
                        }
                    });

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
