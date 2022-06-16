using GameOverlay.Drawing;
using GameOverlay.Windows;
using MapAssist.Helpers;
using MapAssist.Settings;
using MapAssist.Structs;
using MapAssist.Types;
using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Winook;

namespace MapAssist
{
    public class Overlay : IDisposable
    {
        private static readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();

        private readonly GraphicsWindow _window;
        private GameDataReader _gameDataReader;
        private GameData _gameData;
        private Compositor _compositor = new Compositor();
        private static ConfigEditor _configEditor;
        private (MapPosition, bool) _lastMapConfiguration;
        private bool _show = true;
        private static readonly object _lock = new object();
        private bool frameDone = true;

        private static int _hookProcessId = 0;
        private static KeyboardHook _keyboardHook;
        private static MouseHook _mouseHook;
        public static Point mouseRelativePos;

        public Overlay(ConfigEditor configEditor)
        {
            _configEditor = configEditor;
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
            if (disposed) return;

            if (!frameDone) return;
            frameDone = false;

            var gfx = e.Graphics;

            try
            {
                lock (_lock)
                {
                    (MapPosition, bool) MapConfiguration()
                    {
                        return (MapAssistConfiguration.Loaded.RenderingConfiguration.Position, MapAssistConfiguration.Loaded.RenderingConfiguration.OverlayMode);
                    }

                    var (gameData, areaData, changed) = _gameDataReader.Get();
                    _gameData = gameData;

                    if (gameData != null) InstallHooks(gameData.ProcessId);

                    if (changed || _lastMapConfiguration != MapConfiguration())
                    {
                        _compositor.SetArea(areaData);
                        _lastMapConfiguration = MapConfiguration();
                    }

                    gfx.ClearScene();

                    if (_compositor != null && _gameData != null && InGame())
                    {
                        UpdateLocation();

                        if (gfx.Width > 0 && gfx.Height > 0)
                        {
                            var errorLoadingAreaData = _compositor._areaData == null;

                            var overlayHidden = !_show ||
                                errorLoadingAreaData ||
                                (MapAssistConfiguration.Loaded.RenderingConfiguration.ToggleViaInGameMap && !_gameData.MenuOpen.Map) ||
                                (MapAssistConfiguration.Loaded.RenderingConfiguration.ToggleViaInGamePanels && _gameData.MenuOpen.IsAnyMenuOpen()) ||
                                Array.Exists(MapAssistConfiguration.Loaded.HiddenAreas, area => area == _gameData.Area) ||
                                _gameData.Area == Area.None ||
                                gfx.Width == 1 ||
                                gfx.Height == 1;

                            var size = MapAssistConfiguration.Loaded.RenderingConfiguration.Size;
                            var height = MapAssistConfiguration.Loaded.RenderingConfiguration.OverlayMode ? size / 2 : size;

                            var drawBounds = new Rectangle(0, 0, gfx.Width, gfx.Height * 0.78f);
                            switch (MapAssistConfiguration.Loaded.RenderingConfiguration.Position)
                            {
                                case MapPosition.TopLeft:
                                    drawBounds = new Rectangle(PlayerIconWidth() + 40, PlayerIconWidth() + 60, 0, PlayerIconWidth() + 60 + height); // Right will be reset inside compositor
                                    break;

                                case MapPosition.TopRight:
                                    drawBounds = new Rectangle(0, PlayerIconWidth() + 60, gfx.Width, PlayerIconWidth() + 60 + height); // Left will be reset inside compositor
                                    break;
                            }

                            _compositor.Init(gfx, _gameData, drawBounds);

                            if (!overlayHidden)
                            {
                                if (!errorLoadingAreaData)
                                {
                                    _compositor.DrawGamemap(gfx);
                                    _compositor.DrawOverlay(gfx);
                                }
                                _compositor.DrawBuffs(gfx, mouseRelativePos);
                                _compositor.DrawMonsterBar(gfx);
                            }

                            _compositor.DrawPlayerInfo(gfx);
                            _compositor.DrawPortraitsInfo(gfx);

                            var gameInfoAnchor = GameInfoAnchor(MapAssistConfiguration.Loaded.GameInfo.Position);
                            var nextAnchor = _compositor.DrawGameInfo(gfx, gameInfoAnchor, e, errorLoadingAreaData);

                            var itemLogAnchor = (MapAssistConfiguration.Loaded.ItemLog.Position == MapAssistConfiguration.Loaded.GameInfo.Position)
                                ? nextAnchor.Add(0, GameInfoPadding())
                                : GameInfoAnchor(MapAssistConfiguration.Loaded.ItemLog.Position);
                            _compositor.DrawItemLog(gfx, itemLogAnchor);

                            if (Program.isPrecompiled) _compositor.DrawWatermark(gfx);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }

            frameDone = true;
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

        private void InstallHooks(int processId)
        {
            if (_hookProcessId != processId)
            {
                _hookProcessId = processId;

                if (_mouseHook != null)
                {
                    _mouseHook.Uninstall();
                    _mouseHook.Dispose();
                }

                _mouseHook = new MouseHook(processId, MouseMessageTypes.Move);
                _mouseHook.MessageReceived += MouseHook_MessageReceived;
                _mouseHook.InstallAsync();

                if (_keyboardHook != null)
                {
                    _keyboardHook.Uninstall();
                    _keyboardHook.Dispose();
                }

                _keyboardHook = new KeyboardHook(processId);
                _keyboardHook.MessageReceived += KeyboardHook_MessageReceived;
                _keyboardHook.InstallAsync();
            }
        }

        private void MouseHook_MessageReceived(object sender, MouseMessageEventArgs args)
        {
            if (GameManager.IsGameInForeground)
            {
                mouseRelativePos = new Point(args.X - _window.X, args.Y - _window.Y);
            }
        }

        private void KeyboardHook_MessageReceived(object sender, KeyboardMessageEventArgs args)
        {
            if (GameManager.IsGameInForeground && (_gameData == null || !_gameData.MenuOpen.Chat)
                && args.Direction == KeyDirection.Down
                && DateTime.Now.Subtract(GameManager.LastGameProcessForegroundTime) > TimeSpan.FromMilliseconds(500)) // Enable mouse hooks only shortly after the window gets activated
            {
                var keys = new Hotkey((Keys)args.Modifiers, (Keys)args.KeyValue);

                if (InGame())
                {
                    if (keys == new Hotkey(MapAssistConfiguration.Loaded.HotkeyConfiguration.ToggleKey))
                    {
                        MapAssistConfiguration.Loaded.RenderingConfiguration.Offset = new Point(0, 0);
                        _show = !_show;
                    }
                    else if (keys == new Hotkey(MapAssistConfiguration.Loaded.HotkeyConfiguration.HideMapKey))
                    {
                        _show = false;
                    }
                    else if (keys == new Hotkey(MapAssistConfiguration.Loaded.HotkeyConfiguration.MapPositionsKey))
                    {
                        MapAssistConfiguration.Loaded.RenderingConfiguration.Offset = new Point(0, 0);

                        var position = MapAssistConfiguration.Loaded.RenderingConfiguration.Position;
                        MapAssistConfiguration.Loaded.RenderingConfiguration.Position = Enum.GetValues(typeof(MapPosition))
                            .Cast<MapPosition>().Concat(new[] { default(MapPosition) })
                            .SkipWhile(e => !position.Equals(e)).Skip(1).First();
                    }
                    else if (keys == new Hotkey(MapAssistConfiguration.Loaded.HotkeyConfiguration.ZoomInKey))
                    {
                        var zoomLevel = MapAssistConfiguration.Loaded.RenderingConfiguration.ZoomLevel;

                        if (MapAssistConfiguration.Loaded.RenderingConfiguration.ZoomLevel > 0.1f)
                        {
                            MapAssistConfiguration.Loaded.RenderingConfiguration.ZoomLevel -= zoomLevel <= 1 ? 0.1 : 0.2;
                            MapAssistConfiguration.Loaded.RenderingConfiguration.Size +=
                              (int)(MapAssistConfiguration.Loaded.RenderingConfiguration.InitialSize * 0.05f);
                        }
                    }
                    else if (keys == new Hotkey(MapAssistConfiguration.Loaded.HotkeyConfiguration.ZoomOutKey))
                    {
                        var zoomLevel = MapAssistConfiguration.Loaded.RenderingConfiguration.ZoomLevel;

                        if (MapAssistConfiguration.Loaded.RenderingConfiguration.ZoomLevel < 4f)
                        {
                            MapAssistConfiguration.Loaded.RenderingConfiguration.ZoomLevel += zoomLevel >= 1 ? 0.2 : 0.1;
                            MapAssistConfiguration.Loaded.RenderingConfiguration.Size -=
                              (int)(MapAssistConfiguration.Loaded.RenderingConfiguration.InitialSize * 0.05f);
                        }
                    }
                    else if (keys == new Hotkey(MapAssistConfiguration.Loaded.HotkeyConfiguration.ExportItemsKey))
                    {
                        ItemExport.ExportPlayerInventory(_gameData.PlayerUnit, _gameData.AllItems);
                    }

                    var offsetMoveBy = 32; // Brute forced to match in game movements

                    if (keys == new Hotkey("Up"))
                    {
                        MapAssistConfiguration.Loaded.RenderingConfiguration.Offset = MapAssistConfiguration.Loaded.RenderingConfiguration.Offset.Add(0, offsetMoveBy);
                    }
                    else if (keys == new Hotkey("Down"))
                    {
                        MapAssistConfiguration.Loaded.RenderingConfiguration.Offset = MapAssistConfiguration.Loaded.RenderingConfiguration.Offset.Add(0, -offsetMoveBy);
                    }
                    else if (keys == new Hotkey("Left"))
                    {
                        MapAssistConfiguration.Loaded.RenderingConfiguration.Offset = MapAssistConfiguration.Loaded.RenderingConfiguration.Offset.Add(offsetMoveBy, 0);
                    }
                    else if (keys == new Hotkey("Right"))
                    {
                        MapAssistConfiguration.Loaded.RenderingConfiguration.Offset = MapAssistConfiguration.Loaded.RenderingConfiguration.Offset.Add(-offsetMoveBy, 0);
                    }
                }

                if (keys == new Hotkey(MapAssistConfiguration.Loaded.HotkeyConfiguration.ToggleConfigKey))
                {
                    if (_configEditor.Visible)
                    {
                        _configEditor.Close();
                    }
                    else
                    {
                        if (Thread.CurrentThread.GetApartmentState().Equals(ApartmentState.STA))
                        {
                            _configEditor.ShowDialog();
                        }
                        else
                        {
                            var thread = new Thread(new ThreadStart(delegate { _configEditor.ShowDialog(); }));
                            thread.CurrentCulture = thread.CurrentUICulture = CultureInfo.CurrentCulture;
                            thread.SetApartmentState(ApartmentState.STA);
                            thread.Start();
                            thread.Join();
                        }
                    }
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

        private float GameInfoPadding()
        {
            var rect = WindowRect();
            return rect.Height / 100f;
        }

        private Point GameInfoAnchor(GameInfoPosition position)
        {
            switch (position)
            {
                case GameInfoPosition.TopLeft:
                    var margin = _window.Height / 18f;
                    return new Point(PlayerIconWidth() + margin, PlayerIconWidth() + margin);

                case GameInfoPosition.TopRight:
                    var rightMargin = _window.Width / 60f;
                    var topMargin = _window.Height / 35f;
                    return new Point(_window.Width - rightMargin, topMargin);
            }
            return new Point();
        }

        private void _window_DestroyGraphics(object sender, DestroyGraphicsEventArgs e)
        {
            if (_compositor != null) _compositor.Dispose();
            _compositor = null;
        }

        ~Overlay() => Dispose();

        private bool disposed = false;

        public void Dispose()
        {
            lock (_lock)
            {
                if (!disposed)
                {
                    disposed = true; // This first to let GraphicsWindow.DrawGraphics know to return instantly

                    _mouseHook.Dispose();
                    _keyboardHook.Dispose();

                    _window.Dispose(); // This second to dispose of GraphicsWindow
                    if (_compositor != null) _compositor.Dispose(); // This last so it's disposed after GraphicsWindow stops using it
                }
            }
        }
    }
}
