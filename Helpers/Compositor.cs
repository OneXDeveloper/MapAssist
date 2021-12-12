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
using GameOverlay.Windows;
using MapAssist.Settings;
using MapAssist.Structs;
using MapAssist.Types;
using SharpDX;
using SharpDX.Direct2D1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Color = System.Drawing.Color;
using Pen = System.Drawing.Pen;

namespace MapAssist.Helpers
{
    public class Compositor : IDisposable
    {

        public GameData _gameData;
        public readonly AreaData _areaData;
        private readonly IReadOnlyList<PointOfInterest> _pointsOfInterest;

        private Bitmap gamemapDX;
        private Rectangle _drawBounds;
        private readonly float _rotateRadians = (float)(45 * Math.PI / 180f);
        private float scaleWidth = 1;
        private float scaleHeight = 1;
        private const int WALKABLE = 0;
        private const int UNWALKABLE = 1;

        public Compositor(AreaData areaData, IReadOnlyList<PointOfInterest> pointsOfInterest)
        {
            _areaData = areaData;
            _pointsOfInterest = pointsOfInterest;
        }

        public void Init(Graphics gfx, GameData gameData, Rectangle drawBounds)
        {
            _gameData = gameData;
            _drawBounds = drawBounds;
            (scaleWidth, scaleHeight) = GetScaleRatios();

            var mapSize = GetMapSize();
            var renderWidth = MapAssistConfiguration.Loaded.RenderingConfiguration.Size * mapSize.Width / mapSize.Height;
            switch (MapAssistConfiguration.Loaded.RenderingConfiguration.Position)
            {
                case MapPosition.TopLeft:
                    _drawBounds.Right = _drawBounds.Left + renderWidth;
                    break;
                case MapPosition.TopRight:
                    _drawBounds.Left = _drawBounds.Right - renderWidth;
                    break;
            }

            if (gamemapDX != null && gamemapDX.IsDisposed == false) return;

            RenderTarget renderTarget = gfx.GetRenderTarget();

            var imageSize = new Size2((int)_areaData.ViewRectangle.Width, (int)_areaData.ViewRectangle.Height);
            gamemapDX = new Bitmap(renderTarget, imageSize, new BitmapProperties(renderTarget.PixelFormat));
            var bytes = new byte[imageSize.Width * imageSize.Height * 4];

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

                for (var y = 0; y < imageSize.Height; y++)
                {
                    var _y = y + (int)_areaData.ViewRectangle.Top;
                    for (var x = 0; x < imageSize.Width; x++)
                    {
                        var _x = x + (int)_areaData.ViewRectangle.Left;
                        
                        var i = imageSize.Width * 4 * y + x * 4;
                        var type = _areaData.CollisionGrid[_y][_x];
                        var isCurrentPixelWalkable = type == WALKABLE;

                        // // Uncomment this to show a red border for debugging
                        // if (x == 0 || y == 0 || y == imageSize.Height - 1 || x == imageSize.Width - 1)
                        // {
                        //     bytes[i] = 0;
                        //     bytes[i + 1] = 0;
                        //     bytes[i + 2] = 255;
                        //     bytes[i + 3] = 255;
                        //     continue;
                        // }

                        if (type == WALKABLE && maybeWalkableColor != null)
                        {
                            bytes[i] = walkableColor.B;
                            bytes[i + 1] = walkableColor.G;
                            bytes[i + 2] = walkableColor.R;
                            bytes[i + 3] = walkableColor.A;
                        }

                        foreach (var offset in lookOffsets)
                        {
                            var dy = _y + offset[0];
                            var dx = _x + offset[1];

                            var offsetsInBounds =
                                dy >= 0 && dy < imageSize.Height &&
                                dx >= 0 && dx < imageSize.Width;

                            var nextToWalkable = offsetsInBounds && !isCurrentPixelWalkable && _areaData.CollisionGrid[dy][dx] == WALKABLE;

                            if (maybeBorderColor != null && nextToWalkable)
                            {
                                bytes[i] = borderColor.B;
                                bytes[i + 1] = borderColor.G;
                                bytes[i + 2] = borderColor.R;
                                bytes[i + 3] = borderColor.A;
                                break;
                            }
                        }
                    }
                }
            }

            gamemapDX.CopyFromMemory(bytes, imageSize.Width * 4);
        }

