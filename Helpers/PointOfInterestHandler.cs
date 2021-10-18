using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using D2RAssist.Types;

namespace D2RAssist.Helpers
{
    public static class PointOfInterestHandler
    {
        private static readonly HashSet<GameObject> QuestObjects = new HashSet<GameObject>
        {
            GameObject.HoradricCubeChest,
            GameObject.HoradricScrollChest,
            GameObject.StaffOfKingsChest,
            GameObject.HoradricOrifice,
            GameObject.YetAnotherTome, // Summoner in Arcane Sanctuary
            GameObject.FrozenAnya,
            GameObject.InifussTree,
            GameObject.CairnStoneAlpha,
            GameObject.WirtCorpse,
            GameObject.HellForge
        };
        
        private static readonly HashSet<GameObject> GoodChests = new HashSet<GameObject>
        {
            GameObject.GoodChest,
            GameObject.SparklyChest,
            GameObject.ArcaneLargeChestLeft,
            GameObject.ArcaneLargeChestRight,
            GameObject.ArcaneSmallChestLeft,
            GameObject.ArcaneSmallChestRight
        };

        public static List<PointOfInterest> Get(MapApi mapApi, AreaData areaData)
        {
            List<PointOfInterest> pointOfInterest = new List<PointOfInterest>();

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
                            Label = "Tal Rashas Tomb",
                            Position = areaData.AdjacentLevels[realTomb].Exits[0],
                            RenderingSettings = Settings.Rendering.NextArea
                        });
                    }

                    break;
                default:
                    // By default, draw a line to the next highest neighbouring area.
                    // Also draw labels and previous doors for all other areas.
                    if (areaData.AdjacentLevels.Any())
                    {
                        Area highestArea = areaData.AdjacentLevels.Keys.Max();
                        if (highestArea > areaData.Area)
                        {
                            if (areaData.AdjacentLevels[highestArea].Exits.Any())
                            {
                                pointOfInterest.Add(new PointOfInterest
                                {
                                    Label = highestArea.Name(),
                                    Position = areaData.AdjacentLevels[highestArea].Exits[0],
                                    RenderingSettings = Settings.Rendering.NextArea
                                });
                            }
                        }

                        foreach (AdjacentLevel level in areaData.AdjacentLevels.Values)
                        {
                            // Already have something drawn for this.
                            if (level.Area == highestArea)
                            {
                                continue;
                            }

                            foreach (Point position in level.Exits)
                            {
                                pointOfInterest.Add(new PointOfInterest
                                {
                                    Label = level.Area.Name(),
                                    Position = position,
                                    RenderingSettings = Settings.Rendering.PreviousArea
                                });
                            }
                        }
                    }

                    break;
            }

            foreach (KeyValuePair<GameObject, Point[]> objAndPoints in areaData.Objects)
            {
                GameObject obj = objAndPoints.Key;
                Point[] points = objAndPoints.Value;

                if (!points.Any())
                {
                    continue;
                }

                // Waypoints
                if (obj.IsWaypoint())
                {
                    pointOfInterest.Add(new PointOfInterest
                    {
                        Label = obj.ToString(),
                        Position = points[0],
                        RenderingSettings = Settings.Rendering.Waypoint
                    });
                }
                // Quest objects
                else if (QuestObjects.Contains(obj))
                {
                    pointOfInterest.Add(new PointOfInterest
                    {
                        Label = obj.ToString(),
                        Position = points[0],
                        RenderingSettings = Settings.Rendering.Quest
                    });
                }
                // Chests
                else if (GoodChests.Contains(obj))
                {
                    foreach (Point point in points)
                    {
                        pointOfInterest.Add(new PointOfInterest
                        {
                            Label = obj.ToString(),
                            Position = point,
                            RenderingSettings = Settings.Rendering.SuperChest
                        });
                    }
                }
            }

            return pointOfInterest;
        }
    }
}
