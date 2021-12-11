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
using System.Collections.Generic;
using GameOverlay.Drawing;
using MapAssist.Helpers;
using MapAssist.Interfaces;
using MapAssist.Structs;

namespace MapAssist.Types
{
    public class RosterEntry
    {
        public string Name;
        public uint UnitId;
        public PlayerClass PlayerClass;
        public ushort PlayerLevel;
        public ushort PartyID;
        public Area Area;
        public Point Position;
        public uint PartyFlags;
        public uint RosterId;
        public IntPtr pNext;
    }
    public class Roster : IUpdatable<Roster>
    {
        private readonly IntPtr _pFirst;
        private List<RosterEntry> _list = new List<RosterEntry>();
        private Dictionary<uint, RosterEntry> _entriesByUnitId = new Dictionary<uint, RosterEntry>();

        public Roster(IntPtr pFirst)
        {
            _pFirst = pFirst;
            Update();
        }
        public Roster Update()
        {
            using (var processContext = GameManager.GetProcessContext())
            {
                var firstMember = processContext.Read<IntPtr>(_pFirst);
                var entry = GetNewEntry(firstMember);
                _list.Add(entry);
                _entriesByUnitId.Add(entry.UnitId, entry);
                while (entry.pNext != IntPtr.Zero)
                {
                    entry = GetNewEntry(entry.pNext);
                    _list.Add(entry);
                    _entriesByUnitId.Add(entry.UnitId, entry);
                }
            }
            return this;
        }
        private RosterEntry GetNewEntry(IntPtr pAddress)
        {
            using (var processContext = GameManager.GetProcessContext())
            {
                var member = processContext.Read<RosterMember>(pAddress);
                var memberInfo = processContext.Read<RosterInfo>(member.pRosterInfo);
                var entry = new RosterEntry
                {
                    Name = member.Name,
                    UnitId = member.UnitId,
                    PlayerClass = member.PlayerClass,
                    PlayerLevel = member.PlayerLevel,
                    PartyID = member.PartyID,
                    Area = member.Area,
                    Position = new Point((int)member.PosX, (int)member.PosY),
                    PartyFlags = member.PartyFlags,
                    RosterId = memberInfo.RosterId,
                    pNext = member.pNext
                };
                return entry;
            }
        }
        public List<RosterEntry> List => _list;
        public Dictionary<uint, RosterEntry> EntriesByUnitId => _entriesByUnitId;
    }
}
