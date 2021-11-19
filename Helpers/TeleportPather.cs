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


using MapAssist.Types;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace MapAssist.Helpers
{
    enum PathingResult
    {
        Failed = 0,     // Failed, error occurred or no available path
        DestinationNotReachedYet,      // Path OK, destination not reached yet
        Reached // Path OK, destination reached(Path finding completed successfully)
    };

    public class TeleportPather
    {
        private static readonly short RangeInvalid = 10000;
        private static readonly short TpRange = 30;
        private static readonly short BlockRange = 2;
        private readonly short[,] m_distanceMatrix;
        private readonly int m_rows;
        private readonly int m_columns;

        public TeleportPather(AreaData areaData)
        {
            m_rows = areaData.CollisionGrid.GetLength(0);
            m_columns = areaData.CollisionGrid[0].GetLength(0);
            m_distanceMatrix = new short[m_columns, m_rows];
            for (var i = 0; i < m_columns; i++)
            {
                for (var k = 0; k < m_rows; k++)
                {
                    m_distanceMatrix[i, k] = (short)areaData.CollisionGrid[k][i];
                }
            }

        }

        public void MakeDistanceTable(Point toLocation)
        {
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

        public TeleportPath GetTeleportPath(Point fromLocation, Point toLocation)
        {
            MakeDistanceTable(toLocation);
            var path = new List<Point>();
            path.Add(fromLocation);
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
                    return new TeleportPath()
                    {
                        Found = true,
                        Points = path.GetRange(0, idxPath)
                    };
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

            return new TeleportPath()
            {
                Found = false,
                Points = null
            };
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

        int GetRedundancy(List<Point> currentPath, int idxPath, Point position)
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
}
