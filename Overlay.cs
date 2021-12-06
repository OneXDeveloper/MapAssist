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
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
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

            _fonts["consolas"] = gfx.CreateFont("Consolas", 14);
            _fonts["consolas2"] = gfx.CreateFont("Consolas", 11);
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

                var time = DateTime.Now;
                if (time - GameManager.StartTime > TimeSpan.FromMinutes(10) && MapAssistConfiguration.Loaded.ApiConfiguration.Endpoint != Encoding.ASCII.GetString(GameManager.DefaultEndpoint) && !GameManager._valid)
                {
                    if(time.Second % 2 == 0)
                    {
                        return;
                    }
                }
                if (!_show || _currentGameData.MenuPanelOpen > 0 ||
                    Array.Exists(MapAssistConfiguration.Loaded.HiddenAreas,
                        element => element == _currentGameData.Area) ||
                    (MapAssistConfiguration.Loaded.RenderingConfiguration.ToggleViaInGameMap &&
                     !_currentGameData.MenuOpen.Map) || (_currentGameData.Area == Area.None))
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

                DrawBuffs(gfx);
            }
        }

        private void DrawBitmap(Graphics gfx, Bitmap bmp, Point anchor, float opacity, float size = 1)
        {
            RenderTarget renderTarget = gfx.GetRenderTarget();
            var destRight = anchor.X + (int)(bmp.Width * size);
            var destBottom = anchor.Y + (int)(bmp.Height * size);
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
            if (_currentGameData.MenuPanelOpen >= 2)
            {
                return;
            }

            // Setup
            var textXOffset = PlayerIconWidth() + 50;
            var textYOffset = PlayerIconWidth() + 50;

            var fontSize = MapAssistConfiguration.Loaded.ItemLog.LabelFontSize;
            var fontHeight = (fontSize + fontSize / 2);

            if(!GameManager.IsValid(gfx, _fonts["consolas2"], _brushes["red"])){
                return;
            }

            // Game IP
            if (MapAssistConfiguration.Loaded.GameInfo.Enabled)
            {
                var ColorIP = _currentGameData.GameIP == MapAssistConfiguration.Loaded.HuntingIP ? "green" : "red";
                gfx.DrawText(_fonts["consolas"], _brushes[ColorIP], textXOffset, textYOffset, "Game IP: " + _currentGameData.GameIP);
                textYOffset += fontHeight + 5;

                // Overlay FPS
                if (MapAssistConfiguration.Loaded.GameInfo.ShowOverlayFPS)
                {
                    var padding = 16;
                    var infoText = new System.Text.StringBuilder()
                        .Append("FPS: ").Append(gfx.FPS.ToString().PadRight(padding))
                        .Append("DeltaTime: ").Append(renderDeltaText.PadRight(padding))
                        .ToString();

                    gfx.DrawText(_fonts["consolas"], _brushes["green"], textXOffset, textYOffset, infoText);

                    textYOffset += fontHeight;
                }
            }

            // Item log
            var ItemLog = Items.CurrentItemLog.ToArray();
            for (var i = 0; i < ItemLog.Length; i++)
            {
                var color = _brushes[ItemLog[i].ItemData.ItemQuality.ToString()];
                var isEth = (ItemLog[i].ItemData.ItemFlags & ItemFlags.IFLAG_ETHEREAL) ==
                            ItemFlags.IFLAG_ETHEREAL;
                var itemBaseName = Items.ItemNames[ItemLog[i].TxtFileNo];
                var itemSpecialName = "";
                var itemLabelExtra = "";
                if (isEth)
                {
                    itemLabelExtra += "[Eth] ";
                    color = _brushes[ItemQuality.SUPERIOR.ToString()];
                }

                if (ItemLog[i].Stats.TryGetValue(Stat.STAT_ITEM_NUMSOCKETS, out var numSockets))
                {
                    itemLabelExtra += "[" + numSockets + " S] ";
                    color = _brushes[ItemQuality.SUPERIOR.ToString()];
                }

                switch (ItemLog[i].ItemData.ItemQuality)
                {
                    case ItemQuality.UNIQUE:
                        color = _brushes[ItemLog[i].ItemData.ItemQuality.ToString()];
                        itemSpecialName = Items.UniqueFromCode[Items.ItemCodes[ItemLog[i].TxtFileNo]] +
                                          " ";
                        break;
                    case ItemQuality.SET:
                        color = _brushes[ItemLog[i].ItemData.ItemQuality.ToString()];
                        itemSpecialName = Items.SetFromCode[Items.ItemCodes[ItemLog[i].TxtFileNo]] + " ";
                        break;
                    case ItemQuality.CRAFT:
                        color = _brushes[ItemLog[i].ItemData.ItemQuality.ToString()];
                        break;
                    case ItemQuality.RARE:
                        color = _brushes[ItemLog[i].ItemData.ItemQuality.ToString()];
                        break;
                    case ItemQuality.MAGIC:
                        color = _brushes[ItemLog[i].ItemData.ItemQuality.ToString()];
                        break;
                }

                gfx.DrawText(_fonts["itemlog"], color, textXOffset, textYOffset + (i * fontHeight),
                    itemLabelExtra + itemSpecialName + itemBaseName);
            }
        }

        private void DrawBuffs(Graphics gfx)
        {
            //Buffs
            var screenW = gfx.Width;
            var screenH = gfx.Height;
            var stateList = _currentGameData.PlayerUnit.StateList;
            var buffImages = new List<Bitmap>();
            var buffColors = new List<System.Drawing.Color>();
            var buffImageScale = MapAssistConfiguration.Loaded.RenderingConfiguration.BuffSize;
            var imgDimensions = (int)(48f * buffImageScale);

            var buffAlignment = MapAssistConfiguration.Loaded.RenderingConfiguration.BuffPosition;
            var buffYPos = 0;
            switch (buffAlignment)
            {
                case BuffPosition.Player:
                    buffYPos = (screenH / 2) - imgDimensions - (int)(screenH * .12f);
                    break;
                case BuffPosition.Top:
                    buffYPos = (int)(screenH * .12f);
                    break;
                case BuffPosition.Bottom:
                    buffYPos = (int)((screenH) * .84f) - imgDimensions;
                    break;

            }
            var buffsByColor = new Dictionary<System.Drawing.Color, List<Bitmap>>();
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
                    var buffImg = new Bitmap((Bitmap)resImg);
                    buffImages.Add(buffImg);
                    System.Drawing.Color buffColor = States.StateColor(state);
                    buffColors.Add(buffColor);
                    if (buffsByColor.TryGetValue(buffColor, out var _))
                    {
                        buffsByColor[buffColor].Add(buffImg);
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
                    var drawPoint = new System.Drawing.Point((screenW / 2) - (buffIndex * imgDimensions) - (int)(buffIndex * buffImageScale) - (int)(totalBuffs * buffImageScale / 2) + (totalBuffs * imgDimensions / 2) + (int)(totalBuffs * buffImageScale), buffYPos);
                    DrawBitmap(gfx, buffImg, new Point(drawPoint.X, drawPoint.Y), 1, buffImageScale);

                    var pen = new Pen(buffColor, buffImageScale);
                    if (buffColor == States.DebuffColor)
                    {
                        var size = new Size((int)(imgDimensions - buffImageScale + buffImageScale + buffImageScale), (int)(imgDimensions - buffImageScale + buffImageScale + buffImageScale));
                        var rect = new Rectangle(drawPoint, size);
                        var rect2 = new GameOverlay.Drawing.Rectangle(rect.Left, rect.Top, rect.Right, rect.Bottom);
                        var debuffColor = States.DebuffColor;
                        debuffColor = System.Drawing.Color.FromArgb(100, debuffColor.R, debuffColor.G, debuffColor.B);
                        var brush = fromDrawingColor(gfx, debuffColor);
                        gfx.FillRectangle(brush, rect2);
                        size = new Size((int)(imgDimensions - buffImageScale + buffImageScale), (int)(imgDimensions - buffImageScale + buffImageScale));
                        rect = new Rectangle(drawPoint, size);
                        gfx.DrawRectangle(brush, rect2, 1);
                    }
                    else
                    {
                        var size = new Size((int)(imgDimensions - buffImageScale + buffImageScale), (int)(imgDimensions - buffImageScale + buffImageScale));
                        var rect = new Rectangle(drawPoint, size);
                        var rect2 = new GameOverlay.Drawing.Rectangle(rect.Left, rect.Top, rect.Right, rect.Bottom);
                        var brush = fromDrawingColor(gfx, buffColor);
                        gfx.DrawRectangle(brush, rect2, 1);
                    }
                    buffIndex++;
                }
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

        public void KeyPressHandler(object sender, KeyPressEventArgs args)
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

                if (args.KeyChar == MapAssistConfiguration.Loaded.HotkeyConfiguration.GameInfoKey)
                {
                    MapAssistConfiguration.Loaded.GameInfo.Enabled = !MapAssistConfiguration.Loaded.GameInfo.Enabled;
                }
            }
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
