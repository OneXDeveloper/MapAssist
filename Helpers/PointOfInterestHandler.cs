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
using System.Linq;
using System.Threading.Tasks;
using GameOverlay.Drawing;
using MapAssist.Settings;
using MapAssist.Types;

namespace MapAssist.Helpers
{
    public static class PointOfInterestHandler
    {
        private static readonly Dictionary<Area, Area> AreaPreferredNextArea = new Dictionary<Area, Area>()
        {
            [Area.BloodMoor] = Area.ColdPlains,
            [Area.ColdPlains] = Area.StonyField,
            [Area.UndergroundPassageLevel1] = Area.DarkWood,
            [Area.DarkWood] = Area.BlackMarsh,
            [Area.BlackMarsh] = Area.TamoeHighland,
            [Area.RockyWaste] = Area.DryHills,
            [Area.DryHills] = Area.FarOasis,
            [Area.FarOasis] = Area.LostCity,
            [Area.SpiderForest] = Area.GreatMarsh,
            [Area.FlayerJungle] = Area.LowerKurast,
            [Area.KurastBazaar] = Area.UpperKurast,
            [Area.UpperKurast] = Area.KurastCauseway,
            [Area.KurastCauseway] = Area.Travincal,
        };

        private static readonly Dictionary<Area, Area> AreaPreferredQuestArea = new Dictionary<Area, Area>()
        {
            [Area.BloodMoor] = Area.DenOfEvil,
            [Area.ColdPlains] = Area.BurialGrounds,
            [Area.BlackMarsh] = Area.ForgottenTower,
            [Area.DryHills] = Area.HallsOfTheDeadLevel1,
            [Area.FarOasis] = Area.MaggotLairLevel1,
            [Area.LostCity] = Area.ValleyOfSnakes,
            [Area.SpiderForest] = Area.SpiderCavern,
            [Area.FlayerJungle] = Area.FlayerDungeonLevel1,
            [Area.KurastBazaar] = Area.RuinedTemple,
            [Area.CrystallinePassage] = Area.FrozenRiver,
        };

        private static readonly Dictionary<Area, Dictionary<GameObject, string>> AreaSpecificQuestObjects = new Dictionary<Area, Dictionary<GameObject, string>>()
        {
            [Area.StonyField] = new Dictionary<GameObject, string>()
            {
                [GameObject.CairnStoneAlpha] = "Tristram",
            },
            [Area.MatronsDen] = new Dictionary<GameObject, string>()
            {
                [GameObject.SparklyChest] = "Lilith",
            },
            [Area.FurnaceOfPain] = new Dictionary<GameObject, string>()
            {
                [GameObject.SparklyChest] = "Uber Izual",
            },
            [Area.PalaceCellarLevel3] = new Dictionary<GameObject, string>()
            {
                [GameObject.ArcaneSanctuaryPortal] = "Arcane Sanctuary",
            },
        };

        private static readonly Dictionary<Area, Dictionary<GameObject, Area>> AreaPortals = new Dictionary<Area, Dictionary<GameObject, Area>>()
        {
            [Area.FrigidHighlands] = new Dictionary<GameObject, Area>()
            {
                [GameObject.PermanentTownPortal] = Area.Abaddon,
            },
            [Area.ArreatPlateau] = new Dictionary<GameObject, Area>()
            {
                [GameObject.PermanentTownPortal] = Area.PitOfAcheron,
            },
            [Area.FrozenTundra] = new Dictionary<GameObject, Area>()
            {
                [GameObject.PermanentTownPortal] = Area.InfernalPit,
            },
        };

