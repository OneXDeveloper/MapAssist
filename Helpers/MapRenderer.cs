/**
 *   Copyright (C) 2021 okaygo
 *
 *   https://github.com/misterokaygo/D2RAssist/
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
using D2RAssist.Types;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static D2RAssist.MapData;
using System.Text.RegularExpressions;

namespace D2RAssist.Helpers
{
    static class MapRenderer
    {
        public static Point LineEnd = new Point(0, 0);
        public static Bitmap CachedBackground;

        public static class Icons
        {
            public static readonly Bitmap DoorNext = CreateFilledRectangle(Settings.Map.Colors.DoorNext, 10, 10);
            public static readonly Bitmap DoorPrevious = CreateFilledRectangle(Settings.Map.Colors.DoorPrevious, 10, 10);
            public static readonly Bitmap Waypoint = CreateFilledRectangle(Settings.Map.Colors.Waypoint, 10, 10);
            public static readonly Bitmap Player = CreateFilledRectangle(Settings.Map.Colors.Player, 10, 10);
            public static readonly Bitmap Chest = CreateFilledEllipse(Settings.Map.Colors.Chest, 10, 10);
            public static readonly Bitmap SuperChest = CreateFilledEllipse(Settings.Map.Colors.SuperChest, 10, 10);
        }

        public static class MapPointsOfInterest
        {
            public static readonly string[] Chests = { "5", "6", "87", "104", "105", "106", "107", "143", "140", "141", "144", "146", "147", "148", "176", "177", "181", "183", "198", "240", "241", "242", "243", "329", "330", "331", "332", "333", "334", "335", "336", "354", "355", "356", "371", "387", "389", "390", "391", "397", "405", "406", "407", "413", "420", "424", "425", "430", "431", "432", "433", "454", "455", "501", "502", "504", "505", "581" };
            public static readonly string[] SuperChests = { "580"};
            public static readonly string[] Quests = { "61", "152", "266","357", "356", "354", "355", "376" };
            public static readonly string[] Waypoints = { "182", "298", "119", "145", "156", "157", "238", "237", "288", "323", "324", "398", "402", "429", "494", "496", "511", "539", "59", "60", "100" };
        }
            
        private static Bitmap CreateFilledRectangle(Color color, int width, int height)
        {
            Bitmap rectangle = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            Graphics graphics = Graphics.FromImage(rectangle);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.FillRectangle(new SolidBrush(color), 0, 0, width, height);
            graphics.Dispose();
            return rectangle;
        }

        private static Bitmap CreateFilledEllipse(Color color, int width, int height)
        {
            Bitmap ellipse = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            Graphics graphics = Graphics.FromImage(ellipse);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.FillEllipse(new SolidBrush(color), 0, 0, width, height);
            graphics.Dispose();
            return ellipse;
        }

        public static void Clear()
        {
            CachedBackground = null;
            LineEnd = new Point(0, 0);
        }

        public static Bitmap FromMapData(MapData mapData)
        {

            Graphics CachedBackgroundGraphics;
            Bitmap updatedMap;

            if (CachedBackground == null)
            {
                var uncroppedBackground = new Bitmap(mapData.mapRows[0].Length, mapData.mapRows.Length, PixelFormat.Format32bppArgb);

                for (int x = 0; x < mapData.mapRows.Length; x++)
                {
                    for (int y = 0; y < mapData.mapRows[x].Length; y++)
                    {
                        int type = mapData.mapRows[x][y];
                        switch (type)
                        {
                            case 1:
                                uncroppedBackground.SetPixel(y, x, Color.FromArgb(70, 51, 41));
                                break;
                            case -1:
                                // uncroppedBackground.SetPixel(y, x, Color.FromArgb(255, 255, 255));
                                break;
                            case 0:
                                uncroppedBackground.SetPixel(y, x, Color.FromArgb(0, 0, 0));
                                break;
                            case 16:
                                uncroppedBackground.SetPixel(y, x, Color.FromArgb(168, 56, 50));
                                break;
                            case 7:
                                uncroppedBackground.SetPixel(y, x, Color.FromArgb(255, 255, 255));
                                break;
                            case 5:
                                // uncroppedBackground.SetPixel(y, x, Color.FromArgb(0, 0, 0));
                                break;
                            case 33:
                                uncroppedBackground.SetPixel(y, x, Color.FromArgb(0, 0, 255));
                                break;
                            case 23:
                                uncroppedBackground.SetPixel(y, x, Color.FromArgb(0, 0, 255));
                                break;
                            case 4:
                                uncroppedBackground.SetPixel(y, x, Color.FromArgb(0, 255, 255));
                                break;
                            case 21:
                                uncroppedBackground.SetPixel(y, x, Color.FromArgb(255, 0, 255));
                                break;
                            case 20:
                                uncroppedBackground.SetPixel(y, x, Color.FromArgb(70, 51, 41));
                                break;
                            case 17:
                                uncroppedBackground.SetPixel(y, x, Color.FromArgb(255, 51, 255));
                                break;
                            case 3:
                                uncroppedBackground.SetPixel(y, x, Color.FromArgb(255, 0, 255));
                                break;
                            case 19:
                                uncroppedBackground.SetPixel(y, x, Color.FromArgb(0, 51, 255));
                                break;
                            case 2:
                                uncroppedBackground.SetPixel(y, x, Color.FromArgb(10, 51, 23));
                                break;
                            case 37:
                                uncroppedBackground.SetPixel(y, x, Color.FromArgb(50, 51, 23));
                                break;
                            case 6:
                                uncroppedBackground.SetPixel(y, x, Color.FromArgb(80, 51, 33));
                                break;
                            case 39:
                                uncroppedBackground.SetPixel(y, x, Color.FromArgb(20, 11, 33));
                                break;
                            case 53:
                                uncroppedBackground.SetPixel(y, x, Color.FromArgb(10, 11, 43));
                                break;
                            default:
                                uncroppedBackground.SetPixel(y, x, Color.FromArgb(255, 255, 255));
                                break;
                        }
                    }
                }
                CachedBackground = ImageManipulation.CropBitmap(uncroppedBackground);
            }

            double biggestDimension = CachedBackground.Width > CachedBackground.Height ? CachedBackground.Width : CachedBackground.Height;

            if (biggestDimension == 0)
            {
                biggestDimension = 1;
            }

            double multiplier = Settings.Map.Size / biggestDimension;

            if (multiplier == 0)
            {
                multiplier = 1;
            }

            int miniMapPlayerX = (int)((Globals.CurrentGameData.PlayerX - mapData.levelOrigin.x) * multiplier);
            int miniMapPlayerY = (int)((Globals.CurrentGameData.PlayerY - mapData.levelOrigin.y) * multiplier);
            Point playerPoint = new Point(miniMapPlayerX, miniMapPlayerY);
            updatedMap = ImageManipulation.ResizeImage((Bitmap)CachedBackground.Clone(), (int)(CachedBackground.Width * multiplier), (int)(CachedBackground.Height * multiplier));
            CachedBackgroundGraphics = Graphics.FromImage(updatedMap);
            DrawArrows(miniMapPlayerX, miniMapPlayerY, CachedBackgroundGraphics, multiplier, mapData);

            int counter = 0;
            int originX = mapData.levelOrigin.x;
            int originY = mapData.levelOrigin.y;

            foreach (KeyValuePair<string, AdjacentLevel> i in mapData.adjacentLevels)
            {
                if (mapData.adjacentLevels[i.Key].exits.Length == 0)
                {
                    continue;
                }
                int x = mapData.adjacentLevels[i.Key].exits[0].x;
                int y = mapData.adjacentLevels[i.Key].exits[0].y;
                int coordX = (int)(multiplier * (x - originX));
                int coordY = (int)(multiplier * (y - originY));
                var nextLevelPoint = LineEnd = new Point(coordX, coordY);
                if (counter == 0)
                {
                    CachedBackgroundGraphics.DrawImage(Icons.DoorPrevious, nextLevelPoint);
                }
                else
                {
                    CachedBackgroundGraphics.DrawImage(Icons.DoorNext, nextLevelPoint);
                }
                counter++;
            }

            foreach (KeyValuePair<string, XY[]> mapObject in mapData.objects)
            {
                int coordX = MultiplyIntByDouble(mapData.objects[mapObject.Key][0].x - originX, multiplier);
                int coordY = MultiplyIntByDouble(mapData.objects[mapObject.Key][0].y - originY, multiplier);
                Point mapObjectPoint = new Point(coordX, coordY);
                string mapObjectKey = (string)mapObject.Key;
                if (MapPointsOfInterest.Waypoints.Contains(mapObject.Key))
                {
                    CachedBackgroundGraphics.DrawImage(Icons.Waypoint, mapObjectPoint);
                }
                else if (MapPointsOfInterest.Waypoints.Contains(mapObject.Key))
                {
                    CachedBackgroundGraphics.DrawImage(Icons.DoorNext, mapObjectPoint);
                }
                else if (MapPointsOfInterest.SuperChests.Contains(mapObjectKey))
                {
                    foreach (XY coordinates in mapData.objects[mapObject.Key])
                    {
                        coordX = MultiplyIntByDouble(coordinates.x - originX, multiplier);
                        coordY = MultiplyIntByDouble(coordinates.y - originY, multiplier);
                        mapObjectPoint = new Point(coordX, coordY);
                        CachedBackgroundGraphics.DrawImage(Icons.SuperChest, mapObjectPoint);
                    }
                }
                else if (MapPointsOfInterest.Chests.Contains(mapObjectKey))
                {
                    foreach (XY coordinates in mapData.objects[mapObject.Key])
                    {
                        coordX = MultiplyIntByDouble(coordinates.x - originX, multiplier);
                        coordY = MultiplyIntByDouble(coordinates.y - originY, multiplier);
                        mapObjectPoint = new Point(coordX, coordY);
                        CachedBackgroundGraphics.DrawImage(Icons.Chest, mapObjectPoint);
                    }
                }
            }

            CachedBackgroundGraphics.DrawImage(Icons.Player, playerPoint);

            if (Settings.Map.Rotate)
            {
                updatedMap = ImageManipulation.RotateImage(updatedMap, 53, true, false, Color.Transparent);
            }

            return updatedMap;
        }

        private static int MultiplyIntByDouble(int _int, double _double)
        {
            return (int)(_int * _double);
        }

        private static void DrawArrows(int miniMapPlayerX, int miniMapPlayerY, Graphics minimap, double multiplier, MapData mapData)
        {
            // Exits
            foreach (KeyValuePair<string, AdjacentLevel> i in mapData.adjacentLevels)
            {
                if (mapData.adjacentLevels[i.Key].exits.Length == 0)
                {
                    continue;
                }
                int originX = mapData.levelOrigin.x;
                int originY = mapData.levelOrigin.y;
                int x = mapData.adjacentLevels[i.Key].exits[0].x;
                int y = mapData.adjacentLevels[i.Key].exits[0].y;
                int coordX = (int)((x - originX) * multiplier);
                int coordY = (int)((y - originY) * multiplier);

                // Draw arrow
                AdjustableArrowCap bigArrow = new AdjustableArrowCap(5, 5);
                Pen pen = new Pen(Settings.Map.Colors.ArrowExit, 5);
                pen.CustomEndCap = bigArrow;
                minimap.DrawLine(pen, miniMapPlayerX, miniMapPlayerY, coordX, coordY);
                // Draw label
                DrawLabelAt("Area", i.Key, coordX, coordY, minimap);
            }
            
            bool waypointFound = false;
            foreach (KeyValuePair<string, XY[]> mapObject in mapData.objects)
            {
                if (!waypointFound && MapPointsOfInterest.Waypoints.Contains(mapObject.Key))
                {
                    int originX = mapData.levelOrigin.x;
                    int originY = mapData.levelOrigin.y;
                    int pointX = (int)((mapData.objects[mapObject.Key][0].x - originX) * multiplier);
                    int pointY = (int)((mapData.objects[mapObject.Key][0].y - originY) * multiplier);
                    // Draw arrow
                    AdjustableArrowCap bigArrow = new AdjustableArrowCap(5, 5);
                    Pen pen = new Pen(Settings.Map.Colors.ArrowWaypoint, 5);
                    pen.CustomEndCap = bigArrow;
                    minimap.DrawLine(pen, miniMapPlayerX, miniMapPlayerY, pointX, pointY);
                    // Draw label
                    DrawLabelAt("GameObject", mapObject.Key, pointX, pointY, minimap);
                    waypointFound = true;
                }

                if (MapPointsOfInterest.Quests.Contains(mapObject.Key))
                {
                    int originX = mapData.levelOrigin.x;
                    int originY = mapData.levelOrigin.y;
                    int pointX = (int)((mapData.objects[mapObject.Key][0].x - originX) * multiplier);
                    int pointY = (int)((mapData.objects[mapObject.Key][0].y - originY) * multiplier);
                    // Draw arrow
                    AdjustableArrowCap bigArrow = new AdjustableArrowCap(5, 5);
                    Pen pen = new Pen(Settings.Map.Colors.ArrowQuest, 5);
                    pen.CustomEndCap = bigArrow;
                    minimap.DrawLine(pen, miniMapPlayerX, miniMapPlayerY, pointX, pointY);
                    // Draw label
                    DrawLabelAt("GameObject", mapObject.Key, pointX, pointY, minimap);
                }
            }
        }

        private static void DrawLabelAt(string objectType, string objectKey, int posx, int posy, Graphics minimap)
        {
            minimap.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
            // Create font and brush.
            Font drawFont = new Font(Settings.Map.LabelFont, 10);
            SolidBrush drawBrush = new SolidBrush(Settings.Map.Colors.LabelColor);
            // Set format of string.
            StringFormat drawFormat = new StringFormat();
            drawFormat.FormatFlags = StringFormatFlags.NoClip;
            // Draw label line
            int adjustment = 20;
            Pen p = new Pen(Settings.Map.Colors.LabelColor, 1);
            minimap.DrawLine(p, posx + adjustment, posy - (adjustment), posx, posy);
            // Label box
            RectangleF rectangleF = new RectangleF(posx + adjustment, posy - (2 * adjustment), 100, 100);
            if (objectType == "Area")
            {
                string objName = Enum.GetName(typeof(Game.Area), Int32.Parse(objectKey));
                objName = Game.AreaName[Int32.Parse(objectKey)];
                minimap.DrawString(objName, drawFont, drawBrush, rectangleF, drawFormat);
            }
            if (objectType == "GameObject")
            {
                string objName = Enum.GetName(typeof(Game.GameObject), Int32.Parse(objectKey));
                if (objName.Contains("Waypoint")) objName = "Waypoint";
                minimap.DrawString(objName, drawFont, drawBrush, rectangleF, drawFormat);
            }
        }
    }
}