        public void DrawGamemap(Graphics gfx)
        {
            if (_gameData.Area != _areaData.Area)
            {
                throw new ApplicationException("Asked to compose an image for a different area." +
                                                $"Compositor area: {_areaData.Area}, Game data: {_areaData.Area}");
            }

            RenderTarget renderTarget = gfx.GetRenderTarget();

            ClearTransforms(gfx);
            renderTarget.PushAxisAlignedClip(_drawBounds, AntialiasMode.Aliased); // This needs to be before the transformation

            ApplyTransformMapData(gfx);
            renderTarget.DrawBitmap(gamemapDX, MapAssistConfiguration.Loaded.RenderingConfiguration.Opacity, BitmapInterpolationMode.Linear);

            renderTarget.PopAxisAlignedClip();
            ClearTransforms(gfx);
        }

        public void DrawOverlay(Graphics gfx)
        {
            RenderTarget renderTarget = gfx.GetRenderTarget();

            ClearTransforms(gfx);
            renderTarget.PushAxisAlignedClip(_drawBounds, AntialiasMode.Aliased); // This needs to be before the transformation

            ApplyTransformAreaData(gfx);

            DrawPointsOfInterest(gfx);
            DrawMonsters(gfx);
            DrawItems(gfx);
            DrawPlayers(gfx);

            renderTarget.PopAxisAlignedClip();
            ClearTransforms(gfx);
        }

        private void DrawPointsOfInterest(Graphics gfx)
        {
            foreach (var poi in _pointsOfInterest)
            {
                if (poi.RenderingSettings.CanDrawIcon())
                {
                    DrawIcon(gfx, poi.RenderingSettings, poi.Position);
                }

                if (poi.RenderingSettings.CanDrawLine())
                {
                    DrawLine(gfx, poi.RenderingSettings, _gameData.PlayerPosition, MovePointInBounds(poi.Position, _gameData.PlayerPosition));
                }
            }

            foreach (var poi in _pointsOfInterest)
            {
                if (!string.IsNullOrWhiteSpace(poi.Label) && poi.Type != PoiType.Shrine)
                {
                    if (poi.RenderingSettings.CanDrawLine() && poi.RenderingSettings.CanDrawLabel())
                    {
                        DrawText(gfx, poi.RenderingSettings, MovePointInBounds(poi.Position, _gameData.PlayerPosition), poi.Label);
                    }
                    else if (poi.RenderingSettings.CanDrawLabel())
                    {
                        DrawText(gfx, poi.RenderingSettings, poi.Position, poi.Label);
                    }
                }
            }

            foreach (var gameObject in _gameData.Objects)
            {
                if (gameObject.IsShrine())
                {
                    if (MapAssistConfiguration.Loaded.MapConfiguration.Shrine.CanDrawIcon())
                    {
                        DrawIcon(gfx, MapAssistConfiguration.Loaded.MapConfiguration.Shrine, gameObject.Position);
                    }
                    
                    if (MapAssistConfiguration.Loaded.MapConfiguration.Shrine.CanDrawLabel())
                    {
                        var label = Enum.GetName(typeof(ShrineType), gameObject.ObjectData.InteractType);

                        DrawText(gfx, MapAssistConfiguration.Loaded.MapConfiguration.Shrine, gameObject.Position, label);
                    }
                }
            }
        }

        private void DrawMonsters(Graphics gfx)
        {
            var monsterRenderingOrder = new IconRendering[]
            {
                MapAssistConfiguration.Loaded.MapConfiguration.NormalMonster,
                MapAssistConfiguration.Loaded.MapConfiguration.EliteMonster,
                MapAssistConfiguration.Loaded.MapConfiguration.UniqueMonster,
                MapAssistConfiguration.Loaded.MapConfiguration.SuperUniqueMonster,
            };

            foreach (var mobRender in monsterRenderingOrder)
            {
                foreach (var unitAny in _gameData.Monsters)
                {
                    if (mobRender == GetMonsterIconRendering(unitAny.MonsterData) && mobRender.CanDrawIcon())
                    {
                        var monsterPosition = unitAny.Position;

                        DrawIcon(gfx, mobRender, monsterPosition);
                    }
                }
            }

            foreach (var mobRender in monsterRenderingOrder)
            {
                foreach (var unitAny in _gameData.Monsters)
                {
                    if (mobRender == GetMonsterIconRendering(unitAny.MonsterData) && mobRender.CanDrawIcon())
                    {
                        var monsterPosition = unitAny.Position;
                        ApplyTransformAreaDataText(gfx, unitAny.Position);

                        // Draw Monster Immunities on top of monster icon
                        var iCount = unitAny.Immunities.Count;
                        if (iCount > 0)
                        {
                            var iconShape = GetIconShape(mobRender).ToRectangle();

                            var ellipseSize = iconShape.Height * scaleWidth / 10; // Arbirarily set to be a fraction of the the mob icon size. The important point is that it scales with the mob icon consistently.
                            var dx = ellipseSize * 3f; // Amount of space each indicator will take up, including spacing (which is the 1.5)

                            var iX = -dx * (iCount - 1) / 2f; // Moves the first indicator sufficiently left so that the whole group of indicators will be centered.

                            foreach (var immunity in unitAny.Immunities)
                            {
                                var render = new IconRendering()
                                {
                                    IconShape = Shape.Ellipse,
                                    IconColor = ResistColors.ResistColor[immunity],
                                    IconSize = ellipseSize * scaleHeight
                                };

                                var iPoint = monsterPosition.Add(new Point(iX, -iconShape.Height - render.IconSize));
                                DrawIcon(gfx, render, iPoint);
                                iX += dx;
                            }
                        }

                        PopTransform(gfx);
                    }
                }
            }
        }

