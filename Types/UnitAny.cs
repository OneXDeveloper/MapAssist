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
using System.Drawing;
using System.Linq;
using System.Text;
using System.Timers;
using MapAssist.Helpers;
using MapAssist.Interfaces;
using MapAssist.Settings;
using MapAssist.Structs;

namespace MapAssist.Types
{
    public class UnitAny : IUpdatable<UnitAny>
    {
        private readonly IntPtr _pUnit;
        private Structs.UnitAny _unitAny;
        private Act _act;
        private Path _path;
        private Inventory _inventory;
        private MonsterData _monsterData;
        private ItemData _itemData;
        private Dictionary<Stat, int> _statList;
        private List<Resist> _immunities;
        private uint[] _stateFlags;
        private List<State> _stateList;
        private string _name;
        private bool _isMonster;
        private bool _updated;

        public UnitAny(IntPtr pUnit)
        {
            _pUnit = pUnit;
            Update();
        }

        public UnitAny Update()
        {
            if (IsValidPointer())
            {
                using (var processContext = GameManager.GetProcessContext())
                {
                    _unitAny = processContext.Read<Structs.UnitAny>(_pUnit);
                    if (IsValidUnit())
                    {
                        _path = new Path(_unitAny.pPath);
                        var statListStruct = processContext.Read<StatListStruct>(_unitAny.pStatsListEx);
                        var statList = new Dictionary<Stat, int>();
                        var statValues = processContext.Read<StatValue>(statListStruct.Stats.FirstStatPtr,
                            Convert.ToInt32(statListStruct.Stats.Size));
                        foreach (var stat in statValues)
                        {
                            //ensure we dont add duplicates
                            if (!statList.TryGetValue(stat.Stat, out var _))
                            {
                                statList.Add(stat.Stat, stat.Value);
                            }
                        }

                        _statList = statList;
                        _immunities = GetImmunities();
                        switch (_unitAny.UnitType)
                        {
                            case UnitType.Player:
                                if (IsPlayer())
                                {
                                    _stateFlags = statListStruct.StateFlags;
                                    _stateList = GetStateList();
                                    _name = Encoding.ASCII.GetString(processContext.Read<byte>(_unitAny.pUnitData, 16))
                                        .TrimEnd((char)0);
                                    _inventory = processContext.Read<Inventory>(_unitAny.pInventory);
                                    _act = new Act(_unitAny.pAct);
                                }

                                break;
                            case UnitType.Monster:
                                if (IsMonster())
                                {
                                    _monsterData = processContext.Read<MonsterData>(_unitAny.pUnitData);
                                }

                                break;
                            case UnitType.Item:
                                if (MapAssistConfiguration.Loaded.ItemLog.Enabled)
                                {
                                    _itemData = processContext.Read<ItemData>(_unitAny.pUnitData);
                                    if (IsDropped())
                                    {
                                        var processId = processContext.ProcessId;
                                        Items.LogItem(this, processId);
                                    }
                                }
                                break;
                        }
                        _updated = true;
                    }
                }
            }

            return this;
        }

        public string Name => _name;
        public UnitType UnitType => _unitAny.UnitType;
        public uint TxtFileNo => _unitAny.TxtFileNo;
        public uint UnitId => _unitAny.UnitId;
        public uint Mode => _unitAny.Mode;
        public IntPtr UnitDataPtr => _unitAny.pUnitData;
        public Dictionary<Stat, int> Stats => _statList;
        public MonsterData MonsterData => _monsterData;
        public ItemData ItemData => _itemData;
        public Act Act => _act;
        public Path Path => _path;
        public IntPtr StatsListExPtr => _unitAny.pStatsListEx;
        public Inventory Inventory => _inventory;
        public uint OwnerType => _unitAny.OwnerType;
        public ushort X => IsMovable() ? _path.DynamicX : _path.StaticX;
        public ushort Y => IsMovable() ? _path.DynamicY : _path.StaticY;
        public Point Position => new Point(X, Y);
        public UnitAny ListNext => new UnitAny(_unitAny.pListNext);
        public UnitAny RoomNext => new UnitAny(_unitAny.pRoomNext);
        public List<Resist> Immunities => _immunities;
        public uint[] StateFlags => _stateFlags;
        public List<State> StateList => _stateList;

