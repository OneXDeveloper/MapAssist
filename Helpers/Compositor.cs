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
using MapAssist.Structs;
using MapAssist.Types;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;

namespace MapAssist.Helpers
{
    public class Compositor : IDisposable
    {
        private readonly AreaData _areaData;
        private readonly PointF _cropOffset;
        private readonly PointF _origCenter;
        private readonly PointF _rotatedCenter;
        private readonly IReadOnlyList<PointOfInterest> _pointsOfInterest;
        private readonly float _rotateRadians = (float)(45 * Math.PI / 180f);

        private Bitmap gamemap;
        private SharpDX.Direct2D1.Bitmap gamemapDx;
        private float scaleWidth = 1;
        private float scaleHeight = 1;
        private const int WALKABLE = 0;
        private const int UNWALKABLE = 1;

        public Compositor(AreaData areaData, IReadOnlyList<PointOfInterest> pointsOfInterest)
        {
            _areaData = areaData;
            _pointsOfInterest = pointsOfInterest;
            (gamemap, _cropOffset, _origCenter, _rotatedCenter) = ComposeGamemap(areaData, pointsOfInterest);
        }

        private (Bitmap, PointF, PointF, PointF) ComposeGamemap(AreaData areaData, IReadOnlyList<PointOfInterest> pointOfInterest)
        {
            if (areaData == null) return (null, new PointF(0, 0), new PointF(0, 0), new PointF(0, 0));

            var gamemap = new Bitmap(areaData.CollisionGrid[0].Length, areaData.CollisionGrid.Length, PixelFormat.Format32bppArgb);
            
            using (var gfx = Graphics.FromImage(gamemap))
            {
                gfx.CompositingQuality = CompositingQuality.HighQuality;
                gfx.InterpolationMode = InterpolationMode.Bicubic;
                gfx.SmoothingMode = SmoothingMode.HighQuality;
                gfx.PixelOffsetMode = PixelOffsetMode.HighQuality;

                var maybeWalkableColor = MapAssistConfiguration.Loaded.MapColorConfiguration.Walkable;
                var maybeBorderColor = MapAssistConfiguration.Loaded.MapColorConfiguration.Border;

                if (maybeWalkableColor != null || maybeBorderColor != null)
                {
                    var walkableColor = maybeWalkableColor != null ? (Color)maybeWalkableColor : Color.Transparent;
                    var borderColor = maybeBorderColor != null ? (Color)maybeBorderColor : Color.Transparent;
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

                    for (var y = 0; y < areaData.CollisionGrid.Length; y++)
                    {
                        var maxYValue = areaData.CollisionGrid.Length;
                        for (var x = 0; x < areaData.CollisionGrid[y].Length; x++)
                        {
                            var type = areaData.CollisionGrid[y][x];
                            var isCurrentPixelWalkable = type == WALKABLE;

                            if (type == WALKABLE && maybeWalkableColor != null)
                            {
                                gamemap.SetPixel(x, y, walkableColor);
                            }

                            var maxXValue = areaData.CollisionGrid[y].Length;
                            foreach (var offset in lookOffsets)
                            {
                                var dy = y + offset[0];
                                var dx = x + offset[1];

                                var offsetsInBounds =
                                    dy >= 0 && dy < maxYValue &&
                                    dx >= 0 && dx < maxXValue;

                                var nextToWalkable = offsetsInBounds && !isCurrentPixelWalkable && areaData.CollisionGrid[dy][dx] == WALKABLE;

                                if (maybeBorderColor != null && nextToWalkable)
                                {
                                    gamemap.SetPixel(x, y, borderColor);
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            var center = gamemap.Center();
            gamemap = ImageUtils.RotateImage(gamemap, _rotateRadians, true, false, Color.Transparent);
            var rotatedCenter = gamemap.Center();
            var (croppedBackground, cropOffset) = ImageUtils.CropBitmap(gamemap);

            gamemap.Dispose();

            return (croppedBackground, cropOffset, center, rotatedCenter);
        }

        // Drawing each frame
        public void DrawGamemap(GameOverlay.Drawing.Graphics gfx, GameData gameData, PointF anchor)
        {
            if (gameData.Area != _areaData.Area)
            {
                throw new ApplicationException("Asked to compose an image for a different area." +
                                               $"Compositor area: {_areaData.Area}, Game data: {gameData.Area}");
            }

            if (gamemapDx == null)
            {
                SharpDX.Direct2D1.RenderTarget renderTarget = gfx.GetRenderTarget();
                gamemapDx = gamemap.ToDXBitmap(renderTarget);
            }

            DrawBitmap(gfx, gamemapDx, anchor, MapAssistConfiguration.Loaded.RenderingConfiguration.Opacity, preventGameBarOverlap: true);
        }

        public void DrawGameInfo(GameOverlay.Drawing.Graphics gfx, GameData gameData, PointF anchor,
            GameOverlay.Windows.DrawGraphicsEventArgs e, bool errorLoadingAreaData)
        {
            if (gameData.MenuPanelOpen >= 2)
            {
                return;
            }

            // Setup
            var fontSize = MapAssistConfiguration.Loaded.ItemLog.LabelFontSize;
            var fontHeight = (fontSize + fontSize / 2f);

            // Game IP
            if (MapAssistConfiguration.Loaded.GameInfo.Enabled)
            {
                var fontColor = gameData.Session.GameIP == MapAssistConfiguration.Loaded.HuntingIP ? Color.Green : Color.Red;

                var font = CreateFont(gfx, "Consolas", 14);
                var brush = CreateSolidBrush(gfx, fontColor, 1);

                var ipText = "Game IP: " + gameData.Session.GameIP;
                gfx.DrawText(font, brush, anchor.ToGameOverlayPoint(), ipText);
                
                anchor.Y += fontHeight + 5;

                // Overlay FPS
                if (MapAssistConfiguration.Loaded.GameInfo.ShowOverlayFPS)
                {
                    brush = CreateSolidBrush(gfx, Color.FromArgb(0, 255, 0), 1);
                    
                    var fpsText = "FPS: " + gfx.FPS.ToString();
                    var renderText = "DeltaTime: " + e.DeltaTime.ToString();
                    var spaceBetween = 20;

                    var stringSize = gfx.MeasureString(font, fpsText.ToString());
                    gfx.DrawText(font, brush, anchor.ToGameOverlayPoint(), fpsText.ToString());

                    gfx.DrawText(font, brush, anchor.Add(stringSize.X + spaceBetween, 0).ToGameOverlayPoint(), renderText.ToString());
                }
            }

            if (errorLoadingAreaData)
            {
                anchor.Y += fontHeight + 5;
                
                var font = CreateFont(gfx, "Consolas", 20);
                var brush = CreateSolidBrush(gfx, Color.Orange, 1);

                gfx.DrawText(font, brush, anchor.ToGameOverlayPoint(), "ERROR LOADING GAME MAP!");
            }

            anchor.Y += fontHeight + 5;

            DrawItemLog(gfx, gameData, anchor);
        }

        public void DrawItemLog(GameOverlay.Drawing.Graphics gfx, GameData gameData, PointF anchor)
        {
            if (gameData.MenuPanelOpen >= 2)
            {
                return;
            }

            // Setup
            var fontSize = MapAssistConfiguration.Loaded.ItemLog.LabelFontSize;
            var fontHeight = (fontSize + fontSize / 2f);

            // Item Log
            var ItemLog = Items.CurrentItemLog.ToArray();
            for (var i = 0; i < ItemLog.Length; i++)
            {
                var item = ItemLog[i];
                var fontColor = Items.ItemColors[item.ItemData.ItemQuality];

                var font = CreateFont(gfx, MapAssistConfiguration.Loaded.ItemLog.LabelFont, MapAssistConfiguration.Loaded.ItemLog.LabelFontSize);
                
                var isEth = (item.ItemData.ItemFlags & ItemFlags.IFLAG_ETHEREAL) == ItemFlags.IFLAG_ETHEREAL;
                var itemBaseName = Items.ItemName(item.TxtFileNo);
                var itemSpecialName = "";
                var itemLabelExtra = "";
                
                if (isEth)
                {
                    itemLabelExtra += "[Eth] ";
                    fontColor = Items.ItemColors[ItemQuality.SUPERIOR];
                }

                if (ItemLog[i].Stats.TryGetValue(Stat.STAT_ITEM_NUMSOCKETS, out var numSockets))
                {
                    itemLabelExtra += "[" + numSockets + " S] ";
                    fontColor = Items.ItemColors[ItemQuality.SUPERIOR];
                }

                var brush = CreateSolidBrush(gfx, fontColor, 1);
                
                switch (ItemLog[i].ItemData.ItemQuality)
                {
                    case ItemQuality.UNIQUE:
                        itemSpecialName = Items.UniqueName(item.TxtFileNo) + " ";
                        break;
                    case ItemQuality.SET:
                        itemSpecialName = Items.SetName(item.TxtFileNo) + " ";
                        break;
                }

                gfx.DrawText(font, brush, anchor.Add(0, i * fontHeight).ToGameOverlayPoint(), itemLabelExtra + itemSpecialName + itemBaseName);
            }
        }

        public void DrawOverlay(GameOverlay.Drawing.Graphics gfx, GameData gameData, PointF anchor)
        {
            var playerPosition = PlayerPosition(gameData).Add(anchor);

            foreach (PointOfInterest poi in _pointsOfInterest)
            {
                var poiPosition = AdjustedPoint(poi.Position).Add(anchor);

                if (poi.RenderingSettings.CanDrawIcon())
                {
                    DrawIcon(gfx, poi.RenderingSettings, poiPosition);
                }

                if (poi.RenderingSettings.CanDrawLine())
                {
                    DrawLine(gfx, poi.RenderingSettings, playerPosition, poiPosition);
                }

                if (!string.IsNullOrWhiteSpace(poi.Label) && poi.RenderingSettings.CanDrawLabel())
                {
                    var font = CreateFont(gfx, poi.RenderingSettings.LabelFont, poi.RenderingSettings.LabelFontSize);
                    var brush = CreateSolidBrush(gfx, poi.RenderingSettings.LabelColor, 1);
                    var iconShape = GetIconShape(poi.RenderingSettings).ToSizeF();

                    var stringSize = gfx.MeasureString(font, poi.Label);
                    gfx.DrawText(font, brush, poiPosition.Subtract(stringSize.Center()).Subtract(new PointF(0, stringSize.Y / 2 + iconShape.Height)).ToGameOverlayPoint(), poi.Label);
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
                        var monsterPosition = AdjustedPoint(unitAny.Position).Add(anchor);

                        DrawIcon(gfx, mobRender, monsterPosition);
                    }
                }
            }

            foreach (var mobRender in monsterRenderingOrder)
            {
                foreach (var unitAny in gameData.Monsters)
                {
                    if (mobRender == GetMonsterIconRendering(unitAny.MonsterData) && mobRender.CanDrawIcon())
                    {
                        var monsterPosition = AdjustedPoint(unitAny.Position).Add(anchor);

                        // Draw Monster Immunities on top of monster icon
                        var iCount = unitAny.Immunities.Count;
                        if (iCount > 0)
                        {
                            var iconShape = GetIconShape(mobRender).ToSizeF();

                            var ellipseSize = iconShape.Height / 10f; // Arbirarily set to be a fraction of the the mob icon size. The important point is that it scales with the mob icon consistently.
                            var dx = ellipseSize * scaleWidth * 1.5f; // Amount of space each indicator will take up, including spacing (which is the 1.5)

                            var iX = -dx * (iCount - 1) / 2f; // Moves the first indicator sufficiently left so that the whole group of indicators will be centered.

                            foreach (var immunity in unitAny.Immunities)
                            {
                                var render = new IconRendering()
                                {
                                    IconShape = Shape.Ellipse,
                                    IconColor = ResistColors.ResistColor[immunity],
                                    IconSize = ellipseSize
                                };

                                var iPoint = monsterPosition.Add(new PointF(iX, -iconShape.Height));
                                DrawIcon(gfx, render, iPoint);
                                iX += dx;
                            }
                        }
                    }
                }
            }

            if (MapAssistConfiguration.Loaded.ItemLog.Enabled)
            {
                var font = CreateFont(gfx, MapAssistConfiguration.Loaded.MapConfiguration.Item.LabelFont, MapAssistConfiguration.Loaded.MapConfiguration.Item.LabelFontSize);

                foreach (var item in gameData.Items)
                {
                    if (item.IsDropped())
                    {
                        if (!LootFilter.Filter(item))
                        {
                            continue;
                        }

                        var itemPosition = AdjustedPoint(item.Position).Add(anchor);
                        var render = MapAssistConfiguration.Loaded.MapConfiguration.Item;

                        DrawIcon(gfx, render, itemPosition);
                    }
                }

                foreach (var item in gameData.Items)
                {
                    if (item.IsDropped())
                    {
                        if (!LootFilter.Filter(item))
                        {
                            continue;
                        }

                        var itemPosition = AdjustedPoint(item.Position).Add(anchor);
                        var render = MapAssistConfiguration.Loaded.MapConfiguration.Item;

                        var color = Items.ItemColors[item.ItemData.ItemQuality];
                        var brush = CreateSolidBrush(gfx, color, 1);
                        var itemBaseName = Items.ItemName(item.TxtFileNo);
                        var iconShape = GetIconShape(render).ToSizeF();

                        var stringSize = gfx.MeasureString(font, itemBaseName);
                        gfx.DrawText(font, brush, itemPosition.Subtract(stringSize.Center()).Subtract(new PointF(0, stringSize.Y / 2 + iconShape.Height)).ToGameOverlayPoint(), itemBaseName);
                    }
                }
            }

            if (MapAssistConfiguration.Loaded.MapConfiguration.Player.CanDrawIcon())
            {
                DrawIcon(gfx, MapAssistConfiguration.Loaded.MapConfiguration.Player, playerPosition);
            }
        }

        public void DrawBuffs(GameOverlay.Drawing.Graphics gfx, GameData gameData)
        {
            var stateList = gameData.PlayerUnit.StateList;
            var buffColors = new List<Color>();
            var buffImageScale = MapAssistConfiguration.Loaded.RenderingConfiguration.BuffSize;
            var imgDimensions = 48f * buffImageScale;

            var buffAlignment = MapAssistConfiguration.Loaded.RenderingConfiguration.BuffPosition;
            var buffYPos = 0f;
            switch (buffAlignment)
            {
                case BuffPosition.Player:
                    buffYPos = (gfx.Height / 2f) - imgDimensions - (gfx.Height * .12f);
                    break;
                case BuffPosition.Top:
                    buffYPos = gfx.Height * .12f;
                    break;
                case BuffPosition.Bottom:
                    buffYPos = gfx.Height * .8f;
                    break;

            }

            var buffsByColor = new Dictionary<Color, List<SharpDX.Direct2D1.Bitmap>>();
            var totalBuffs = 0;

            buffsByColor.Add(States.DebuffColor, new List<SharpDX.Direct2D1.Bitmap>());
            buffsByColor.Add(States.PassiveColor, new List<SharpDX.Direct2D1.Bitmap>());
            buffsByColor.Add(States.AuraColor, new List<SharpDX.Direct2D1.Bitmap>());
            buffsByColor.Add(States.BuffColor, new List<SharpDX.Direct2D1.Bitmap>());
            
            foreach (var state in stateList)
            {
                var stateStr = Enum.GetName(typeof(State), state).Substring(6);
                var resImg = Properties.Resources.ResourceManager.GetObject(stateStr);

                if (resImg != null)
                {
                    Color buffColor = States.StateColor(state);
                    buffColors.Add(buffColor);
                    if (buffsByColor.TryGetValue(buffColor, out var _))
                    {
                        buffsByColor[buffColor].Add(CreateResourceBitmap(gfx, stateStr));
                        totalBuffs++;
                    }
                }
            }
            
            var buffIndex = 1;
            foreach (var buff in buffsByColor)
            {
                for (var i = 0; i < buff.Value.Count; i++)
                {
                    var buffImg = buff.Value[i];
                    var buffColor = buff.Key;
                    var drawPoint = new PointF((gfx.Width / 2f) - (buffIndex * imgDimensions) - (buffIndex * buffImageScale) - (totalBuffs * buffImageScale / 2f) + (totalBuffs * imgDimensions / 2f) + (totalBuffs * buffImageScale), buffYPos);
                    DrawBitmap(gfx, buffImg, drawPoint, 1, size: buffImageScale, zoomScaling: false);

                    var pen = new Pen(buffColor, buffImageScale);
                    if (buffColor == States.DebuffColor)
                    {
                        var size = new SizeF(imgDimensions - buffImageScale + buffImageScale + buffImageScale, imgDimensions - buffImageScale + buffImageScale + buffImageScale);
                        var rect = new GameOverlay.Drawing.Rectangle(drawPoint.X, drawPoint.Y, drawPoint.X + size.Width, drawPoint.Y + size.Height);

                        var debuffColor = States.DebuffColor;
                        debuffColor = Color.FromArgb(100, debuffColor.R, debuffColor.G, debuffColor.B);
                        var brush = CreateSolidBrush(gfx, debuffColor, 1);

                        gfx.FillRectangle(brush, rect);
                        gfx.DrawRectangle(brush, rect, 1);
                    }
                    else
                    {
                        var size = new SizeF(imgDimensions - buffImageScale + buffImageScale, imgDimensions - buffImageScale + buffImageScale);
                        var rect = new GameOverlay.Drawing.Rectangle(drawPoint.X, drawPoint.Y, drawPoint.X + size.Width, drawPoint.Y + size.Height);

                        var brush = CreateSolidBrush(gfx, buffColor, 1);
                        gfx.DrawRectangle(brush, rect, 1);
                    }

                    buffIndex++;
                }
            }
        }

        // Utility Functions
        private void DrawBitmap(GameOverlay.Drawing.Graphics gfx, SharpDX.Direct2D1.Bitmap bitmapDX, PointF anchor, float opacity,
            float size = 1, bool zoomScaling = true, bool preventGameBarOverlap = false)
        {
            var scaling = new PointF((zoomScaling ? scaleWidth : 1) * size, (zoomScaling ? scaleHeight : 1) * size);

            SharpDX.Direct2D1.RenderTarget renderTarget = gfx.GetRenderTarget();

            var sourceRect = new SharpDX.Mathematics.Interop.RawRectangleF(0, 0, bitmapDX.Size.Width, bitmapDX.Size.Height);
            var destRect = new SharpDX.Mathematics.Interop.RawRectangleF(
                anchor.X,
                anchor.Y,
                anchor.X + bitmapDX.Size.Width * scaling.X,
                anchor.Y + bitmapDX.Size.Height * scaling.Y);

            if (preventGameBarOverlap)
            {
                var extraHeight = Math.Max(destRect.Bottom - gfx.Height * 0.8f, 0);

                if (extraHeight > 0)
                {
                    destRect.Bottom -= extraHeight;
                    sourceRect.Bottom -= extraHeight / scaling.Y;
                }
            }

            renderTarget.DrawBitmap(bitmapDX, destRect, opacity, SharpDX.Direct2D1.BitmapInterpolationMode.Linear, sourceRect);
        }

        private void DrawIcon(GameOverlay.Drawing.Graphics gfx, IconRendering rendering, PointF position)
        {
            var fill = !rendering.IconShape.ToString().ToLower().EndsWith("outline");
            var brush = CreateSolidBrush(gfx, rendering.IconColor);

            var points = GetIconShape(rendering).Select(point => point.Add(position)).ToArray();

            using (var geo = points.ToGeometry(gfx, fill)) { 
                switch (rendering.IconShape)
                {
                    case Shape.Ellipse:
                    case Shape.EllipseOutline:
                        if (rendering.IconShape == Shape.Ellipse)
                        {
                            gfx.FillEllipse(brush, position.ToGameOverlayPoint(), rendering.IconSize * scaleWidth / 2f, rendering.IconSize * scaleWidth / 2f);
                        }
                        else
                        {
                            gfx.DrawEllipse(brush, position.ToGameOverlayPoint(), rendering.IconSize * scaleWidth / 2f, rendering.IconSize * scaleWidth / 2f, rendering.IconThickness);
                        }

                        break;
                    case Shape.Polygon:
                        gfx.FillGeometry(geo, brush);

                        break;
                    case Shape.Cross:
                        gfx.DrawGeometry(geo, brush, rendering.IconThickness);

                        break;
                    default:
                        if (points == null) break;

                        if (fill)
                        {
                            gfx.FillGeometry(geo, brush);
                        }
                        else
                        {
                            gfx.DrawGeometry(geo, brush, rendering.IconThickness);
                        }

                        break;
                }
            }
        }

        private void DrawLine(GameOverlay.Drawing.Graphics gfx, PointOfInterestRendering rendering, PointF startPosition, PointF endPosition)
        {
            var brush = CreateSolidBrush(gfx, rendering.LineColor);
            var line = new GameOverlay.Drawing.Line(startPosition.ToGameOverlayPoint(), endPosition.ToGameOverlayPoint());

            gfx.DrawLine(brush, line, rendering.LineThickness);
            
            if (rendering.CanDrawArrowHead())
            {
                var deltaX = endPosition.X >= startPosition.X ? endPosition.X - startPosition.X : startPosition.X - endPosition.X;
                var deltaY = endPosition.Y >= startPosition.Y ? endPosition.Y - startPosition.Y : startPosition.Y - endPosition.Y;

                var angle = endPosition.Subtract(startPosition).Angle();
                var height = (float)(Math.Sqrt(3) / 2);

                var points = new PointF[]
                {
                    new PointF(-height, 0.5f),
                    new PointF(-height, -0.5f),
                    new PointF(0, 0),
                }.Select(point => point.Multiply(rendering.ArrowHeadSize * scaleWidth).Rotate(angle).Add(endPosition).ToGameOverlayPoint()).ToArray();

                gfx.FillTriangle(brush, points[0], points[1], points[2]);
            }
        }

        private PointF[] GetIconShape(IconRendering render)
        {
            switch (render.IconShape)
            {
                case Shape.Square:
                case Shape.SquareOutline:
                    return new PointF[]
                    {
                        new PointF(0, 0),
                        new PointF(render.IconSize, 0),
                        new PointF(render.IconSize, render.IconSize),
                        new PointF(0, render.IconSize)
                    }.Select(point => point.Subtract(render.IconSize / 2f).Rotate(_rotateRadians).Multiply(scaleWidth, scaleHeight)).ToArray();
                case Shape.Ellipse: 
                case Shape.EllipseOutline: // Use a rectangle since that's effectively the same size and that's all this function is used for at the moment
                    return new PointF[]
                    {
                        new PointF(0, 0),
                        new PointF(render.IconSize, 0),
                        new PointF(render.IconSize, render.IconSize),
                        new PointF(0, render.IconSize)
                    }.Select(point => point.Subtract(render.IconSize / 2f).Multiply(scaleWidth)).ToArray();
                case Shape.Polygon:
                    var halfSize = render.IconSize / 2f;
                    var cutSize = render.IconSize / 10f;
                    
                    return new PointF[]
                    {
                        new PointF(0, halfSize), new PointF(halfSize - cutSize, halfSize - cutSize),
                        new PointF(halfSize, 0), new PointF(halfSize + cutSize, halfSize - cutSize),
                        new PointF(render.IconSize, halfSize),
                        new PointF(halfSize + cutSize, halfSize + cutSize),
                        new PointF(halfSize, render.IconSize),
                        new PointF(halfSize - cutSize, halfSize + cutSize)
                    }.Select(point => point.Subtract(halfSize).Multiply(scaleWidth, scaleHeight)).ToArray();
                case Shape.Cross:
                    var a = render.IconSize * 0.25f;
                    var b = render.IconSize * 0.50f;
                    var c = render.IconSize * 0.75f;
                    var d = render.IconSize;

                    return new PointF[]
                    {
                        new PointF(0, a), new PointF(a, 0), new PointF(b, a), new PointF(c, 0),
                        new PointF(d, a), new PointF(c, b), new PointF(d, c), new PointF(c, d),
                        new PointF(b, c), new PointF(a, d), new PointF(0, c), new PointF(a, b)
                    }.Select(point => point.Subtract(render.IconSize / 2f).Multiply(scaleWidth, scaleHeight)).ToArray();
            }
            return new PointF[]
            {
                        new PointF(0, 0)
            };
        }

        private IconRendering GetMonsterIconRendering(MonsterData monsterData)
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

        public void CalcResizeRatios()
        {
            var multiplier = 5.5f - MapAssistConfiguration.Loaded.RenderingConfiguration.ZoomLevel; // Hitting +/- should make the map bigger/smaller, respectively, like in overlay = false mode

            if (!MapAssistConfiguration.Loaded.RenderingConfiguration.OverlayMode)
            {
                float biggestDimension = Math.Max(gamemap.Width, gamemap.Height);

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

                (scaleWidth, scaleHeight) = (multiplier * widthShrink, multiplier * heightShrink);
            }
            else
            {
                (scaleWidth, scaleHeight) = (multiplier, multiplier);
            }
        }

        public SizeF GetMapSize()
        {
            return new SizeF(gamemap.Width * scaleWidth, gamemap.Height * scaleHeight);
        }

        public PointF PlayerPosition(GameData gameData)
        {
            return AdjustedPoint(gameData.PlayerPosition);
        }

        private PointF AdjustedPoint(PointF p)
        {
            var newP = p.Subtract(_areaData.Origin).Rotate(_rotateRadians, _origCenter)
                .Subtract(_origCenter.Subtract(_rotatedCenter))
                .Subtract(_cropOffset)
                .Multiply(scaleWidth, scaleHeight);

            return newP;
        }

        // Creates and cached resources
        private Dictionary<string, SharpDX.Direct2D1.Bitmap> cacheBitmaps = new Dictionary<string, SharpDX.Direct2D1.Bitmap>();
        private SharpDX.Direct2D1.Bitmap CreateResourceBitmap(GameOverlay.Drawing.Graphics gfx, string name)
        {
            var key = name;

            if (!cacheBitmaps.ContainsKey(key))
            {
                var renderTarget = gfx.GetRenderTarget();

                var resImg = Properties.Resources.ResourceManager.GetObject(name);
                cacheBitmaps[key] = new Bitmap((Bitmap)resImg).ToDXBitmap(renderTarget);
            }

            return cacheBitmaps[key];
        }

        private Dictionary<(string, float), GameOverlay.Drawing.Font> cacheFonts = new Dictionary<(string, float), GameOverlay.Drawing.Font>();
        private GameOverlay.Drawing.Font CreateFont(GameOverlay.Drawing.Graphics gfx, string fontFamilyName, float size)
        {
            var key = (fontFamilyName, size);
            if (!cacheFonts.ContainsKey(key)) cacheFonts[key] = gfx.CreateFont(fontFamilyName, size);
            return cacheFonts[key];
        }

        private Dictionary<(Color, float?), GameOverlay.Drawing.SolidBrush> cacheBrushes = new Dictionary<(Color, float?), GameOverlay.Drawing.SolidBrush>();
        private GameOverlay.Drawing.SolidBrush CreateSolidBrush(GameOverlay.Drawing.Graphics gfx, Color color,
            float? opacity = null)
        {
            if (opacity == null) opacity = MapAssistConfiguration.Loaded.RenderingConfiguration.Opacity;

            var key = (color, opacity);
            if (!cacheBrushes.ContainsKey(key)) cacheBrushes[key] = gfx.CreateSolidBrush(color.SetOpacity((float)opacity).ToGameOverlayColor());
            return cacheBrushes[key];
        }

        public void Dispose()
        {
            if (gamemapDx != null) gamemapDx.Dispose();

            foreach (var item in cacheBitmaps.Values) item.Dispose();
            foreach (var item in cacheFonts.Values) item.Dispose();
            foreach (var item in cacheBrushes.Values) item.Dispose();
        }
    }
}