        private static readonly Dictionary<GameObject, string> QuestObjects = new Dictionary<GameObject, string>
        {
            [GameObject.CairnStoneAlpha] = "Tristram",
            [GameObject.WirtCorpse] = "Wirt's Leg",
            [GameObject.InifussTree] = "Inifuss Tree",
            [GameObject.Malus] = "Malus",
            [GameObject.HoradricScrollChest] = "Horadric Scroll",
            [GameObject.HoradricCubeChest] = "Horadric Cube",
            [GameObject.StaffOfKingsChest] = "Staff of Kings",
            [GameObject.YetAnotherTome] = "Summoner",
            [GameObject.HoradricOrifice] = "Orifice",
            [GameObject.KhalimChest1] = "Khalim's Heart",
            [GameObject.KhalimChest2] = "Khalim's Brain",
            [GameObject.KhalimChest3] = "Khalim's Eye",
            [GameObject.GidbinnAltarDecoy] = "Gidbinn",
            [GameObject.HellForge] = "Hell Forge",
            [GameObject.DrehyaWildernessStartPosition] = "Anya",
            [GameObject.NihlathakWildernessStartPosition] = "Nihlathak",
            [GameObject.CagedWussie] = "Prisoners",
        };

        private static readonly HashSet<GameObject> SuperChests = new HashSet<GameObject>
        {
            GameObject.GoodChest,
            GameObject.SparklyChest,
            GameObject.ArcaneLargeChestLeft,
            GameObject.ArcaneLargeChestRight,
            GameObject.ArcaneSmallChestLeft,
            GameObject.ArcaneSmallChestRight,
            GameObject.ExpansionSpecialChest,
        };

        private static readonly HashSet<GameObject> NormalChests = new HashSet<GameObject>
        {
            GameObject.LargeChestRight,
            GameObject.LargeChestLeft,
            GameObject.TombLargeChestL,
            GameObject.TombLargeChestR,
            GameObject.Act1LargeChestRight,
            GameObject.Act1TallChestRight,
            GameObject.Act1MediumChestRight,
            GameObject.Act1LargeChest1,
            GameObject.Act2MediumChestRight,
            GameObject.Act2LargeChestRight,
            GameObject.Act2LargeChestLeft,
            GameObject.MediumChestLeft,
            GameObject.LargeChestLeft2,
            GameObject.JungleChest,
            GameObject.JungleMediumChestLeft,
            GameObject.TallChestLeft,
            GameObject.Gchest1L,
            GameObject.Gchest2R,
            GameObject.Gchest3R,
            GameObject.GLchest3L,
            GameObject.MafistoLargeChestLeft,
            GameObject.MafistoLargeChestRight,
            GameObject.MafistoMediumChestLeft,
            GameObject.MafistoMediumChestRight,
            GameObject.SpiderLairLargeChestLeft,
            GameObject.SpiderLairTallChestLeft,
            GameObject.SpiderLairMediumChestRight,
            GameObject.SpiderLairTallChestRight,
            GameObject.HoradricCubeChest,
            GameObject.HoradricScrollChest,
            GameObject.StaffOfKingsChest,
            GameObject.LargeChestR,
            GameObject.InnerHellBoneChest,
            GameObject.KhalimChest1,
            GameObject.KhalimChest2,
            GameObject.KhalimChest3,
            GameObject.ExpansionChestRight,
            GameObject.ExpansionWoodChestLeft,
            GameObject.BurialChestLeft,
            GameObject.BurialChestRight,
            GameObject.ExpansionChestLeft,
            GameObject.ExpansionWoodChestRight,
            GameObject.ExpansionSmallChestLeft,
            GameObject.ExpansionSmallChestRight,
            GameObject.ExpansionExplodingChest,
            GameObject.ExpansionSnowyWoodChestLeft,
            GameObject.ExpansionSnowyWoodChestRight,
            GameObject.ExpansionSnowyWoodChest2Left,
            GameObject.ExpansionSnowyWoodChest2Right,
            GameObject.NotSoGoodChest,
        };

        private static readonly HashSet<GameObject> ArmorWeapRacks = new HashSet<GameObject>
        {
            GameObject.ExpansionArmorStandRight,
            GameObject.ExpansionArmorStandLeft,
            GameObject.ArmorStandRight,
            GameObject.ArmorStandLeft,
            GameObject.ExpansionWeaponRackRight,
            GameObject.ExpansionWeaponRackLeft,
            GameObject.WeaponRackRight,
            GameObject.WeaponRackLeft,
        };

