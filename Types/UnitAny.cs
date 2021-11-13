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
using System.Text;
using MapAssist.Helpers;
using MapAssist.Interfaces;
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
        private string _name;

        public UnitAny(IntPtr pUnit)
        {
            _pUnit = pUnit;
            Update();
        }

        public UnitAny Update()
        {
            if (IsValid())
            {
                using (var processContext = GameManager.GetProcessContext())
                {
                    _unitAny = processContext.Read<Structs.UnitAny>(_pUnit);
                    _path = new Path(_unitAny.pPath);
                    switch (_unitAny.UnitType)
                    {
                        case UnitType.Player:
                            if (IsPlayer())
                            {
                                _name = Encoding.ASCII.GetString(processContext.Read<byte>(_unitAny.pUnitData, 16)).TrimEnd((char)0);
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
        public MonsterData MonsterData => _monsterData;
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

        public bool IsMovable()
        {
            return !(UnitType == UnitType.Object || UnitType == UnitType.Item);
        }

        public bool IsValid()
        {
            return _pUnit != IntPtr.Zero;
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
                    var expansionCharacter = processContext.Read<byte>(processContext.FromOffset(Offsets.ExpansionCheck)) == 1;
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
            if (_unitAny.UnitType != UnitType.Monster) return false;
            if (_unitAny.Mode == 0 || _unitAny.Mode == 12) return false;
            if (NPC.Dummies.TryGetValue(_unitAny.TxtFileNo, out var _)) { return false; }
            return true;
        }

        public bool IsElite()
        {
            return _monsterData.MonsterType > 0;
        }

        public override bool Equals(object obj) => obj is UnitAny other && Equals(other);

        public bool Equals(UnitAny unit) => UnitId == unit.UnitId;

        public override int GetHashCode() => UnitId.GetHashCode();

        public static bool operator ==(UnitAny unit1, UnitAny unit2) => unit1.Equals(unit2);

        public static bool operator !=(UnitAny unit1, UnitAny unit2) => !(unit1 == unit2);
    }
}
