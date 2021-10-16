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
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static D2RAssist.Types.Game;

namespace D2RAssist.Helpers
{
    public static class MapApi
    {
        public async static Task CreateNewSession()
        {
            if (Globals.MapApiSession != null)
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.DeleteAsync(Settings.Api.Endpoint + "sessions/" + Globals.MapApiSession.id);
                }

                Globals.MapApiSession = null;
            }

            var values = new Dictionary<string, uint> {
                {"difficulty", Globals.CurrentGameData.Difficulty},
                {"mapid", Globals.CurrentGameData.MapSeed}
            };

            using (HttpClient client = new HttpClient())
            {
                var json = JsonConvert.SerializeObject(values);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync(Settings.Api.Endpoint + "sessions/", content);
                Globals.MapApiSession = JsonConvert.DeserializeObject<MapApiSession>(await response.Content.ReadAsStringAsync());
            }

            Globals.MapData = null;
            MapRenderer.Clear();
        }

        public static async Task<MapData> GetMapData(Area area, bool shouldClearMap = true)
        {
            if (Globals.MapApiSession == null)
            {
                return null;
            }

            if (shouldClearMap)
            {
                MapRenderer.Clear();
            }

            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(Settings.Api.Endpoint + "sessions/" + Globals.MapApiSession.id + "/areas/" + (uint)area);
                return JsonConvert.DeserializeObject<MapData>(await response.Content.ReadAsStringAsync());
            }
        }
    }
}
