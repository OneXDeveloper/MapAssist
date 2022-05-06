using MapAssist.Helpers;
using MapAssist.Settings;
using System;
using System.Collections.Generic;

namespace MapAssist.Types
{
    public enum Area : uint
    {
        Abaddon = 125,
        AncientTunnels = 65,
        ArcaneSanctuary = 74,
        ArreatPlateau = 112,
        ArreatSummit = 120,
        Barracks = 28,
        BlackMarsh = 6,
        BloodMoor = 2,
        BloodyFoothills = 110,
        BurialGrounds = 17,
        CanyonOfTheMagi = 46,
        CatacombsLevel1 = 34,
        CatacombsLevel2 = 35,
        CatacombsLevel3 = 36,
        CatacombsLevel4 = 37,
        Cathedral = 33,
        CaveLevel1 = 9,
        CaveLevel2 = 13,
        ChaosSanctuary = 108,
        CityOfTheDamned = 106,
        ClawViperTempleLevel1 = 58,
        ClawViperTempleLevel2 = 61,
        ColdPlains = 3,
        Crypt = 18,
        CrystallinePassage = 113,
        DarkWood = 5,
        DenOfEvil = 8,
        DisusedFane = 95,
        DisusedReliquary = 99,
        DrifterCavern = 116,
        DryHills = 42,
        DuranceOfHateLevel1 = 100,
        DuranceOfHateLevel2 = 101,
        DuranceOfHateLevel3 = 102,
        DurielsLair = 73,
        FarOasis = 43,
        FlayerDungeonLevel1 = 88,
        FlayerDungeonLevel2 = 89,
        FlayerDungeonLevel3 = 91,
        FlayerJungle = 78,
        ForgottenReliquary = 96,
        ForgottenSands = 134,
        ForgottenTemple = 97,
        ForgottenTower = 20,
        FrigidHighlands = 111,
        FrozenRiver = 114,
        FrozenTundra = 117,
        FurnaceOfPain = 135,
        GlacialTrail = 115,
        GreatMarsh = 77,
        HallsOfAnguish = 122,
        HallsOfPain = 123,
        HallsOfTheDeadLevel1 = 56,
        HallsOfTheDeadLevel2 = 57,
        HallsOfTheDeadLevel3 = 60,
        HallsOfVaught = 124,
        HaremLevel1 = 50,
        HaremLevel2 = 51,
        Harrogath = 109,
        HoleLevel1 = 11,
        HoleLevel2 = 15,
        IcyCellar = 119,
        InfernalPit = 127,
        InnerCloister = 32,
        JailLevel1 = 29,
        JailLevel2 = 30,
        JailLevel3 = 31,
        KurastBazaar = 80,
        KurastCauseway = 82,
        KurastDocks = 75,
        LostCity = 44,
        LowerKurast = 79,
        LutGholein = 40,
        MaggotLairLevel1 = 62,
        MaggotLairLevel2 = 63,
        MaggotLairLevel3 = 64,
        MatronsDen = 133,
        Mausoleum = 19,
        MonasteryGate = 26,
        MooMooFarm = 39,
        NihlathaksTemple = 121,
        None = 0,
        OuterCloister = 27,
        OuterSteppes = 104,
        PalaceCellarLevel1 = 52,
        PalaceCellarLevel2 = 53,
        PalaceCellarLevel3 = 54,
        PitLevel1 = 12,
        PitLevel2 = 16,
        PitOfAcheron = 126,
        PlainsOfDespair = 105,
        RiverOfFlame = 107,
        RockyWaste = 41,
        RogueEncampment = 1,
        RuinedFane = 98,
        RuinedTemple = 94,
        SewersLevel1Act2 = 47,
        SewersLevel1Act3 = 92,
        SewersLevel2Act2 = 48,
        SewersLevel2Act3 = 93,
        SewersLevel3Act2 = 49,
        SpiderCave = 84,
        SpiderCavern = 85,
        SpiderForest = 76,
        StonyField = 4,
        StonyTombLevel1 = 55,
        StonyTombLevel2 = 59,
        SwampyPitLevel1 = 86,
        SwampyPitLevel2 = 87,
        SwampyPitLevel3 = 90,
        TalRashasTomb1 = 66,
        TalRashasTomb2 = 67,
        TalRashasTomb3 = 68,
        TalRashasTomb4 = 69,
        TalRashasTomb5 = 70,
        TalRashasTomb6 = 71,
        TalRashasTomb7 = 72,
        TamoeHighland = 7,
        TheAncientsWay = 118,
        ThePandemoniumFortress = 103,
        TheWorldstoneChamber = 132,
        TheWorldStoneKeepLevel1 = 128,
        TheWorldStoneKeepLevel2 = 129,
        TheWorldStoneKeepLevel3 = 130,
        ThroneOfDestruction = 131,
        TowerCellarLevel1 = 21,
        TowerCellarLevel2 = 22,
        TowerCellarLevel3 = 23,
        TowerCellarLevel4 = 24,
        TowerCellarLevel5 = 25,
        Travincal = 83,
        Tristram = 38,
        UberTristram = 136,
        UndergroundPassageLevel1 = 10,
        UndergroundPassageLevel2 = 14,
        UpperKurast = 81,
        ValleyOfSnakes = 45,
        MapsAncientTemple = 137,
        MapsDesecratedTemple = 138,
        MapsFrigidPlateau = 139,
        MapsInfernalTrial = 140,
        MapsRuinedCitadel = 141,
    }