        private static readonly HashSet<GameObject> Shrines = new HashSet<GameObject>
        {
            GameObject.Shrine,
            GameObject.HornShrine,
            GameObject.ForestAltar,
            GameObject.DesertShrine1,
            GameObject.DesertShrine2,
            GameObject.DesertShrine3,
            GameObject.DesertShrine4,
            GameObject.DesertShrine5,
            GameObject.SteleDesertMagicShrine,
            GameObject.JungleShrine2,
            GameObject.JungleShrine3,
            GameObject.JungleShrine4,
            GameObject.JungleShrine5,
        };

        public static List<PointOfInterest> Get(MapApi mapApi, AreaData areaData, GameData gameData)
        {
            var pointOfInterest = new List<PointOfInterest>();
            var areaRenderDecided = new List<Area>();

            if (areaData.Area == Area.UberTristram) return pointOfInterest; // No actual points of interest here, Wirt's leg appears without this line

            switch (areaData.Area)
            {
                case Area.CanyonOfTheMagi:
                    // Work out which tomb is the right once. 
                    // Load the maps for all of the tombs, and check which one has the Orifice.
                    // Declare that tomb as point of interest.
                    Area[] tombs = new[]
                    {
                        Area.TalRashasTomb1, Area.TalRashasTomb2, Area.TalRashasTomb3, Area.TalRashasTomb4,
                        Area.TalRashasTomb5, Area.TalRashasTomb6, Area.TalRashasTomb7
                    };
                    var realTomb = Area.None;
                    Parallel.ForEach(tombs, tombArea =>
                    {
                        AreaData tombData = mapApi.GetMapData(tombArea);
                        if (tombData.Objects.ContainsKey(GameObject.HoradricOrifice))
                        {
                            realTomb = tombArea;
                        }
                    });

                    if (realTomb != Area.None && areaData.AdjacentLevels[realTomb].Exits.Any())
                    {
                        pointOfInterest.Add(new PointOfInterest
                        {
                            Label = Utils.GetAreaLabel(realTomb, gameData.Difficulty),
                            Position = areaData.AdjacentLevels[realTomb].Exits[0],
                            RenderingSettings = MapAssistConfiguration.Loaded.MapConfiguration.NextArea,
                            Type = PoiType.NextArea
                        });
                    }

                    break;
                default:
                    if (areaData.AdjacentLevels.Any())
                    {
                        // Next Area Point of Interest
                        if (areaData.Area == Area.TamoeHighland)
                        {
                            var monastery = areaData.AdjacentLevels.First(level => level.Key == Area.MonasteryGate).Value;

                            var monasteryArea = mapApi.GetMapData(Area.MonasteryGate);
                            var outerCloister = monasteryArea.AdjacentLevels.First(level => level.Key == Area.OuterCloister).Value;

                            pointOfInterest.Add(new PointOfInterest
                            {
                                Label = Utils.GetAreaLabel(monastery.Area, gameData.Difficulty),
                                Position = new Point(outerCloister.Exits[0].X, monastery.Exits[0].Y),
                                RenderingSettings = MapAssistConfiguration.Loaded.MapConfiguration.NextArea,
                                Type = PoiType.NextArea
                            });
                            areaRenderDecided.Add(Area.MonasteryGate);
                        }
                        else if (areaData.Area == Area.OuterCloister)
                        {
                            var fountain = areaData.Objects.First(obj => obj.Key == GameObject.RogueFountain).Value;
                            var doors = areaData.Objects
                                .Where(obj => obj.Key == GameObject.DoorCourtyardLeft || obj.Key == GameObject.DoorCourtyardRight).ToArray();

                            // determine farthest courtyard door from the fountain...
                            if (fountain.Any())
                            {
                                var minDistance = 0d;
                                Point farDoorPoint = default;
                                foreach (var doorType in doors)
                                {
                                    var points = doorType.Value;
                                    foreach (var point in points)
                                    {
                                        var distance = Math.Sqrt(Math.Pow(point.X - fountain[0].X, 2) + Math.Pow(point.Y - fountain[0].Y, 2));
                                        if (distance > minDistance)
                                        {
                                            minDistance = distance;
                                            farDoorPoint = point;
                                        }
                                    }
                                }

                                if (farDoorPoint != default)
                                {
                                    pointOfInterest.Add(new PointOfInterest
                                    {
                                        Label = Utils.GetAreaLabel(Area.Barracks, gameData.Difficulty),
                                        Position = farDoorPoint,
                                        RenderingSettings = MapAssistConfiguration.Loaded.MapConfiguration.NextArea,
                                        Type = PoiType.NextArea
                                    });
                                }
                            }
                        }
                        else if (AreaPreferredNextArea.TryGetValue(areaData.Area, out var nextArea))
                        {
                            var nextLevel = areaData.AdjacentLevels[nextArea];
                            if (nextLevel.Exits.Any())
                            {
                                pointOfInterest.Add(new PointOfInterest
                                {
                                    Label = Utils.GetAreaLabel(nextArea, gameData.Difficulty),
                                    Position = nextLevel.Exits[0],
                                    RenderingSettings = MapAssistConfiguration.Loaded.MapConfiguration.NextArea,
                                    Type = PoiType.NextArea
                                });
                                areaRenderDecided.Add(nextArea);
                            }
                        }
                        else
                        {
                            var maxAdjacentArea = areaData.AdjacentLevels.Keys.Max();
                            if (maxAdjacentArea > areaData.Area)
                            {
                                var nextLevel = areaData.AdjacentLevels[maxAdjacentArea];
                                if (nextLevel.Exits.Any())
                                {
                                    pointOfInterest.Add(new PointOfInterest
                                    {
                                        Label = Utils.GetAreaLabel(maxAdjacentArea, gameData.Difficulty),
                                        Position = nextLevel.Exits[0],
                                        RenderingSettings = MapAssistConfiguration.Loaded.MapConfiguration.NextArea,
                                        Type = PoiType.NextArea
                                    });
                                    areaRenderDecided.Add(maxAdjacentArea);
                                }
                            }
                        }

                        // Quest Area Point of Interest
                        if (AreaPreferredQuestArea.TryGetValue(areaData.Area, out var questArea))
                        {
                            var questLevel = areaData.AdjacentLevels[questArea];
                            if (questLevel.Exits.Any())
                            {
                                pointOfInterest.Add(new PointOfInterest
                                {
                                    Label = Utils.GetAreaLabel(questArea, gameData.Difficulty),
                                    Position = questLevel.Exits[0],
                                    RenderingSettings = MapAssistConfiguration.Loaded.MapConfiguration.Quest,
                                    Type = PoiType.Quest
                                });
                                areaRenderDecided.Add(questArea);
                            }
                        }

                        // Previous Area Point of Interest
                        if (areaData.Area == Area.MonasteryGate)
                        {
                            var outerCloister = areaData.AdjacentLevels.First(level => level.Key == Area.OuterCloister).Value;
                            var tamoe = areaData.AdjacentLevels.First(level => level.Key == Area.TamoeHighland).Value;

                            pointOfInterest.Add(new PointOfInterest
                            {
                                Label = Utils.GetAreaLabel(tamoe.Area, gameData.Difficulty),
                                Position = new Point(outerCloister.Exits[0].X, tamoe.Exits[0].Y),
                                RenderingSettings = MapAssistConfiguration.Loaded.MapConfiguration.PreviousArea,
                                Type = PoiType.PreviousArea
                            });
                        }
                        else
                        {
                            foreach (var level in areaData.AdjacentLevels.Values)
                            {
                                // Already made render decision for this.
                                if (areaRenderDecided.Contains(level.Area))
                                {
                                    continue;
                                }

                                foreach (var position in level.Exits)
                                {
                                    pointOfInterest.Add(new PointOfInterest
                                    {
                                        Label = Utils.GetAreaLabel(level.Area, gameData.Difficulty),
                                        Position = position,
                                        RenderingSettings = MapAssistConfiguration.Loaded.MapConfiguration.PreviousArea,
                                        Type = PoiType.PreviousArea
                                    });
                                }
                            }
                        }
                    }

                    break;
            }
            
            foreach (var objAndPoints in areaData.Objects)
            {
                GameObject obj = objAndPoints.Key;
                Point[] points = objAndPoints.Value;

                if (!points.Any())
                {
                    continue;
                }

                // Area-specific quest objects
                if (AreaSpecificQuestObjects.ContainsKey(areaData.Area))
                {
                    if (AreaSpecificQuestObjects[areaData.Area].ContainsKey(obj))
                    {
                        pointOfInterest.Add(new PointOfInterest
                        {
                            Label = AreaSpecificQuestObjects[areaData.Area][obj],
                            Position = points[0],
                            RenderingSettings = MapAssistConfiguration.Loaded.MapConfiguration.Quest,
                            Type = PoiType.AreaSpecificQuest
                        });
                    }
                }

                // Area-specific landmarks
                if (AreaPortals.ContainsKey(areaData.Area))
                {
                    if (AreaPortals[areaData.Area].ContainsKey(obj))
                    {
                        pointOfInterest.Add(new PointOfInterest
                        {
                            Label = Utils.GetPortalName(AreaPortals[areaData.Area][obj], gameData.Difficulty),
                            Position = points[0],
                            RenderingSettings = MapAssistConfiguration.Loaded.MapConfiguration.Portal,
                            Type = PoiType.AreaPortal
                        });
                    }
                }

                // Waypoints
                if (obj.IsWaypoint())
                {
                    pointOfInterest.Add(new PointOfInterest
                    {
                        Label = areaData.Area.Name(),
                        Position = points[0],
                        RenderingSettings = MapAssistConfiguration.Loaded.MapConfiguration.Waypoint,
                        Type = PoiType.Waypoint
                    });
                }
                // Quest objects
                else if (QuestObjects.TryGetValue(obj, out var questObjectName))
                {
                    var usePoints = obj == GameObject.CagedWussie ? points : // Mark all 3 sets of prisoners in Frigid Highlands as quest destinations
                        new Point[] { points[0] };

                    foreach (var point in usePoints)
                    { 
                        pointOfInterest.Add(new PointOfInterest
                        {
                            Label = questObjectName,
                            Position = point,
                            RenderingSettings = MapAssistConfiguration.Loaded.MapConfiguration.Quest
                        });
                    }
                }
                // Shrines
                else if (Shrines.Contains(obj))
                {
                    foreach (var point in points)
                    {
                        pointOfInterest.Add(new PointOfInterest
                        {
                            Label = obj.ToString(),
                            Position = point,
                            RenderingSettings = MapAssistConfiguration.Loaded.MapConfiguration.Shrine,
                            Type = PoiType.Shrine
                        });
                    }
                }
                // Super Chest
                else if (SuperChests.Contains(obj))
                {
                    foreach (var point in points)
                    {
                        pointOfInterest.Add(new PointOfInterest
                        {
                            Label = obj.ToString(),
                            Position = point,
                            RenderingSettings = MapAssistConfiguration.Loaded.MapConfiguration.SuperChest,
                            Type = PoiType.SuperChest
                        });
                    }
                }
                // Normal Chest
                else if (NormalChests.Contains(obj))
                {
                    foreach (var point in points)
                    {
                        pointOfInterest.Add(new PointOfInterest
                        {
                            Label = obj.ToString(),
                            Position = point,
                            RenderingSettings = MapAssistConfiguration.Loaded.MapConfiguration.NormalChest,
                            Type = PoiType.NormalChest
                        });
                    }
                }
                // Armor Stands & Weapon Racks
                else if (ArmorWeapRacks.Contains(obj))
                {
                    foreach (var point in points)
                    {
                        pointOfInterest.Add(new PointOfInterest
                        {
                            Label = obj.ToString(),
                            Position = point,
                            RenderingSettings = MapAssistConfiguration.Loaded.MapConfiguration.ArmorWeapRack,
                            Type = PoiType.ArmorWeapRack
                        });
                    }
                }
            }

            switch(areaData.Area)
            {
                case Area.PlainsOfDespair:
                    foreach (var objAndPoints in areaData.NPCs)
                    {
                        pointOfInterest.Add(new PointOfInterest
                        {
                            Label = "Izual",
                            Position = objAndPoints.Value[0],
                            RenderingSettings = MapAssistConfiguration.Loaded.MapConfiguration.Quest
                        });
                    }
                    break;
            }

            return pointOfInterest;
        }
    }
}
