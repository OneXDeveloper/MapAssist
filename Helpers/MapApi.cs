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

using D2RAssist.Types;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
#pragma warning disable 649

namespace D2RAssist.Helpers
{
    public class MapApi : IDisposable
    {
        public static readonly HttpClient Client = HttpClient();
        private readonly string _endpoint;
        private readonly string _sessionId;
        private readonly ConcurrentDictionary<Area, AreaData> _cache;
        private readonly BlockingCollection<Area[]> _prefetchRequests;
        private readonly Thread _thread;
        private readonly HttpClient _client;

        private string CreateSession(string endpoint, Difficulty difficulty, uint mapSeed)
        {
            Dictionary<string, uint> values = new Dictionary<string, uint>
            {
                { "difficulty", (uint)difficulty },
                { "mapid", mapSeed }
            };

            string json = JsonConvert.SerializeObject(values);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            HttpResponseMessage response = _client.PostAsync(endpoint + "sessions/", content).GetAwaiter().GetResult();
            var session =
                JsonConvert.DeserializeObject<MapApiSession>(response.Content.ReadAsStringAsync().GetAwaiter()
                    .GetResult());
            return session.id;
        }

        private void DestroySession(string endpoint, string sessionId)
        {
            HttpResponseMessage response =
                _client.DeleteAsync(endpoint + "sessions/" + sessionId).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
        }

        public MapApi(HttpClient client, string endpoint, Difficulty difficulty, uint mapSeed)
        {
            _client = client;
            _endpoint = endpoint;
            _sessionId = CreateSession(endpoint, difficulty, mapSeed); ;
            // Cache for pre-fetching maps for the surrounding areas.
            _cache = new ConcurrentDictionary<Area, AreaData>();
            _prefetchRequests = new BlockingCollection<Area[]>();
            _thread = new Thread(Prefetch);
            _thread.Start();

            if (Settings.Map.PrefetchAreas.Any())
            {
                _prefetchRequests.Add(Settings.Map.PrefetchAreas);
            }
        }

        public AreaData GetMapData(Area area)
        {
            if (!_cache.TryGetValue(area, out AreaData areaData))
            {
                // Not in the cache, block.
                Console.WriteLine($"Cache miss on {area}");
                areaData = GetMapDataInternal(area);
            }

            Area[] adjacentAreas = areaData.AdjacentLevels.Keys.ToArray();
            if (adjacentAreas.Any())
            {
                _prefetchRequests.Add(adjacentAreas);
            }

            return areaData;
        }

        private void Prefetch()
        {
            while (true)
            {
                Area[] areas = _prefetchRequests.Take();
                if (Settings.Map.ClearPrefetchedOnAreaChange)
                {
                    _cache.Clear();
                }

                // Special value telling us to exit.
                if (areas.Length == 0)
                {
                    Console.WriteLine("Prefetch thread terminating");
                    return;
                }

                foreach (Area area in areas)
                {
                    if (_cache.ContainsKey(area)) continue;

                    _cache[area] = GetMapDataInternal(area);
                    Console.WriteLine($"Prefetched {area}");
                }
            }
        }

        private AreaData GetMapDataInternal(Area area)
        {
            HttpResponseMessage response = _client.GetAsync(_endpoint + "sessions/" + _sessionId +
                                                            "/areas/" + (uint)area).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
            var rawMapData =
                JsonConvert.DeserializeObject<RawAreaData>(response.Content.ReadAsStringAsync().GetAwaiter()
                    .GetResult());
            return rawMapData.ToInternal(area);
        }

        private static HttpClient HttpClient()
        {
            var client = new HttpClient(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            });
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
            client.DefaultRequestHeaders.AcceptEncoding.Add(
                new StringWithQualityHeaderValue("gzip"));
            client.DefaultRequestHeaders.AcceptEncoding.Add(
                new StringWithQualityHeaderValue("deflate"));
            return client;
        }

        public void Dispose()
        {
            _prefetchRequests.Add(new Area[] { });
            _thread.Join();
            DestroySession(_endpoint, _sessionId);
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private class MapApiSession
        {
            public string id;
            public uint difficulty;
            public uint mapId;
        }
    }
}