    public static class AreaExtensions
    {
        public static Dictionary<string, LocalizedObj> LocalizedAreas = new Dictionary<string, LocalizedObj>();
        private static readonly Dictionary<Area, AreaLabel> _areaLabels = new Dictionary<Area, AreaLabel>()
        {
            [Area.None] = new AreaLabel() {
                Text = "None",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.RogueEncampment] = new AreaLabel() {
                Text = "Rogue Encampment",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.BloodMoor] = new AreaLabel() {
                Text = "Blood Moor",
                Level = new int[] { 1, 36, 67 }
            },
            [Area.ColdPlains] = new AreaLabel() {
                Text = "Cold Plains",
                Level = new int[] { 2, 36, 68 }
            },
            [Area.StonyField] = new AreaLabel() {
                Text = "Stony Field",
                Level = new int[] { 4, 37, 68 }
            },
            [Area.DarkWood] = new AreaLabel() {
                Text = "Dark Wood",
                Level = new int[] { 5, 38, 68 }
            },
            [Area.BlackMarsh] = new AreaLabel() {
                Text = "Black Marsh",
                Level = new int[] { 6, 38, 69 }
            },
            [Area.TamoeHighland] = new AreaLabel() {
                Text = "Tamoe Highland",
                Level = new int[] { 8, 39, 69 }
            },
            [Area.DenOfEvil] = new AreaLabel() {
                Text = "Den of Evil",
                Level = new int[] { 1, 36, 79 }
            },
            [Area.CaveLevel1] = new AreaLabel() {
                Text = "Cave Level 1",
                Level = new int[] { 2, 36, 77 }
            },
            [Area.UndergroundPassageLevel1] = new AreaLabel() {
                Text = "Underground Passage Level 1",
                Level = new int[] { 4, 37, 69 }
            },
            [Area.HoleLevel1] = new AreaLabel() {
                Text = "Hole Level 1",
                Level = new int[] { 5, 38, 80 }
            },
            [Area.PitLevel1] = new AreaLabel() {
                Text = "Pit Level 1",
                Level = new int[] { 7, 39, 85 }
            },
            [Area.CaveLevel2] = new AreaLabel() {
                Text = "Cave Level 2",
                Level = new int[] { 2, 37, 78 }
            },
            [Area.UndergroundPassageLevel2] = new AreaLabel() {
                Text = "Underground Passage Level 2",
                Level = new int[] { 4, 38, 85 }
            },
            [Area.HoleLevel2] = new AreaLabel() {
                Text = "Hole Level 2",
                Level = new int[] { 5, 39, 81 }
            },
            [Area.PitLevel2] = new AreaLabel() {
                Text = "Pit Level 2",
                Level = new int[] { 7, 40, 85 }
            },
            [Area.BurialGrounds] = new AreaLabel() {
                Text = "Burial Grounds",
                Level = new int[] { 3, 36, 80 }
            },
            [Area.Crypt] = new AreaLabel() {
                Text = "Crypt",
                Level = new int[] { 3, 37, 83 }
            },
            [Area.Mausoleum] = new AreaLabel() {
                Text = "Mausoleum",
                Level = new int[] { 3, 37, 85 }
            },
            [Area.ForgottenTower] = new AreaLabel() {
                Text = "Forgotten Tower",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.TowerCellarLevel1] = new AreaLabel() {
                Text = "Tower Cellar Level 1",
                Level = new int[] { 7, 38, 75 }
            },
            [Area.TowerCellarLevel2] = new AreaLabel() {
                Text = "Tower Cellar Level 2",
                Level = new int[] { 7, 39, 76 }
            },
            [Area.TowerCellarLevel3] = new AreaLabel() {
                Text = "Tower Cellar Level 3",
                Level = new int[] { 7, 40, 77 }
            },
            [Area.TowerCellarLevel4] = new AreaLabel() {
                Text = "Tower Cellar Level 4",
                Level = new int[] { 7, 41, 78 }
            },
            [Area.TowerCellarLevel5] = new AreaLabel() {
                Text = "Tower Cellar Level 5",
                Level = new int[] { 7, 42, 79 }
            },
            [Area.MonasteryGate] = new AreaLabel() {
                Text = "Monastery Gate",
                Level = new int[] { 8, 40, 70 }
            },
            [Area.OuterCloister] = new AreaLabel() {
                Text = "Outer Cloister",
                Level = new int[] { 9, 40, 70 }
            },
            [Area.Barracks] = new AreaLabel() {
                Text = "Barracks",
                Level = new int[] { 9, 40, 70 }
            },
            [Area.JailLevel1] = new AreaLabel() {
                Text = "Jail Level 1",
                Level = new int[] { 10, 41, 71 }
            },
            [Area.JailLevel2] = new AreaLabel() {
                Text = "Jail Level 2",
                Level = new int[] { 10, 41, 71 }
            },
            [Area.JailLevel3] = new AreaLabel() {
                Text = "Jail Level 3",
                Level = new int[] { 10, 41, 71 }
            },
            [Area.InnerCloister] = new AreaLabel() {
                Text = "Inner Cloister",
                Level = new int[] { 10, 41, 72 }
            },
            [Area.Cathedral] = new AreaLabel() {
                Text = "Cathedral",
                Level = new int[] { 11, 42, 72 }
            },
            [Area.CatacombsLevel1] = new AreaLabel() {
                Text = "Catacombs Level 1",
                Level = new int[] { 11, 42, 72 }
            },
            [Area.CatacombsLevel2] = new AreaLabel() {
                Text = "Catacombs Level 2",
                Level = new int[] { 11, 42, 73 }
            },
            [Area.CatacombsLevel3] = new AreaLabel() {
                Text = "Catacombs Level 3",
                Level = new int[] { 12, 43, 73 }
            },
            [Area.CatacombsLevel4] = new AreaLabel() {
                Text = "Catacombs Level 4",
                Level = new int[] { 12, 43, 73 }
            },
            [Area.Tristram] = new AreaLabel() {
                Text = "Tristram",
                Level = new int[] { 6, 39, 76 }
            },
            [Area.MooMooFarm] = new AreaLabel() {
                Text = "Moo Moo Farm",
                Level = new int[] { 28, 64, 81 }
            },
            [Area.LutGholein] = new AreaLabel() {
                Text = "Lut Gholein",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.RockyWaste] = new AreaLabel() {
                Text = "Rocky Waste",
                Level = new int[] { 14, 43, 75 }
            },
            [Area.DryHills] = new AreaLabel() {
                Text = "Dry Hills",
                Level = new int[] { 15, 44, 76 }
            },
            [Area.FarOasis] = new AreaLabel() {
                Text = "Far Oasis",
                Level = new int[] { 16, 45, 76 }
            },
            [Area.LostCity] = new AreaLabel() {
                Text = "Lost City",
                Level = new int[] { 17, 46, 77 }
            },
            [Area.ValleyOfSnakes] = new AreaLabel() {
                Text = "Valley of Snakes",
                Level = new int[] { 18, 46, 77 }
            },
            [Area.CanyonOfTheMagi] = new AreaLabel() {
                Text = "Canyon of the Magi",
                Level = new int[] { 16, 48, 79 }
            },
            [Area.SewersLevel1Act2] = new AreaLabel() {
                Text = "Sewers Level 1",
                Level = new int[] { 13, 43, 74 }
            },
            [Area.SewersLevel2Act2] = new AreaLabel() {
                Text = "Sewers Level 2",
                Level = new int[] { 13, 43, 74 }
            },
            [Area.SewersLevel3Act2] = new AreaLabel() {
                Text = "Sewers Level 3",
                Level = new int[] { 14, 44, 75 }
            },
            [Area.HaremLevel1] = new AreaLabel() {
                Text = "Harem Level 1",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.HaremLevel2] = new AreaLabel() {
                Text = "Harem Level 2",
                Level = new int[] { 13, 47, 78 }
            },
            [Area.PalaceCellarLevel1] = new AreaLabel() {
                Text = "Palace Cellar Level 1",
                Level = new int[] { 13, 47, 78 }
            },
            [Area.PalaceCellarLevel2] = new AreaLabel() {
                Text = "Palace Cellar Level 2",
                Level = new int[] { 13, 47, 78 }
            },
            [Area.PalaceCellarLevel3] = new AreaLabel() {
                Text = "Palace Cellar Level 3",
                Level = new int[] { 13, 48, 78 }
            },
            [Area.StonyTombLevel1] = new AreaLabel() {
                Text = "Stony Tomb Level 1",
                Level = new int[] { 12, 44, 85 }
            },
            [Area.HallsOfTheDeadLevel1] = new AreaLabel() {
                Text = "Halls of the Dead Level 1",
                Level = new int[] { 12, 44, 79 }
            },
            [Area.HallsOfTheDeadLevel2] = new AreaLabel() {
                Text = "Halls of the Dead Level 2",
                Level = new int[] { 13, 45, 81 }
            },
            [Area.ClawViperTempleLevel1] = new AreaLabel() {
                Text = "Claw Viper Temple Level 1",
                Level = new int[] { 14, 47, 82 }
            },
            [Area.StonyTombLevel2] = new AreaLabel() {
                Text = "Stony Tomb Level 2",
                Level = new int[] { 12, 44, 85 }
            },
            [Area.HallsOfTheDeadLevel3] = new AreaLabel() {
                Text = "Halls of the Dead Level 3",
                Level = new int[] { 13, 45, 82 }
            },
            [Area.ClawViperTempleLevel2] = new AreaLabel() {
                Text = "Claw Viper Temple Level 2",
                Level = new int[] { 14, 47, 83 }
            },
            [Area.MaggotLairLevel1] = new AreaLabel() {
                Text = "Maggot Lair Level 1",
                Level = new int[] { 17, 45, 84 }
            },
            [Area.MaggotLairLevel2] = new AreaLabel() {
                Text = "Maggot Lair Level 2",
                Level = new int[] { 17, 45, 84 }
            },
            [Area.MaggotLairLevel3] = new AreaLabel() {
                Text = "Maggot Lair Level 3",
                Level = new int[] { 17, 46, 85 }
            },
            [Area.AncientTunnels] = new AreaLabel() {
                Text = "Ancient Tunnels",
                Level = new int[] { 17, 46, 85 }
            },
            [Area.TalRashasTomb1] = new AreaLabel() {
                Text = "Tal Rasha's Tomb #1",
                Level = new int[] { 17, 49, 80 }
            },
            [Area.TalRashasTomb2] = new AreaLabel() {
                Text = "Tal Rasha's Tomb #2",
                Level = new int[] { 17, 49, 80 }
            },
            [Area.TalRashasTomb3] = new AreaLabel() {
                Text = "Tal Rasha's Tomb #3",
                Level = new int[] { 17, 49, 80 }
            },
            [Area.TalRashasTomb4] = new AreaLabel() {
                Text = "Tal Rasha's Tomb #4",
                Level = new int[] { 17, 49, 80 }
            },
            [Area.TalRashasTomb5] = new AreaLabel() {
                Text = "Tal Rasha's Tomb #5",
                Level = new int[] { 17, 49, 80 }
            },
            [Area.TalRashasTomb6] = new AreaLabel() {
                Text = "Tal Rasha's Tomb #6",
                Level = new int[] { 17, 49, 80 }
            },
            [Area.TalRashasTomb7] = new AreaLabel() {
                Text = "Tal Rasha's Tomb #7",
                Level = new int[] { 17, 49, 80 }
            },
            [Area.DurielsLair] = new AreaLabel() {
                Text = "Duriel's Lair",
                Level = new int[] { 17, 49, 80 }
            },
            [Area.ArcaneSanctuary] = new AreaLabel() {
                Text = "Arcane Sanctuary",
                Level = new int[] { 14, 48, 79 }
            },
            [Area.KurastDocks] = new AreaLabel() {
                Text = "Kurast Docktown",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.SpiderForest] = new AreaLabel() {
                Text = "Spider Forest",
                Level = new int[] { 21, 49, 79 }
            },
            [Area.GreatMarsh] = new AreaLabel() {
                Text = "Great Marsh",
                Level = new int[] { 21, 50, 80 }
            },
            [Area.FlayerJungle] = new AreaLabel() {
                Text = "Flayer Jungle",
                Level = new int[] { 22, 50, 80 }
            },
            [Area.LowerKurast] = new AreaLabel() {
                Text = "Lower Kurast",
                Level = new int[] { 22, 52, 80 }
            },
            [Area.KurastBazaar] = new AreaLabel() {
                Text = "Kurast Bazaar",
                Level = new int[] { 22, 52, 81 }
            },
            [Area.UpperKurast] = new AreaLabel() {
                Text = "Upper Kurast",
                Level = new int[] { 23, 52, 81 }
            },
            [Area.KurastCauseway] = new AreaLabel() {
                Text = "Kurast Causeway",
                Level = new int[] { 24, 53, 81 }
            },
            [Area.Travincal] = new AreaLabel() {
                Text = "Travincal",
                Level = new int[] { 24, 54, 82 }
            },
            [Area.SpiderCave] = new AreaLabel() {
                Text = "Arachnid Lair",
                Level = new int[] { 21, 50, 85 }
            },
            [Area.SpiderCavern] = new AreaLabel() {
                Text = "Spider Cavern",
                Level = new int[] { 21, 50, 79 }
            },
            [Area.SwampyPitLevel1] = new AreaLabel() {
                Text = "Swampy Pit Level 1",
                Level = new int[] { 21, 51, 85 }
            },
            [Area.SwampyPitLevel2] = new AreaLabel() {
                Text = "Swampy Pit Level 2",
                Level = new int[] { 21, 51, 85 }
            },
            [Area.FlayerDungeonLevel1] = new AreaLabel() {
                Text = "Flayer Dungeon Level 1",
                Level = new int[] { 22, 51, 81 }
            },
            [Area.FlayerDungeonLevel2] = new AreaLabel() {
                Text = "Flayer Dungeon Level 2",
                Level = new int[] { 22, 51, 82 }
            },
            [Area.SwampyPitLevel3] = new AreaLabel() {
                Text = "Swampy Pit Level 3",
                Level = new int[] { 21, 51, 85 }
            },
            [Area.FlayerDungeonLevel3] = new AreaLabel() {
                Text = "Flayer Dungeon Level 3",
                Level = new int[] { 22, 51, 83 }
            },
            [Area.SewersLevel1Act3] = new AreaLabel() {
                Text = "Sewers Level 1",
                Level = new int[] { 23, 52, 85 }
            },
            [Area.SewersLevel2Act3] = new AreaLabel() {
                Text = "Sewers Level 2",
                Level = new int[] { 24, 53, 85 }
            },
            [Area.RuinedTemple] = new AreaLabel() {
                Text = "Ruined Temple",
                Level = new int[] { 23, 53, 85 }
            },
            [Area.DisusedFane] = new AreaLabel() {
                Text = "Disused Fane",
                Level = new int[] { 23, 53, 85 }
            },
            [Area.ForgottenReliquary] = new AreaLabel() {
                Text = "Forgotten Reliquary",
                Level = new int[] { 23, 53, 85 }
            },
            [Area.ForgottenTemple] = new AreaLabel() {
                Text = "Forgotten Temple",
                Level = new int[] { 24, 54, 85 }
            },
            [Area.RuinedFane] = new AreaLabel() {
                Text = "Ruined Fane",
                Level = new int[] { 24, 54, 85 }
            },
            [Area.DisusedReliquary] = new AreaLabel() {
                Text = "Disused Reliquary",
                Level = new int[] { 24, 54, 85 }
            },
            [Area.DuranceOfHateLevel1] = new AreaLabel() {
                Text = "Durance of Hate Level 1",
                Level = new int[] { 25, 55, 83 }
            },
            [Area.DuranceOfHateLevel2] = new AreaLabel() {
                Text = "Durance of Hate Level 2",
                Level = new int[] { 25, 55, 83 }
            },
            [Area.DuranceOfHateLevel3] = new AreaLabel() {
                Text = "Durance of Hate Level 3",
                Level = new int[] { 25, 55, 83 }
            },
            [Area.ThePandemoniumFortress] = new AreaLabel() {
                Text = "Pandemonium Fortress",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.OuterSteppes] = new AreaLabel() {
                Text = "Outer Steppes",
                Level = new int[] { 26, 56, 82 }
            },
            [Area.PlainsOfDespair] = new AreaLabel() {
                Text = "Plains of Despair",
                Level = new int[] { 26, 56, 83 }
            },
            [Area.CityOfTheDamned] = new AreaLabel() {
                Text = "City of the Damned",
                Level = new int[] { 27, 57, 84 }
            },
            [Area.RiverOfFlame] = new AreaLabel() {
                Text = "River of Flame",
                Level = new int[] { 27, 57, 85 }
            },
            [Area.ChaosSanctuary] = new AreaLabel() {
                Text = "Chaos Sanctuary",
                Level = new int[] { 28, 58, 85 }
            },
            [Area.Harrogath] = new AreaLabel() {
                Text = "Harrogath",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.BloodyFoothills] = new AreaLabel() {
                Text = "Bloody Foothills",
                Level = new int[] { 24, 58, 80 }
            },
            [Area.FrigidHighlands] = new AreaLabel() {
                Text = "Frigid Highlands",
                Level = new int[] { 25, 59, 81 }
            },
            [Area.ArreatPlateau] = new AreaLabel() {
                Text = "Arreat Plateau",
                Level = new int[] { 26, 60, 81 }
            },
            [Area.CrystallinePassage] = new AreaLabel() {
                Text = "Crystalline Passage",
                Level = new int[] { 29, 61, 82 }
            },
            [Area.FrozenRiver] = new AreaLabel() {
                Text = "Frozen River",
                Level = new int[] { 29, 61, 83 }
            },
            [Area.GlacialTrail] = new AreaLabel() {
                Text = "Glacial Trail",
                Level = new int[] { 29, 61, 83 }
            },
            [Area.DrifterCavern] = new AreaLabel() {
                Text = "Drifter Cavern",
                Level = new int[] { 29, 61, 85 }
            },
            [Area.FrozenTundra] = new AreaLabel() {
                Text = "Frozen Tundra",
                Level = new int[] { 27, 60, 81 }
            },
            [Area.TheAncientsWay] = new AreaLabel() {
                Text = "Ancients' Way",
                Level = new int[] { 29, 62, 82 }
            },
            [Area.IcyCellar] = new AreaLabel() {
                Text = "Icy Cellar",
                Level = new int[] { 29, 62, 85 }
            },
            [Area.ArreatSummit] = new AreaLabel() {
                Text = "Arreat Summit",
                Level = new int[] { 37, 68, 87 }
            },
            [Area.NihlathaksTemple] = new AreaLabel() {
                Text = "Nihlathaks Temple",
                Level = new int[] { 32, 63, 83 }
            },
            [Area.HallsOfAnguish] = new AreaLabel() {
                Text = "Halls of Anguish",
                Level = new int[] { 33, 63, 83 }
            },
            [Area.HallsOfPain] = new AreaLabel() {
                Text = "Halls of Death's Calling",
                Level = new int[] { 34, 64, 84 }
            },
            [Area.HallsOfVaught] = new AreaLabel() {
                Text = "Halls of Vaught",
                Level = new int[] { 36, 64, 84 }
            },
            [Area.Abaddon] = new AreaLabel() {
                Text = "Abaddon",
                Level = new int[] { 39, 60, 85 }
            },
            [Area.PitOfAcheron] = new AreaLabel() {
                Text = "Pit of Acheron",
                Level = new int[] { 39, 61, 85 }
            },
            [Area.InfernalPit] = new AreaLabel() {
                Text = "Infernal Pit",
                Level = new int[] { 39, 62, 85 }
            },
            [Area.TheWorldStoneKeepLevel1] = new AreaLabel() {
                Text = "Worldstone Keep Level 1",
                Level = new int[] { 39, 65, 85 }
            },
            [Area.TheWorldStoneKeepLevel2] = new AreaLabel() {
                Text = "Worldstone Keep Level 2",
                Level = new int[] { 40, 65, 85 }
            },
            [Area.TheWorldStoneKeepLevel3] = new AreaLabel() {
                Text = "Worldstone Keep Level 3",
                Level = new int[] { 42, 66, 85 }
            },
            [Area.ThroneOfDestruction] = new AreaLabel() {
                Text = "Throne of Destruction",
                Level = new int[] { 43, 66, 85 }
            },
            [Area.TheWorldstoneChamber] = new AreaLabel() {
                Text = "Worldstone Chamber",
                Level = new int[] { 43, 66, 85 }
            },
            [Area.MatronsDen] = new AreaLabel() {
                Text = "Pandemonium Run 1",
                Level = new int[] { 0, 0, 83 }
            },
            [Area.ForgottenSands] = new AreaLabel() {
                Text = "Pandemonium Run 2",
                Level = new int[] { 0, 0, 83 }
            },
            [Area.FurnaceOfPain] = new AreaLabel() {
                Text = "Pandemonium Run 3",
                Level = new int[] { 0, 0, 83 }
            },
            [Area.UberTristram] = new AreaLabel() {
                Text = "Tristram",
                Level = new int[] { 0, 0, 83 }
            },
        };

