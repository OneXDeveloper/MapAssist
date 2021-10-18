/**
 *   Copyright (C) 2021 okaygo
 *
 *   https://github.com/misterokaygo/D2RAssist/
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
using System.Windows.Forms;
using D2RAssist.Types;
using D2RAssist.Helpers;
using Gma.System.MouseKeyHook;

namespace D2RAssist
{
    public partial class Overlay : Form
    {
        // Move to windows external
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint TOPMOST_FLAGS = SWP_NOMOVE | SWP_NOSIZE;
        private readonly Timer _timer = new Timer();
        private GameData _currentGameData;
        private Compositor _compositor;
        private MapApi _mapApi;
        private bool _show = true;
        private Screen _screen;

        public Overlay(IKeyboardMouseEvents keyboardMouseEvents)
        {
            Settings.Map.InitMapColors();
            InitializeComponent();
            keyboardMouseEvents.KeyPress += (_, args) =>
            {
                if (InGame() && args.KeyChar == Settings.Map.ToggleKey)
                {
                    _show = !_show;
                }
            };
        }

        private void Overlay_Load(object sender, EventArgs e)
        {
            Rectangle screen = Screen.PrimaryScreen.WorkingArea;
            int width = Width >= screen.Width ? screen.Width : (screen.Width + Width) / 2;
            int height = Height >= screen.Height ? screen.Height : (screen.Height + Height) / 2;
            Location = new Point((screen.Width - width) / 2, (screen.Height - height) / 2);
            Size = new Size(width, height);
            Opacity = Settings.Map.Opacity;

            _timer.Interval = Settings.Map.UpdateTime;
            _timer.Tick += MapUpdateTimer_Tick;
            _timer.Start();

            if (Settings.Map.AlwaysOnTop)
            {
                var initialStyle = (uint)WindowsExternal.GetWindowLongPtr(Handle, -20);
                WindowsExternal.SetWindowLong(Handle, -20, initialStyle | 0x80000 | 0x20);
                WindowsExternal.SetWindowPos(Handle, HWND_TOPMOST, 0, 0, 0, 0, TOPMOST_FLAGS);
            }

            mapOverlay.Location = new Point(0, 0);
            mapOverlay.Width = Width;
            mapOverlay.Height = Height;
            mapOverlay.BackColor = Color.Transparent;
        }

        private void Overlay_FormClosing(object sender, EventArgs e)
        {
            MapApi.Client.Dispose();
            _mapApi?.Dispose();
        }

        private void MapUpdateTimer_Tick(object sender, EventArgs e)
        {
            _timer.Stop();

            GameData gameData = GameMemory.GetGameData();
            if (gameData != null)
            {
                if (gameData.HasGameChanged(_currentGameData))
                {
                    Console.WriteLine($"Game changed: {gameData}");
                    _mapApi?.Dispose();
                    _mapApi = new MapApi(MapApi.Client, Settings.Api.Endpoint, gameData.Difficulty, gameData.MapSeed);
                }

                if (gameData.HasMapChanged(_currentGameData))
                {
                    Console.WriteLine($"Area changed: {gameData.Area}");
                    if (gameData.Area != Area.None)
                    {
                        AreaData areaData = _mapApi.GetMapData(gameData.Area);
                        List<PointOfInterest> pointsOfInterest = PointOfInterestHandler.Get(_mapApi, areaData);
                        _compositor = new Compositor(areaData, pointsOfInterest);
                    }
                    else
                    {
                        _compositor = null;
                    }
                }

                _currentGameData = gameData;

                if (ShouldHideMap())
                {
                    mapOverlay.Hide();
                }
                else
                {
                    mapOverlay.Show();
                    mapOverlay.Refresh();
                }
            }

            _timer.Start();
        }

        private bool ShouldHideMap()
        {
            if (!_show) return true;
            if (_currentGameData.Area == Area.None) return true;
            if (Settings.Map.HideInTown && _currentGameData.Area.IsTown()) return true;
            if (!InGame()) return true;
            if (Settings.Map.ToggleViaInGameMap && !_currentGameData.MapShown) return true;
            return false;
        }

        private bool InGame()
        {
            return _currentGameData != null && _currentGameData.MainWindowHandle != IntPtr.Zero &&
                   WindowsExternal.GetForegroundWindow() == _currentGameData.MainWindowHandle;
        }

        private void MapOverlay_Paint(object sender, PaintEventArgs e)
        {
            if (_compositor == null)
            {
                return;
            }

            UpdateLocation();

            Bitmap gameMap = _compositor.Compose(_currentGameData);
            var anchor = new Point(0, 0);
            switch (Settings.Map.Position)
            {
                case MapPosition.TopRight:
                    anchor = new Point(_screen.WorkingArea.Width - gameMap.Width, 0);
                    break;
                case MapPosition.TopLeft:
                    anchor = new Point(0, 0);
                    break;
            }

            e.Graphics.DrawImage(gameMap, anchor);
        }

        /// <summary>
        /// Update the location and size of the form relative to the D2R window location.
        /// </summary>
        private void UpdateLocation()
        {
            _screen = Screen.FromHandle(_currentGameData.MainWindowHandle);
            Location = new Point(_screen.WorkingArea.X, _screen.WorkingArea.Y);
            Size = new Size(_screen.WorkingArea.Width, _screen.WorkingArea.Height);
            mapOverlay.Size = Size;
        }
    }
}
