using MapAssist.Helpers;
using MapAssist.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public float Life => Stats.TryGetValue(Types.Stats.Stat.Life, out var val) && Types.Stats.StatShifts.TryGetValue(Types.Stats.Stat.Life, out var shift) ? val >> shift : 0;
        public float MaxLife => Stats.TryGetValue(Types.Stats.Stat.MaxLife, out var val) && Types.Stats.StatShifts.TryGetValue(Types.Stats.Stat.Life, out var shift) ? val >> shift : 0;
        public float Mana => Stats.TryGetValue(Types.Stats.Stat.Mana, out var val) && Types.Stats.StatShifts.TryGetValue(Types.Stats.Stat.Life, out var shift) ? val >> shift : 0;
        public float MaxMana => Stats.TryGetValue(Types.Stats.Stat.MaxMana, out var val) && Types.Stats.StatShifts.TryGetValue(Types.Stats.Stat.Life, out var shift) ? val >> shift : 0;
        public float LifePercentage => 100f * Life / MaxLife;
        public float ManaPercentage => 100f * Mana / MaxMana;

        public long Experience
        {
            get
            {
                var maxInt = (long)int.MaxValue + 1;
                Stats.TryGetValue(Types.Stats.Stat.Experience, out var exp);
                return exp < 0 ? maxInt + exp + maxInt : exp;
            }
        }

        public float LevelProgress
        {
            get
            {
                if (Stats.TryGetValue(Types.Stats.Stat.Level, out var lvl) && lvl > 0)
                {
                    if (lvl == 99) return 100f;

                    var numer = Experience - PlayerLevelsExp[lvl - 1];
                    var denom = PlayerLevelsExp[lvl] - PlayerLevelsExp[lvl - 1];

                    return 100f * numer / denom;
                }

                return 0;
            }
        }

        public Dictionary<Resist, int> GetResists(Difficulty difficulty)
        {
            var penalty = (ushort)difficulty == 2 ? 100 : (ushort)difficulty == 1 ? 40 : 0;

            return new (Resist, Stats.Stat, Stats.Stat)[] {
                (Resist.Fire, Types.Stats.Stat.FireResist, Types.Stats.Stat.MaxFireResist),
                (Resist.Lightning, Types.Stats.Stat.LightningResist, Types.Stats.Stat.MaxLightningResist),
                (Resist.Cold, Types.Stats.Stat.ColdResist, Types.Stats.Stat.MaxColdResist),
                (Resist.Poison, Types.Stats.Stat.PoisonResist, Types.Stats.Stat.MaxPoisonResist),
            }.ToDictionary(item => item.Item1, item =>
            {
                Stats.TryGetValue(item.Item2, out var res);
                Stats.TryGetValue(item.Item3, out var maxRes);

                return Math.Min(res - penalty, 75 + maxRes);
            });
        }

        private bool GetState(State state)
        {
            return (StateFlags[(int)state >> 5] & StateMasks.gdwBitMasks[(int)state & 31]) > 0;
        }

        public override string HashString => Name + "/" + Position.X + "/" + Position.Y;

        private static readonly long[] PlayerLevelsExp = new long[]
        {
            0, 500, 1500, 3750, 7875, 14175, 22680, 32886, 44396, 57715, 72144, 90180, 112725, 140906, 176132, 220165, 275207, 344008, 430010, 537513, 671891, 839864, 1049830, 1312287, 1640359, 2050449, 2563061, 3203826, 3902260, 4663553, 5493363, 6397855, 7383752, 8458379, 9629723, 10906488, 12298162, 13815086, 15468534, 17270791, 19235252, 21376515, 23710491, 26254525, 29027522, 32050088, 35344686, 38935798, 42850109, 47116709, 51767302, 56836449, 62361819, 68384473, 74949165, 82104680, 89904191, 98405658, 107672256, 117772849, 128782495, 140783010, 153863570, 168121381, 183662396, 200602101, 219066380, 239192444, 261129853, 285041630, 311105466, 339515048, 370481492, 404234916, 441026148, 481128591, 524840254, 572485967, 624419793, 681027665, 742730244, 809986056, 883294891, 963201521, 1050299747, 1145236814, 1248718217, 1361512946, 1484459201, 1618470619, 1764543065, 1923762030, 2097310703, 2286478756, 2492671933, 2717422497, 2962400612, 3229426756, 3520485254
        };
    }
}
