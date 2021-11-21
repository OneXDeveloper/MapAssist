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
        private Dictionary<(uint, Difficulty, Area, Point), (List<Point>, long)> PathCache = new Dictionary<(uint, Difficulty, Area, Point), (List<Point>, long)>();

        private readonly Grid Grid;

        private readonly AreaData _areaData;

        // Stuff for teleport pathing
        private static readonly short RangeInvalid = 10000;
        private static readonly short TpRange = Settings.Map.TpRange;
        private static readonly short BlockRange = 2;
        private short[,] m_distanceMatrix;
        private int m_rows;
        private int m_columns;

        public Pathing(AreaData areaData)
        {
            _areaData = areaData;
            Grid = _areaData.MapToGrid();
        }

        public List<Point> GetPathToLocation(uint mapId, Difficulty difficulty, PathLineStyle movementMode, Point fromLocation, Point toLocation)
        {
            // check if we have a valid cache entry for this path. if thats the case we are done and can return the cache entry
            (uint mapId, Difficulty difficulty, Area area, Point toLocation) pathCacheKey = (mapId, difficulty, _areaData.Area, toLocation);

            if (PathCache.ContainsKey(pathCacheKey) && ((DateTimeOffset.Now.ToUnixTimeMilliseconds() - PathCache[pathCacheKey].Item2) < Settings.Map.PathUpdateTime))
            {
                return PathCache[pathCacheKey].Item1;
            }

            var result = new List<Point>();

            // cancel if the provided points dont map into the collisionMap of the AreaData
            if (!_areaData.TryMapToPointInMap(fromLocation, out var fromPosition) || !_areaData.TryMapToPointInMap(toLocation, out var toPosition))
            {
                return result;
            }

            if (movementMode == PathLineStyle.Teleport)
            {
                var pathFound = false;
                var teleportPath = GetTeleportPath(fromPosition, toPosition, out pathFound);
                if (pathFound)
                {
                    /* the TeleportPather returns the Points on the path withoug the Origin offset. 
                     * The Compositor expects this offset so we have to add it before we can return the result */
                    result = teleportPath.Select(p => new Point(p.X += _areaData.Origin.X, p.Y += _areaData.Origin.Y)).Skip(1).ToList();
                }
            }
            else
            {
                var pathFinder = new PathFinder();
                var fromGridPosition = new GridPosition(fromPosition.X, fromPosition.Y);
                var toGridPosition = new GridPosition(toPosition.X, toPosition.Y);

                var path = pathFinder.FindPath(fromGridPosition, toGridPosition, Grid);
                var endPosition = path.Edges.LastOrDefault()?.End.Position;
                if (endPosition.HasValue && _areaData.MapToPoint(endPosition.Value) == toLocation)
                {
                    result = path.Edges.Where((p, i) => i % 3 == 0 || i == path.Edges.Count - 1).Select(e => _areaData.MapToPoint(e.End.Position)).ToList();
                }
            }

            // add the calculated path to the cache
            PathCache[pathCacheKey] = (result, DateTimeOffset.Now.ToUnixTimeMilliseconds());

            return result;
        }

        private List<Point> GetTeleportPath(Point fromLocation, Point toLocation, out bool pathFound)
        {
            MakeDistanceTable(toLocation);
            var path = new List<Point>
            {
                fromLocation
            };
            var idxPath = 1;

            var bestMove = new BestMove
            {
                Move = fromLocation,
                Result = PathingResult.DestinationNotReachedYet
            };

            var move = GetBestMove(bestMove.Move, toLocation, BlockRange);
            while (move.Result != PathingResult.Failed && idxPath < 100)
            {
                // Reached?
                if (move.Result == PathingResult.Reached)
                {
                    AddToListAtIndex(path, toLocation, idxPath);
                    idxPath++;
                    pathFound = true;
                    return path.GetRange(0, idxPath);
                }

                // Perform a redundancy check
                var nRedundancy = GetRedundancy(path, idxPath, move.Move);
                if (nRedundancy == -1)
                {
                    // no redundancy
                    AddToListAtIndex(path, move.Move, idxPath);
                    idxPath++;
                }
                else
                {
                    // redundancy found, discard all redundant steps
                    idxPath = nRedundancy + 1;
                    AddToListAtIndex(path, move.Move, idxPath);
                }

                move = GetBestMove(move.Move, toLocation, BlockRange);
            }

            pathFound = false;
            return null;
        }

        private void MakeDistanceTable(Point toLocation)
        {
            m_rows = _areaData.CollisionGrid.GetLength(0);
            m_columns = _areaData.CollisionGrid[0].GetLength(0);
            m_distanceMatrix = new short[m_columns, m_rows];
            for (var i = 0; i < m_columns; i++)
            {
                for (var k = 0; k < m_rows; k++)
                {
                    m_distanceMatrix[i, k] = (short)_areaData.CollisionGrid[k][i];
                }
            }

            for (var x = 0; x < m_columns; x++)
            {
                for (var y = 0; y < m_rows; y++)
                {
                    if ((m_distanceMatrix[x, y] % 2) == 0)
                        m_distanceMatrix[x, y] = (short)CalculateDistance(x, y, toLocation.X, toLocation.Y);
                    else
                        m_distanceMatrix[x, y] = RangeInvalid;
                }
            }

            m_distanceMatrix[toLocation.X, toLocation.Y] = 1;
        }


        private void AddToListAtIndex(List<Point> list, Point point, int index)
        {
            if (index < list.Count)
            {
                list[index] = point;
                return;
            }
            else if (index == list.Count)
            {
                list.Add(point);
                return;
            }

            throw new InvalidOperationException();
        }

        private BestMove GetBestMove(Point position, Point toLocation, int blockRange)
        {
            if (CalculateDistance(toLocation, position) <= TpRange)
            {
                return new BestMove
                {
                    Result = PathingResult.Reached,
                    Move = toLocation
                };
            }

            if (!IsValidIndex(position.X, position.Y))
            {
                return new BestMove
                {
                    Result = PathingResult.Failed,
                    Move = new Point(0, 0)
                };
            }

            Block(position, blockRange);

            var best = new Point(0, 0);
            int value = RangeInvalid;

            for (var x = position.X - TpRange; x <= position.X + TpRange; x++)
            {
                for (var y = position.Y - TpRange; y <= position.Y + TpRange; y++)
                {
                    if (!IsValidIndex(x, y))
                        continue;

                    var p = new Point((ushort)x, (ushort)y);

                    if (m_distanceMatrix[p.X, p.Y] < value && CalculateDistance(p, position) <= TpRange)
                    {
                        value = m_distanceMatrix[p.X, p.Y];
                        best = p;
                    }
                }
            }

            if (value >= RangeInvalid || best == null)
            {
                return new BestMove
                {
                    Result = PathingResult.Failed,
                    Move = new Point(0, 0)
                };
            }

            Block(best, blockRange);
            return new BestMove
            {
                Result = PathingResult.DestinationNotReachedYet,
                Move = best
            };
        }


        private void Block(Point position, int nRange)
        {
            nRange = Math.Max(nRange, 1);

            for (var i = position.X - nRange; i < position.X + nRange; i++)
            {
                for (var j = position.Y - nRange; j < position.Y + nRange; j++)
                {
                    if (IsValidIndex(i, j))
                        m_distanceMatrix[i, j] = RangeInvalid;
                }
            }
        }

        private int GetRedundancy(List<Point> currentPath, int idxPath, Point position)
        {
            // step redundancy check
            for (var i = 1; i < idxPath; i++)
            {
                if (CalculateDistance(currentPath[i].X, currentPath[i].Y, position.X, position.Y) <= TpRange / 2.0)
                    return i;
            }

            return -1;
        }

        private bool IsValidIndex(int x, int y)
        {
            return x >= 0 && x < m_columns && y >= 0 && y < m_rows;
        }

        private static double CalculateDistance(long x1, long y1, long x2, long y2)
        {
            return Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
        }

        private static double CalculateDistance(Point point1, Point point2)
        {
            return CalculateDistance(point1.X, point1.Y, point2.X, point2.Y);
        }

        private struct BestMove
        {
            public PathingResult Result { get; set; }

            public Point Move { get; set; }
        }

    }

    enum PathingResult
    {
        Failed = 0,     // Failed, error occurred or no available path
        DestinationNotReachedYet,      // Path OK, destination not reached yet
        Reached // Path OK, destination reached(Path finding completed successfully)
    };

}
