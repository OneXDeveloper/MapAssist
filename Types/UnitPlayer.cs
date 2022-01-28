using MapAssist.Helpers;
using MapAssist.Structs;
using System;
using System.Collections.Generic;
using System.Text;

namespace MapAssist.Types
{
    public class UnitPlayer : UnitAny
    {
        public string Name { get; private set; }
        public Act Act { get; private set; }
        public Skills Skills { get; private set; }
        public List<State> StateList { get; private set; }
        public bool InParty { get; private set; }
        public bool IsHostile { get; private set; }
        public RosterEntry RosterEntry { get; private set; }

        public UnitPlayer(IntPtr ptrUnit) : base(ptrUnit)
        {
        }

        public new UnitPlayer Update()
        {
            if (base.Update())
            {
                using (var processContext = GameManager.GetProcessContext())
                {
                    Name = Encoding.ASCII.GetString(processContext.Read<byte>(Struct.pUnitData, 16)).TrimEnd((char)0);
                    Act = new Act(Struct.pAct);
                    //Inventory = processContext.Read<Inventory>(Struct.ptrInventory);
                    Skills = new Skills(Struct.pSkills);
                    StateList = GetStateList();
                }

                return this;
            }

            return null;
        }

        public UnitPlayer UpdateRosterEntry(Roster rosterData)
        {
            if (rosterData.EntriesByUnitId.TryGetValue(UnitId, out var rosterEntry))
            {
                RosterEntry = rosterEntry;
            }

            return this;
        }

        public UnitPlayer UpdateParties(RosterEntry player)
        {
            if (player != null)
            {
                if (player.PartyID != ushort.MaxValue && PartyID == player.PartyID)
                {
                    InParty = true;
                    IsHostile = false;
                }
                else
                {
                    InParty = false;
                    IsHostile = IsHostileTo(player);
                }
            }

            return this;
        }

        public bool IsPlayerUnit
        {
            get
            {
                if (Struct.pInventory != IntPtr.Zero)
                {
                    using (var processContext = GameManager.GetProcessContext())
                    {
                        var playerInfoPtr = processContext.Read<PlayerInfo>(GameManager.ExpansionCheckOffset);
                        var playerInfo = processContext.Read<PlayerInfoStrc>(playerInfoPtr.pPlayerInfo);
                        var expansionCharacter = playerInfo.Expansion;

                        var userBaseOffset = expansionCharacter ? 0x70 : 0x30;
                        var checkUser1 = expansionCharacter ? 0 : 1;

                        var userBaseCheck = processContext.Read<int>(IntPtr.Add(Struct.pInventory, userBaseOffset));
                        if (userBaseCheck != checkUser1)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        private ushort PartyID
        {
            get
            {
                if (RosterEntry != null)
                {
                    return RosterEntry.PartyID;
                }

                return ushort.MaxValue; // not in party
            }
        }

        private bool IsHostileTo(RosterEntry otherUnit)
        {
            if (UnitId == otherUnit.UnitId)
            {
                return false;
            }

            if (RosterEntry != null)
            {
                using (var processContext = GameManager.GetProcessContext())
                {
                    var hostileInfo = RosterEntry.HostileInfo;

                    while (true)
                    {
                        if (hostileInfo.UnitId == otherUnit.UnitId)
                        {
                            return hostileInfo.HostileFlag > 0;
                        }

                        if (hostileInfo.NextHostileInfo == IntPtr.Zero) break;
                        hostileInfo = processContext.Read<HostileInfo>(hostileInfo.NextHostileInfo);
                    }
                }
            }

            return false;
        }

        private List<State> GetStateList()
        {
            var stateList = new List<State>();
            for (var i = 0; i <= States.StateCount; i++)
            {
                if (GetState((State)i))
                {
                    stateList.Add((State)i);
                }
            }
            return stateList;
        }

        private bool GetState(State state)
        {
            return (StateFlags[(int)state >> 5] & StateMasks.gdwBitMasks[(int)state & 31]) > 0;
        }

        public override string HashString => Name + "/" + Position.X + "/" + Position.Y;
    }
}
