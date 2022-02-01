using MapAssist.Helpers;
using MapAssist.Structs;
using System;
using System.Collections.Generic;
using System.Text;
using static MapAssist.Types.Stats;

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
        public Dictionary<Stat, int> GetResists(int resPenalty)
        {
            var resists = new Dictionary<Stat, int>();
            resists.Add(Stat.FireResist, CalculateResist(Stat.FireResist, Stat.MaxFireResist, resPenalty));
            resists.Add(Stat.LightningResist, CalculateResist(Stat.LightningResist, Stat.MaxLightningResist, resPenalty));
            resists.Add(Stat.ColdResist, CalculateResist(Stat.ColdResist, Stat.MaxColdResist, resPenalty));
            resists.Add(Stat.PoisonResist, CalculateResist(Stat.PoisonResist, Stat.MaxPoisonResist, resPenalty));

            return resists;
        }

        private int CalculateResist(Stat _res, Stat _maxRes, int resPenalty)
        {
            Stats.TryGetValue(_res, out var res);
            Stats.TryGetValue(_maxRes, out var maxRes);
            return (res - resPenalty <= 75 + maxRes) ? res - resPenalty : 75 + maxRes;
        }
        public float HealthPercentage
        {
            get
            {
                if (Stats.TryGetValue(Stat.Life, out var health) &&
                    Stats.TryGetValue(Stat.MaxLife, out var maxHp) && maxHp > 0)
                {
                    return ((float)health / maxHp) * 100f;
                }
                return 0.0f;
            }
        }
        public float ManaPercentage
        {
            get
            {
                if (Stats.TryGetValue(Stat.Mana, out var mana) &&
                    Stats.TryGetValue(Stat.MaxMana, out var maxMana) && maxMana > 0)
                {
                    return ((float)mana / maxMana) * 100f;
                }
                return 0.0f;
            }
        }
        public long ActualExperience
        {
            get
            {
                const long maxInt = (long)int.MaxValue + 1;
                Stats.TryGetValue(Stat.Experience, out var exp);
                return exp < 0 ? maxInt + exp + maxInt : exp;
            }
        }
        public float LevelPercentage
        {
            get
            {
                if (Stats.TryGetValue(Stat.Level, out var lvl) && lvl > 0)
                {
                    var expBetweenLevels = PlayerLevelsExp[lvl + 1] - PlayerLevelsExp[lvl];
                    var expToLevelUp = PlayerLevelsExp[lvl + 1] - ActualExperience;
                    return 100f - ((float)expToLevelUp / expBetweenLevels * 100f);
                }
                return 0.0f;
            }
        }
        public static int GetPlayerStatShifted(UnitPlayer unitPlayer, Stat stat)
        {
            return unitPlayer.Stats.TryGetValue(stat, out var statValue) && StatShifts.TryGetValue(stat, out var shift) ? statValue >> shift : 0;
        }

        private bool GetState(State state)
        {
            return (StateFlags[(int)state >> 5] & StateMasks.gdwBitMasks[(int)state & 31]) > 0;
        }

        public override string HashString => Name + "/" + Position.X + "/" + Position.Y;

        public static readonly Dictionary<int, long> PlayerLevelsExp = new Dictionary<int, long>()
        {
            {1, 0},
            {2, 500},
            {3, 1500},
            {4, 3750},
            {5, 7875},
            {6, 14175},
            {7, 22680},
            {8, 32886},
            {9, 44396},
            {10, 57715},
            {11, 72144},
            {12, 90180},
            {13, 112725},
            {14, 140906},
            {15, 176132},
            {16, 220165},
            {17, 275207},
            {18, 344008},
            {19, 430010},
            {20, 537513},
            {21, 671891},
            {22, 839864},
            {23, 1049830},
            {24, 1312287},
            {25, 1640359},
            {26, 2050449},
            {27, 2563061},
            {28, 3203826},
            {29, 3902260},
            {30, 4663553},
            {31, 5493363},
            {32, 6397855},
            {33, 7383752},
            {34, 8458379},
            {35, 9629723},
            {36, 10906488},
            {37, 12298162},
            {38, 13815086},
            {39, 15468534},
            {40, 17270791},
            {41, 19235252},
            {42, 21376515},
            {43, 23710491},
            {44, 26254525},
            {45, 29027522},
            {46, 32050088},
            {47, 35344686},
            {48, 38935798},
            {49, 42850109},
            {50, 47116709},
            {51, 51767302},
            {52, 56836449},
            {53, 62361819},
            {54, 68384473},
            {55, 74949165},
            {56, 82104680},
            {57, 89904191},
            {58, 98405658},
            {59, 107672256},
            {60, 117772849},
            {61, 128782495},
            {62, 140783010},
            {63, 153863570},
            {64, 168121381},
            {65, 183662396},
            {66, 200602101},
            {67, 219066380},
            {68, 239192444},
            {69, 261129853},
            {70, 285041630},
            {71, 311105466},
            {72, 339515048},
            {73, 370481492},
            {74, 404234916},
            {75, 441026148},
            {76, 481128591},
            {77, 524840254},
            {78, 572485967},
            {79, 624419793},
            {80, 681027665},
            {81, 742730244},
            {82, 809986056},
            {83, 883294891},
            {84, 963201521},
            {85, 1050299747},
            {86, 1145236814},
            {87, 1248718217},
            {88, 1361512946},
            {89, 1484459201},
            {90, 1618470619},
            {91, 1764543065},
            {92, 1923762030},
            {93, 2097310703},
            {94, 2286478756},
            {95, 2492671933},
            {96, 2717422497},
            {97, 2962400612},
            {98, 3229426756},
            {99, 3520485254},
        };
    }
}
