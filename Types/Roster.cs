using GameOverlay.Drawing;
using MapAssist.Helpers;
using MapAssist.Interfaces;
using MapAssist.Structs;
using System;
using System.Collections.Generic;
using System.Text;

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
        public IntPtr pHostileInfo;
        public uint FirstHostileUnitId;
        public HostileInfo HostileInfo;
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
                var hostilePtr = processContext.Read<IntPtr>(member.pHostileInfo);
                var hostileInfo = processContext.Read<HostileInfo>(hostilePtr);
                var entry = new RosterEntry
                {
                    Name = Encoding.UTF8.GetString(member.Name).TrimEnd((char)0),
                    UnitId = member.UnitId,
                    PlayerClass = member.PlayerClass,
                    PlayerLevel = member.PlayerLevel,
                    PartyID = member.PartyID,
                    Area = member.Area,
                    Position = new Point((int)member.PosX, (int)member.PosY),
                    PartyFlags = member.PartyFlags,
                    pHostileInfo = hostilePtr,
                    FirstHostileUnitId = hostileInfo.UnitId,
                    HostileInfo = hostileInfo,
                    pNext = member.pNext
                };
                return entry;
            }
        }

        public List<RosterEntry> List => _list;
        public Dictionary<uint, RosterEntry> EntriesByUnitId => _entriesByUnitId;
    }
}
