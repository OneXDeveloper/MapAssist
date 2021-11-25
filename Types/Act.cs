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
    public class Act : IUpdatable<Act>
    {
        private readonly IntPtr _pAct = IntPtr.Zero;
        private Structs.Act _act;

        public Act(IntPtr pAct)
        {
            _pAct = pAct;
            Update();
        }

        public Act Update()
        {
            using (var processContext = GameManager.GetProcessContext())
            {
                _act = processContext.Read<Structs.Act>(_pAct);
            }

            return this;
        }

        public uint MapSeed => _act.MapSeed;
        public uint ActId => _act.ActId;
        public ActMisc ActMisc => new ActMisc(_act.pActMisc);
    }
}
