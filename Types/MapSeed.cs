using MapAssist.Helpers;
using MapAssist.Interfaces;
using System;

namespace MapAssist.Types
{
    public class MapSeed : IUpdatable<MapSeed>
    {
        private readonly IntPtr _pMapSeed;
        public uint Seed { get; private set; }

        public MapSeed(IntPtr pMapSeed)
        {
            _pMapSeed = pMapSeed;
            Update();
        }

        public MapSeed Update()
        {
            using (var processContext = GameManager.GetProcessContext())
            {
                try
                {
                    var pMapSeedData = processContext.Read<IntPtr>(_pMapSeed);
                    var mapSeedData = processContext.Read<Structs.MapSeed>(pMapSeedData);

                    Seed = mapSeedData.check > 0 ? mapSeedData.mapSeed1 : mapSeedData.mapSeed2; // Use this if check offset is 0x110
                    //Seed = mapSeedData.check > 0 ? mapSeedData.mapSeed2 : mapSeedData.mapSeed1; // Use this if check offset is 0x124
                    //Seed = mapSeedData.check > 0 ? mapSeedData.mapSeed1 : mapSeedData.mapSeed2; // Use this if check offset is 0x830
                }
                catch (Exception) { }
            }
            return this;
        }
    }
}
