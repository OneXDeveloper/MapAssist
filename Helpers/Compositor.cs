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
using System.Linq;

namespace MapAssist.Helpers
{
    public class Compositor
    {
        private readonly AreaData _areaData;
        private readonly PointF _cropOffset;
        private readonly PointF _origCenter;
        private readonly PointF _rotatedCenter;
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

        public (Bitmap, PointF) Compose(GameData gameData)
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

            var localPlayerPosition = AdjustedPoint(gameData.PlayerPosition);

            using (var gfx = Graphics.FromImage(image))
            {
                gfx.CompositingQuality = CompositingQuality.HighQuality;
                gfx.InterpolationMode = InterpolationMode.Bicubic;
                gfx.SmoothingMode = SmoothingMode.HighQuality;
                gfx.PixelOffsetMode = PixelOffsetMode.HighQuality;

                foreach (PointOfInterest poi in _pointsOfInterest)
                {
                    var poiIcon = GetIcon(poi.RenderingSettings);
                    var poiPosition = AdjustedPoint(poi.Position);

                    if (poi.RenderingSettings.CanDrawIcon())
                    {
                        gfx.DrawImage(poiIcon, poiPosition.OffsetFrom(GetIconOffset(poi.RenderingSettings)));
                    }

                    if (poi.RenderingSettings.CanDrawLine())
                    {
                        var pen = new Pen(poi.RenderingSettings.LineColor, poi.RenderingSettings.LineThickness);

                        if (poi.RenderingSettings.CanDrawArrowHead())
                        {
                            pen.CustomEndCap = new AdjustableArrowCap(poi.RenderingSettings.ArrowHeadSize, poi.RenderingSettings.ArrowHeadSize);
                        }

                        gfx.DrawLine(pen, localPlayerPosition, poiPosition);
                    }

                    if (!string.IsNullOrWhiteSpace(poi.Label) && poi.RenderingSettings.CanDrawLabel())
                    {
                        var poiFont = GetFont(poi.RenderingSettings);

                        var stringSize = gfx.MeasureString(poi.Label, poiFont);

                        gfx.DrawString(poi.Label, poiFont, new SolidBrush(poi.RenderingSettings.LabelColor), poiPosition.OffsetFrom(stringSize.Center()).OffsetFrom(new PointF(0, poiIcon.Height)));
                    }
                }

                if (MapAssistConfiguration.Loaded.MapConfiguration.Player.CanDrawIcon())
                {
                    var playerIcon = GetIcon(MapAssistConfiguration.Loaded.MapConfiguration.Player);
                    var playerPosition = localPlayerPosition.OffsetFrom(GetIconOffset(MapAssistConfiguration.Loaded.MapConfiguration.Player));
                    gfx.DrawImage(playerIcon, playerPosition);
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
                            var icon = GetIcon(mobRender);
                            var monsterPosition = AdjustedPoint(unitAny.Position).OffsetFrom(GetIconOffset(mobRender));

                            gfx.DrawImage(icon, monsterPosition);
                        }
                    }
                }

