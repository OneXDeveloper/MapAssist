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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using MapAssist.Types;
using MapAssist.Settings;
using MapAssist.Structs;

namespace MapAssist.Helpers
{
    public class Compositor
    {
        private readonly AreaData _areaData;
        private readonly Point _cropOffset;
        private readonly Point _origCenter;
        private readonly Point _rotatedCenter;
        private readonly IReadOnlyList<PointOfInterest> _pointsOfInterest;
        private readonly Dictionary<(string, int), Font> _fontCache = new Dictionary<(string, int), Font>();
        private readonly int _rotateDegrees = 45;

        private readonly Dictionary<(Shape, int, Color, float, float), Bitmap> _iconCache =
            new Dictionary<(Shape, int, Color, float, float), Bitmap>();

        private Bitmap background;
        private Bitmap scaledBackground;
        private float scaleWidth = 1;
        private float scaleHeight = 1;
        private float lastZoom = -1;

        public Compositor(AreaData areaData, IReadOnlyList<PointOfInterest> pointsOfInterest)
        {
            _areaData = areaData;
            _pointsOfInterest = pointsOfInterest;
            (background, _cropOffset, _origCenter, _rotatedCenter) = DrawBackground(areaData, pointsOfInterest);
        }

        public (Bitmap, Point) Compose(GameData gameData, float zoomLevel)
        {
            if (gameData.Area != _areaData.Area)
            {
                throw new ApplicationException("Asked to compose an image for a different area." +
                                               $"Compositor area: {_areaData.Area}, Game data: {gameData.Area}");
            }

            var image = (Bitmap)background.Clone();

            if (lastZoom != MapAssistConfiguration.Loaded.RenderingConfiguration.ZoomLevel)
            {
                (scaleWidth, scaleHeight) = CalcResizeRatios(image);

                image = ImageUtils.ResizeImage(image, (int)(image.Width * scaleWidth), (int)(image.Height * scaleHeight));

                scaledBackground = (Bitmap)image.Clone();

                lastZoom = MapAssistConfiguration.Loaded.RenderingConfiguration.ZoomLevel;
            }
            else
            {
                image = (Bitmap)scaledBackground.Clone();
            }

            var localPlayerPosition = adjustedPoint(gameData.PlayerPosition);

            using (var imageGraphics = Graphics.FromImage(image))
            {
                imageGraphics.CompositingQuality = CompositingQuality.HighQuality;
                imageGraphics.InterpolationMode = InterpolationMode.Bicubic;
                imageGraphics.SmoothingMode = SmoothingMode.HighQuality;
                imageGraphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                if (MapAssistConfiguration.Loaded.MapConfiguration.Player.CanDrawIcon())
                {
                    Bitmap playerIcon = GetIcon(MapAssistConfiguration.Loaded.MapConfiguration.Player);
                    var playerPosition = localPlayerPosition.OffsetFrom(GetIconOffset(MapAssistConfiguration.Loaded.MapConfiguration.Player));
                    imageGraphics.DrawImage(playerIcon, playerPosition);
                }

                // The lines are dynamic, and follow the player, so have to be drawn here.
                // The rest can be done in DrawBackground.
                foreach (PointOfInterest poi in _pointsOfInterest)
                {
                    if (poi.RenderingSettings.CanDrawLine())
                    {
                        var pen = new Pen(poi.RenderingSettings.LineColor, poi.RenderingSettings.LineThickness);
                        if (poi.RenderingSettings.CanDrawArrowHead())
                        {
                            pen.CustomEndCap = new AdjustableArrowCap(poi.RenderingSettings.ArrowHeadSize,
                                poi.RenderingSettings.ArrowHeadSize);
                        }

                        var poiPosition = adjustedPoint(poi.Position);

                        imageGraphics.DrawLine(pen, localPlayerPosition, poiPosition);
                    }
                }

                var monsterRenderingOrder = new IconRendering[]
                {
                    MapAssistConfiguration.Loaded.MapConfiguration.NormalMonster,
                    MapAssistConfiguration.Loaded.MapConfiguration.EliteMonster,
                    MapAssistConfiguration.Loaded.MapConfiguration.UniqueMonster,
                    MapAssistConfiguration.Loaded.MapConfiguration.SuperUniqueMonster,
                };

                foreach (var mobRender in monsterRenderingOrder)
                {
                    foreach (var unitAny in gameData.Monsters)
                    {
                        if (mobRender == GetMonsterIconRendering(unitAny.MonsterData) && mobRender.CanDrawIcon())
                        {
                            Bitmap icon = GetIcon(mobRender);
                            var monsterPosition = adjustedPoint(unitAny.Position).OffsetFrom(GetIconOffset(mobRender));

                            imageGraphics.DrawImage(icon, monsterPosition);
                        }
                    }
                }

                foreach (var mobRender in monsterRenderingOrder)
                {
                    foreach (var unitAny in gameData.Monsters)
                    {
                        if (mobRender == GetMonsterIconRendering(unitAny.MonsterData) && mobRender.CanDrawIcon())
                        {
                            Bitmap icon = GetIcon(mobRender);
                            var monsterPosition = adjustedPoint(unitAny.Position).OffsetFrom(GetIconOffset(mobRender));

                            // Draw Monster Immunities on top of monster icon
                            var iCount = unitAny.Immunities.Count;
                            if (iCount > 0)
                            {
                                var rectSize = mobRender.IconSize / 3; // Arbirarily made the size set to 1/3rd of the mob icon size. The import point is that it scales with the mob icon consistently.
                                var dx = rectSize * scaleWidth * 1.5; // Amount of space each indicator will take up, including spacing (which is the 1.5)

                                var iX = -icon.Width / 2f // Start at the center of the mob icon
                                    + (rectSize * scaleWidth) / 2 // Make sure the center of the indicator lines up with the center of the mob icon
                                    - dx * (iCount - 1) / 2; // Moves the first indicator sufficiently left so that the whole group of indicators will be centered.

                                foreach (var immunity in unitAny.Immunities)
                                {
                                    var iPoint = new Point((int)Math.Round(iX), icon.Height / 2 + icon.Height / 12); // 1/12th of the height just helps move the icon a bit up to make it look nicer. Purely arbitrary.
                                    var brush = new SolidBrush(ResistColors.ResistColor[immunity]);
                                    var rect = new Rectangle(monsterPosition.OffsetFrom(iPoint), new Size((int)(rectSize * scaleWidth), (int)(rectSize * scaleWidth))); // Scale both by the width since width isn't impacted by depth in overlay mode
                                    imageGraphics.FillEllipse(brush, rect);
                                    iX += dx;
                                }
                            }
                        }
                    }
                }

                if (MapAssistConfiguration.Loaded.ItemLog.Enabled)
                {
                    var font = new Font(MapAssistConfiguration.Loaded.MapConfiguration.Item.LabelFont, MapAssistConfiguration.Loaded.MapConfiguration.Item.LabelFontSize);
                    foreach (var item in gameData.Items)
                    {
                        if (item.IsDropped())
                        {
                            if (!LootFilter.Filter(item))
                            {
                                continue;
                            }
                            var color = Items.ItemColors[item.ItemData.ItemQuality];
                            Bitmap icon = GetIcon(MapAssistConfiguration.Loaded.MapConfiguration.Item);
                            var itemPosition = adjustedPoint(item.Position).OffsetFrom(GetIconOffset(MapAssistConfiguration.Loaded.MapConfiguration.Item));
                            imageGraphics.DrawImage(icon, itemPosition);
                            var itemBaseName = Items.ItemNames[item.TxtFileNo];
                            imageGraphics.DrawString(itemBaseName, font,
                                new SolidBrush(color),
                                itemPosition.OffsetFrom(new Point(-icon.Width - 5, 0)));
                        }
                    }
                }
            }

            return (image, localPlayerPosition);
        }

