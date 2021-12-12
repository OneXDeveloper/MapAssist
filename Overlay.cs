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
using Gma.System.MouseKeyHook;
using MapAssist.Helpers;
using MapAssist.Settings;
using MapAssist.Types;
using System;
using System.Windows.Forms;
using Graphics = GameOverlay.Drawing.Graphics;

namespace MapAssist
{
    public class Overlay : IDisposable
    {
        private static readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();
      
        private readonly GraphicsWindow _window;
        private GameDataReader _gameDataReader;
        private GameData _gameData;
        private Compositor _compositor;
        private bool _show = true;

        public Overlay()
        {
            _gameDataReader = new GameDataReader();

            GameOverlay.TimerService.EnableHighPrecisionTimers();

            var gfx = new Graphics() { MeasureFPS = true };
            gfx.PerPrimitiveAntiAliasing = true;
            gfx.TextAntiAliasing = true;

            _window = new GraphicsWindow(0, 0, 1, 1, gfx) { FPS = 60, IsVisible = true };

            _window.DrawGraphics += _window_DrawGraphics;
            _window.DestroyGraphics += _window_DestroyGraphics;
        }

        private void _window_DrawGraphics(object sender, DrawGraphicsEventArgs e)
        {
            var gfx = e.Graphics;

            try
            {
                (_compositor, _gameData) = _gameDataReader.Get();

                gfx.ClearScene();

                if (_compositor != null && InGame() && _compositor != null && _gameData != null)
                {
                    UpdateLocation();

                    var errorLoadingAreaData = _compositor._areaData == null;

                    var overlayHidden = !_show ||
                        errorLoadingAreaData ||
                        (MapAssistConfiguration.Loaded.RenderingConfiguration.ToggleViaInGameMap && !_gameData.MenuOpen.Map) ||
                        (MapAssistConfiguration.Loaded.RenderingConfiguration.ToggleViaInGamePanels && _gameData.MenuPanelOpen > 0) ||
                        (MapAssistConfiguration.Loaded.RenderingConfiguration.ToggleViaInGamePanels && _gameData.MenuOpen.EscMenu) ||
                        Array.Exists(MapAssistConfiguration.Loaded.HiddenAreas, area => area == _gameData.Area) ||
                        _gameData.Area == Area.None ||
                        gfx.Width == 1 ||
                        gfx.Height == 1;

                    var size = MapAssistConfiguration.Loaded.RenderingConfiguration.Size;

                    var drawBounds = new Rectangle(0, 0, gfx.Width, gfx.Height * 0.8f);
                    switch (MapAssistConfiguration.Loaded.RenderingConfiguration.Position)
                    {
                        case MapPosition.TopLeft:
                            drawBounds = new Rectangle(PlayerIconWidth() + 40, PlayerIconWidth() + 100, 0, PlayerIconWidth() + 100 + size);
                            break;
                        case MapPosition.TopRight:
                            drawBounds = new Rectangle(0, 100, gfx.Width, 100 + size);
                            break;
                    }

                    _compositor.Init(gfx, _gameData, drawBounds);

                    if (!overlayHidden)
                    {
                        _compositor.DrawGamemap(gfx);
                        _compositor.DrawOverlay(gfx);
                        _compositor.DrawBuffs(gfx);
                    }

                    _compositor.DrawGameInfo(gfx, new Point(PlayerIconWidth() + 50, PlayerIconWidth() + 50), e, errorLoadingAreaData);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
        }

        public void Run()
        {
            _window.Create();
            _window.Join();
        }

        private bool InGame()
        {
            return _gameData != null && _gameData.MainWindowHandle != IntPtr.Zero;
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

        /// <summary>
        /// Resize overlay to currently active screen
        /// </summary>
        private void UpdateLocation()
        {
            var rect = WindowRect();
            var ultraWideMargin = UltraWideMargin();

            _window.Resize((int)(rect.Left + ultraWideMargin), (int)rect.Top, (int)(rect.Right - rect.Left - ultraWideMargin * 2), (int)(rect.Bottom - rect.Top));
            _window.PlaceAbove(_gameData.MainWindowHandle);
        }

        private Rectangle WindowRect()
        {
            WindowBounds rect;
            WindowHelper.GetWindowClientBounds(_gameData.MainWindowHandle, out rect);

            return new Rectangle(rect.Left, rect.Top, rect.Right, rect.Bottom);
        }

        private float UltraWideMargin()
        {
            var rect = WindowRect();
            return (float)Math.Max(Math.Round(((rect.Width + 2) - (rect.Height + 4) * 2.1f) / 2f), 0);
        }

        private float PlayerIconWidth()
        {
            var rect = WindowRect();
            return rect.Height / 20f;
        }

        ~Overlay()
        {
            Dispose(false);
        }

        private void _window_DestroyGraphics(object sender, DestroyGraphicsEventArgs e)
        {
            if (_compositor != null) _compositor.Dispose();
            _compositor = null;
        }

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (_compositor != null) _compositor.Dispose();

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
