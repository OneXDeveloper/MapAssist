using MapAssist.Types;
using System;
using System.Collections.Generic;

namespace MapAssist.Helpers
{
    public class GameDataReader
    {
        private static readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();
        private volatile GameData _gameData;
        private AreaData _areaData;
        private List<PointOfInterest> _pointsOfInterest;
        private MapApi _mapApi;

        public (GameData, AreaData, List<PointOfInterest>, bool) Get()
        {
            var gameData = GameMemory.GetGameData();
            var changed = false;

            if (gameData != null)
            {
                if (gameData.HasGameChanged(_gameData))
                {
                    _log.Info($"Game changed to {gameData.Difficulty} with {gameData.MapSeed} seed");
                    _mapApi = new MapApi(gameData.Difficulty, gameData.MapSeed);
                }

                if (gameData.HasMapChanged(_gameData) && gameData.Area != Area.None)
                {
                    _log.Info($"Area changed to {gameData.Area}");
                    _areaData = _mapApi.GetMapData(gameData.Area);

                    if (_areaData != null)
                    {
                        _pointsOfInterest = PointOfInterestHandler.Get(_mapApi, _areaData, gameData);
                        _log.Info($"Found {_pointsOfInterest.Count} points of interest");
                    }
                    else
                    {
                        _log.Info($"Area data not loaded");
                    }

                    changed = true;
                }
            }

            _gameData = gameData;

            return (_gameData, _areaData, _pointsOfInterest, changed);
        }
    }
}