        private static IconRendering GetMonsterIconRendering(MonsterData monsterData)
        {
            if ((monsterData.MonsterType & MonsterTypeFlags.SuperUnique) == MonsterTypeFlags.SuperUnique)
            {
                return MapAssistConfiguration.Loaded.MapConfiguration.SuperUniqueMonster;
            }

            if ((monsterData.MonsterType & MonsterTypeFlags.Unique) == MonsterTypeFlags.Unique)
            {
                return MapAssistConfiguration.Loaded.MapConfiguration.UniqueMonster;
            }

            if (monsterData.MonsterType > 0)
            {
                return MapAssistConfiguration.Loaded.MapConfiguration.EliteMonster;
            }

            return MapAssistConfiguration.Loaded.MapConfiguration.NormalMonster;
        }

        private (Bitmap, Point, Point, Point) DrawBackground(AreaData areaData, IReadOnlyList<PointOfInterest> pointOfInterest)
        {
            var background = new Bitmap(areaData.CollisionGrid[0].Length, areaData.CollisionGrid.Length,
                PixelFormat.Format32bppArgb);
            using (var backgroundGraphics = Graphics.FromImage(background))
            {
                backgroundGraphics.FillRectangle(new SolidBrush(Color.Transparent), 0, 0,
                    areaData.CollisionGrid[0].Length,
                    areaData.CollisionGrid.Length);
                backgroundGraphics.CompositingQuality = CompositingQuality.HighQuality;
                backgroundGraphics.InterpolationMode = InterpolationMode.Bicubic;
                backgroundGraphics.SmoothingMode = SmoothingMode.HighQuality;
                backgroundGraphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                for (var y = 0; y < areaData.CollisionGrid.Length; y++)
                {
                    for (var x = 0; x < areaData.CollisionGrid[y].Length; x++)
                    {
                        var type = areaData.CollisionGrid[y][x];
                        Color? typeColor = MapAssistConfiguration.Loaded.MapColorConfiguration.LookupMapColor(type);
                        if (typeColor != null)
                        {
                            background.SetPixel(x, y, (Color)typeColor);
                        }
                    }
                }

                foreach (PointOfInterest poi in pointOfInterest)
                {
                    if (poi.RenderingSettings.CanDrawIcon())
                    {
                        Bitmap icon = GetIcon(poi.RenderingSettings);
                        Point origin = poi.Position
                            .OffsetFrom(areaData.Origin)
                            .OffsetFrom(GetIconOffset(poi.RenderingSettings));
                        backgroundGraphics.DrawImage(icon, origin);
                    }

                    if (!string.IsNullOrWhiteSpace(poi.Label) && poi.RenderingSettings.CanDrawLabel())
                    {
                        Font font = GetFont(poi.RenderingSettings);
                        backgroundGraphics.DrawString(poi.Label, font,
                            new SolidBrush(poi.RenderingSettings.LabelColor),
                            poi.Position.OffsetFrom(areaData.Origin));
                    }
                }

                var center = new Point(background.Width / 2, background.Height / 2);

                background = ImageUtils.RotateImage(background, _rotateDegrees, true, false, Color.Transparent);
                var rotatedCenter = new Point(background.Width / 2, background.Height / 2);

                var (newBackground, cropOffset) = ImageUtils.CropBitmap(background);

                return (newBackground, cropOffset, center, rotatedCenter);
            }
        }

