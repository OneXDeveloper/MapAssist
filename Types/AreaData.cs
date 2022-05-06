using GameOverlay.Drawing;
using MapAssist.Helpers;
using MapAssist.Settings;
using System.Collections.Generic;
using System.Linq;

namespace MapAssist.Types
{
    public class AdjacentLevel
    {
        public Area Area;
        public Point[] Exits;
        public bool IsPortal;
    }

    public class AreaData
    {
        public Area Area;
        public Point Origin;
        public int MapPadding = 0;
        public Dictionary<Area, AdjacentLevel> AdjacentLevels;
        public Dictionary<Area, AreaData> AdjacentAreas = new Dictionary<Area, AreaData>();
        public int[][] CollisionGrid;
        public Rectangle ViewInputRect;
        public Rectangle ViewOutputRect;
        public Dictionary<Npc, Point[]> NPCs;
        public Dictionary<GameObject, Point[]> Objects;

        public void CalcViewAreas(float angleRadians)
        {
            var points = new List<Point>(); // All non-unwalkable points used for calculating the input and output view dimensions

            // Calculate borders
            var lookOffsets = new int[][] {
                            new int[] { -1, -1 },
                            new int[] { -1, 0 },
                            new int[] { -1, 1 },
                            new int[] { 0, -1 },
                            new int[] { 0, 1 },
                            new int[] { 1, -1 },
                            new int[] { 1, 0 },
                            new int[] { 1, 1 }
                        };

            for (var y = 0; y < CollisionGrid.Length; y++)
            {
                for (var x = 0; x < CollisionGrid[0].Length; x++)
                {
                    var type = CollisionGrid[y][x];
                    var isCurrentPixelWalkable = type == 0;
                    var isCurrentPixelUnwalkable = type == -1;

                    if (isCurrentPixelWalkable)
                    {
                        points.Add(new Point(x, y));
                        continue;
                    }

                    foreach (var offset in lookOffsets)
                    {
                        var dy = y + offset[0];
                        var dx = x + offset[1];

                        var offsetInBounds =
                            dy >= 0 && dy < CollisionGrid.Length &&
                            dx >= 0 && dx < CollisionGrid[0].Length;

                        if (offsetInBounds && CollisionGrid[dy][dx] == 0)
                        {
                            CollisionGrid[y][x] = 1; // Wall
                            points.Add(new Point(x, y));
                            break;
                        }
                    }
                }
            }

            if (MapAssistConfiguration.Loaded.RenderingConfiguration.OverlayMode)
            {
                ViewOutputRect = ViewInputRect = new Rectangle(0, 0, CollisionGrid[0].Length, CollisionGrid.Length);
            }
            else
            {
                ViewInputRect = points.ToArray().ToRectangle(1);
                ViewOutputRect = points.Select(point => point.Subtract(ViewInputRect.Left + ViewInputRect.Width / 2, ViewInputRect.Top + ViewInputRect.Height / 2).Rotate(angleRadians)).ToArray().ToRectangle(1);
            }
        }
    
        public bool IncludesPoint(Point point)
        {
            var adjPoint = point.Subtract(Origin);
            return adjPoint.X > 0 && 
                adjPoint.Y > 0 && 
                adjPoint.X < ViewInputRect.Width - MapPadding * 2 && 
                adjPoint.Y < ViewInputRect.Height - MapPadding * 2;
        }
    }

    class AreaLabel
    {
        public string Text;
        public int[] Level;
    }
}
