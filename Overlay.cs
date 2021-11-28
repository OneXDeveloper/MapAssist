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

using GameOverlay.Windows;
using Gma.System.MouseKeyHook;
using MapAssist.Helpers;
using MapAssist.Settings;
using MapAssist.Types;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Numerics;
using Color = GameOverlay.Drawing.Color;
using Font = GameOverlay.Drawing.Font;
using Graphics = GameOverlay.Drawing.Graphics;
using Image = GameOverlay.Drawing.Image;
using Point = GameOverlay.Drawing.Point;
using SolidBrush = GameOverlay.Drawing.SolidBrush;

namespace MapAssist
{
    public class Overlay : IDisposable
    {
        private readonly GraphicsWindow _window;

        private System.Windows.Forms.NotifyIcon _trayIcon;

        private GameData _currentGameData;
        private GameDataCache _gameDataCache;
        private Compositor _compositor;
        private AreaData _areaData;
        private bool _show = true;

        private readonly Dictionary<string, SolidBrush> _brushes;
        private readonly Dictionary<string, Font> _fonts;

        public Overlay(IKeyboardMouseEvents keyboardMouseEvents)
        {
            _gameDataCache = new GameDataCache();
            
            var gfx = new Graphics() {MeasureFPS = true};

            _brushes = new Dictionary<string, SolidBrush>();
            _fonts = new Dictionary<string, Font>();

            int desiredFps = Math.Min(1000 / MapAssistConfiguration.Loaded.UpdateTime, 30);
            _window = new GraphicsWindow(0, 0, 1, 1, gfx) {FPS = desiredFps, IsVisible = true};

            _window.DrawGraphics += _window_DrawGraphics;
            _window.SetupGraphics += _window_SetupGraphics;
            _window.DestroyGraphics += _window_DestroyGraphics;

            keyboardMouseEvents.KeyPress += (_, args) =>
            {
                if (InGame())
                {
                    if (args.KeyChar == MapAssistConfiguration.Loaded.HotkeyConfiguration.ToggleKey)
                    {
                        _show = !_show;
                    }

                    if (args.KeyChar == MapAssistConfiguration.Loaded.HotkeyConfiguration.ZoomInKey)
                    {
                        if (MapAssistConfiguration.Loaded.RenderingConfiguration.ZoomLevel > 0.25f)
                        {
                            MapAssistConfiguration.Loaded.RenderingConfiguration.ZoomLevel -= 0.25f;
                            MapAssistConfiguration.Loaded.RenderingConfiguration.Size =
                                (int)(MapAssistConfiguration.Loaded.RenderingConfiguration.Size * 1.15f);
                        }
                    }

                    if (args.KeyChar == MapAssistConfiguration.Loaded.HotkeyConfiguration.ZoomOutKey)
                    {
                        if (MapAssistConfiguration.Loaded.RenderingConfiguration.ZoomLevel < 4f)
                        {
                            MapAssistConfiguration.Loaded.RenderingConfiguration.ZoomLevel += 0.25f;
                            MapAssistConfiguration.Loaded.RenderingConfiguration.Size =
                                (int)(MapAssistConfiguration.Loaded.RenderingConfiguration.Size * .85f);
                        }
                    }
                }
            };

            _trayIcon = new System.Windows.Forms.NotifyIcon()
            {
                Icon = Properties.Resources.Icon1,
                ContextMenu =
                    new System.Windows.Forms.ContextMenu(
                        new System.Windows.Forms.MenuItem[] {new System.Windows.Forms.MenuItem("Exit", Exit)}),
                Text = "MapAssist",
                Visible = true
            };
        }

        void Exit(object sender, EventArgs e)
        {
            _trayIcon.Visible = false;

            Dispose();
        }

