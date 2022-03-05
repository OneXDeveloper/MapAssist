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

using GameOverlay.Drawing;
using MapAssist.Settings;
using MapAssist.Types;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MapAssist.Helpers
{
    public static class PointOfInterestHandler
    {
        private static Dictionary<GameObject, string> QuestObjects;

        private static Dictionary<Area, Dictionary<GameObject, string>> AreaSpecificQuestObjects;

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
            [Area.TamoeHighland] = Area.PitLevel1,
            [Area.DryHills] = Area.HallsOfTheDeadLevel1,
            [Area.FarOasis] = Area.MaggotLairLevel1,
            [Area.LostCity] = Area.ValleyOfSnakes,
            [Area.SpiderForest] = Area.SpiderCavern,
            [Area.FlayerJungle] = Area.FlayerDungeonLevel1,
            [Area.KurastBazaar] = Area.RuinedTemple,
            [Area.CrystallinePassage] = Area.FrozenRiver,
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

        public static void UpdateLocalizationNames()
        {
            QuestObjects = new Dictionary<GameObject, string>
            {
                [GameObject.CairnStoneAlpha] = Area.Tristram.Name(),
                [GameObject.WirtCorpse] = Items.GetItemNameFromKey("leg"),
                [GameObject.InifussTree] = Items.GetItemNameFromKey("Inifuss"),
                [GameObject.Malus] = Items.GetItemNameFromKey("Malus"),
                [GameObject.HoradricScrollChest] = Items.GetItemNameFromKey("tr1"),
                [GameObject.HoradricCubeChest] = Items.GetItemNameFromKey("box"),
                [GameObject.StaffOfKingsChest] = Items.GetItemNameFromKey("Staff of Kings"),
                [GameObject.YetAnotherTome] = AreaExtensions.NameFromKey("The Summoner"),
                [GameObject.HoradricOrifice] = Items.GetItemNameFromKey("orifice"),
                [GameObject.KhalimChest1] = Items.GetItemNameFromKey("qhr"),
                [GameObject.KhalimChest2] = Items.GetItemNameFromKey("qbr"),
                [GameObject.KhalimChest3] = Items.GetItemNameFromKey("qey"),
                [GameObject.GidbinnAltarDecoy] = Items.GetItemNameFromKey("gidbinn"),
                [GameObject.HellForge] = Items.GetItemNameFromKey("Hellforge"),
                [GameObject.DrehyaWildernessStartPosition] = AreaExtensions.NameFromKey("Drehya"), //anya
                [GameObject.NihlathakWildernessStartPosition] = AreaExtensions.NameFromKey("Nihlathak"),
                [GameObject.CagedWussie] = AreaExtensions.NameFromKey("cagedwussie1"),
            };
            AreaSpecificQuestObjects = new Dictionary<Area, Dictionary<GameObject, string>>()
            {
                [Area.MatronsDen] = new Dictionary<GameObject, string>()
                {
                    [GameObject.SparklyChest] = AreaExtensions.NameFromKey("Lilith"),
                },
                [Area.FurnaceOfPain] = new Dictionary<GameObject, string>()
                {
                    [GameObject.SparklyChest] = AreaExtensions.NameFromKey("Izual"),
                },
                [Area.PalaceCellarLevel3] = new Dictionary<GameObject, string>()
                {
                    [GameObject.ArcaneSanctuaryPortal] = AreaExtensions.NameFromKey("Arcane Sanctuary"),
                },
            };
        }

        public static List<PointOfInterest> Get(MapApi mapApi, AreaData areaData, GameData gameData)
        {
            var pointsOfInterest = GetArea(mapApi, areaData, gameData);

            if (AreaExtensions.RequiresStitching(areaData.Area))
            {
                foreach (var adjacentArea in areaData.AdjacentAreas.Values.ToList())
                {
                    if (AreaExtensions.RequiresStitching(adjacentArea.Area))
                    {
                        var adjacentPoi = GetArea(mapApi, adjacentArea, gameData).Where(a => !pointsOfInterest.Any(b => a.Position.Subtract(b.Position).Length() < 5)).ToList(); // Prevent poi in an adjacent area from overlapping with poi in the current area
                        pointsOfInterest.AddRange(adjacentPoi);
                    }
                }
            }

            return pointsOfInterest;
        }

        public static List<PointOfInterest> GetArea(MapApi mapApi, AreaData areaData, GameData gameData)
        {
            var pointsOfInterest = new List<PointOfInterest>();
            var areaRenderDecided = new List<Area>();

            if (areaData.Area == Area.UberTristram) return pointsOfInterest; // No actual points of interest here, Wirt's leg appears without this line

            if (areaData.AdjacentLevels.Any())
            {
                // Next Area Point of Interest
                if (areaData.Area == Area.CanyonOfTheMagi)
                {
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
                        pointsOfInterest.Add(new PointOfInterest
                        {
                            Area = areaData.Area,
                            NextArea = realTomb,
                            Label = realTomb.MapLabel(gameData.Difficulty),
                            Position = areaData.AdjacentLevels[realTomb].Exits[0],
                            RenderingSettings = MapAssistConfiguration.Loaded.MapConfiguration.NextArea,
                            Type = PoiType.NextArea
                        });
                        areaRenderDecided.Add(realTomb);
                    }
                }
                else if (areaData.Area == Area.TamoeHighland)
                {
                    var monastery = areaData.AdjacentLevels.First(level => level.Key == Area.MonasteryGate).Value;

                    var monasteryArea = mapApi.GetMapData(Area.MonasteryGate);
                    var outerCloister = monasteryArea.AdjacentLevels.First(level => level.Key == Area.OuterCloister).Value;

                    pointsOfInterest.Add(new PointOfInterest
                    {
                        Area = areaData.Area,
                        NextArea = Area.MonasteryGate,
                        Label = Area.MonasteryGate.MapLabel(gameData.Difficulty),
                        Position = new Point(outerCloister.Exits[0].X, monastery.Exits[0].Y),
                        RenderingSettings = MapAssistConfiguration.Loaded.MapConfiguration.NextArea,
                        Type = PoiType.NextArea
                    });
                    areaRenderDecided.Add(Area.MonasteryGate);
                }
                else if (areaData.Area == Area.OuterCloister)
                {
                    // Barracks Door is based on waypoint position
                    var waypoint = areaData.Objects.First(obj => obj.Key == GameObject.WaypointPortal).Value.First();
                    switch (waypoint.X)
                    {
                        case 15129:
                            // Waypoint = { X: 15129, Y: 4954 }
                            // SE Door = { X: 15280, Y: 4940 }
                            pointsOfInterest.Add(new PointOfInterest
                            {
                                Area = areaData.Area,
                                NextArea = Area.Barracks,
                                Label = Area.Barracks.MapLabel(gameData.Difficulty),
                                Position = new Point(15280, 4940),
                                RenderingSettings = MapAssistConfiguration.Loaded.MapConfiguration.NextArea,
                                Type = PoiType.NextArea
                            });
                            areaRenderDecided.Add(Area.OuterCloister);
                            break;

                        case 15154:
                            // Waypoint = { X: 15154, Y: 4919 }
                            // NE Door = { X: 15141, Y: 4802 }
                            pointsOfInterest.Add(new PointOfInterest
                            {
                                Area = areaData.Area,
                                NextArea = Area.Barracks,
                                Label = Area.Barracks.MapLabel(gameData.Difficulty),
                                Position = new Point(15141, 4802),
                                RenderingSettings = MapAssistConfiguration.Loaded.MapConfiguration.NextArea,
                                Type = PoiType.NextArea
                            });
                            areaRenderDecided.Add(Area.OuterCloister);
                            break;

                        case 15159:
                            // Waypoint = { X: 15159, Y: 4934 }
                            // NW Door = { X: 15002, Y: 4943 }
                            pointsOfInterest.Add(new PointOfInterest
                            {
                                Area = areaData.Area,
                                NextArea = Area.Barracks,
                                Label = Area.Barracks.MapLabel(gameData.Difficulty),
                                Position = new Point(15002, 4943),
                                RenderingSettings = MapAssistConfiguration.Loaded.MapConfiguration.NextArea,
                                Type = PoiType.NextArea
                            });
                            areaRenderDecided.Add(Area.OuterCloister);
                            break;
                    }
                }
                else if (areaData.Area == Area.InnerCloister)
                {
                    // Cathedral door
                    pointsOfInterest.Add(new PointOfInterest
                    {
                        Area = areaData.Area,
                        NextArea = Area.Cathedral,
                        Label = Area.Cathedral.MapLabel(gameData.Difficulty),
                        Position = new Point(20053, 5000),
                        RenderingSettings = MapAssistConfiguration.Loaded.MapConfiguration.NextArea,
                        Type = PoiType.NextArea
                    });
                    areaRenderDecided.Add(Area.InnerCloister);
                }
                else if (AreaPreferredNextArea.TryGetValue(areaData.Area, out var nextArea))
                {
                    var nextLevel = areaData.AdjacentLevels[nextArea];
                    if (nextLevel.Exits.Any())
                    {
                        pointsOfInterest.Add(new PointOfInterest
                        {
                            Area = areaData.Area,
                            NextArea = nextArea,
                            Label = nextArea.MapLabel(gameData.Difficulty),
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
                            pointsOfInterest.Add(new PointOfInterest
                            {
                                Area = areaData.Area,
                                NextArea = maxAdjacentArea,
                                Label = maxAdjacentArea.MapLabel(gameData.Difficulty),
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
                        pointsOfInterest.Add(new PointOfInterest
                        {
                            Area = areaData.Area,
                            NextArea = questArea,
                            Label = questArea.MapLabel(gameData.Difficulty),
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

                    pointsOfInterest.Add(new PointOfInterest
                    {
                        Area = areaData.Area,
                        NextArea = tamoe.Area,
                        Label = tamoe.Area.MapLabel(gameData.Difficulty),
                        Position = new Point(outerCloister.Exits[0].X, tamoe.Exits[0].Y),
                        RenderingSettings = MapAssistConfiguration.Loaded.MapConfiguration.PreviousArea,
                        Type = PoiType.PreviousArea
                    });
                }
                else if (areaData.Area == Area.Barracks)
                {
                    var outerCloisterArea = mapApi.GetMapData(Area.OuterCloister);
                    var barracksAreaData = GetArea(mapApi, outerCloisterArea, gameData);
                    var barracks = barracksAreaData.FirstOrDefault(poi => poi.Type == PoiType.NextArea);

                    pointsOfInterest.Add(new PointOfInterest
                    {
                        Area = areaData.Area,
                        NextArea = Area.OuterCloister,
                        Label = Area.OuterCloister.MapLabel(gameData.Difficulty),
                        Position = barracks.Position,
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
                            pointsOfInterest.Add(new PointOfInterest
                            {
                                Area = areaData.Area,
                                NextArea = level.Area,
                                Label = level.Area.MapLabel(gameData.Difficulty),
                                Position = position,
                                RenderingSettings = MapAssistConfiguration.Loaded.MapConfiguration.PreviousArea,
                                Type = PoiType.PreviousArea
                            });
                        }
                    }
                }
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
                        pointsOfInterest.Add(new PointOfInterest
                        {
                            Area = areaData.Area,
                            Label = AreaSpecificQuestObjects[areaData.Area][obj],
                            Position = points[0],
                            RenderingSettings = MapAssistConfiguration.Loaded.MapConfiguration.Quest,
                            Type = PoiType.AreaSpecificQuest
                        });
                    }
                }

                // Area-specific quest objects
                if (AreaSpecificQuestObjects.ContainsKey(areaData.Area))
                {
                    if (AreaSpecificQuestObjects[areaData.Area].ContainsKey(obj))
                    {
                        pointsOfInterest.Add(new PointOfInterest
                        {
                            Area = areaData.Area,
                            Label = AreaSpecificQuestObjects[areaData.Area][obj],
                            Position = points[0],
                            RenderingSettings = MapAssistConfiguration.Loaded.MapConfiguration.Quest,
                            Type = PoiType.AreaSpecificQuest
                        });
                    }
                }

                // Area-specific portals
                if (AreaPortals.ContainsKey(areaData.Area))
                {
                    if (AreaPortals[areaData.Area].ContainsKey(obj))
                    {
                        pointsOfInterest.Add(new PointOfInterest
                        {
                            Area = areaData.Area,
                            Label = AreaPortals[areaData.Area][obj].PortalLabel(gameData.Difficulty),
                            Position = points[0],
                            RenderingSettings = MapAssistConfiguration.Loaded.MapConfiguration.Portal,
                            Type = PoiType.AreaPortal
                        });
                    }
                }

                // Waypoints
                if (obj.IsWaypoint())
                {
                    pointsOfInterest.Add(new PointOfInterest
                    {
                        Area = areaData.Area,
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
                        pointsOfInterest.Add(new PointOfInterest
                        {
                            Area = areaData.Area,
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
                        pointsOfInterest.Add(new PointOfInterest
                        {
                            Area = areaData.Area,
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
                        pointsOfInterest.Add(new PointOfInterest
                        {
                            Area = areaData.Area,
                            Label = obj.ToString(),
                            Position = point,
                            RenderingSettings = MapAssistConfiguration.Loaded.MapConfiguration.SuperChest,
                            Type = PoiType.SuperChest
                        });
                    }
                }
                // Armor Stands & Weapon Racks
                else if (ArmorWeapRacks.Contains(obj))
                {
                    foreach (var point in points)
                    {
                        pointsOfInterest.Add(new PointOfInterest
                        {
                            Area = areaData.Area,
                            Label = obj.ToString(),
                            Position = point,
                            RenderingSettings = MapAssistConfiguration.Loaded.MapConfiguration.ArmorWeapRack,
                            Type = PoiType.ArmorWeapRack
                        });
                    }
                }
            }

            switch (areaData.Area)
            {
                case Area.PlainsOfDespair:
                    foreach (var objAndPoints in areaData.NPCs)
                    {
                        pointsOfInterest.Add(new PointOfInterest
                        {
                            Area = areaData.Area,
                            Label = AreaExtensions.NameFromKey("Izual"),
                            Position = objAndPoints.Value[0],
                            RenderingSettings = MapAssistConfiguration.Loaded.MapConfiguration.Quest
                        });
                    }
                    break;
            }

            return pointsOfInterest;
        }
    }
}
