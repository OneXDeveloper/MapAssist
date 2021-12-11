using MapAssist.Settings;
using MapAssist.Types;
using System;
using System.Collections.Generic;

namespace MapAssist.Helpers
{
    public class GameDataReader
    {
        private Compositor _compositor;
        private volatile GameData _gameData;
        private MapApi _mapApi;

        public (Compositor, GameData) Get()
        {
            var gameData = GameMemory.GetGameData();

            if (gameData != null)
            {
                if (gameData.HasGameChanged(_gameData))
                {
                    _mapApi = new MapApi(gameData.Difficulty, gameData.MapSeed);
                }

                if (gameData.HasMapChanged(_gameData))
                {
                    Compositor compositor = null;

                    if (gameData.Area != Area.None)
                    {
                        var areaData = _mapApi.GetMapData(gameData.Area);

                        var pointsOfInterest = new List<PointOfInterest>();

                        if (areaData != null)
                        {
                            pointsOfInterest = PointOfInterestHandler.Get(_mapApi, areaData);
                        }

                        compositor = new Compositor(areaData, pointsOfInterest);
                    }

                    _compositor = compositor;
                }
            }

            _gameData = gameData;

            return (_compositor, _gameData);
        }
    }
}
