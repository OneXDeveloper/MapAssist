/**
 *   Copyright (C) 2021 okaygo
 *
 *   https://github.com/misterokaygo/MapAssist/
 *
 *  This program is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program.  If not, see <https://www.gnu.org/licenses/>.
 **/

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
