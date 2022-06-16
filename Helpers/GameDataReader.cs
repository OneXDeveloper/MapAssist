using MapAssist.Settings;
using MapAssist.Types;
using System.Linq;

namespace MapAssist.Helpers
{
    public class GameDataReader
    {
        private static readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();
        private volatile GameData _gameData;
        private AreaData _areaData;
        private MapApi _mapApi;
        private Locale _language;

        public (GameData, AreaData, bool) Get()
        {
            var gameData = GameMemory.GetGameData();
            var changed = false;

            if (gameData != null)
            {
                if (gameData.HasGameChanged(_gameData))
                {
                    if (gameData.MapSeed == 0 && !gameData.MapSeedReady)
                    {
                        _log.Info($"Brute forcing first map seed");

                        changed = true;
                        _areaData = null;
                    }
                    else
                    {
                        _log.Info($"Game changed to {gameData.Difficulty} with {gameData.MapSeed} seed");
                    }

                    _mapApi = new MapApi(gameData);
                }

                if (gameData.HasMapChanged(_gameData) && gameData.MapSeed > 0 && gameData.Area != Area.None)
                {
                    _log.Info($"Area changed to {gameData.Area}");
                    _areaData = _mapApi.GetMapData(gameData.Area);

                    if (_areaData == null)
                    {
                        _log.Info($"Area data not loaded");
                    }
                    else if (_areaData.PointsOfInterest == null)
                    {
                        _areaData.PointsOfInterest = PointOfInterestHandler.Get(_mapApi, _areaData, gameData);
                        _language = MapAssistConfiguration.Loaded.LanguageCode;
                        _log.Info($"Found {_areaData.PointsOfInterest.Count} points of interest");
                    }

                    changed = true;
                }

                if (_areaData != null && _language != MapAssistConfiguration.Loaded.LanguageCode)
                {
                    var areaDatas = new[] { _areaData }.Concat(_areaData.AdjacentAreas.Values).ToArray();

                    foreach (var areaData in areaDatas)
                    {
                        if (_areaData.PointsOfInterest != null)
                        {
                            areaData.PointsOfInterest = PointOfInterestHandler.Get(_mapApi, areaData, gameData);
                        }
                    }

                    _language = MapAssistConfiguration.Loaded.LanguageCode;
                }
            }

            _gameData = gameData;

            ImportFromGameData();

            return (_gameData, _areaData, changed);
        }

        private void ImportFromGameData()
        {
            if (_gameData == null || _areaData == null) return;

            foreach (var gameObject in _gameData.Objects)
            {
                if (!_areaData.IncludesPoint(gameObject.Position)) continue;

                if (gameObject.IsShrine || gameObject.IsWell)
                {
                    var existingPoint = _areaData.PointsOfInterest.FirstOrDefault(x => x.Position == gameObject.Position);

                    if (existingPoint != null)
                    {
                        existingPoint.Label = gameObject.Name();
                    }
                    else
                    {
                        _areaData.PointsOfInterest.Add(new PointOfInterest()
                        {
                            Area = _areaData.Area,
                            Label = gameObject.Name(),
                            Position = gameObject.Position,
                            RenderingSettings = MapAssistConfiguration.Loaded.MapConfiguration.Shrine,
                            Type = PoiType.Shrine
                        });
                    }
                }
            }
        }
    }
}
