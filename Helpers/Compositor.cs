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

namespace MapAssist.Helpers
{
    public class Compositor
    {
        private readonly AreaData _areaData;
        private readonly Bitmap _background;
        public readonly Point CropOffset;
        private readonly IReadOnlyList<PointOfInterest> _pointsOfInterest;
        private readonly Dictionary<(string, int), Font> _fontCache = new Dictionary<(string, int), Font>();

        private readonly Dictionary<(Shape, int, Color, float), Bitmap> _iconCache =
            new Dictionary<(Shape, int, Color, float), Bitmap>();

        public Compositor(AreaData areaData, IReadOnlyList<PointOfInterest> pointOfInterest)
        {
            _areaData = areaData;
            _pointsOfInterest = pointOfInterest;
            (_background, CropOffset) = DrawBackground(areaData, pointOfInterest);
        }

        public Bitmap Compose(GameData gameData, bool scale = true)
        {
            if (gameData.Area != _areaData.Area)
            {
                throw new ApplicationException("Asked to compose an image for a different area." +
                                               $"Compositor area: {_areaData.Area}, Game data: {gameData.Area}");
            }

            var image = (Bitmap)_background.Clone();

            using (var imageGraphics = Graphics.FromImage(image))
            {
                imageGraphics.CompositingQuality = CompositingQuality.HighSpeed;
                imageGraphics.InterpolationMode = InterpolationMode.Bicubic;
                imageGraphics.SmoothingMode = SmoothingMode.HighSpeed;
                imageGraphics.PixelOffsetMode = PixelOffsetMode.HighSpeed;

                Point localPlayerPosition = gameData.PlayerPosition
                    .OffsetFrom(_areaData.Origin)
                    .OffsetFrom(CropOffset)
                    .OffsetFrom(GetIconOffset(MapAssistConfiguration.Loaded.MapConfiguration.Player.IconSize));

                var playerIconRadius = GetIconRadius(MapAssistConfiguration.Loaded.MapConfiguration.Player.IconSize);

                if (MapAssistConfiguration.Loaded.MapConfiguration.Player.CanDrawIcon())
                {
                    Bitmap playerIcon = GetIcon(MapAssistConfiguration.Loaded.MapConfiguration.Player);
                    imageGraphics.DrawImage(playerIcon, localPlayerPosition);
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

                        var localPlayerCenterPosition = new Point(
                            localPlayerPosition.X + playerIconRadius,
                            localPlayerPosition.Y + playerIconRadius
                        );
                        var poiPosition = poi.Position.OffsetFrom(_areaData.Origin).OffsetFrom(CropOffset);

                        imageGraphics.DrawLine(pen, localPlayerCenterPosition, poiPosition);
                    }
                }

                foreach (var unitAny in gameData.Monsters)
                {
                    var mobRender = unitAny.IsElite() ? MapAssistConfiguration.Loaded.MapConfiguration.EliteMonster : MapAssistConfiguration.Loaded.MapConfiguration.NormalMonster;

                    if (mobRender.CanDrawIcon())
                    {
                        // Draw Monster Icon
                        Bitmap icon = GetIcon(mobRender);
                        Point origin = unitAny.Position
                            .OffsetFrom(_areaData.Origin)
                            .OffsetFrom(CropOffset)
                            .OffsetFrom(GetIconOffset(mobRender.IconSize));
                        imageGraphics.DrawImage(icon, origin);

                        // Draw Monster Immunities
                        var iCount = unitAny.Immunities.Count;
                        if (iCount > 0)
                        {
                            var shortOffset = mobRender.IconShape == Shape.Cross;
                            var iY = shortOffset ? --iCount : iCount;
                            var iX = shortOffset ? -iY : -(iY - 2);

                            foreach (var immunity in unitAny.Immunities)
                            {
                                var iPoint = new Point(iX, iY);
                                var brush = new SolidBrush(ResistColors.ResistColor[immunity]);
                                var rect = new Rectangle(origin.OffsetFrom(iPoint), new Size(2, 2));
                                imageGraphics.FillRectangle(brush, rect);
                                iY -= 2;
                                iX += 2;
                            }
                        }
                    }
                }

                var font = new Font(MapAssistConfiguration.Loaded.MapConfiguration.Item.LabelFont, MapAssistConfiguration.Loaded.MapConfiguration.Item.LabelFontSize);
                foreach (var item in gameData.Items)
                {
                    if (!LootFilter.Filter(item))
                    {
                        continue;
                    }
                    var color = Items.ItemColors[item.ItemData.ItemQuality];
                    Bitmap icon = GetIcon(MapAssistConfiguration.Loaded.MapConfiguration.Item);
                    Point origin = item.Position
                        .OffsetFrom(_areaData.Origin)
                        .OffsetFrom(CropOffset)
                        .OffsetFrom(GetIconOffset(MapAssistConfiguration.Loaded.MapConfiguration.Item.IconSize));
                    imageGraphics.DrawImage(icon, origin);
                    var itemBaseName = Items.ItemNames[item.TxtFileNo];
                    imageGraphics.DrawString(itemBaseName, font,
                        new SolidBrush(color), 
                        item.Position
                        .OffsetFrom(_areaData.Origin)
                        .OffsetFrom(CropOffset).OffsetFrom(new Point((int)(itemBaseName.Length * 2.5f), 0)));
                }
            }

