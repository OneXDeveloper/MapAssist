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
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using Bitmap = System.Drawing.Bitmap;
using Color = GameOverlay.Drawing.Color;
using Font = GameOverlay.Drawing.Font;
using Graphics = GameOverlay.Drawing.Graphics;
using Point = GameOverlay.Drawing.Point;
using SolidBrush = GameOverlay.Drawing.SolidBrush;

namespace MapAssist
{
    public class Overlay : IDisposable
    {
        private readonly GraphicsWindow _window;

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

            _window = new GraphicsWindow(0, 0, 1, 1, gfx) {FPS = 60, IsVisible = true};

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

                var smallCornerSize = new Size(640, 360);

                var (gamemap, playerCenter) = _compositor.Compose(_currentGameData,
                    MapAssistConfiguration.Loaded.RenderingConfiguration.ZoomLevel);

                Point anchor;
                switch (MapAssistConfiguration.Loaded.RenderingConfiguration.Position)
                {
                    case MapPosition.Center:
                        if (MapAssistConfiguration.Loaded.RenderingConfiguration.OverlayMode)
                        {
                            anchor = new Point(_window.Width / 2 - playerCenter.X,
                                _window.Height / 2 - playerCenter.Y);
                        }
                        else
                        {
                            anchor = new Point(_window.Width / 2 - gamemap.Width / 2,
                                _window.Height / 2 - gamemap.Height / 2);
                        }
                        break;
                    case MapPosition.TopRight:
                        if (MapAssistConfiguration.Loaded.RenderingConfiguration.OverlayMode)
                        {
                            anchor = new Point(_window.Width - smallCornerSize.Width, 100);
                        }
                        else
                        {
                            anchor = new Point(_window.Width - gamemap.Width, 100);
                        }                        
                        break;
                    default:
                        anchor = new Point(PlayerIconWidth() + 40, 100);
                        break;
                }