        private void _window_SetupGraphics(object sender, SetupGraphicsEventArgs e)
        {
            var gfx = e.Graphics;

            _brushes["green"] = gfx.CreateSolidBrush(0, 255, 0);
            _brushes["red"] = gfx.CreateSolidBrush(255, 0, 0);
            _brushes[ItemQuality.INFERIOR.ToString()] = fromDrawingColor(gfx, Items.ItemColors[ItemQuality.INFERIOR]);
            _brushes[ItemQuality.NORMAL.ToString()] = fromDrawingColor(gfx, Items.ItemColors[ItemQuality.NORMAL]);
            _brushes[ItemQuality.SUPERIOR.ToString()] = fromDrawingColor(gfx, Items.ItemColors[ItemQuality.SUPERIOR]);
            _brushes[ItemQuality.MAGIC.ToString()] = fromDrawingColor(gfx, Items.ItemColors[ItemQuality.MAGIC]);
            _brushes[ItemQuality.SET.ToString()] = fromDrawingColor(gfx, Items.ItemColors[ItemQuality.SET]);
            _brushes[ItemQuality.RARE.ToString()] = fromDrawingColor(gfx, Items.ItemColors[ItemQuality.RARE]);
            _brushes[ItemQuality.UNIQUE.ToString()] = fromDrawingColor(gfx, Items.ItemColors[ItemQuality.UNIQUE]);
            _brushes[ItemQuality.CRAFT.ToString()] = fromDrawingColor(gfx, Items.ItemColors[ItemQuality.CRAFT]);

            if (e.RecreateResources) return;

            _fonts["consolas"] = gfx.CreateFont("Consolas", 14);
            _fonts["itemlog"] = gfx.CreateFont(MapAssistConfiguration.Loaded.ItemLog.LabelFont,
                MapAssistConfiguration.Loaded.ItemLog.LabelFontSize);
        }

        private SolidBrush fromDrawingColor(Graphics g, System.Drawing.Color c) =>
            g.CreateSolidBrush(fromDrawingColor(c));

        private Color fromDrawingColor(System.Drawing.Color c) =>
            new Color(c.R, c.G, c.B, c.A);

        private void _window_DrawGraphics(object sender, DrawGraphicsEventArgs e)
        {
            var gfx = e.Graphics;

            UpdateGameData();

            gfx.ClearScene();

            if (_compositor == null || !InGame())
            {
                return;
            }

            if (_compositor != null && _currentGameData != null)
            {
                UpdateLocation();
                DrawGameInfo(gfx, e.DeltaTime.ToString());

                if (!_show ||
                    Array.Exists(MapAssistConfiguration.Loaded.HiddenAreas,
                        element => element == _currentGameData.Area) ||
                    (MapAssistConfiguration.Loaded.RenderingConfiguration.ToggleViaInGameMap &&
                     !_currentGameData.MapShown) || (_currentGameData.Area == Area.None))
                {
                    return;
                }

                Bitmap gamemap = _compositor.Compose(_currentGameData,
                    !MapAssistConfiguration.Loaded.RenderingConfiguration.OverlayMode);

                Point anchor;
                if (MapAssistConfiguration.Loaded.RenderingConfiguration.OverlayMode)
                {
                    DrawMapOverlayMode(gfx, gamemap, out gamemap, out anchor);
                }
                else
                {
                    switch (MapAssistConfiguration.Loaded.RenderingConfiguration.Position)
                    {
                        case MapPosition.Center:
                            anchor = new Point(_window.Width / 2 - gamemap.Width / 2,
                                _window.Height / 2 - gamemap.Height / 2);
                            break;
                        case MapPosition.TopRight:
                            anchor = new Point(_window.Width - gamemap.Width, 0);
                            break;
                        default:
                            anchor = new Point(0, 0);
                            break;
                    }
                }

                using (var image = new Image(gfx, ImageToByte(gamemap)))
                {
                    gfx.DrawImage(image, anchor, (float)MapAssistConfiguration.Loaded.RenderingConfiguration.Opacity);
                }
            }
        }

