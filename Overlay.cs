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
using System.Drawing;
using System.Windows.Forms;
using Graphics = GameOverlay.Drawing.Graphics;

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

        public Overlay(IKeyboardMouseEvents keyboardMouseEvents)
        {
            _gameDataCache = new GameDataCache();

            GameOverlay.TimerService.EnableHighPrecisionTimers();

            var gfx = new Graphics() { MeasureFPS = true };
            gfx.PerPrimitiveAntiAliasing = true;
            gfx.TextAntiAliasing = true;

            _window = new GraphicsWindow(0, 0, 1, 1, gfx) { FPS = 1000 / MapAssistConfiguration.Loaded.UpdateTime, IsVisible = true };

            _window.SetupGraphics += _window_SetupGraphics;
            _window.DrawGraphics += _window_DrawGraphics;
            _window.DestroyGraphics += _window_DestroyGraphics;
        }

        private void _window_SetupGraphics(object sender, SetupGraphicsEventArgs e)
        {

        }

        private void _window_DrawGraphics(object sender, DrawGraphicsEventArgs e)
        {
            var gfx = e.Graphics;

            UpdateGameData();

            gfx.ClearScene();

            if (_compositor != null && InGame() && _currentGameData != null)
            {
                UpdateLocation();

                var errorLoadingAreaData = _areaData == null;

                var overlayHidden = !_show ||
                    errorLoadingAreaData ||
                    (MapAssistConfiguration.Loaded.RenderingConfiguration.ToggleViaInGameMap && !_currentGameData.MenuOpen.Map) ||
                    (MapAssistConfiguration.Loaded.RenderingConfiguration.ToggleViaInGamePanels && _currentGameData.MenuPanelOpen > 0) ||
                    (MapAssistConfiguration.Loaded.RenderingConfiguration.ToggleViaInGamePanels && _currentGameData.MenuOpen.EscMenu) ||
                    Array.Exists(MapAssistConfiguration.Loaded.HiddenAreas, area => area == _currentGameData.Area) ||
                    (_currentGameData.Area == Area.None);

                if (!overlayHidden)
                {
                    _compositor.CalcResizeRatios();

                    var playerCenter = _compositor.PlayerPosition(_currentGameData);
                    var size = _compositor.GetMapSize();

                    PointF anchor;
                    switch (MapAssistConfiguration.Loaded.RenderingConfiguration.Position)
                    {
                        case MapPosition.Center:
                            if (MapAssistConfiguration.Loaded.RenderingConfiguration.OverlayMode)
                            {
                                anchor = _window.Center().Subtract(playerCenter).Add(2, -8); // Brute forced to perfectly line up with the in game map
                            }
                            else
                            {
                                anchor = _window.Center().Subtract(size.Center());
                            }
                            break;
                        case MapPosition.TopRight:
                            anchor = new PointF(_window.Width - size.Width, 100);
                            break;
                        default:
                            anchor = new PointF(PlayerIconWidth() + 40, 100);
                            break;
                    }

                    _compositor.DrawGamemap(gfx, _currentGameData, anchor);
                    _compositor.DrawOverlay(gfx, _currentGameData, anchor);
                    _compositor.DrawBuffs(gfx, _currentGameData);
                }

                _compositor.DrawGameInfo(gfx, _currentGameData, new PointF(PlayerIconWidth() + 50f, PlayerIconWidth() + 50f), e, errorLoadingAreaData);
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
            return _currentGameData != null && _currentGameData.MainWindowHandle != IntPtr.Zero;
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
            return (int)Math.Max(Math.Round(((size.Width + 2) - (size.Height + 4) * 2.1f) / 2f), 0);
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
