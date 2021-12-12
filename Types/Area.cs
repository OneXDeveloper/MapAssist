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
        private readonly static Dictionary<Area, AreaLabel> _areaLabels = new Dictionary<Area, AreaLabel>()
        {
            [Area.None] = {
                Name = "None",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.RogueEncampment] = {
                Name = "Rogue Encampment",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.BloodMoor] = {
                Name = "Blood Moor",
                Level = new int[] { 1, 36, 67 }
            },
            [Area.ColdPlains] = {
                Name = "Cold Plains",
                Level = new int[] { 2, 36, 68 }
            },
            [Area.StonyField] = {
                Name = "Stony Field",
                Level = new int[] { 4, 37, 68 }
            },
            [Area.DarkWood] = {
                Name = "Dark Wood",
                Level = new int[] { 5, 38, 68 }
            },
            [Area.BlackMarsh] = {
                Name = "Black Marsh",
                Level = new int[] { 6, 38, 69 }
            },
            [Area.TamoeHighland] = {
                Name = "Tamoe Highland",
                Level = new int[] { 8, 39, 69 }
            },
            [Area.DenOfEvil] = {
                Name = "Den of Evil",
                Level = new int[] { 1, 36, 79 }
            },
            [Area.CaveLevel1] = {
                Name = "Cave Level 1",
                Level = new int[] { 2, 36, 77 }
            },
            [Area.UndergroundPassageLevel1] = {
                Name = "Underground Passage Level 1",
                Level = new int[] { 4, 37, 69 }
            },
            [Area.HoleLevel1] = {
                Name = "Hole Level 1",
                Level = new int[] { 5, 38, 80 }
            },
            [Area.PitLevel1] = {
                Name = "Pit Level 1",
                Level = new int[] { 7, 39, 85 }
            },
            [Area.CaveLevel2] = {
                Name = "Cave Level 2",
                Level = new int[] { 2, 37, 78 }
            },
            [Area.UndergroundPassageLevel2] = {
                Name = "Underground Passage Level 2",
                Level = new int[] { 4, 38, 83 }
            },
            [Area.HoleLevel2] = {
                Name = "Hole Level 2",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.PitLevel2] = {
                Name = "Pit Level 2",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.BurialGrounds] = {
                Name = "Burial Grounds",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.Crypt] = {
                Name = "Crypt",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.Mausoleum] = {
                Name = "Mausoleum",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.ForgottenTower] = {
                Name = "Forgotten Tower",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.TowerCellarLevel1] = {
                Name = "Tower Cellar Level 1",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.TowerCellarLevel2] = {
                Name = "Tower Cellar Level 2",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.TowerCellarLevel3] = {
                Name = "Tower Cellar Level 3",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.TowerCellarLevel4] = {
                Name = "Tower Cellar Level 4",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.TowerCellarLevel5] = {
                Name = "Tower Cellar Level 5",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.MonasteryGate] = {
                Name = "Monastery Gate",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.OuterCloister] = {
                Name = "Outer Cloister",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.Barracks] = {
                Name = "Barracks",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.JailLevel1] = {
                Name = "Jail Level 1",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.JailLevel2] = {
                Name = "Jail Level 2",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.JailLevel3] = {
                Name = "Jail Level 3",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.InnerCloister] = {
                Name = "Inner Cloister",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.Cathedral] = {
                Name = "Cathedral",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.CatacombsLevel1] = {
                Name = "Catacombs Level 1",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.CatacombsLevel2] = {
                Name = "Catacombs Level 2",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.CatacombsLevel3] = {
                Name = "Catacombs Level 3",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.CatacombsLevel4] = {
                Name = "Catacombs Level 4",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.Tristram] = {
                Name = "Tristram",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.MooMooFarm] = {
                Name = "Cow Level",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.LutGholein] = {
                Name = "Lut Gholein",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.RockyWaste] = {
                Name = "Rocky Waste",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.DryHills] = {
                Name = "Dry Hills",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.FarOasis] = {
                Name = "Far Oasis",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.LostCity] = {
                Name = "Lost City",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.ValleyOfSnakes] = {
                Name = "Valley of Snakes",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.CanyonOfTheMagi] = {
                Name = "Canyon of the Magi",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.SewersLevel1Act2] = {
                Name = "Sewers Level 1",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.SewersLevel2Act2] = {
                Name = "Sewers Level 2",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.SewersLevel3Act2] = {
                Name = "Sewers Level 3",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.HaremLevel1] = {
                Name = "Harem Level 1",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.HaremLevel2] = {
                Name = "Harem Level 2",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.PalaceCellarLevel1] = {
                Name = "Palace Cellar Level 1",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.PalaceCellarLevel2] = {
                Name = "Palace Cellar Level 2",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.PalaceCellarLevel3] = {
                Name = "Palace Cellar Level 3",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.StonyTombLevel1] = {
                Name = "Stony Tomb Level 1",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.HallsOfTheDeadLevel1] = {
                Name = "Halls of the Dead Level 1",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.HallsOfTheDeadLevel2] = {
                Name = "Halls of the Dead Level 2",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.ClawViperTempleLevel1] = {
                Name = "Claw Viper Temple Level 1",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.StonyTombLevel2] = {
                Name = "Stony Tomb Level 2",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.HallsOfTheDeadLevel3] = {
                Name = "Halls of the Dead Level 3",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.ClawViperTempleLevel2] = {
                Name = "Claw Viper Temple Level 2",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.MaggotLairLevel1] = {
                Name = "Maggot Lair Level 1",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.MaggotLairLevel2] = {
                Name = "Maggot Lair Level 2",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.MaggotLairLevel3] = {
                Name = "Maggot Lair Level 3",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.AncientTunnels] = {
                Name = "Ancient Tunnels",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.TalRashasTomb1] = {
                Name = "Tal Rashas Tomb #1",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.TalRashasTomb2] = {
                Name = "Tal Rashas Tomb #2",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.TalRashasTomb3] = {
                Name = "Tal Rashas Tomb #3",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.TalRashasTomb4] = {
                Name = "Tal Rashas Tomb #4",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.TalRashasTomb5] = {
                Name = "Tal Rashas Tomb #5",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.TalRashasTomb6] = {
                Name = "Tal Rashas Tomb #6",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.TalRashasTomb7] = {
                Name = "Tal Rashas Tomb #7",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.DurielsLair] = {
                Name = "Duriels Lair",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.ArcaneSanctuary] = {
                Name = "Arcane Sanctuary",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.KurastDocks] = {
                Name = "Kurast Docks",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.SpiderForest] = {
                Name = "Spider Forest",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.GreatMarsh] = {
                Name = "Great Marsh",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.FlayerJungle] = {
                Name = "Flayer Jungle",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.LowerKurast] = {
                Name = "Lower Kurast",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.KurastBazaar] = {
                Name = "Kurast Bazaar",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.UpperKurast] = {
                Name = "Upper Kurast",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.KurastCauseway] = {
                Name = "Kurast Causeway",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.Travincal] = {
                Name = "Travincal",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.SpiderCave] = {
                Name = "Spider Cave",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.SpiderCavern] = {
                Name = "Spider Cavern",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.SwampyPitLevel1] = {
                Name = "Swampy Pit Level 1",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.SwampyPitLevel2] = {
                Name = "Swampy Pit Level 2",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.FlayerDungeonLevel1] = {
                Name = "Flayer Dungeon Level 1",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.FlayerDungeonLevel2] = {
                Name = "Flayer Dungeon Level 2",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.SwampyPitLevel3] = {
                Name = "Swampy Pit Level 3",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.FlayerDungeonLevel3] = {
                Name = "Flayer Dungeon Level 3",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.SewersLevel1Act3] = {
                Name = "Sewers Level 1",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.SewersLevel2Act3] = {
                Name = "Sewers Level 2",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.RuinedTemple] = {
                Name = "Ruined Temple",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.DisusedFane] = {
                Name = "Disused Fane",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.ForgottenReliquary] = {
                Name = "Forgotten Reliquary",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.ForgottenTemple] = {
                Name = "Forgotten Temple",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.RuinedFane] = {
                Name = "Ruined Fane",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.DisusedReliquary] = {
                Name = "Disused Reliquary",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.DuranceOfHateLevel1] = {
                Name = "Durance of Hate Level 1",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.DuranceOfHateLevel2] = {
                Name = "Durance of Hate Level 2",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.DuranceOfHateLevel3] = {
                Name = "Durance of Hate Level 3",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.ThePandemoniumFortress] = {
                Name = "Pandemonium Fortress",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.OuterSteppes] = {
                Name = "Outer Steppes",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.PlainsOfDespair] = {
                Name = "Plains of Despair",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.CityOfTheDamned] = {
                Name = "City of the Damned",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.RiverOfFlame] = {
                Name = "River of Flame",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.ChaosSanctuary] = {
                Name = "Chaos Sanctuary",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.Harrogath] = {
                Name = "Harrogath",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.BloodyFoothills] = {
                Name = "Bloody Foothills",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.FrigidHighlands] = {
                Name = "Frigid Highlands",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.ArreatPlateau] = {
                Name = "Arreat Plateau",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.CrystallinePassage] = {
                Name = "Crystalline Passage",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.FrozenRiver] = {
                Name = "Frozen River",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.GlacialTrail] = {
                Name = "Glacial Trail",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.DrifterCavern] = {
                Name = "Drifter Cavern",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.FrozenTundra] = {
                Name = "Frozen Tundra",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.TheAncientsWay] = {
                Name = "Ancients' Way",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.IcyCellar] = {
                Name = "Icy Cellar",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.ArreatSummit] = {
                Name = "Arreat Summit",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.NihlathaksTemple] = {
                Name = "Nihlathaks Temple",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.HallsOfAnguish] = {
                Name = "Halls of Anguish",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.HallsOfPain] = {
                Name = "Halls of Pain",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.HallsOfVaught] = {
                Name = "Halls of Vaught",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.Abaddon] = {
                Name = "Abaddon",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.PitOfAcheron] = {
                Name = "Pit of Acheron",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.InfernalPit] = {
                Name = "Infernal Pit",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.TheWorldStoneKeepLevel1] = {
                Name = "Worldstone Keep Level 1",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.TheWorldStoneKeepLevel2] = {
                Name = "Worldstone Keep Level 2",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.TheWorldStoneKeepLevel3] = {
                Name = "Worldstone Keep Level 3",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.ThroneOfDestruction] = {
                Name = "Throne of Destruction",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.TheWorldstoneChamber] = {
                Name = "Worldstone Chamber",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.MatronsDen] = {
                Name = "Matron's Den",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.ForgottenSands] = {
                Name = "Forgotten Sands",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.FurnaceOfPain] = {
                Name = "Furnace of Pain",
                Level = new int[] { 0, 0, 0 }
            },
            [Area.UberTristram] = {
                Name = "Uber Tristram",
                Level = new int[] { 0, 0, 0 }
            },
        };

        public static string Name(this Area area)
        {
            return _areaLabels.TryGetValue(area, out var label) ? label.Name : area.ToString();
        }

        public static bool IsValid(this Area area)
        {
            return (uint)area >= 1 && (uint)area <= 136;
        }
    }
}