        private void DrawMapOverlayMode(Graphics gfx, Bitmap inMap, out Bitmap outMap, out Point anchor)
        {
            float w = 0;
            float h = 0;
            var scale = 0.0F;
            var center = new Vector2();

            if (ConfigurationManager.AppSettings["ZoomLevelDefault"] == null)
            {
                MapAssistConfiguration.Loaded.RenderingConfiguration.ZoomLevel = 1;
            }

            switch (MapAssistConfiguration.Loaded.RenderingConfiguration.Position)
            {
                case MapPosition.Center:
                    w = _window.Width;
                    h = _window.Height;
                    scale = (1024.0F / h * w * 3f / 4f / 2.3F) *
                            MapAssistConfiguration.Loaded.RenderingConfiguration.ZoomLevel;
                    center = new Vector2(w / 2, h / 2 + 20);
                    break;
                case MapPosition.TopLeft:
                    w = 640;
                    h = 360;
                    scale = (1024.0F / h * w * 3f / 4f / 3.35F + 48) *
                            MapAssistConfiguration.Loaded.RenderingConfiguration.ZoomLevel;
                    center = new Vector2(w / 2, h / 2);
                    break;
                case MapPosition.TopRight:
                    w = 640;
                    h = 360;
                    scale = (1024.0F / h * w * 3f / 4f / 3.35F + 40) *
                            MapAssistConfiguration.Loaded.RenderingConfiguration.ZoomLevel;
                    center = new Vector2(w / 2, h / 2);
                    break;
            }

            var cropOffset = new System.Drawing.Point();

            if (_compositor != null)
            {
                cropOffset = _compositor.CropOffset;
            }

            System.Drawing.Point playerPosInArea = _currentGameData.PlayerPosition.OffsetFrom(_areaData.Origin)
                .OffsetFrom(cropOffset);

            var playerPos = new Vector2(playerPosInArea.X, playerPosInArea.Y);

            Vector2 Transform(Vector2 p) =>
                center +
                DeltaInWorldToMinimapDelta(
                    p - playerPos,
                    (float)Math.Sqrt(w * w + h * h),
                    scale,
                    0);

            var p1 = Transform(new Vector2(0, 0));
            var p2 = Transform(new Vector2(inMap.Width, 0));
            var p4 = Transform(new Vector2(0, inMap.Height));

            PointF[] destinationPoints = {new PointF(p1.X, p1.Y), new PointF(p2.X, p2.Y), new PointF(p4.X, p4.Y)};

            var b = new Bitmap((int)w, (int)h);

            using (var g = System.Drawing.Graphics.FromImage(b))
            {
                g.DrawImage(inMap, destinationPoints);
            }

            outMap = b;

            if (MapAssistConfiguration.Loaded.RenderingConfiguration.Position == MapPosition.TopRight)
            {
                anchor = new Point(_window.Width - outMap.Width, 0);
            }
            else
            {
                anchor = new Point(0, 0);
            }
        }

