using MapAssist.Helpers;
using MapAssist.Structs;
using System;
using System.Collections.Generic;

namespace MapAssist.Types
{
    public class UnitMonster : UnitAny
    {
        public MonsterData MonsterData { get; set; }
        public MonStats MonsterStats { get; private set; }
        public List<Resist> Immunities { get; set; }
        public Npc Npc => (Npc)TxtFileNo;

        public UnitMonster(IntPtr ptrUnit) : base(ptrUnit)
        {
        }

        public new UnitMonster Update()
        {
            if (base.Update() == UpdateResult.Updated)
            {
                using (var processContext = GameManager.GetProcessContext())
                {
                    MonsterData = processContext.Read<MonsterData>(Struct.pUnitData);
                    MonsterStats = processContext.Read<MonStats>(MonsterData.pMonStats);
                    Immunities = GetImmunities();
                }

                return this;
            }

            return null;
        }

        private List<Resist> GetImmunities()
        {
            Stats.TryGetValue(Types.Stats.Stat.DamageReduced, out var resistanceDamage);
            Stats.TryGetValue(Types.Stats.Stat.MagicResist, out var resistanceMagic);
            Stats.TryGetValue(Types.Stats.Stat.FireResist, out var resistanceFire);
            Stats.TryGetValue(Types.Stats.Stat.LightningResist, out var resistanceLightning);
            Stats.TryGetValue(Types.Stats.Stat.ColdResist, out var resistanceCold);
            Stats.TryGetValue(Types.Stats.Stat.PoisonResist, out var resistancePoison);

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

        public float HealthPercentage
        {
            get
            {
                if (Stats.TryGetValue(Types.Stats.Stat.Life, out var health) &&
                    Stats.TryGetValue(Types.Stats.Stat.MaxLife, out var maxHp) && maxHp > 0)
                {
                    return (float)health / maxHp;
                }
                return 0.0f;
            }
        }

        public MonsterTypeFlags MonsterType
        {
            get
            {
                var monsterTypes = new List<MonsterTypeFlags>() {
                    MonsterTypeFlags.SuperUnique,
                    MonsterTypeFlags.Champion,
                    MonsterTypeFlags.Minion,
                    MonsterTypeFlags.Unique
                };

                foreach (var monType in monsterTypes)
                {
                    if ((MonsterData.MonsterType & monType) == monType)
                    {
                        return monType;
                    }
                }

                return MonsterTypeFlags.Other;
            }
        }

        public override string HashString => Npc + "/" + Position.X + "/" + Position.Y;
    }
}