        private void DrawItems(Graphics gfx)
        {
            if (MapAssistConfiguration.Loaded.ItemLog.Enabled)
            {
                foreach (var item in _gameData.Items)
                {
                    if (item.IsDropped())
                    {
                        if (!LootFilter.Filter(item))
                        {
                            continue;
                        }

                        var itemPosition = item.Position;
                        var render = MapAssistConfiguration.Loaded.MapConfiguration.Item;

                        DrawIcon(gfx, render, itemPosition);
                    }
                }

                foreach (var item in _gameData.Items)
                {
                    if (item.IsDropped())
                    {
                        if (!LootFilter.Filter(item))
                        {
                            continue;
                        }

                        if (Items.ItemColors.TryGetValue(item.ItemData.ItemQuality, out var color))
                        {
                            var itemBaseName = Items.ItemName(item.TxtFileNo);

                            DrawText(gfx, MapAssistConfiguration.Loaded.MapConfiguration.Item, item.Position, itemBaseName,
                                color: color);
                        }
                    }
                }
            }
        }

        private void DrawPlayers(Graphics gfx)
        {
            if (_gameData.Roster.EntriesByUnitId.TryGetValue(GameManager.PlayerUnit.UnitId, out var myPlayerEntry))
            {
                var canDrawIcon = MapAssistConfiguration.Loaded.MapConfiguration.Player.CanDrawIcon();
                var canDrawLabel = MapAssistConfiguration.Loaded.MapConfiguration.Player.CanDrawLabel();
                var canDrawNonPartyIcon = MapAssistConfiguration.Loaded.MapConfiguration.NonPartyPlayer.CanDrawIcon();
                var canDrawNonPartyLabel = MapAssistConfiguration.Loaded.MapConfiguration.NonPartyPlayer.CanDrawLabel();

                foreach (var player in _gameData.Roster.List)
                {
                    var myPlayer = player.UnitId == myPlayerEntry.UnitId;
                    var inMyParty = player.PartyID == myPlayerEntry.PartyID;
                    var playerName = player.Name;

                    if (_gameData.Players.TryGetValue(player.UnitId, out var playerUnit))
                    {
                        // use data from the unit table if available
                        if (inMyParty && player.PartyID < ushort.MaxValue) // partyid is max if player is not in a party
                        {
                            if (canDrawIcon)
                            {
                                DrawIcon(gfx, MapAssistConfiguration.Loaded.MapConfiguration.Player, playerUnit.Position);
                            }
                            if (canDrawLabel && !myPlayer)
                            {
                                DrawText(gfx, MapAssistConfiguration.Loaded.MapConfiguration.Player, playerUnit.Position, playerName, 
                                    color: MapAssistConfiguration.Loaded.MapConfiguration.Player.LabelColor);
                            }
                        }
                        else
                        {
                            if (!myPlayer)
                            {
                                if (canDrawNonPartyIcon)
                                {
                                    DrawIcon(gfx, MapAssistConfiguration.Loaded.MapConfiguration.NonPartyPlayer, playerUnit.Position);
                                }
                            }
                            else if (canDrawIcon)
                            {
                                DrawIcon(gfx, MapAssistConfiguration.Loaded.MapConfiguration.Player, playerUnit.Position);
                            }

                            if (canDrawNonPartyLabel && !myPlayer)
                            {
                                DrawText(gfx, MapAssistConfiguration.Loaded.MapConfiguration.NonPartyPlayer, playerUnit.Position, playerName,
                                    color: MapAssistConfiguration.Loaded.MapConfiguration.NonPartyPlayer.LabelColor);
                            }
                        }
                    }
                    else
                    {
                        var inCurrentOrAdjacentArea = player.Area == _gameData.Area || _areaData.AdjacentLevels.Keys.Contains(player.Area);
                        if (!inCurrentOrAdjacentArea) continue;

                        // otherwise use the data from the roster
                        // only draw if in the same party, otherwise position/area data will not be up to date
                        if (inMyParty && player.PartyID < ushort.MaxValue)
                        {
                            if (canDrawIcon)
                            {
                                DrawIcon(gfx, MapAssistConfiguration.Loaded.MapConfiguration.Player, player.Position);
                            }

                            if (canDrawLabel && !myPlayer)
                            {
                                DrawText(gfx, MapAssistConfiguration.Loaded.MapConfiguration.Player, player.Position, playerName, 
                                    color: MapAssistConfiguration.Loaded.MapConfiguration.Player.LabelColor);
                            }
                        }
                    }
                }
            }
        }

