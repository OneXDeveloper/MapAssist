using MapAssist.Helpers;
using System;
using System.IO;
using System.Linq;

namespace MapAssist.Types
{
    public class MapSeed
    {
        private string D2sPath { get; set; } = "";
        private ulong SeedHash { get; set; } = 0;
        private ulong GameSeedXor { get; set; } = 0;

        public bool NeedsPlayer => D2sPath == "";
        public bool NeedsSeed => !NeedsPlayer && GameSeedXor == 0;
        public bool IsReady => !NeedsPlayer && !NeedsSeed;

        public uint Get(ulong seedHash)
        {
            if (GameSeedXor == 0) return 0;

            return (uint)(seedHash ^ GameSeedXor);
        }

        public void SetPlayer(UnitPlayer player)
        {
            var path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Saved Games/Diablo II Resurrected", $"{player.Name}.d2s");
            if (File.Exists(path))
            {
                D2sPath = path;
                SeedHash = player.SeedHash;
            }
        }

        public void SetKnownSeed()
        {
            var mapData = File.ReadAllBytes(D2sPath);

            using (var processContext = GameManager.GetProcessContext())
            {
                var knownSeed = BitConverter.ToUInt32(mapData.Skip(0xab).Take(0x4).ToArray(), 0);
                GameSeedXor = (ulong)knownSeed ^ SeedHash;
            }
        }
    }
}
