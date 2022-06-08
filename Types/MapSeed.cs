using MapAssist.Helpers;
using System.ComponentModel;

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
                    var foundSeed = D2Hash.Reverse(EndSeedHash);

                    if (foundSeed != null)
                    {
                        GameSeedXor = (uint)InitSeedHash ^ (uint)foundSeed;
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