                if (MapAssistConfiguration.Loaded.RenderingConfiguration.OverlayMode && MapAssistConfiguration.Loaded.RenderingConfiguration.Position != MapPosition.Center)
                {
                    var newBitmap = new Bitmap(smallCornerSize.Width, smallCornerSize.Height);
                    using (var g = System.Drawing.Graphics.FromImage(newBitmap))
                    {
                        g.DrawImage(gamemap, 0, 0,
                            new Rectangle((int)(playerCenter.X - smallCornerSize.Width / 2), (int)(playerCenter.Y - smallCornerSize.Height / 2), smallCornerSize.Width, smallCornerSize.Height),
                            GraphicsUnit.Pixel);
                    }

                    DrawBitmap(gfx, newBitmap, anchor, (float)MapAssistConfiguration.Loaded.RenderingConfiguration.Opacity);
                }
                else
                {
                    DrawBitmap(gfx, gamemap, anchor, (float)MapAssistConfiguration.Loaded.RenderingConfiguration.Opacity);
                }
            }
        }

        private void DrawBitmap(Graphics gfx, Bitmap bmp, Point anchor, float opacity)
        {
            RenderTarget renderTarget = gfx.GetRenderTarget();
            var destRight = anchor.X + bmp.Width;
            var destBottom = anchor.Y + bmp.Height;
            BitmapData bmpData = null;
            SharpDX.Direct2D1.Bitmap newBmp = null;
            try
            {
                bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly,
                    bmp.PixelFormat);
                var numBytes = bmpData.Stride * bmp.Height;
                var byteData = new byte[numBytes];
                IntPtr ptr = bmpData.Scan0;
                Marshal.Copy(ptr, byteData, 0, numBytes);

                newBmp = new SharpDX.Direct2D1.Bitmap(renderTarget, new Size2(bmp.Width, bmp.Height), new BitmapProperties(renderTarget.PixelFormat));
                newBmp.CopyFromMemory(byteData, bmpData.Stride);
                
                renderTarget.DrawBitmap(
                    newBmp,
                    new RawRectangleF(anchor.X, anchor.Y, destRight, destBottom),
                    opacity,
                    BitmapInterpolationMode.Linear);
                
            }
            finally
            {
                newBmp?.Dispose();
                if (bmpData != null) bmp.UnlockBits(bmpData);
            }
        }

        private void DrawGameInfo(Graphics gfx, string renderDeltaText)
        {
            // Setup
            var screenW = _window.Width;
            var screenH = _window.Height;

            var fontSize = MapAssistConfiguration.Loaded.ItemLog.LabelFontSize;
            var fontHeight = (fontSize + fontSize / 2);

            var textXOffset = PlayerIconWidth() + 40;
            var textYOffset = 20;

            switch (MapAssistConfiguration.Loaded.GameInfo.Position)
            {
                case GameInfoPosition.TopLeft:
                    DrawGameIP(textXOffset, textYOffset, false, gfx);
                    textYOffset += fontHeight + 5;
                    if (DrawOverlayFPS(renderDeltaText, textXOffset, textYOffset, false, gfx))
                    {
                        textYOffset += fontHeight + 5;
                    }
                    for (var i = 0; i < Items.CurrentItemLog.Count; i++)
                    {
                        DrawItemLogUnit(Items.CurrentItemLog[i], textXOffset, textYOffset, false, gfx);
                        textYOffset += fontHeight + 5;
                    }

                    break;
                case GameInfoPosition.BottomRight:
                    textXOffset = screenW - 20;
                    textYOffset = screenH - 20;
                    DrawGameIP(textXOffset, textYOffset, true, gfx);
                    textYOffset -= fontHeight + 5;
                    if (DrawOverlayFPS(renderDeltaText, textXOffset, textYOffset, true, gfx))
                    {
                        textYOffset -= fontHeight + 5;
                    }
                    for (var i = Items.CurrentItemLog.Count - 1; i >= 0; i--)
                    {
                        DrawItemLogUnit(Items.CurrentItemLog[i], textXOffset, textYOffset, true, gfx);
                        textYOffset -= fontHeight + 5;
                    }

                    break;
            }
        }

        private void DrawGameIP(int xOffset, int yOffset, bool alignRight, Graphics gfx)
        {
            var font = _fonts["consolas"];
            var brush = _brushes["red"];

            var ipText = "Game IP: " + _currentGameData.GameIP;
            var dimensions = gfx.MeasureString(font, font.FontSize, ipText);

            if (!alignRight)
            {
                gfx.DrawText(font, brush, xOffset, yOffset, ipText);
            }
            else
            {
                gfx.DrawText(font, brush, xOffset - dimensions.X, yOffset - dimensions.Y, ipText);
            }
        }

        private bool DrawOverlayFPS(string renderDeltaText, int xOffset, int yOffset, bool alignRight, Graphics gfx)
        {
            // Overlay FPS
            if (MapAssistConfiguration.Loaded.GameInfo.ShowOverlayFPS)
            {
                var font = _fonts["consolas"];
                var brush = _brushes["green"];

                var fpsText = "FPS: " + gfx.FPS.ToString();
                var renderText = "DeltaTime: " + renderDeltaText.ToString();
                var spaceBetween = 20;

                var fpsDimensions = gfx.MeasureString(font, font.FontSize, fpsText);
                var renderDimensions = gfx.MeasureString(font, font.FontSize, renderText);

                if (!alignRight)
                {
                    gfx.DrawText(font, brush, xOffset, yOffset, fpsText);
                    gfx.DrawText(font, brush, xOffset + fpsDimensions.X + spaceBetween, yOffset, renderText);
                }
                else
                {
                    gfx.DrawText(font, brush, xOffset - renderDimensions.X - fpsDimensions.X - spaceBetween, yOffset - fpsDimensions.Y, fpsText);
                    gfx.DrawText(font, brush, xOffset - renderDimensions.X, yOffset - renderDimensions.Y, renderText);
                }

                return true;
            }

            return false;
        }

        private void DrawItemLogUnit(UnitAny unit, int xOffset, int yOffset, bool alignRight, Graphics gfx)
        {
            var font = _fonts["itemlog"];
            // Default color
            var color = _brushes[unit.ItemData.ItemQuality.ToString()];
            var labels = new List<string>();
            
            var isEth = (unit.ItemData.ItemFlags & ItemFlags.IFLAG_ETHEREAL) == ItemFlags.IFLAG_ETHEREAL;
            if (isEth)
            {
                labels.Add("[Eth]");
                color = _brushes[ItemQuality.SUPERIOR.ToString()];
            }

            if (unit.Stats.TryGetValue(Stat.STAT_ITEM_NUMSOCKETS, out var numSockets))
            {
                labels.Add($"[${numSockets}S]");
                color = _brushes[ItemQuality.SUPERIOR.ToString()];
            }

            var itemCode = Items.ItemCodes[unit.TxtFileNo];
            switch (unit.ItemData.ItemQuality)
            {
                case ItemQuality.UNIQUE:
                    if (Items.UniqueFromCode.TryGetValue(itemCode, out var uniqName))
                    {
                        labels.Add(uniqName);
                    }
                    break;
                case ItemQuality.SET:
                    if (Items.SetFromCode.TryGetValue(itemCode, out var setName))
                    {
                        labels.Add(setName);
                    }
                    break;
                case ItemQuality.NORMAL:
                    // Override runes to be a brighter color
                    if (Items.ItemNames[unit.TxtFileNo].EndsWith(" Rune"))
                    {
                        color = _brushes[ItemQuality.CRAFT.ToString()];
                    }

                    break;
            }

            labels.Add(Items.ItemNames[unit.TxtFileNo]);
            
            var itemText = string.Join(" ", labels);
            var dimensions = gfx.MeasureString(font, font.FontSize, itemText);

            if (!alignRight)
            {
                gfx.DrawText(font, color, xOffset, yOffset, itemText);
            }
            else
            {
                gfx.DrawText(font, color, xOffset - dimensions.X, yOffset - dimensions.Y, itemText);
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
            var rect = WindowRect();
            var ultraWideMargin = UltraWideMargin();

            _window.Resize(rect.Left + ultraWideMargin, rect.Top, rect.Right - rect.Left - ultraWideMargin * 2, rect.Bottom - rect.Top);
            _window.PlaceAbove(_currentGameData.MainWindowHandle);
        }

        private WindowBounds WindowRect()
        {
            WindowBounds rect;
            WindowHelper.GetWindowClientBounds(_currentGameData.MainWindowHandle, out rect);

            return rect;
        }

        private Size WindowSize()
        {
            var rect = WindowRect();
            return new Size(rect.Right - rect.Left, rect.Bottom - rect.Top);
        }

        private int UltraWideMargin()
        {
            var size = WindowSize();
            return (int)Math.Max(Math.Round((size.Width - size.Height * 2.1) / 2), 0);
        }

        private int PlayerIconWidth()
        {
            var size = WindowSize();
            return (int)Math.Round(size.Height / 20f);
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