        private static readonly HashSet<Area> StitchedAreas = new HashSet<Area>()
        {
            Area.RogueEncampment,
            Area.BloodMoor,
            Area.ColdPlains,
            Area.StonyField,
            Area.DarkWood,
            Area.BlackMarsh,
            Area.TamoeHighland,
            Area.BurialGrounds,
            Area.MonasteryGate,
            Area.OuterCloister,
            Area.Barracks,
            Area.InnerCloister,
            Area.Cathedral,
            Area.LutGholein,
            Area.RockyWaste,
            Area.DryHills,
            Area.FarOasis,
            Area.LostCity,
            Area.ValleyOfSnakes,
            Area.CanyonOfTheMagi,
            Area.KurastDocks,
            Area.SpiderForest,
            Area.GreatMarsh,
            Area.FlayerJungle,
            Area.LowerKurast,
            Area.KurastBazaar,
            Area.UpperKurast,
            Area.KurastCauseway,
            Area.Travincal,
            Area.ThePandemoniumFortress,
            Area.OuterSteppes,
            Area.PlainsOfDespair,
            Area.CityOfTheDamned,
            Area.RiverOfFlame,
            Area.ChaosSanctuary,
            Area.Harrogath,
            Area.BloodyFoothills,
            Area.FrigidHighlands,
            Area.ArreatPlateau,
        };

