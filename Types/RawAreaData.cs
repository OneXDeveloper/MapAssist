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
using System;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InconsistentNaming
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnassignedField.Global
// ReSharper disable CollectionNeverUpdated.Global

namespace MapAssist.Types
{
    public class XY
    {
        public int x;
        public int y;

        public Point ToPoint()
        {
            return new Point(x, y);
        }
    }

    public class XY2
    {
        public int x0;
        public int y0;
        public int x1;
        public int y1;
    }

    public class Exit
    {
        public XY[] offsets;
        public bool isPortal;

        public AdjacentLevel ToInternal(Area area)
        {
            return new AdjacentLevel
            {
                Area = area,
                Exits = offsets.Select(o => o.ToPoint()).ToArray(),
                IsPortal = isPortal,
            };
        }
    }

    public class RawAreaData
    {
        public XY2 crop;
        public XY offset;
        public Dictionary<string, Exit> exits;
        public int[] mapData;
        public Dictionary<string, XY[]> npcs;
        public Dictionary<string, XY[]> objects;

        public AreaData ToInternal(Area area)
        {
            if (exits == null) exits = new Dictionary<string, Exit>();
            if (npcs == null) npcs = new Dictionary<string, XY[]>();
            if (objects == null) objects = new Dictionary<string, XY[]>();

            return new AreaData
            {
                Area = area,
                Origin = offset.ToPoint(),
                CollisionGrid = GetCollisionGid(),
                AdjacentLevels = exits
                    .Select(o =>
                    {
                        var adjacentArea = Area.None;
                        if (int.TryParse(o.Key, out var parsed))
                        {
                            adjacentArea = (Area)parsed;
                        }

                        AdjacentLevel level = o.Value.ToInternal(adjacentArea);
                        return (adjacentArea, level);
                    })
                    .Where(o => o.adjacentArea != Area.None)
                    .ToDictionary(k => k.adjacentArea, v => v.level),
                NPCs = npcs.Select(o =>
                    {
                        Point[] positions = o.Value.Select(j => j.ToPoint()).ToArray();
                        var npc = Npc.Invalid;
                        if (int.TryParse(o.Key, out var parsed))
                        {
                            npc = (Npc)parsed;
                        }

                        return (npc, positions);
                    })
                    .Where(o => o.npc != Npc.Invalid)
                    .ToDictionary(k => k.npc, v => v.positions),
                Objects = objects.Select(o =>
                    {
                        Point[] positions = o.Value.Select(j => j.ToPoint()).ToArray();
                        var gameObject = GameObject.NotApplicable;
                        if (int.TryParse(o.Key, out var parsed))
                        {
                            gameObject = (GameObject)parsed;
                        }

                        return (gameObject, positions);
                    })
                    .Where(o => o.gameObject != GameObject.NotApplicable)
                    .ToDictionary(k => k.gameObject, v => v.positions)
            };
        }
    
        private int[][] GetCollisionGid()
        {
            var mapRows = new int[crop.y1 - crop.y0][];
            var unwalkableTile = new int[] { -1 };
            var unwalkableRow = new int[][] { new int[crop.x1 - crop.x0 + 2].Select(_ => -1).ToArray() };

            var iy = 0;
            var val = -1;

            foreach (var v in mapData)
            {
                if (mapRows[iy] == null)
                {
                    mapRows[iy] = new int[0];
                }

                if (v != -1)
                {
                    mapRows[iy] = mapRows[iy].Concat(new int[v].Select(_ => val)).ToArray();

                    val = -1 - val;
                }
                else
                {
                    mapRows[iy] = unwalkableTile.Concat(mapRows[iy]).Concat(unwalkableTile).ToArray(); // Prepend and append with one unwalkable tile for improved border drawing

                    iy++;
                    val = -1;
                }
            }

            mapRows = unwalkableRow.Concat(mapRows).Concat(unwalkableRow).ToArray(); // Prepend and append with one unwalkable row of tiles for improved border drawing

            return mapRows;
        }
    }
}
