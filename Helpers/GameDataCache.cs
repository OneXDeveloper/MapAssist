using MapAssist.Settings;
using MapAssist.Types;
using System;
using System.Threading;

namespace MapAssist.Helpers
{
    public class GameDataCache : IDisposable
    {
        private object _lock = new object();
        private volatile bool _stopRequested;
        private readonly Thread _thread;
        private volatile GameData _gameData;
        private MapApi _mapApi;
        private AreaData _areaData;
        private Compositor _compositor;

        public GameDataCache()
        {
            _thread = new Thread(Update) {IsBackground = true};
            _thread.Start();
        }

        public Tuple<GameData, Compositor, AreaData> Get()
        {
            lock (_lock)
            {
                return new Tuple<GameData, Compositor, AreaData>(_gameData, _compositor, _areaData);
            }
        }

        private void Update()
        {
            while (!_stopRequested)
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
                
                // Sleep until next time to update
                Thread.Sleep(MapAssistConfiguration.Loaded.UpdateTime);
            }
        }

        public void Dispose()
        {
            _stopRequested = true;
            _thread.Join();
            _mapApi?.Dispose();
        }
    }
}
