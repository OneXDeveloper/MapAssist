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

using System;
using MapAssist.Helpers;
using MapAssist.Interfaces;

namespace MapAssist.Types
{
    public class ActMisc : IUpdatable<ActMisc>
    {
        private readonly IntPtr _pActMisc = IntPtr.Zero;
        private Structs.ActMisc _actMisc;

        public ActMisc(IntPtr pActMisc)
        {
            _pActMisc = pActMisc;
            Update();
        }

        public ActMisc Update()
        {
            using (var processContext = GameManager.GetProcessContext())
            {
                _actMisc = processContext.Read<Structs.ActMisc>(_pActMisc);
            }

            return this;
        }

        public Difficulty GameDifficulty => _actMisc.GameDifficulty;
        public Act Act => new Act(_actMisc.pAct);
        public Level LevelFirst => new Level(_actMisc.pLevelFirst);
        public Area RealTombArea => _actMisc.RealTombArea;
    }
}