        private Point adjustedPoint(Point p)
        {
            p = ImageUtils.RotatePoint(p.OffsetFrom(_areaData.Origin), _origCenter, _rotateDegrees)
                .OffsetFrom(_origCenter.OffsetFrom(_rotatedCenter))
                .OffsetFrom(_cropOffset);

            p = new Point(
                (int)(p.X * scaleWidth),
                (int)(p.Y * scaleHeight)
            );

            return p;
        }

        private (float, float) CalcResizeRatios(Bitmap image)
        {
            var multiplier = 4.25f - MapAssistConfiguration.Loaded.RenderingConfiguration.ZoomLevel; // Hitting +/- should make the map bigger/smaller, respectively, like in overlay = false mode

            if (!MapAssistConfiguration.Loaded.RenderingConfiguration.OverlayMode)
            {
                float biggestDimension = Math.Max(image.Width, image.Height);

                multiplier = MapAssistConfiguration.Loaded.RenderingConfiguration.Size / biggestDimension;

                if (multiplier == 0)
                {
                    multiplier = 1;
                }
            }

            if (multiplier != 1 || MapAssistConfiguration.Loaded.RenderingConfiguration.OverlayMode)
            {
                var heightShrink = MapAssistConfiguration.Loaded.RenderingConfiguration.OverlayMode ? 0.5f : 1f;

                return (multiplier, multiplier * heightShrink);
            }

            return (multiplier, multiplier);
        }

        private Font GetFont(PointOfInterestRendering poiSettings)
        {
            (string LabelFont, int LabelFontSize) cacheKey = (poiSettings.LabelFont, poiSettings.LabelFontSize);
            if (!_fontCache.ContainsKey(cacheKey))
            {
                var font = new Font(poiSettings.LabelFont,
                    poiSettings.LabelFontSize);
                _fontCache[cacheKey] = font;
            }

            return _fontCache[cacheKey];
        }