        public void DrawBuffs(Graphics gfx)
        {
            ClearTransforms(gfx);

            var stateList = _gameData.PlayerUnit.StateList;
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

            var buffsByColor = new Dictionary<Color, List<Bitmap>>();
            var totalBuffs = 0;

            buffsByColor.Add(States.DebuffColor, new List<Bitmap>());
            buffsByColor.Add(States.PassiveColor, new List<Bitmap>());
            buffsByColor.Add(States.AuraColor, new List<Bitmap>());
            buffsByColor.Add(States.BuffColor, new List<Bitmap>());

            foreach (var state in stateList)
            {
                var stateStr = Enum.GetName(typeof(State), state).Substring(6);
                var resImg = Properties.Resources.ResourceManager.GetObject(stateStr);

                if (resImg != null)
                {
                    Color buffColor = States.StateColor(state);
                    if (state == State.STATE_CONVICTION)
                    {
                        if (GameManager.PlayerUnit.Skill.RightSkillId == Skills.SKILL_CONVICTION) //add check later for if infinity is equipped
                        {
                            buffColor = States.BuffColor;
                        } else
                        {
                            buffColor = States.DebuffColor;
                        }
                    }
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
                    var drawPoint = new Point((gfx.Width / 2f) - (buffIndex * imgDimensions) - (buffIndex * buffImageScale) - (totalBuffs * buffImageScale / 2f) + (totalBuffs * imgDimensions / 2f) + (totalBuffs * buffImageScale), buffYPos);
                    DrawBitmap(gfx, buffImg, drawPoint, 1, size: buffImageScale);

                    var pen = new Pen(buffColor, buffImageScale);
                    if (buffColor == States.DebuffColor)
                    {
                        var size = new Point(imgDimensions - buffImageScale + buffImageScale + buffImageScale, imgDimensions - buffImageScale + buffImageScale + buffImageScale);
                        var rect = new Rectangle(drawPoint.X, drawPoint.Y, drawPoint.X + size.X, drawPoint.Y + size.Y);

                        var debuffColor = States.DebuffColor;
                        debuffColor = Color.FromArgb(100, debuffColor.R, debuffColor.G, debuffColor.B);
                        var brush = CreateSolidBrush(gfx, debuffColor, 1);

                        gfx.FillRectangle(brush, rect);
                        gfx.DrawRectangle(brush, rect, 1);
                    }
                    else
                    {
                        var size = new Point(imgDimensions - buffImageScale + buffImageScale, imgDimensions - buffImageScale + buffImageScale);
                        var rect = new Rectangle(drawPoint.X, drawPoint.Y, drawPoint.X + size.X, drawPoint.Y + size.Y);

                        var brush = CreateSolidBrush(gfx, buffColor, 1);
                        gfx.DrawRectangle(brush, rect, 1);
                    }

                    buffIndex++;
                }
            }

            ClearTransforms(gfx);
        }

