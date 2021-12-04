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

using MapAssist.Settings;
using MapAssist.Types;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

#pragma warning disable 649

namespace MapAssist.Helpers
{
    public class MapApi : IDisposable
    {
        public static readonly HttpClient Client = HttpClient(MapAssistConfiguration.Loaded.ApiConfiguration.Endpoint, MapAssistConfiguration.Loaded.ApiConfiguration.Token);
        private static readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();
        private readonly string _sessionId;
        private readonly ConcurrentDictionary<Area, AreaData> _cache;
        private readonly HttpClient _client;

        private string CreateSession(Difficulty difficulty, uint mapSeed)
        {
            var values = new Dictionary<string, uint> {{"difficulty", (uint)difficulty}, {"mapid", mapSeed}};

            var json = JsonConvert.SerializeObject(values);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            HttpResponseMessage response = _client.PostAsync("sessions/", content).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
            var session =
                JsonConvert.DeserializeObject<MapApiSession>(response.Content.ReadAsStringAsync().GetAwaiter()
                    .GetResult());
            return session.id;
        }

        private void DestroySession(string sessionId)
        {
            HttpResponseMessage response =
                _client.DeleteAsync("sessions/" + sessionId).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
        }

        public MapApi(HttpClient client, Difficulty difficulty, uint mapSeed)
        {
            _client = client;
            _sessionId = CreateSession(difficulty, mapSeed);
            // Cache for pre-fetching maps for the surrounding areas.
            _cache = new ConcurrentDictionary<Area, AreaData>();

            Prefetch(MapAssistConfiguration.Loaded.PrefetchAreas);
        }

        public AreaData GetMapData(Area area)
        {
            if (!_cache.TryGetValue(area, out AreaData areaData))
            {
                // Not in the cache, block.
                _log.Info($"Cache miss on {area}");
                areaData = GetMapDataInternal(area);
            }

            Area[] adjacentAreas = areaData.AdjacentLevels.Keys.ToArray();
            if (adjacentAreas.Any())
            {
                Prefetch(adjacentAreas);
            }

            return areaData;
        }

        private void Prefetch(params Area[] areas)
        {
            var prefetchBackgroundWorker = new BackgroundWorker();
            prefetchBackgroundWorker.DoWork += (sender, args) =>
            {
                if (MapAssistConfiguration.Loaded.ClearPrefetchedOnAreaChange)
                {
                    _cache.Clear();
                }

                // Special value telling us to exit.
                if (areas.Length == 0)
                {
                    _log.Info("Prefetch worker terminating");
                    return;
                }

                foreach (Area area in areas)
                {
                    if (_cache.ContainsKey(area)) continue;

                    _cache[area] = GetMapDataInternal(area);
                    _log.Info($"Prefetched {area}");
                }
            };
            prefetchBackgroundWorker.RunWorkerAsync();
            prefetchBackgroundWorker.Dispose();
        }

        private AreaData GetMapDataInternal(Area area)
        {
            HttpResponseMessage response = _client.GetAsync("sessions/" + _sessionId +
                                                            "/areas/" + (uint)area).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
            var content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var rawMapData = JsonConvert.DeserializeObject<RawAreaData>(content);
            return rawMapData.ToInternal(area);
        }

        private static HttpClient HttpClient(string endpoint, string token)
        {
            var client = new HttpClient(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            }) {BaseAddress = new Uri(endpoint)};
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
            client.DefaultRequestHeaders.AcceptEncoding.Add(
                new StringWithQualityHeaderValue("gzip"));
            client.DefaultRequestHeaders.AcceptEncoding.Add(
                new StringWithQualityHeaderValue("deflate"));
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }

            return client;
        }

        public void Dispose()
        {
            try
            {
                DestroySession(_sessionId);
            }
            catch (HttpRequestException) // Prevent HttpRequestException if D2MapAPI is closed before this program.
            {
                _log.Info("D2MapAPI server was closed, session was already destroyed.");
            }
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "ReSharper")]
        private class MapApiSession
        {
            public string id;
            public uint difficulty;
            public uint mapId;
        }
    }
}
