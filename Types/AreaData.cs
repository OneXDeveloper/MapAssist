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
        public Dictionary<Area, AdjacentLevel> AdjacentLevels;
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
                ViewInputRect = points.ToArray().ToRectangle();
                ViewOutputRect = points.Select(point => point.Subtract(ViewInputRect.Left + ViewInputRect.Width / 2, ViewInputRect.Top + ViewInputRect.Height / 2).Rotate(angleRadians)).ToArray().ToRectangle();
            }
        }
    }
}
