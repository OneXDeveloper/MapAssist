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
using System.Drawing;
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

    public class RawAdjacentLevel
    {
        public XY[] exits;
        public XY origin;
        public int width;
        public int height;

        public AdjacentLevel ToInternal(Area area)
        {
            return new AdjacentLevel
            {
                Area = area,
                Origin = origin.ToPoint(),
                Exits = exits.Select(o => o.ToPoint()).ToArray(),
                Width = width,
                Height = height,
            };
        }
    }

    public class RawAreaData
    {
        public XY levelOrigin;
        public Dictionary<string, RawAdjacentLevel> adjacentLevels;
        public int[][] mapRows;
        public Dictionary<string, XY[]> npcs;
        public Dictionary<string, XY[]> objects;

        public AreaData ToInternal(Area area)
        {
            return new AreaData
            {
                Area = area,
                Origin = levelOrigin.ToPoint(),
                AdjacentLevels = adjacentLevels
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
                    .ToDictionary(k => k.gameObject, v => v.positions),
                CollisionGrid = mapRows
            };
        }
    }
}
