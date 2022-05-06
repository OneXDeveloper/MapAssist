using MapAssist.Helpers;
using MapAssist.Interfaces;
using MapAssist.Structs;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace MapAssist.Types
{
    public class Skills : IUpdatable<Skills>
    {
        private readonly IntPtr _pSkills;
        public List<SkillPoints> AllSkills { get; set; }
        public Skill RightSkillId { get; set; }
        public Skill LeftSkillId { get; set; }
        public Skill UsedSkillId { get; set; }

        public Skills(IntPtr pSkills)
        {
            _pSkills = pSkills;
            Update();
        }

        public Skills Update()
        {
            using (var processContext = GameManager.GetProcessContext())
            {
                var skillList = processContext.Read<SkillList>(_pSkills);

                var skill = processContext.Read<SkillStrc>(skillList.pRightSkill);
                var skillTxt = processContext.Read<SkillTxt>(skill.SkillTxt);
                RightSkillId = skillTxt.Id;

                skill = processContext.Read<SkillStrc>(skillList.pLeftSkill);
                skillTxt = processContext.Read<SkillTxt>(skill.SkillTxt);
                LeftSkillId = skillTxt.Id;

                skill = processContext.Read<SkillStrc>(skillList.pUsedSkill);
                skillTxt = processContext.Read<SkillTxt>(skill.SkillTxt);
                UsedSkillId = skillTxt.Id;

                AllSkills = new List<SkillPoints>();
                var skillPtr = skillList.pFirstSkill;
                while (true)
                {
                    skill = processContext.Read<SkillStrc>(skillPtr);
                    skillTxt = processContext.Read<SkillTxt>(skill.SkillTxt);
                    AllSkills.Add(new SkillPoints()
                    {
                        Skill = skillTxt.Id,
                        HardPoints = skill.HardPoints,
                        Quantity = skill.Quantity,
                        Charges = skill.Charges
                    });

                    skillPtr = skill.pNextSkill;
                    if (skillPtr == IntPtr.Zero) break;
                }
            }

            return this;
        }
    }

    public enum Skill : short
    {
        Unset = -1,
        Attack = 0,
        Kick,
        Throw,
        Unsummon,
        LeftHandThrow,
        LeftHandSwing,
        MagicArrow,
        FireArrow,
        InnerSight,
        CriticalStrike,
        Jab,
        ColdArrow,
        MultipleShot,
        Dodge,
        PowerStrike,
        PoisonJavelin,
        ExplodingArrow,
        SlowMissiles,
        Avoid,
        Impale,
        LightningBolt,
        IceArrow,
        GuidedArrow,
        Penetrate,
        ChargedStrike,
        PlagueJavelin,
        Strafe,
        ImmolationArrow,
        Decoy,
        Evade,
        Fend,
        FreezingArrow,
        Valkyrie,
        Pierce,
        LightningStrike,
        LightningFury,
        FireBolt,
        Warmth,
        ChargedBolt,
        IceBolt,
        FrozenArmor,
        Inferno,
        StaticField,
        Telekinesis,
        FrostNova,
        IceBlast,
        Blaze,
        FireBall,
        Nova,
        Lightning,
        ShiverArmor,
        FireWall,
        Enchant,
        ChainLightning,
        Teleport,
        GlacialSpike,
        Meteor,
        ThunderStorm,
        EnergyShield,
        Blizzard,
        ChillingArmor,
        FireMastery,
        Hydra,
        LightningMastery,
        FrozenOrb,
        ColdMastery,
        AmplifyDamage,
        Teeth,
        BoneArmor,
        SkeletonMastery,
        RaiseSkeleton,
        DimVision,
        Weaken,
        PoisonDagger,
        CorpseExplosion,
        ClayGolem,
        IronMaiden,
        Terror,
        BoneWall,
        GolemMastery,
        RaiseSkeletalMage,
        Confuse,
        LifeTap,
        PoisonExplosion,
        BoneSpear,
        BloodGolem,
        Attract,
        Decrepify,
        BonePrison,
        SummonResist,
        IronGolem,
        LowerResist,
        PoisonNova,
        BoneSpirit,
        FireGolem,
        Revive,
        Sacrifice,
        Smite,
        Might,
        Prayer,
        ResistFire,
        HolyBolt,
        HolyFire,
        Thorns,
        Defiance,
        ResistCold,
        Zeal,
        Charge,
        BlessedAim,
        Cleansing,
        ResistLightning,
        Vengeance,
        BlessedHammer,
        Concentration,
        HolyFreeze,
        Vigor,
        Conversion,
        HolyShield,
        HolyShock,
        Sanctuary,
        Meditation,
        FistOfTheHeavens,
        Fanaticism,
        Conviction,
        Redemption,
        Salvation,
        Bash,
        BladeMastery,
        AxeMastery,
        MaceMastery,
        Howl,
        FindPotion,
        Leap,
        DoubleSwing,
        PolearmMastery,
        ThrowingMastery,
        SpearMastery,
        Taunt,
        Shout,
        Stun,
        DoubleThrow,
        IncreasedStamina,
        FindItem,
        LeapAttack,
        Concentrate,
        IronSkin,
        BattleCry,
        Frenzy,
        IncreasedSpeed,
        BattleOrders,
        GrimWard,
        Whirlwind,
        Berserk,
        NaturalResistance,
        WarCry,
        BattleCommand,
        FireHit,
        UnHolyBolt,
        SkeletonRaise,
        MaggotEgg,
        ShamanFire,
        MagottUp,
        MagottDown,
        MagottLay,
        AndrialSpray,
        Jump,
        SwarmMove,
        Nest,
        QuickStrike,
        VampireFireball,
        VampireFirewall,
        VampireMeteor,
        GargoyleTrap,
        SpiderLay,
        VampireHeal,
        VampireRaise,
        Submerge,
        FetishAura,
        FetishInferno,
        ZakarumHeal,
        Emerge,
        Resurrect,
        Bestow,
        MissileSkill1,
        MonTeleport,
        PrimeLightning,
        PrimeBolt,
        PrimeBlaze,
        PrimeFirewall,
        PrimeSpike,
        PrimeIceNova,
        PrimePoisonball,
        PrimePoisonNova,
        DiabLight,
        DiabCold,
        DiabFire,
        FingerMageSpider,
        DiabWall,
        DiabRun,
        DiabPrison,
        PoisonBallTrap,
        AndyPoisonBolt,
        HireableMissile,
        DesertTurret,
        ArcaneTower,
        MonBlizzard,
        Mosquito,
        CursedBallTrapRight,
        CursedBallTrapLeft,
        MonFrozenArmor,
        MonBoneArmor,
        MonBoneSpirit,
        MonCurseCast,
        HellMeteor,
        RegurgitatorEat,
        MonFrenzy,
        QueenDeath,
        ScrollOfIdentify,
        TomeOfIdentify,
        ScrollOfTownPortal,
        TomeOfTownPortal,
        Raven,
        PoisonCreeper,
        Werewolf,
        Lycanthropy,
        Firestorm,
        OakSage,
        SummonSpiritWolf,
        Werebear,
        MoltenBoulder,
        ArcticBlast,
        CarrionVine,
        FeralRage,
        Maul,
        Fissure,
        CycloneArmor,
        HeartOfWolverine,
        SummonDireWolf,
        Rabies,
        FireClaws,
        Twister,
        SolarCreeper,
        Hunger,
        ShockWave,
        Volcano,
        Tornado,
        SpiritOfBarbs,
        SummonGrizzly,
        Fury,
        Armageddon,
        Hurricane,
        FireBlast,
        ClawMastery,
        PsychicHammer,
        TigerStrike,
        DragonTalon,
        ShockWeb,
        BladeSentinel,
        BurstOfSpeed,
        FistsOfFire,
        DragonClaw,
        ChargedBoltSentry,
        WakeOfFire,
        WeaponBlock,
        CloakOfShadows,
        CobraStrike,
        BladeFury,
        Fade,
        ShadowWarrior,
        ClawsOfThunder,
        DragonTail,
        LightningSentry,
        WakeOfInferno,
        MindBlast,
        BladesOfIce,
        DragonFlight,
        DeathSentry,
        BladeShield,
        Venom,
        ShadowMaster,
        PhoenixStrike,
        WakeOfDestructionSentry,
        ImpInferno,
        ImpFireball,
        BaalTaunt,
        BaalCorpseExplode,
        BaalMonsterSpawn,
        CatapultChargedBall,
        CatapultSpikeBall,
        SuckBlood,
        CryHelp,
        HealingVortex,
        Teleport2,
        SelfResurrect,
        VineAttack,
        OverseerWhip,
        BarbsAura,
        WolverineAura,
        OakSageAura,
        ImpFireMissile,
        Impregnate,
        SiegeBeastStomp,
        MinionSpawner,
        CatapultBlizzard,
        CatapultPlague,
        CatapultMeteor,
        BoltSentry,
        CorpseCycler,
        DeathMaul,
        DefenseCurse,
        BloodMana,
        monInfernoSentry,
        monDeathSentry,
        sentryLightning,
        fenrisRage,
        BaalTentacle,
        BaalNova,
        BaalInferno,
        BaalColdMissiles,
        MegademonInferno,
        EvilHutSpawner,
        CountessFirewall,
        ImpBolt,
        HorrorArcticBlast,
        deathSentryLtng,
        VineCycler,
        BearSmite,
        Resurrect2,
        BloodLordFrenzy,
        BaalTeleport,
        ImpTeleport,
        BaalCloneTeleport,
        ZakarumLightning,
        VampireMissile,
        MephistoMissile,
        DoomKnightMissile,
        RogueMissile,
        HydraMissile,
        NecromageMissile,
        MonBow,
        MonFireArrow,
        MonColdArrow,
        MonExplodingArrow,
        MonFreezingArrow,
        MonPowerStrike,
        SuccubusBolt,
        MephFrostNova,
        MonIceSpear,
        ShamanIce,
        Diablogeddon,
        DeleriumChange,
        NihlathakCorpseExplosion,
        SerpentCharge,
        TrapNova,
        UnHolyBoltEx,
        ShamanFireEx,
        ImpFireMissileEx,
        FixedSiegeBeastStomp,

        Any = short.MaxValue
    };

    public enum SkillTree
    {
        AmazonBowAndCrossbow = 0,
        AmazonPassiveAndMagic = 1,
        AmazonJavelinAndSpear = 2,
        SorceressFire = 8,
        SorceressLightning = 9,
        SorceressCold = 10,
        NecromancerCurses = 16,
        NecromancerPoisonAndBone = 17,
        NecromancerSummoning = 18,
        PaladinCombatSkills = 24,
        PaladinOffensiveAuras = 25,
        PaladinDefensiveAuras = 26,
        BarbarianCombatSkills = 32,
        BarbarianMasteries = 33,
        BarbarianWarcries = 34,
        DruidSummoning = 40,
        DruidShapeShifting = 41,
        DruidElemental = 42,
        AssassinTraps = 48,
        AssassinShadowDisciplines = 49,
        AssassinMartialArts = 50,

        Any = 0xFFFF
    }

    public class SkillPoints
    {
        public Skill Skill;
        public uint HardPoints;
        public uint Quantity;
        public uint Charges;
    }

    public static class SkillExtensions
    {
        public static Dictionary<PlayerClass, SkillTree[]> ClassToSkillTreeDict = new Dictionary<PlayerClass, SkillTree[]>()
        {
            [PlayerClass.Amazon] = new SkillTree[] { SkillTree.AmazonBowAndCrossbow, SkillTree.AmazonPassiveAndMagic, SkillTree.AmazonJavelinAndSpear },
            [PlayerClass.Sorceress] = new SkillTree[] { SkillTree.SorceressFire, SkillTree.SorceressCold, SkillTree.SorceressLightning },
            [PlayerClass.Necromancer] = new SkillTree[] { SkillTree.NecromancerCurses, SkillTree.NecromancerPoisonAndBone, SkillTree.NecromancerSummoning },
            [PlayerClass.Paladin] = new SkillTree[] { SkillTree.PaladinCombatSkills, SkillTree.PaladinOffensiveAuras, SkillTree.PaladinDefensiveAuras },
            [PlayerClass.Barbarian] = new SkillTree[] { SkillTree.BarbarianCombatSkills, SkillTree.BarbarianMasteries, SkillTree.BarbarianWarcries },
            [PlayerClass.Druid] = new SkillTree[] { SkillTree.DruidSummoning, SkillTree.DruidShapeShifting, SkillTree.DruidElemental },
            [PlayerClass.Assassin] = new SkillTree[] { SkillTree.AssassinTraps, SkillTree.AssassinShadowDisciplines, SkillTree.AssassinMartialArts }
        };

        public static Dictionary<SkillTree, Skill[]> SkillTreeToSkillDict = new Dictionary<SkillTree, Skill[]>()
        {
            [SkillTree.AmazonBowAndCrossbow] = new Skill[] { Skill.MagicArrow, Skill.FireArrow, Skill.ColdArrow, Skill.MultipleShot, Skill.ExplodingArrow, Skill.IceArrow, Skill.GuidedArrow, Skill.ImmolationArrow, Skill.Strafe, Skill.FreezingArrow },
            [SkillTree.AmazonPassiveAndMagic] = new Skill[] { Skill.InnerSight, Skill.Dodge, Skill.CriticalStrike, Skill.SlowMissiles, Skill.Avoid, Skill.Penetrate, Skill.Decoy, Skill.Evade, Skill.Valkyrie, Skill.Pierce },
            [SkillTree.AmazonJavelinAndSpear] = new Skill[] { Skill.Jab, Skill.PowerStrike, Skill.PoisonJavelin, Skill.Impale, Skill.LightningBolt, Skill.ChargedStrike, Skill.PlagueJavelin, Skill.Fend, Skill.LightningStrike, Skill.LightningFury },
            [SkillTree.SorceressFire] = new Skill[] { Skill.FireBolt, Skill.Warmth, Skill.Inferno, Skill.Blaze, Skill.FireBall, Skill.FireWall, Skill.Enchant, Skill.Meteor, Skill.FireMastery, Skill.Hydra },
            [SkillTree.SorceressLightning] = new Skill[] { Skill.ChargedBolt, Skill.Telekinesis, Skill.StaticField, Skill.Lightning, Skill.Nova, Skill.ChainLightning, Skill.Teleport, Skill.ThunderStorm, Skill.EnergyShield, Skill.LightningMastery },
            [SkillTree.SorceressCold] = new Skill[] { Skill.IceBolt, Skill.FrozenArmor, Skill.FrostNova, Skill.IceBlast, Skill.ShiverArmor, Skill.GlacialSpike, Skill.Blizzard, Skill.ChillingArmor, Skill.FrozenOrb, Skill.ColdMastery },
            [SkillTree.NecromancerCurses] = new Skill[] { Skill.AmplifyDamage, Skill.DimVision, Skill.Weaken, Skill.IronMaiden, Skill.Terror, Skill.Confuse, Skill.LifeTap, Skill.Attract, Skill.Decrepify, Skill.LowerResist },
            [SkillTree.NecromancerPoisonAndBone] = new Skill[] { Skill.Teeth, Skill.BoneArmor, Skill.PoisonDagger, Skill.CorpseExplosion, Skill.BoneWall, Skill.PoisonExplosion, Skill.BoneSpear, Skill.BonePrison, Skill.PoisonNova, Skill.BoneSpirit },
            [SkillTree.NecromancerSummoning] = new Skill[] { Skill.RaiseSkeleton, Skill.SkeletonMastery, Skill.ClayGolem, Skill.GolemMastery, Skill.RaiseSkeletalMage, Skill.BloodGolem, Skill.SummonResist, Skill.IronGolem, Skill.FireGolem, Skill.Revive },
            [SkillTree.PaladinCombatSkills] = new Skill[] { Skill.Sacrifice, Skill.HolyBolt, Skill.Smite, Skill.Zeal, Skill.Charge, Skill.Vengeance, Skill.BlessedHammer, Skill.Conversion, Skill.HolyShield, Skill.FistOfTheHeavens },
            [SkillTree.PaladinOffensiveAuras] = new Skill[] { Skill.Might, Skill.HolyFire, Skill.Thorns, Skill.BlessedAim, Skill.Concentration, Skill.HolyFreeze, Skill.HolyShock, Skill.Sanctuary, Skill.Fanaticism, Skill.Conviction },
            [SkillTree.PaladinDefensiveAuras] = new Skill[] { Skill.Prayer, Skill.ResistFire, Skill.ResistCold, Skill.ResistLightning, Skill.Defiance, Skill.Cleansing, Skill.Vigor, Skill.Meditation, Skill.Redemption, Skill.Salvation },
            [SkillTree.BarbarianCombatSkills] = new Skill[] { Skill.Bash, Skill.DoubleSwing, Skill.Leap, Skill.DoubleThrow, Skill.Stun, Skill.LeapAttack, Skill.Concentrate, Skill.Frenzy, Skill.Whirlwind, Skill.Berserk },
            [SkillTree.BarbarianMasteries] = new Skill[] { Skill.BladeMastery, Skill.AxeMastery, Skill.MaceMastery, Skill.PolearmMastery, Skill.ThrowingMastery, Skill.SpearMastery, Skill.IncreasedStamina, Skill.IronSkin, Skill.IncreasedSpeed, Skill.NaturalResistance },
            [SkillTree.BarbarianWarcries] = new Skill[] { Skill.Howl, Skill.FindPotion, Skill.Shout, Skill.Taunt, Skill.BattleCry, Skill.FindItem, Skill.BattleOrders, Skill.GrimWard, Skill.WarCry, Skill.BattleCommand },
            [SkillTree.DruidSummoning] = new Skill[] { Skill.Raven, Skill.PoisonCreeper, Skill.OakSage, Skill.SummonSpiritWolf, Skill.CarrionVine, Skill.HeartOfWolverine, Skill.SummonDireWolf, Skill.SolarCreeper, Skill.SpiritOfBarbs, Skill.SummonGrizzly },
            [SkillTree.DruidShapeShifting] = new Skill[] { Skill.Werewolf, Skill.Lycanthropy, Skill.Werebear, Skill.Maul, Skill.FeralRage, Skill.FireClaws, Skill.Rabies, Skill.ShockWave, Skill.Hunger, Skill.Fury },
            [SkillTree.DruidElemental] = new Skill[] { Skill.Firestorm, Skill.MoltenBoulder, Skill.ArcticBlast, Skill.CycloneArmor, Skill.Fissure, Skill.Twister, Skill.Volcano, Skill.Tornado, Skill.Hurricane, Skill.Armageddon },
            [SkillTree.AssassinTraps] = new Skill[] { Skill.FireBlast, Skill.ShockWeb, Skill.BladeSentinel, Skill.ChargedBoltSentry, Skill.WakeOfFire, Skill.BladeFury, Skill.LightningSentry, Skill.WakeOfInferno, Skill.DeathSentry, Skill.BladeShield },
            [SkillTree.AssassinShadowDisciplines] = new Skill[] { Skill.ClawMastery, Skill.PsychicHammer, Skill.BurstOfSpeed, Skill.CloakOfShadows, Skill.WeaponBlock, Skill.Fade, Skill.ShadowWarrior, Skill.MindBlast, Skill.Venom, Skill.ShadowMaster },
            [SkillTree.AssassinMartialArts] = new Skill[] { Skill.TigerStrike, Skill.DragonTalon, Skill.FistsOfFire, Skill.DragonClaw, Skill.CobraStrike, Skill.ClawsOfThunder, Skill.BladesOfIce, Skill.DragonTail, Skill.DragonFlight, Skill.PhoenixStrike }
        };

        public static string Name(this SkillTree skillTree)
        {
            TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
            return string.Concat(skillTree.ToString().Select((x, j) => j > 0 && char.IsUpper(x) ? " " + x.ToString() : x.ToString()));
        }

        public static string Name(this Skill skill)
        {
            TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
            return string.Concat(skill.ToString().Select((x, j) => j > 0 && char.IsUpper(x) ? " " + x.ToString() : x.ToString()));
        }

        public static PlayerClass GetPlayerClass(this SkillTree skillTree)
        {
            return ClassToSkillTreeDict.Where(x => x.Value.Contains(skillTree)).Select(x => x.Key).FirstOrDefault();
        }

        public static SkillTree GetSkillTree(this Skill skill)
        {
            return SkillTreeToSkillDict.Where(x => x.Value.Contains(skill)).Select(x => x.Key).FirstOrDefault();
        }

        public static PlayerClass GetPlayerClass(this Skill skill)
        {
            return skill.GetSkillTree().GetPlayerClass();
        }
    }
}