        public static string MapLabel(this Area area, Difficulty difficulty)
        {
            var label = area.Name();
            var areaLevel = area.Level(difficulty);
            if (areaLevel > 0)
            {
                label += $" ({areaLevel})";
            }
            return label;
        }

        public static string PortalLabel(this Area area, Difficulty difficulty, string playerName = null)
        {
            if (playerName != null)
            {
                switch (area)
                {
                    case Area.RogueEncampment:
                    case Area.LutGholein:
                    case Area.KurastDocks:
                    case Area.ThePandemoniumFortress:
                    case Area.Harrogath:
                        return $"TP ({playerName})";

                    default:
                        return $"{area.Name()} ({playerName})";
                }
            }
            return area.MapLabel(difficulty);
        }

        public static string NameFromKey(string key)
        {
            LocalizedObj localItem;
            if (!LocalizedAreas.TryGetValue(key, out localItem))
            {
                return "AreaNameNotFound";
            }
            var lang = MapAssistConfiguration.Loaded.LanguageCode;
            var prop = localItem.GetType().GetProperty(lang.ToString()).GetValue(localItem, null);
            return prop.ToString();
        }

        public static string Name(this Area area)
        {
            var areaLabel = _areaLabels.TryGetValue(area, out var label) ? label.Text : area.ToString();

            LocalizedObj localItem;
            if (!LocalizedAreas.TryGetValue(areaLabel, out localItem))
            {
                return area.ToString();
            }
            var lang = MapAssistConfiguration.Loaded.LanguageCode;
            var prop = localItem.GetType().GetProperty(lang.ToString()).GetValue(localItem, null);
            return prop.ToString();
        }

        public static string NameInternal(this Area area)
        {
            return _areaLabels.TryGetValue(area, out var label) ? label.Text : area.ToString();
        }

        public static int Level(this Area area, Difficulty difficulty)
        {
            return _areaLabels.TryGetValue(area, out var label) ? label.Level[(int)difficulty] : 0;
        }

        public static bool IsValid(this Area area)
        {
            return (uint)area >= 1 && (uint)area <= 136;
        }

        public static bool RequiresStitching(this Area area)
        {
            return StitchedAreas.Contains(area);
        }
        public static bool IsTown(this Area area)
        {
            return area == Area.RogueEncampment ||
                area == Area.LutGholein ||
                area == Area.KurastDocks ||
                area == Area.ThePandemoniumFortress ||
                area == Area.Harrogath;
        }
    }
}
