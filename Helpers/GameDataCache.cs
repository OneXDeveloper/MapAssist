using MapAssist.Settings;
using MapAssist.Types;
using System;

namespace MapAssist.Helpers
{
    public class GameDataCache : IDisposable
    {
        private object _lock = new object();
        private volatile GameData _gameData;
        private MapApi _mapApi;
        private AreaData _areaData;
        private Compositor _compositor;
        private System.Timers.Timer _updateTimer;

        public GameDataCache()
        {
            RunUpdateTimer();
        }

        public Tuple<GameData, Compositor, AreaData> Get()
        {
            lock (_lock)
            {
                return new Tuple<GameData, Compositor, AreaData>(_gameData, _compositor, _areaData);
            }
        }

        private void RunUpdateTimer()
        {
            _updateTimer = new System.Timers.Timer(MapAssistConfiguration.Loaded.UpdateTime);
            _updateTimer.Elapsed += (sender, args) =>
            {
                lock (_lock)
                {
                    var gameData = GameMemory.GetGameData();

                    if (gameData != null)
                    {
                        if (gameData.HasGameChanged(_gameData))
                        {
                            _mapApi?.Dispose();
                            _mapApi = new MapApi(MapApi.Client, gameData.Difficulty, gameData.MapSeed);
                        }

                        if (gameData.HasMapChanged(_gameData))
                        {
                            Compositor compositor = null;

                            if (gameData.Area != Area.None)
                            {
                                _areaData = _mapApi.GetMapData(gameData.Area);
                                var pointsOfInterest = PointOfInterestHandler.Get(_mapApi, _areaData);
                                compositor = new Compositor(_areaData, pointsOfInterest);
                            }

                            _compositor = compositor;
                        }
                    }

                    _gameData = gameData;
                }
                _updateTimer.Start();
            };
            _updateTimer.AutoReset = false;
            _updateTimer.Start();
        }

        public void Dispose()
        {
            _updateTimer.Stop();
            _updateTimer.Dispose();
            _mapApi?.Dispose();
        }
    }
}