            double multiplier = 1;

            if (scale)
            {
                double biggestDimension = Math.Max(image.Width, image.Height);

                multiplier = MapAssistConfiguration.Loaded.RenderingConfiguration.Size / biggestDimension;

                if (multiplier == 0)
                {
                    multiplier = 1;
                }
            }

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (multiplier != 1)
            {
                image = ImageUtils.ResizeImage(image, (int)(image.Width * multiplier),
                    (int)(image.Height * multiplier));
            }

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (scale && MapAssistConfiguration.Loaded.RenderingConfiguration.Rotate)
            {
                image = ImageUtils.RotateImage(image, 53, true, false, Color.Transparent);
            }

            return image;
        }

        private (Bitmap, Point) DrawBackground(AreaData areaData, IReadOnlyList<PointOfInterest> pointOfInterest)
        {
            var background = new Bitmap(areaData.CollisionGrid[0].Length, areaData.CollisionGrid.Length,
                PixelFormat.Format32bppArgb);
            using (var backgroundGraphics = Graphics.FromImage(background))
            {
                backgroundGraphics.FillRectangle(new SolidBrush(Color.Transparent), 0, 0,
                    areaData.CollisionGrid[0].Length,
                    areaData.CollisionGrid.Length);
                backgroundGraphics.CompositingQuality = CompositingQuality.HighSpeed;
                backgroundGraphics.InterpolationMode = InterpolationMode.Bicubic;
                backgroundGraphics.SmoothingMode = SmoothingMode.HighSpeed;
                backgroundGraphics.PixelOffsetMode = PixelOffsetMode.HighSpeed;

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
                            .OffsetFrom(GetIconOffset(poi.RenderingSettings.IconSize));
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

                return ImageUtils.CropBitmap(background);
            }
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
            (Shape IconShape, int IconSize, Color Color, float LineThickness) cacheKey = (
                poiSettings.IconShape,
                poiSettings.IconSize,
                poiSettings.IconColor,
                poiSettings.IconThickness
            );
            if (!_iconCache.ContainsKey(cacheKey))
            {
                var bitmap = new Bitmap(poiSettings.IconSize, poiSettings.IconSize, PixelFormat.Format32bppArgb);
                var pen = new Pen(poiSettings.IconColor, poiSettings.IconThickness);
                var brush = new SolidBrush(poiSettings.IconColor);
                using (var g = Graphics.FromImage(bitmap))
                {
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    switch (poiSettings.IconShape)
                    {
                        case Shape.Ellipse:
                            g.FillEllipse(brush, 0, 0, poiSettings.IconSize, poiSettings.IconSize);
                            break;
                        case Shape.Square:
                            g.FillRectangle(brush, 0, 0, poiSettings.IconSize, poiSettings.IconSize);
                            break;
                        case Shape.SquareOutline:
                            g.DrawRectangle(pen, 0, 0, poiSettings.IconSize - 1, poiSettings.IconSize - 1);
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
                            g.FillPolygon(brush, curvePoints);
                            break;
                        case Shape.Cross:
                            var a = poiSettings.IconSize * 0.0833333f;
                            var b = poiSettings.IconSize * 0.3333333f;
                            var c = poiSettings.IconSize * 0.6666666f;
                            var d = poiSettings.IconSize * 0.9166666f;
                            PointF[] crossLinePoints =
                            {
                                new PointF(c, a), new PointF(c, b), new PointF(d, b), new PointF(d, c),
                                new PointF(c, c), new PointF(c, d), new PointF(b, d), new PointF(b, c),
                                new PointF(a, c), new PointF(a, b), new PointF(b, b), new PointF(b, a),
                                new PointF(c, a)
                            };
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

        private int GetIconRadius(int iconSize)
        {
            return (int)Math.Floor((decimal)iconSize / 2);
        }

        private Point GetIconOffset(int iconSize)
        {
            var radius = GetIconRadius(iconSize);
            return new Point(radius, radius);
        }
    }
}