        public void DrawGameInfo(Graphics gfx, Point anchor,
            DrawGraphicsEventArgs e, bool errorLoadingAreaData)
        {
            if (_gameData.MenuPanelOpen >= 2)
            {
                return;
            }

            ClearTransforms(gfx);

            // Setup
            var fontSize = MapAssistConfiguration.Loaded.ItemLog.LabelFontSize;
            var fontHeight = (fontSize + fontSize / 2f);

            // Game IP
            if (MapAssistConfiguration.Loaded.GameInfo.Enabled)
            {
                var fontColor = _gameData.Session.GameIP == MapAssistConfiguration.Loaded.HuntingIP ? Color.Green : Color.Red;

                var ipText = "Game IP: " + _gameData.Session.GameIP;
                DrawText(gfx, anchor, ipText, "Consolas", 14, fontColor);

                anchor.Y += fontHeight + 5;

                // Overlay FPS
                if (MapAssistConfiguration.Loaded.GameInfo.ShowOverlayFPS)
                {
                    var fpsText = "FPS: " + gfx.FPS.ToString() + "   " + "DeltaTime: " + e.DeltaTime.ToString();
                    DrawText(gfx, anchor, fpsText, "Consolas", 14, Color.FromArgb(0, 255, 0));

                    anchor.Y += fontHeight + 5;
                }
            }

            if (errorLoadingAreaData)
            {
                DrawText(gfx, anchor, "ERROR LOADING GAME MAP!", "Consolas", 20, Color.Orange);
                anchor.Y += fontHeight + 5;
            }

            DrawItemLog(gfx, anchor);
        }