        private void DrawGameInfo(Graphics gfx, string renderDeltaText)
        {
            // Setup
            var screenW = _window.Width;
            var blackBarWidth = screenW > 2880 ? (screenW - 2880) / 4 : 0;
            var textXOffset = blackBarWidth + (int)(screenW * .06f);

            var fontSize = MapAssistConfiguration.Loaded.ItemLog.LabelFontSize;
            var fontHeight = (fontSize + fontSize / 2);
            var fontOffset = fontHeight;

            // Game IP
            gfx.DrawText(_fonts["consolas"], _brushes["red"], textXOffset, fontOffset,
                "Game IP: " + _currentGameData.GameIP);
            fontOffset += fontHeight + 5;

            // Overlay FPS
            if (MapAssistConfiguration.Loaded.GameInfo.ShowOverlayFPS)
            {
                var padding = 16;
                var infoText = new System.Text.StringBuilder()
                    .Append("FPS: ").Append(gfx.FPS.ToString().PadRight(padding))
                    .Append("DeltaTime: ").Append(renderDeltaText.PadRight(padding))
                    .ToString();

                gfx.DrawText(_fonts["consolas"], _brushes["green"], textXOffset, fontOffset, infoText);

                fontOffset += fontHeight;
            }

            // Item log
            for (var i = 0; i < Items.CurrentItemLog.Count; i++)
            {
                var color = _brushes[Items.CurrentItemLog[i].ItemData.ItemQuality.ToString()];
                var isEth = (Items.CurrentItemLog[i].ItemData.ItemFlags & ItemFlags.IFLAG_ETHEREAL) ==
                            ItemFlags.IFLAG_ETHEREAL;
                var itemBaseName = Items.ItemNames[Items.CurrentItemLog[i].TxtFileNo];
                var itemSpecialName = "";
                var itemLabelExtra = "";
                if (isEth)
                {
                    itemLabelExtra += "[Eth] ";
                    color = _brushes[ItemQuality.SUPERIOR.ToString()];
                }

                if (Items.CurrentItemLog[i].Stats.TryGetValue(Stat.STAT_ITEM_NUMSOCKETS, out var numSockets))
                {
                    itemLabelExtra += "[" + numSockets + " S] ";
                    color = _brushes[ItemQuality.SUPERIOR.ToString()];
                }

                switch (Items.CurrentItemLog[i].ItemData.ItemQuality)
                {
                    case ItemQuality.UNIQUE:
                        color = _brushes[Items.CurrentItemLog[i].ItemData.ItemQuality.ToString()];
                        itemSpecialName = Items.UniqueFromCode[Items.ItemCodes[Items.CurrentItemLog[i].TxtFileNo]] +
                                          " ";
                        break;
                    case ItemQuality.SET:
                        color = _brushes[Items.CurrentItemLog[i].ItemData.ItemQuality.ToString()];
                        itemSpecialName = Items.SetFromCode[Items.ItemCodes[Items.CurrentItemLog[i].TxtFileNo]] + " ";
                        break;
                    case ItemQuality.CRAFT:
                        color = _brushes[Items.CurrentItemLog[i].ItemData.ItemQuality.ToString()];
                        break;
                    case ItemQuality.RARE:
                        color = _brushes[Items.CurrentItemLog[i].ItemData.ItemQuality.ToString()];
                        break;
                    case ItemQuality.MAGIC:
                        color = _brushes[Items.CurrentItemLog[i].ItemData.ItemQuality.ToString()];
                        break;
                }

                gfx.DrawText(_fonts["itemlog"], color, textXOffset, fontOffset + (i * fontHeight),
                    itemLabelExtra + itemSpecialName + itemBaseName);
            }
        }

        private static byte[] ImageToByte(System.Drawing.Image img)
        {
            using (var stream = new MemoryStream())
            {
                img.Save(stream, ImageFormat.Png);
                return stream.ToArray();
            }
        }

        public void Run()
        {
            _window.Create();
            _window.Join();
        }

        private void UpdateGameData()
        {
            var gameData = _gameDataCache.Get();
            _currentGameData = gameData.Item1;
            _compositor = gameData.Item2;
            _areaData = gameData.Item3;
        }

        private bool InGame()
        {
            return _currentGameData != null && _currentGameData.MainWindowHandle != IntPtr.Zero &&
                   WindowsExternal.GetForegroundWindow() == _currentGameData.MainWindowHandle;
        }

        public Vector2 DeltaInWorldToMinimapDelta(Vector2 delta, double diag, float scale, float deltaZ = 0)
        {
            var CAMERA_ANGLE = -26F * 3.14159274F / 180;

            var cos = (float)(diag * Math.Cos(CAMERA_ANGLE) / scale);
            var sin = (float)(diag * Math.Sin(CAMERA_ANGLE) /
                              scale);

            return new Vector2((delta.X - delta.Y) * cos, deltaZ - (delta.X + delta.Y) * sin);
        }

        /// <summary>
        /// Resize overlay to currently active screen
        /// </summary>
        private void UpdateLocation()
        {
            _window.FitTo(_currentGameData.MainWindowHandle, true);
            _window.PlaceAbove(_currentGameData.MainWindowHandle);
        }

        ~Overlay()
        {
            Dispose(false);
        }

        private void _window_DestroyGraphics(object sender, DestroyGraphicsEventArgs e)
        {
            _gameDataCache?.Dispose();

            foreach (var pair in _brushes) pair.Value.Dispose();
            foreach (var pair in _fonts) pair.Value.Dispose();

            _compositor = null;
        }

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                _window.Dispose();

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