                foreach (var mobRender in monsterRenderingOrder)
                {
                    foreach (var unitAny in gameData.Monsters)
                    {
                        if (mobRender == GetMonsterIconRendering(unitAny.MonsterData) && mobRender.CanDrawIcon())
                        {
                            var icon = GetIcon(mobRender);
                            var monsterPosition = AdjustedPoint(unitAny.Position).OffsetFrom(GetIconOffset(mobRender));

                            // Draw Monster Immunities on top of monster icon
                            var iCount = unitAny.Immunities.Count;
                            if (iCount > 0)
                            {
                                var rectSize = mobRender.IconSize / 4f; // Arbirarily made the size set to 1/4rd of the mob icon size. The import point is that it scales with the mob icon consistently.
                                var dx = rectSize * scaleWidth * 1.5f; // Amount of space each indicator will take up, including spacing (which is the 1.5)

                                var iX = -icon.Width / 2f // Start at the center of the mob icon
                                    + (rectSize * scaleWidth) / 2f // Make sure the center of the indicator lines up with the center of the mob icon
                                    - dx * (iCount - 1) / 2f; // Moves the first indicator sufficiently left so that the whole group of indicators will be centered.

                                foreach (var immunity in unitAny.Immunities)
                                {
                                    var iPoint = new PointF(iX, icon.Height / 2f - icon.Height / 6f); // 1/6th of the height just helps move the icon a bit vertically to make it look nicer. Purely arbitrary.
                                    var brush = new SolidBrush(ResistColors.ResistColor[immunity]);
                                    var rect = new Rectangle(monsterPosition.OffsetFrom(iPoint).ToPoint(), new SizeF(rectSize * scaleWidth, rectSize * scaleWidth).ToSize()); // Scale both by the width since width isn't impacted by depth in overlay mode
                                    gfx.FillEllipse(brush, rect);
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
                            var icon = GetIcon(MapAssistConfiguration.Loaded.MapConfiguration.Item);
                            var itemPosition = AdjustedPoint(item.Position);
                            var itemBaseName = Items.ItemNames[item.TxtFileNo];

                            var stringSize = gfx.MeasureString(itemBaseName, font);

                            gfx.DrawImage(icon, itemPosition.OffsetFrom(GetIconOffset(MapAssistConfiguration.Loaded.MapConfiguration.Item)));
                            gfx.DrawString(itemBaseName, font, new SolidBrush(color), itemPosition.OffsetFrom(stringSize.Center()).OffsetFrom(new Point(0, icon.Height)));
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

        private (Bitmap, PointF, PointF, PointF) DrawBackground(AreaData areaData, IReadOnlyList<PointOfInterest> pointOfInterest)
        {
            var padding = 10;
            background = new Bitmap(areaData.CollisionGrid[0].Length, areaData.CollisionGrid.Length, PixelFormat.Format32bppArgb);
            using (var gfx = Graphics.FromImage(background))
            {
                gfx.CompositingQuality = CompositingQuality.HighQuality;
                gfx.InterpolationMode = InterpolationMode.Bicubic;
                gfx.SmoothingMode = SmoothingMode.HighQuality;
                gfx.PixelOffsetMode = PixelOffsetMode.HighQuality;

                for (var y = 0; y < areaData.CollisionGrid.Length; y++)
                {
                    for (var x = 0; x < areaData.CollisionGrid[y].Length; x++)
                    {
                        var type = areaData.CollisionGrid[y][x];
                        var typeColor = MapAssistConfiguration.Loaded.MapColorConfiguration.LookupMapColor(type);
                        if (typeColor != null)
                        {
                            background.SetPixel(x, y, (Color)typeColor);
                        }
                    }
                }
            }

            var center = new PointF(background.Width / 2f, background.Height / 2f);

            background = ImageUtils.RotateImage(background, _rotateDegrees, true, false, Color.Transparent);
            var rotatedCenter = new PointF(background.Width / 2f, background.Height / 2f);

            var (newBackground, cropOffset) = ImageUtils.CropBitmap(background, padding);

            return (newBackground, cropOffset, center, rotatedCenter);
        }

        private PointF AdjustedPoint(PointF p)
        {
            var newP = p.OffsetFrom(_areaData.Origin).Rotate(_rotateDegrees, _origCenter)
                .OffsetFrom(_origCenter.OffsetFrom(_rotatedCenter))
                .OffsetFrom(_cropOffset)
                .Multiply(scaleWidth, scaleHeight);

            return newP;
        }

        private (float, float) CalcResizeRatios(Bitmap image)
        {
            var multiplier = 5.5f - MapAssistConfiguration.Loaded.RenderingConfiguration.ZoomLevel; // Hitting +/- should make the map bigger/smaller, respectively, like in overlay = false mode

            if (!MapAssistConfiguration.Loaded.RenderingConfiguration.OverlayMode)
            {
                float biggestDimension = Math.Max(image.Width, image.Height);

                multiplier = MapAssistConfiguration.Loaded.RenderingConfiguration.Size / biggestDimension;

                if (multiplier == 0)
                {
                    multiplier = 1;
                }
            }
            else if (MapAssistConfiguration.Loaded.RenderingConfiguration.Position != MapPosition.Center)
            {
                multiplier *= 0.5f;
            }

            if (multiplier != 1 || MapAssistConfiguration.Loaded.RenderingConfiguration.OverlayMode)
            {
                var heightShrink = MapAssistConfiguration.Loaded.RenderingConfiguration.OverlayMode ? 0.5f : 1f;
                var widthShrink = MapAssistConfiguration.Loaded.RenderingConfiguration.OverlayMode ? 1f : 1f;

                return (multiplier * widthShrink, multiplier * heightShrink);
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
                var iconPadding = 5; // Some extra padding to make sure nothing get trimmed
                Bitmap bitmap = null;
                var pen = new Pen(poiSettings.IconColor, poiSettings.IconThickness);
                var brush = new SolidBrush(poiSettings.IconColor);
                
                switch (poiSettings.IconShape)
                {
                    case Shape.Square:
                    case Shape.SquareOutline:
                        var squarePoints = new PointF[]
                        {
                            new PointF(0, 0),
                            new PointF(poiSettings.IconSize, 0),
                            new PointF(poiSettings.IconSize, poiSettings.IconSize),
                            new PointF(0, poiSettings.IconSize)
                        }.Select(point => point.Rotate(_rotateDegrees).Multiply(scaleWidth, scaleHeight)).ToArray().MoveToOrigin(iconPadding);

                        bitmap = squarePoints.ToBitmap(iconPadding);
                        using (var g = Graphics.FromImage(bitmap))
                        {
                            g.SmoothingMode = SmoothingMode.HighQuality;
                            if (poiSettings.IconShape == Shape.Square)
                            {
                                g.FillPolygon(brush, squarePoints);
                            }
                            else
                            {
                                g.DrawPolygon(pen, squarePoints);
                            }
                        }
                        break;
                    case Shape.Ellipse:
                    case Shape.EllipseOutline:
                        bitmap = new SizeF(poiSettings.IconSize * scaleWidth + poiSettings.IconThickness, poiSettings.IconSize * scaleWidth + poiSettings.IconThickness).ToBitmap(iconPadding); // Don't distort - use scaleWidth for both
                        using (var g = Graphics.FromImage(bitmap))
                        {
                            g.SmoothingMode = SmoothingMode.HighQuality;
                            if (poiSettings.IconShape == Shape.Ellipse)
                            {
                                g.FillEllipse(brush, iconPadding, iconPadding, poiSettings.IconSize * scaleWidth, poiSettings.IconSize * scaleWidth);
                            }
                            else
                            {
                                g.DrawEllipse(pen, iconPadding, iconPadding, poiSettings.IconSize * scaleWidth, poiSettings.IconSize * scaleWidth);
                            }
                        }
                        break;
                    case Shape.Polygon:
                        var halfSize = poiSettings.IconSize / 2f;
                        var cutSize = poiSettings.IconSize / 10f;
                        var polygonPoints = new PointF[]
                        {
                                new PointF(0, halfSize), new PointF(halfSize - cutSize, halfSize - cutSize),
                                new PointF(halfSize, 0), new PointF(halfSize + cutSize, halfSize - cutSize),
                                new PointF(poiSettings.IconSize, halfSize),
                                new PointF(halfSize + cutSize, halfSize + cutSize),
                                new PointF(halfSize, poiSettings.IconSize),
                                new PointF(halfSize - cutSize, halfSize + cutSize)
                        }.Select(point => point.Multiply(scaleWidth)).ToArray().MoveToOrigin(iconPadding);

                        bitmap = polygonPoints.ToBitmap(iconPadding);
                        using (var g = Graphics.FromImage(bitmap))
                        {
                            g.SmoothingMode = SmoothingMode.HighQuality;
                            g.FillPolygon(brush, polygonPoints);
                        }
                        break;
                    case Shape.Cross:
                        var a = poiSettings.IconSize * 0.25f;
                        var b = poiSettings.IconSize * 0.50f;
                        var c = poiSettings.IconSize * 0.75f;
                        var d = poiSettings.IconSize;

                        var crossPoints = new PointF[]
                        {
                                new PointF(0, a), new PointF(a, 0), new PointF(b, a), new PointF(c, 0),
                                new PointF(d, a), new PointF(c, b), new PointF(d, c), new PointF(c, d),
                                new PointF(b, c), new PointF(a, d), new PointF(0, c), new PointF(a, b),
                                new PointF(0, a),
                        }.Select(point => point.Multiply(scaleWidth, scaleHeight)).ToArray().MoveToOrigin(iconPadding);

                        bitmap = crossPoints.ToBitmap(iconPadding);
                        using (var g = Graphics.FromImage(bitmap))
                        {
                            g.SmoothingMode = SmoothingMode.HighQuality;

                            for (var p = 0; p < crossPoints.Length - 1; p++)
                            {
                                g.DrawLine(pen, crossPoints[p], crossPoints[p + 1]);
                            }
                        }
                        break;
                }

                _iconCache[cacheKey] = bitmap;
            }

            return _iconCache[cacheKey];
        }

        private PointF GetIconOffset(IconRendering poiSettings)
        {
            var bitmap = GetIcon(poiSettings);
            return bitmap.Center();
        }
    }
}