        public void DrawItemLog(Graphics gfx, Point anchor)
        {
            if (_gameData.MenuPanelOpen >= 2)
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

                Color fontColor;
                if (!Items.ItemColors.TryGetValue(item.ItemData.ItemQuality, out fontColor))
                {
                    continue;
                }

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

                gfx.DrawText(font, brush, anchor.Add(0, i * fontHeight), itemLabelExtra + itemSpecialName + itemBaseName);
            }
        }

        // Utility Functions
        private void DrawBitmap(Graphics gfx, Bitmap bitmapDX, Point anchor, float opacity,
            float size = 1)
        {
            RenderTarget renderTarget = gfx.GetRenderTarget();

            var sourceRect = new Rectangle(0, 0, bitmapDX.Size.Width, bitmapDX.Size.Height);
            var destRect = new Rectangle(
                anchor.X,
                anchor.Y,
                anchor.X + bitmapDX.Size.Width * size,
                anchor.Y + bitmapDX.Size.Height * size);

            renderTarget.DrawBitmap(bitmapDX, destRect, opacity, BitmapInterpolationMode.Linear, sourceRect);
        }

        private void DrawIcon(Graphics gfx, IconRendering rendering, Point position)
        {
            var fill = !rendering.IconShape.ToString().ToLower().EndsWith("outline");
            var brush = CreateSolidBrush(gfx, rendering.IconColor);

            var points = GetIconShape(rendering).Select(point => point.Add(position)).ToArray();

            using (var geo = points.ToGeometry(gfx, fill))
            {
                switch (rendering.IconShape)
                {
                    case Shape.Ellipse:
                    case Shape.EllipseOutline:
                        if (rendering.IconShape == Shape.Ellipse)
                        {
                            gfx.FillEllipse(brush, position, rendering.IconSize / scaleHeight, rendering.IconSize / scaleHeight);
                        }
                        else
                        {
                            gfx.DrawEllipse(brush, position, rendering.IconSize / scaleHeight, rendering.IconSize / scaleHeight, rendering.IconThickness / scaleHeight);
                        }

                        break;
                    case Shape.Polygon:
                        gfx.FillGeometry(geo, brush);

                        break;
                    case Shape.Cross:
                        gfx.DrawGeometry(geo, brush, rendering.IconThickness / scaleHeight);

                        break;
                    default:
                        if (points == null) break;

                        if (fill)
                        {
                            gfx.FillGeometry(geo, brush);
                        }
                        else
                        {
                            gfx.DrawGeometry(geo, brush, rendering.IconThickness / scaleHeight);
                        }

                        break;
                }
            }
        }

        private void DrawLine(Graphics gfx, PointOfInterestRendering rendering, Point startPosition, Point endPosition)
        {
            var brush = CreateSolidBrush(gfx, rendering.LineColor);

            if (rendering.CanDrawArrowHead())
            {
                var angle = endPosition.Subtract(startPosition).Angle();

                var points = new Point[]
                {
                    new Point((float)(Math.Sqrt(3) / -2), 0.5f),
                    new Point((float)(Math.Sqrt(3) / -2), -0.5f),
                    new Point(0, 0),
                }.Select(point => point.Multiply(rendering.ArrowHeadSize).Rotate(angle).Add(endPosition)).ToArray();

                endPosition = endPosition.Rotate(-angle, startPosition).Subtract(rendering.ArrowHeadSize / 2f, 0).Rotate(angle, startPosition);
                var line = new Line(startPosition, endPosition);

                gfx.DrawLine(brush, line, rendering.LineThickness / scaleHeight);
                gfx.FillTriangle(brush, points[0], points[1], points[2]);
            }
            else
            {
                var line = new Line(startPosition, endPosition);
                gfx.DrawLine(brush, line, rendering.LineThickness);
            }
        }

        private void DrawText(Graphics gfx, PointOfInterestRendering rendering, Point position, string text,
            Color? color = null)
        {
            ApplyTransformAreaDataText(gfx, position);

            var useColor = color == null ? rendering.LabelColor : (Color)color;

            var font = CreateFont(gfx, rendering.LabelFont, rendering.LabelFontSize);
            var iconShape = GetIconShape(rendering).ToRectangle();
            var textSize = gfx.MeasureString(font, text);

            var playerCoord = Vector2.Transform(_gameData.PlayerPosition.ToVector(), transforms.Last());
            var textCoord = Vector2.Transform(position.ToVector(), transforms.Last());

            var multiplier = playerCoord.Y < textCoord.Y ? 1 : -1;

            position = position.Add(new Point(0, textSize.Y / 2 + (iconShape.Height + 10) * multiplier));
            position = MoveTextInBounds(position, text, textSize);

            DrawText(gfx, position, text, rendering.LabelFont, rendering.LabelFontSize, useColor,
                centerText: true);

            PopTransform(gfx);
        }

        private void DrawText(Graphics gfx, Point position, string text, string fontFamily, float fontSize, Color color,
            bool centerText = false)
        {
            var font = CreateFont(gfx, fontFamily, fontSize);
            var brush = CreateSolidBrush(gfx, color, 1);

            if (centerText)
            {
                var stringSize = gfx.MeasureString(font, text);
                position = position.Subtract(stringSize.X / 2, stringSize.Y);
            }

            gfx.DrawText(font, brush, position, text);
        }

        private Point[] GetIconShape(IconRendering render)
        {
            switch (render.IconShape)
            {
                case Shape.Square:
                case Shape.SquareOutline:
                    return new Point[]
                    {
                        new Point(0, 0),
                        new Point(render.IconSize, 0),
                        new Point(render.IconSize, render.IconSize),
                        new Point(0, render.IconSize)
                    }.Select(point => point.Subtract(render.IconSize / 2f)).ToArray();
                case Shape.Ellipse:
                case Shape.EllipseOutline: // Use a rectangle since that's effectively the same size and that's all this function is used for at the moment
                    return new Point[]
                    {
                        new Point(0, 0),
                        new Point(render.IconSize, 0),
                        new Point(render.IconSize, render.IconSize),
                        new Point(0, render.IconSize)
                    }.Select(point => point.Subtract(render.IconSize / 2f)).ToArray();
                case Shape.Polygon:
                    var halfSize = render.IconSize / 2f;
                    var cutSize = render.IconSize / 10f;

                    return new Point[]
                    {
                        new Point(0, halfSize), new Point(halfSize - cutSize, halfSize - cutSize),
                        new Point(halfSize, 0), new Point(halfSize + cutSize, halfSize - cutSize),
                        new Point(render.IconSize, halfSize),
                        new Point(halfSize + cutSize, halfSize + cutSize),
                        new Point(halfSize, render.IconSize),
                        new Point(halfSize - cutSize, halfSize + cutSize)
                    }.Select(point => point.Subtract(halfSize).Rotate(-_rotateRadians)).ToArray();
                case Shape.Cross:
                    var a = render.IconSize * 0.25f;
                    var b = render.IconSize * 0.50f;
                    var c = render.IconSize * 0.75f;
                    var d = render.IconSize;

                    return new Point[]
                    {
                        new Point(0, a), new Point(a, 0), new Point(b, a), new Point(c, 0),
                        new Point(d, a), new Point(c, b), new Point(d, c), new Point(c, d),
                        new Point(b, c), new Point(a, d), new Point(0, c), new Point(a, b)
                    }.Select(point => point.Subtract(render.IconSize / 2f).Rotate(-_rotateRadians)).ToArray();
            }

            return new Point[]
            {
                new Point(0, 0)
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

        private Point MovePointInBounds(Point point, Point origin)
        {
            var resizeScale = 1f;
            
            var startScreenCoord = Vector2.Transform(origin.ToVector(), transforms.Last());
            var endScreenCoord = Vector2.Transform(point.ToVector(), transforms.Last());

            if (endScreenCoord.X < _drawBounds.Left) resizeScale = Math.Min(resizeScale, (_drawBounds.Left - startScreenCoord.X) / (endScreenCoord.X - startScreenCoord.X));
            if (endScreenCoord.X > _drawBounds.Right) resizeScale = Math.Min(resizeScale, (_drawBounds.Right - startScreenCoord.X) / (endScreenCoord.X - startScreenCoord.X));
            if (endScreenCoord.Y < _drawBounds.Top) resizeScale = Math.Min(resizeScale, (_drawBounds.Top - startScreenCoord.Y) / (endScreenCoord.Y - startScreenCoord.Y));
            if (endScreenCoord.Y > _drawBounds.Bottom) resizeScale = Math.Min(resizeScale, (_drawBounds.Bottom - startScreenCoord.Y) / (endScreenCoord.Y - startScreenCoord.Y));

            if (resizeScale < 1)
            {
                return point.Subtract(origin).Multiply(resizeScale).Add(origin);
            }
            else
            {
                return point;
            }
        }

        private Point MoveTextInBounds(Point point, string text, Point size)
        {
            var screenCoord = Vector2.Transform(point.ToVector(), transforms.Last());
            var halfSize = size.Multiply(1 / 2f);

            if (screenCoord.X - halfSize.X < _drawBounds.Left) point.X += _drawBounds.Left - screenCoord.X + halfSize.X;
            if (screenCoord.X + halfSize.X > _drawBounds.Right) point.X += _drawBounds.Right - screenCoord.X - halfSize.X;
            if (screenCoord.Y - halfSize.Y < _drawBounds.Top) point.Y += _drawBounds.Top - screenCoord.Y + size.Y;
            if (screenCoord.Y + halfSize.Y > _drawBounds.Bottom) point.Y += _drawBounds.Bottom - screenCoord.Y;

            return point;
        }

        private Rectangle GetMapSize()
        {
            return new Point[]
                {
                    new Point(0, 0),
                    new Point(0, 1),
                    new Point(1, 1),
                    new Point(1, 0),
                }.Select(point => point.Multiply(_areaData.ViewRectangle.Width, _areaData.ViewRectangle.Height).Rotate(_rotateRadians)).ToArray().ToRectangle();
        }

        private (float, float) GetScaleRatios()
        {
            var multiplier = 5.5f - MapAssistConfiguration.Loaded.RenderingConfiguration.ZoomLevel; // Hitting +/- should make the map bigger/smaller, respectively, like in overlay = false mode

            if (!MapAssistConfiguration.Loaded.RenderingConfiguration.OverlayMode)
            {
                var mapSize = GetMapSize();
                
                multiplier = MapAssistConfiguration.Loaded.RenderingConfiguration.Size / mapSize.Height;

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
            else
            {
                return (multiplier, multiplier);
            }
        }

        private void ApplyTransformMapData(Graphics gfx)
        {
            Matrix3x2 matrix;

            if (MapAssistConfiguration.Loaded.RenderingConfiguration.OverlayMode)
            {
                matrix = Matrix3x2.CreateTranslation(_areaData.Origin.ToVector())
                    * Matrix3x2.CreateTranslation(Vector2.Negate(_gameData.PlayerPosition.ToVector()));
            }
            else
            {
                matrix = Matrix3x2.CreateTranslation(Vector2.Negate(new Vector2(_areaData.ViewRectangle.Left, _areaData.ViewRectangle.Top)))
                    * Matrix3x2.CreateTranslation(Vector2.Negate(new Vector2(_areaData.ViewRectangle.Width / 2, _areaData.ViewRectangle.Height / 2)));
            }

            matrix *= Matrix3x2.CreateRotation(_rotateRadians)
                * Matrix3x2.CreateScale(scaleWidth, scaleHeight);

            if (MapAssistConfiguration.Loaded.RenderingConfiguration.Position == MapPosition.Center)
            {
                matrix *= Matrix3x2.CreateTranslation(new Vector2(gfx.Width / 2, gfx.Height / 2))
                    * Matrix3x2.CreateTranslation(new Vector2(2, -8)); // Brute forced to perfectly line up with the in game map;
            }
            else
            {
                matrix *= Matrix3x2.CreateTranslation(new Vector2(_drawBounds.Left, _drawBounds.Top))
                    * Matrix3x2.CreateTranslation(new Vector2(_drawBounds.Width / 2, _drawBounds.Height / 2));
            }

            ApplyTransform(gfx, matrix);
        }

        public void ApplyTransformAreaData(Graphics gfx)
        {
            ApplyTransformMapData(gfx);

            var matrix = Matrix3x2.CreateTranslation(Vector2.Negate(_areaData.Origin.ToVector()));

            if (!MapAssistConfiguration.Loaded.RenderingConfiguration.OverlayMode)
            {
                matrix = matrix
                    * Matrix3x2.CreateTranslation(Vector2.Negate(new Vector2(_areaData.ViewRectangle.Left, _areaData.ViewRectangle.Top)));
            }

            ApplyTransform(gfx, matrix, multiplyAfter: false);
        }

        private void ApplyTransformAreaDataText(Graphics gfx, Point point)
        {
            var matrix = transforms.Last();
            var centerVector = Vector2.Transform(((Point)point).ToVector(), matrix);

            ApplyTransform(gfx,
                Matrix3x2.CreateTranslation(Vector2.Negate(centerVector))
                * Matrix3x2.CreateScale(1 / scaleWidth, 1 / scaleHeight)
                * Matrix3x2.CreateRotation(-_rotateRadians)
                * Matrix3x2.CreateTranslation(centerVector)
            );
        }

        // Graphics transformations
        private List<Matrix3x2> transforms = new List<Matrix3x2>();
        private void ApplyTransform(Graphics gfx, Matrix3x2 matrix,
            bool multiplyAfter = true)
        {
            var newTransform = transforms.Count == 0 ? matrix : multiplyAfter ? transforms.Last() * matrix : matrix * transforms.Last();
            transforms.Add(newTransform);

            gfx.GetRenderTarget().Transform = newTransform.ToDXMatrix();
        }
        
        private Matrix3x2 PopTransform(Graphics gfx)
        {
            var transform = transforms.Last();

            transforms.RemoveAt(transforms.Count - 1);
            gfx.GetRenderTarget().Transform = (transforms.Count == 0 ? Matrix3x2.Identity : transforms.Last()).ToDXMatrix();

            return transform;
        }

        private void ClearTransforms(Graphics gfx)
        {
            transforms.Clear();
            gfx.GetRenderTarget().Transform = Matrix3x2.Identity.ToDXMatrix();
        }

        // Creates and cached resources
        private Dictionary<string, Bitmap> cacheBitmaps = new Dictionary<string, Bitmap>();
        private Bitmap CreateResourceBitmap(Graphics gfx, string name)
        {
            var key = name;

            if (!cacheBitmaps.ContainsKey(key))
            {
                var renderTarget = gfx.GetRenderTarget();

                var resImg = Properties.Resources.ResourceManager.GetObject(name);
                cacheBitmaps[key] = new System.Drawing.Bitmap((System.Drawing.Bitmap)resImg).ToDXBitmap(renderTarget);
            }

            return cacheBitmaps[key];
        }

        private Dictionary<(string, float), Font> cacheFonts = new Dictionary<(string, float), Font>();
        private Font CreateFont(Graphics gfx, string fontFamilyName, float size)
        {
            var key = (fontFamilyName, size);
            if (!cacheFonts.ContainsKey(key)) cacheFonts[key] = gfx.CreateFont(fontFamilyName, size);
            return cacheFonts[key];
        }

        private Dictionary<(Color, float?), SolidBrush> cacheBrushes = new Dictionary<(Color, float?), SolidBrush>();
        private SolidBrush CreateSolidBrush(Graphics gfx, Color color,
            float? opacity = null)
        {
            if (opacity == null) opacity = MapAssistConfiguration.Loaded.RenderingConfiguration.IconOpacity;

            var key = (color, opacity);
            if (!cacheBrushes.ContainsKey(key)) cacheBrushes[key] = gfx.CreateSolidBrush(color.SetOpacity((float)opacity).ToGameOverlayColor());
            return cacheBrushes[key];
        }

        public void Dispose()
        {
            if (gamemapDX != null) gamemapDX.Dispose();

            foreach (var item in cacheBitmaps.Values) item.Dispose();
            foreach (var item in cacheFonts.Values) item.Dispose();
            foreach (var item in cacheBrushes.Values) item.Dispose();
        }
    }
}