        public bool IsMovable()
        {
            return !(UnitType == UnitType.Object || UnitType == UnitType.Item);
        }

        public bool IsValidPointer()
        {
            return _pUnit != IntPtr.Zero;
        }
        public bool IsValidUnit()
        {
            return _unitAny.pUnitData != IntPtr.Zero && _unitAny.pStatsListEx != IntPtr.Zero && _unitAny.UnitType <= UnitType.Tile;
        }

        public bool IsPlayer()
        {
            return UnitType == UnitType.Player;
        }

        public bool IsPlayerUnit()
        {
            using (var processContext = GameManager.GetProcessContext())
            {
                if (IsPlayer() && _unitAny.pInventory != IntPtr.Zero)
                {
                    var expansionCharacter = processContext.Read<byte>(GameManager.ExpansionCheckOffset) == 1;
                    var userBaseOffset = 0x30;
                    var checkUser1 = 1;
                    if (expansionCharacter)
                    {
                        userBaseOffset = 0x70;
                        checkUser1 = 0;
                    }

                    var userBaseCheck = processContext.Read<int>(IntPtr.Add(_unitAny.pInventory, userBaseOffset));
                    if (userBaseCheck != checkUser1)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public bool IsMonster()
        {
            if (_updated)
            {
                return _isMonster;
            }
            else
            {
                if (_unitAny.UnitType != UnitType.Monster) return false;
                if (_unitAny.Mode == 0 || _unitAny.Mode == 12) return false;
                if (NPC.Dummies.TryGetValue(_unitAny.TxtFileNo, out var _)) { return false; }

                _isMonster = true;
                return true;
            }
        }

        public bool IsDropped()
        {
            var itemMode = (ItemMode)_unitAny.Mode;
            return itemMode == ItemMode.DROPPING || itemMode == ItemMode.ONGROUND;
        }

        public string ItemHash()
        {
            return Items.ItemNames[TxtFileNo] + "/" + Position.X + "/" + Position.Y;
        }

        private List<Resist> GetImmunities()
        {
            _statList.TryGetValue(Stat.STAT_DAMAGERESIST, out var resistanceDamage);
            _statList.TryGetValue(Stat.STAT_MAGICRESIST, out var resistanceMagic);
            _statList.TryGetValue(Stat.STAT_FIRERESIST, out var resistanceFire);
            _statList.TryGetValue(Stat.STAT_LIGHTRESIST, out var resistanceLightning);
            _statList.TryGetValue(Stat.STAT_COLDRESIST, out var resistanceCold);
            _statList.TryGetValue(Stat.STAT_POISONRESIST, out var resistancePoison);

            var resists = new List<int>
            {
                resistanceDamage,
                resistanceMagic,
                resistanceFire,
                resistanceLightning,
                resistanceCold,
                resistancePoison
            };
            var immunities = new List<Resist>();

            for (var i = 0; i < 6; i++)
            {
                if (resists[i] >= 100)
                {
                    immunities.Add((Resist)i);
                }
            }

            return immunities;
        }
        
        public bool GetState(State state)
        {
            return (StateFlags[(int)state >> 5] & StateMasks.gdwBitMasks[(int)state & 31]) > 0;
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

        public override bool Equals(object obj) => obj is UnitAny other && Equals(other);

        public bool Equals(UnitAny unit) => UnitId == unit.UnitId;

        public override int GetHashCode() => UnitId.GetHashCode();

        public static bool operator ==(UnitAny unit1, UnitAny unit2) => unit1.Equals(unit2);

        public static bool operator !=(UnitAny unit1, UnitAny unit2) => !(unit1 == unit2);
    }
}
