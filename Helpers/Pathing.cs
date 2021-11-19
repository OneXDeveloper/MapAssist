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


using MapAssist.Settings;
using MapAssist.Types;
using System.Collections.Generic;
using Roy_T.AStar.Paths;
using Roy_T.AStar.Primitives;
using System.Linq;
using System.Drawing;
using System;
using Roy_T.AStar.Grids;

namespace MapAssist.Helpers
{
    public class Pathing
    {
        // cache for calculated paths. each cache entry is only valid for a specified amount of time
        private Dictionary<(uint, Difficulty, Area, Point), (List<Point>, long)> _pathCache = new Dictionary<(uint, Difficulty, Area, Point), (List<Point>, long)>();

        /* cache for generated grids (data structure used by the A* algorithm). generating these grids from the AreaData takes quite a lot of time 
         * and they dont change as long as the AreaData doesnt change. So we can cache these results without any timing constraints */
        private Dictionary<(uint, Difficulty, Area), Grid> _gridCache = new Dictionary<(uint, Difficulty, Area), Grid>();

        public List<Point> GetPathToLocation(uint mapId, Difficulty difficulty, Area area, AreaData map, MovementMode movementMode, Point fromLocation, Point toLocation)
        {
            // check if we have a valid cache entry for this path. if thats the case we are done and can return the cache entry
            (uint mapId, Difficulty difficulty, Area area, Point toLocation) pathCacheKey = (mapId, difficulty, area, toLocation);

            if (_pathCache.ContainsKey(pathCacheKey) && ((DateTimeOffset.Now.ToUnixTimeMilliseconds() - _pathCache[pathCacheKey].Item2) < 1000))
            {
                return _pathCache[pathCacheKey].Item1;
            }

            var result = new List<Point>();

            // cancel if the provided points dont map into the collisionMap of the AreaData
            if (!map.TryMapToPointInMap(fromLocation, out var fromPosition) || !map.TryMapToPointInMap(toLocation, out var toPosition))
            {
                return result;
            }

            if (movementMode == MovementMode.Teleport)
            {
                var teleportPather = new TeleportPather(map);
                var teleportPath = teleportPather.GetTeleportPath(fromPosition, toPosition);
                if (teleportPath.Found)
                {
                    /* the TeleportPather returns the Points on the path withoug the Origin offset. 
                     * The Compositor expects this offset so we have to add it before we can return the result */
                    result = teleportPath.Points.Select(p => new Point(p.X += map.Origin.X, p.Y += map.Origin.Y)).Skip(1).ToList();
                }
            }
            else
            {

                var pathFinder = new PathFinder();
                var fromGridPosition = new GridPosition(fromPosition.X, fromPosition.Y);
                var toGridPosition = new GridPosition(toPosition.X, toPosition.Y);

                // use the cached grid for this AreaData if possible. Generate a new grid otherwise
                (uint mapId, Difficulty difficulty, Area area) gridCacheKey = (mapId, difficulty, area);
                Grid grid;
                if (_gridCache.ContainsKey(gridCacheKey))
                {
                    grid = _gridCache[gridCacheKey];
                }
                else
                {
                    grid = map.MapToGrid();
                    _gridCache[gridCacheKey] = grid;
                }

                var path = pathFinder.FindPath(fromGridPosition, toGridPosition, grid);
                var endPosition = path.Edges.LastOrDefault()?.End.Position;
                if (endPosition.HasValue && map.MapToPoint(endPosition.Value) == toLocation)
                {
                    result = path.Edges.Where((p, i) => i % 3 == 0 || i == path.Edges.Count - 1).Select(e => map.MapToPoint(e.End.Position)).ToList();
                }
            }

            // add the calculated path to the cache
            _pathCache[pathCacheKey] = (result, DateTimeOffset.Now.ToUnixTimeMilliseconds());

            return result;
        }
    }
}