        private Bitmap GetIcon(IconRendering poiSettings)
        {
            (Shape IconShape, int IconSize, Color Color, float LineThickness, float ZoomLevel) cacheKey = (
                poiSettings.IconShape,
                poiSettings.IconSize,
                poiSettings.IconColor,
                poiSettings.IconThickness,
                MapAssistConfiguration.Loaded.RenderingConfiguration.ZoomLevel
            );
            if (!_iconCache.ContainsKey(cacheKey))
            {
                var distort = poiSettings.IconShape == Shape.Cross ? true : false;
                var width = (int)Math.Ceiling(poiSettings.IconSize * scaleWidth + poiSettings.IconThickness);
                var height = (int)Math.Ceiling(poiSettings.IconSize * (distort ? scaleHeight : scaleWidth) + poiSettings.IconThickness);

                var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                var pen = new Pen(poiSettings.IconColor, poiSettings.IconThickness);
                var brush = new SolidBrush(poiSettings.IconColor);
                using (var g = Graphics.FromImage(bitmap))
                {
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    switch (poiSettings.IconShape)
                    {
                        case Shape.Ellipse:
                            g.FillEllipse(brush, 0, 0, poiSettings.IconSize * scaleWidth, poiSettings.IconSize * scaleWidth);
                            break;
                        case Shape.EllipseOutline:
                            g.DrawEllipse(pen, 0, 0, poiSettings.IconSize * scaleWidth, poiSettings.IconSize * scaleWidth);
                            break;
                        case Shape.Square:
                            g.FillRectangle(brush, 0, 0, poiSettings.IconSize * scaleWidth, poiSettings.IconSize * scaleWidth);
                            break;
                        case Shape.SquareOutline:
                            g.DrawRectangle(pen, 0, 0, poiSettings.IconSize * scaleWidth - 1, poiSettings.IconSize * scaleWidth - 1);
                            break;
                        case Shape.Polygon:
                            var halfSize = poiSettings.IconSize / 2;
                            var cutSize = poiSettings.IconSize / 10;
                            PointF[] curvePoints =
                            {
                                new PointF(0, halfSize), new PointF(halfSize - cutSize, halfSize - cutSize),
                                new PointF(halfSize, 0), new PointF(halfSize + cutSize, halfSize - cutSize),
                                new PointF(poiSettings.IconSize, halfSize),
                                new PointF(halfSize + cutSize, halfSize + cutSize),
                                new PointF(halfSize, poiSettings.IconSize),
                                new PointF(halfSize - cutSize, halfSize + cutSize)
                            };

                            for (var i = 0; i < curvePoints.Length; i++)
                            {
                                curvePoints[i] = new PointF(curvePoints[i].X * scaleWidth, curvePoints[i].Y * scaleWidth);
                            }

                            g.FillPolygon(brush, curvePoints);
                            break;
                        case Shape.Cross:
                            var a = poiSettings.IconSize * 0.25f;
                            var b = poiSettings.IconSize * 0.50f;
                            var c = poiSettings.IconSize * 0.75f;
                            var d = poiSettings.IconSize;

                            PointF[] crossLinePoints =
                            {
                                new PointF(0, a), new PointF(a, 0), new PointF(b, a), new PointF(c, 0),
                                new PointF(d, a), new PointF(c, b), new PointF(d, c), new PointF(c, d),
                                new PointF(b, c), new PointF(a, d), new PointF(0, c), new PointF(a, b),
                                new PointF(0, a),
                            };

                            for (var i = 0; i < crossLinePoints.Length; i++)
                            {
                                crossLinePoints[i] = new PointF(crossLinePoints[i].X * scaleWidth, crossLinePoints[i].Y * scaleHeight);
                            }

                            for (var p = 0; p < crossLinePoints.Length - 1; p++)
                            {
                                g.DrawLine(pen, crossLinePoints[p], crossLinePoints[p + 1]);
                            }

                            break;
                    }
                }

                _iconCache[cacheKey] = bitmap;
            }

            return _iconCache[cacheKey];
        }

        private Point GetIconOffset(IconRendering poiSettings)
        {
            var bitmap = GetIcon(poiSettings);
            return new Point(bitmap.Width / 2, bitmap.Height / 2);
        }
    }
}
