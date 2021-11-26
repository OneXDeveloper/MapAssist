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
using System.Windows.Forms;
using MapAssist.Types;
using MapAssist.Helpers;
using MapAssist.Settings;
using Gma.System.MouseKeyHook;
using System.Numerics;
using System.Configuration;

namespace MapAssist
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
        private AreaData _areaData;
        private MapApi _mapApi;
        private bool _show = true;
        private Screen _screen;

        public Overlay(IKeyboardMouseEvents keyboardMouseEvents)
        {
            InitializeComponent();
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
                            MapAssistConfiguration.Loaded.RenderingConfiguration.Size = (int)(MapAssistConfiguration.Loaded.RenderingConfiguration.Size * 1.15f);
                        }
                    }

                    if (args.KeyChar == MapAssistConfiguration.Loaded.HotkeyConfiguration.ZoomOutKey)
                    {
                        if (MapAssistConfiguration.Loaded.RenderingConfiguration.ZoomLevel < 4f)
                        {
                            MapAssistConfiguration.Loaded.RenderingConfiguration.ZoomLevel += 0.25f;
                            MapAssistConfiguration.Loaded.RenderingConfiguration.Size = (int)(MapAssistConfiguration.Loaded.RenderingConfiguration.Size * .85f);
                        }
                    }
                }
            };
        }

        private void Overlay_Load(object sender, EventArgs e)
        {
            Rectangle screen = Screen.PrimaryScreen.WorkingArea;
            var width = Width >= screen.Width ? screen.Width : (screen.Width + Width) / 2;
            var height = Height >= screen.Height ? screen.Height : (screen.Height + Height) / 2;
            Location = new Point((screen.Width - width) / 2, (screen.Height - height) / 2);
            Size = new Size(width, height);
            Opacity = MapAssistConfiguration.Loaded.RenderingConfiguration.Opacity;

            _timer.Interval = MapAssistConfiguration.Loaded.UpdateTime;
            _timer.Tick += MapUpdateTimer_Tick;
            _timer.Start();

            if (MapAssistConfiguration.Loaded.RenderingConfiguration.AlwaysOnTop) SetTopMost();

            mapOverlay.Location = new Point(0, 0);
            mapOverlay.Width = Width;
            mapOverlay.Height = Height;
            mapOverlay.BackColor = Color.Transparent;
        }

        private void Overlay_FormClosing(object sender, EventArgs e)
        {
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
                    _mapApi = new MapApi(MapApi.Client, gameData.Difficulty, gameData.MapSeed);
                }

                if (gameData.HasMapChanged(_currentGameData))
                {
                    Console.WriteLine($"Area changed: {gameData.Area}");
                    if (gameData.Area != Area.None)
                    {
                        _areaData = _mapApi.GetMapData(gameData.Area);
                        List<PointOfInterest> pointsOfInterest = PointOfInterestHandler.Get(_mapApi, _areaData);
                        _compositor = new Compositor(_areaData, pointsOfInterest);
                    }
                    else
                    {
                        _compositor = null;
                    }
                }
            }

            _currentGameData = gameData;

                    if (MapAssistConfiguration.Loaded.RenderingConfiguration.AlwaysOnTop) SetTopMost();
            mapOverlay.Refresh();

            _timer.Start();
        }

        private void SetTopMost()
        {
            var initialStyle = (uint)WindowsExternal.GetWindowLongPtr(Handle, -20);
            WindowsExternal.SetWindowLong(Handle, -20, initialStyle | 0x80000 | 0x20);
            WindowsExternal.SetWindowPos(Handle, HWND_TOPMOST, 0, 0, 0, 0, TOPMOST_FLAGS);
        }

        private bool InGame()
        {
            return _currentGameData != null && _currentGameData.MainWindowHandle != IntPtr.Zero &&
                   WindowsExternal.GetForegroundWindow() == _currentGameData.MainWindowHandle;
        }

        private void MapOverlay_Paint(object sender, PaintEventArgs e)
        {
            if (_compositor == null || !InGame())
            {
                return;
            }

            if (MapAssistConfiguration.Loaded.GameInfo.AlwaysShow == false && !_show)
            {
                return;
            }
            var screenW = Screen.PrimaryScreen.WorkingArea.Width;
            var blackBarWidth = 0;
            if (screenW > 2880)
            {
                blackBarWidth = (screenW - 2880) / 4;
            }
            
            var overlayWidthDiff = screenW - Width;
            var textXOffset = blackBarWidth + (int)(screenW * .06f) - overlayWidthDiff;
            var fontSize = MapAssistConfiguration.Loaded.ItemLog.LabelFontSize;
            var fontHeight = (fontSize + fontSize / 2);
            var font = new Font(MapAssistConfiguration.Loaded.ItemLog.LabelFont, fontSize);
            var stringFormat = new StringFormat();
            stringFormat.Alignment = StringAlignment.Near;
            stringFormat.LineAlignment = StringAlignment.Near;

            var color = Color.Red;
            e.Graphics.DrawString("Game IP: " + _currentGameData.GameIP, font,
            new SolidBrush(color),
            new Point(textXOffset, fontHeight), stringFormat);

            /*stringFormat.Alignment = StringAlignment.Center;
            stringFormat.LineAlignment = StringAlignment.Near;
            var stateList = _currentGameData.PlayerUnit.StateList;
            color = Color.DarkOrange;
            var stateCount = 0;
            foreach (var state in stateList)
            {
                var stateStr = Enum.GetName(typeof(State), state).Substring(6);
                e.Graphics.DrawString(stateStr, font,
                new SolidBrush(color),
                new Point(screenW / 2, (stateCount * fontHeight)), stringFormat);
                stateCount++;
            }*/

            if (MapAssistConfiguration.Loaded.ItemLog.AlwaysShow == false && !_show)
            {
                return;
            }


            stringFormat.Alignment = StringAlignment.Near;
            stringFormat.LineAlignment = StringAlignment.Near;
            for (var i = 0; i < Items.CurrentItemLog.Count; i++)
            {
                color = Items.ItemColors[Items.CurrentItemLog[i].ItemData.ItemQuality];
                var isEth = (Items.CurrentItemLog[i].ItemData.ItemFlags & ItemFlags.IFLAG_ETHEREAL) == ItemFlags.IFLAG_ETHEREAL;
                var itemBaseName = Items.ItemNames[Items.CurrentItemLog[i].TxtFileNo];
                var itemSpecialName = "";
                var itemLabelExtra = "";
                if (isEth)
                {
                    itemLabelExtra += "[Eth] ";
                    color = Items.ItemColors[ItemQuality.SUPERIOR];
                }
                if(Items.CurrentItemLog[i].Stats.TryGetValue(Stat.STAT_ITEM_NUMSOCKETS, out var numSockets))
                {
                    itemLabelExtra += "[" + numSockets + " S] ";
                    color = Items.ItemColors[ItemQuality.SUPERIOR];
                }
                switch (Items.CurrentItemLog[i].ItemData.ItemQuality)
                {
                    case ItemQuality.UNIQUE:
                        color = Items.ItemColors[Items.CurrentItemLog[i].ItemData.ItemQuality];
                        itemSpecialName = Items.UniqueFromCode[Items.ItemCodes[Items.CurrentItemLog[i].TxtFileNo]] + " ";
                        break;
                    case ItemQuality.SET:
                        color = Items.ItemColors[Items.CurrentItemLog[i].ItemData.ItemQuality];
                        itemSpecialName = Items.SetFromCode[Items.ItemCodes[Items.CurrentItemLog[i].TxtFileNo]] + " ";
                        break;
                    case ItemQuality.CRAFT:
                        color = Items.ItemColors[Items.CurrentItemLog[i].ItemData.ItemQuality];
                        break;
                    case ItemQuality.RARE:
                        color = Items.ItemColors[Items.CurrentItemLog[i].ItemData.ItemQuality];
                        break;
                    case ItemQuality.MAGIC:
                        color = Items.ItemColors[Items.CurrentItemLog[i].ItemData.ItemQuality];
                        break;
                }
                e.Graphics.DrawString(itemLabelExtra + itemSpecialName + itemBaseName, font,
                new SolidBrush(color),
                new Point(textXOffset, (fontHeight * 2) + (i * (fontSize + fontSize / 2))), stringFormat);
            }

            if (!_show || Array.Exists(MapAssistConfiguration.Loaded.HiddenAreas, element => element == _currentGameData.Area) || (MapAssistConfiguration.Loaded.RenderingConfiguration.ToggleViaInGameMap && !_currentGameData.MapShown) || (_currentGameData.Area == Area.None))
            {
                return;
            }

            UpdateLocation();

            Bitmap gameMap = _compositor.Compose(_currentGameData, !MapAssistConfiguration.Loaded.RenderingConfiguration.OverlayMode);

            if (MapAssistConfiguration.Loaded.RenderingConfiguration.OverlayMode)
            {
                float w = 0;
                float h = 0;
                var scale = 0.0F;
                var center = new Vector2();

                switch (MapAssistConfiguration.Loaded.RenderingConfiguration.Position)
                {
                    case MapPosition.Center:
                        w = _screen.WorkingArea.Width;
                        h = _screen.WorkingArea.Height;
                        scale = (1024.0F / h * w * 3f / 4f / 2.3F) * MapAssistConfiguration.Loaded.RenderingConfiguration.ZoomLevel;
                        center = new Vector2(w / 2, h / 2 + 20);

                        e.Graphics.SetClip(new RectangleF(0, 0, w, h));
                        break;
                    case MapPosition.TopLeft:
                        w = 640;
                        h = 360;
                        scale = (1024.0F / h * w * 3f / 4f / 3.35F) * MapAssistConfiguration.Loaded.RenderingConfiguration.ZoomLevel;
                        center = new Vector2(w / 2, (h / 2) + 48);

                        e.Graphics.SetClip(new RectangleF(0, 50, w, h));
                        break;
                    case MapPosition.TopRight:
                        w = 640;
                        h = 360;
                        scale = (1024.0F / h * w * 3f / 4f / 3.35F) * MapAssistConfiguration.Loaded.RenderingConfiguration.ZoomLevel;
                        center = new Vector2(w / 2, (h / 2) + 40);

                        e.Graphics.TranslateTransform(_screen.WorkingArea.Width - w, -8);
                        e.Graphics.SetClip(new RectangleF(0, 50, w, h));
                        break;
                }

                Point playerPosInArea = _currentGameData.PlayerPosition.OffsetFrom(_areaData.Origin)
                    .OffsetFrom(_compositor.CropOffset);

                var playerPos = new Vector2(playerPosInArea.X, playerPosInArea.Y);

                Vector2 Transform(Vector2 p) =>
                    center +
                    DeltaInWorldToMinimapDelta(
                        p - playerPos,
                        (float)Math.Sqrt(w * w + h * h),
                        scale,
                        0);

                var p1 = Transform(new Vector2(0, 0));
                var p2 = Transform(new Vector2(gameMap.Width, 0));
                var p4 = Transform(new Vector2(0, gameMap.Height));

                PointF[] destinationPoints = {
                    new PointF(p1.X, p1.Y),
                    new PointF(p2.X, p2.Y),
                    new PointF(p4.X, p4.Y)
                };

                e.Graphics.DrawImage(gameMap, destinationPoints);
            }
            else
            {
                var anchor = new Point(0, 0);
                switch (MapAssistConfiguration.Loaded.RenderingConfiguration.Position)
                {
                    case MapPosition.Center:
                        anchor = new Point(_screen.WorkingArea.Width / 2, _screen.WorkingArea.Height / 2);
                        break;
                    case MapPosition.TopRight:
                        anchor = new Point(_screen.WorkingArea.Width - gameMap.Width, 0);
                        break;
                    case MapPosition.TopLeft:
                        anchor = new Point(0, 0);
                        break;
                }

                e.Graphics.DrawImage(gameMap, anchor);
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
        /// Update the location and size of the form relative to the window location.
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
