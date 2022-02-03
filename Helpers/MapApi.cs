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
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Path = System.IO.Path;

#pragma warning disable 649

namespace MapAssist.Helpers
{
    public class MapApi
    {
        private static readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();
        private static Process _pipeClient;
        private static readonly object _pipeRequestLock = new object();
        private const string _procName = "MAServer.exe";

        private readonly ConcurrentDictionary<Area, AreaData> _cache;
        private Difficulty _difficulty;
        private uint _mapSeed;

        private static int trailingSlash = -1; // -1 = Still trying to figure out, 0 = remove trailing slash, 1 = required trailing slash
        private static int trailingSlashTry = 0; // Which trailing slash preference to try

        private static readonly Dictionary<string, uint> GameCRC32 = new Dictionary<string, uint> {
            {"1.11a", 0xf44cd0cf },
            {"1.11b", 0x8fd3f392 },
            {"1.12a", 0xab566eaa },
            {"1.13c", 0xea2f0e6e },
            {"1.13d", 0xb3d69c47 },
        };
        private static readonly Dictionary<string, uint> StormCRC32 = new Dictionary<string, uint> {
            {"1.11a", 0x9f06891d },
            {"1.11b", 0xb6390775 },
            {"1.12a", 0xe5b0f351 },
            {"1.13c", 0x5711a8b4 },
            {"1.13d", 0xbdb6784e }
        };

        public static bool StartPipedChild()
        {
            // We have an exclusive lock on the MA process.
            // So we can kill off any previously lingering map servers
            // in case we had a weird shutdown that didn't clean up appropriately.
            StopPipeServers();

            var procFile = Path.Combine(Environment.CurrentDirectory, _procName);
            if (!File.Exists(procFile))
            {
                throw new Exception("Unable to start map server. Check Anti Virus settings.");
            }

            var path = FindD2LoD();

            if (trailingSlash == 0 || trailingSlashTry == 0) // Remove trailing slash
            {
                path = path.TrimEnd('\\');
            }
            else if (trailingSlash == 1 || trailingSlashTry == 1) // Require trailing slash
            {
                path = path.TrimEnd('\\') + "\\";
            }

            _pipeClient = new Process();
            _pipeClient.StartInfo.FileName = procFile;
            _pipeClient.StartInfo.Arguments = "\"" + path + "\"";
            _pipeClient.StartInfo.UseShellExecute = false;
            _pipeClient.StartInfo.RedirectStandardOutput = true;
            _pipeClient.StartInfo.RedirectStandardInput = true;
            _pipeClient.StartInfo.RedirectStandardError = true;
            _pipeClient.Start();

            var (startupLength, _) = MapApiRequest().Result;
            _log.Info($"{_procName} has started");

            return startupLength == 0;
        }

        private static string FindD2LoD()
        {
            var providedPath = MapAssistConfiguration.Loaded.D2LoDPath;
            if (!string.IsNullOrEmpty(providedPath))
            {
                if (Path.HasExtension(providedPath))
                {
                    var config1 = new ConfigEditor();
                    MessageBox.Show("Provided D2 LoD path is not set to a directory." + Environment.NewLine + Environment.NewLine + "Please provide a path to a D2 LoD 1.13c installation and restart MapAssist.");
                    config1.ShowDialog();
                    return null;
                }

                if (IsValidD2LoDPath(providedPath))
                {
                    _log.Info("User provided D2 LoD path is valid");
                    return providedPath;
                }

                var config = new ConfigEditor();
                _log.Info("User provided D2 LoD path is invalid");
                MessageBox.Show("Provided D2 LoD path is not the correct version." + Environment.NewLine + Environment.NewLine + "Please provide a path to a D2 LoD 1.13c installation and restart MapAssist.");
                config.ShowDialog();
                return null;
            }

            var installPath = Registry.GetValue("HKEY_CURRENT_USER\\SOFTWARE\\Blizzard Entertainment\\Diablo II", "InstallPath", "INVALID") as string;
            if (installPath == "INVALID" || !IsValidD2LoDPath(installPath))
            {
                _log.Info("Registry-provided D2 LoD path not found or invalid");
                MessageBox.Show("Unable to automatically locate D2 LoD installation." + Environment.NewLine + Environment.NewLine + "Please provide a path to a D2 LoD 1.13c installation and restart MapAssist.");
                var config = new ConfigEditor();
                config.ShowDialog();
                return null;
            }

            _log.Info("Registry-provided D2 LoD path is valid");
            return installPath;
        }

        private static bool IsValidD2LoDPath(string path)
        {
            try
            {
                var gamePath = Path.Combine(path, "game.exe");
                if (File.Exists(gamePath))
                {
                    var fileChecksum = Files.Checksum.FileChecksum(gamePath);
                    foreach(KeyValuePair<string, uint> kvp in GameCRC32)
                    {
                        var allowedChecksum = kvp.Value;
                        if (fileChecksum == allowedChecksum)
                        {
                            _log.Info("Valid D2 LoD version identified by Game.exe - v" + kvp.Key);
                            return true;
                        }
                    }
                } else
                {
                    gamePath = Path.Combine(path, "storm.dll");
                    if (File.Exists(gamePath))
                    {
                        var fileChecksum = Files.Checksum.FileChecksum(gamePath);
                        foreach (KeyValuePair<string, uint> kvp in StormCRC32)
                        {
                            var allowedChecksum = kvp.Value;
                            if (fileChecksum == allowedChecksum)
                            {
                                _log.Info("Valid D2 LoD version identified by Storm.dll - v" + kvp.Key);
                                return true;
                            }
                        }
                    }
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static async Task<(uint, string)> MapApiRequest(byte[] writeBytes = null, int timeout = 1000)
        {
            if (_pipeClient.HasExited)
            {
                _log.Warn($"{_procName} has exited unexpectedly");
            }

            if (disposed || _pipeClient.HasExited)
            {
                return (0, null);
            }

            var pipeInput = _pipeClient.StandardInput;
            var pipeOutput = _pipeClient.StandardOutput;

            Func<int, Task<byte[]>> ReadBytes = async (readBytesLength) =>
            {
                var data = new byte[0];
                var cts = new CancellationTokenSource(timeout);

                while (!disposed && !_pipeClient.HasExited && !cts.IsCancellationRequested && data.Length < readBytesLength)
                {
                    var tryReadLength = readBytesLength - data.Length;
                    var chunk = new byte[tryReadLength];
                    var dataReadLength = await pipeOutput.BaseStream.ReadAsync(chunk, 0, tryReadLength, cts.Token);

                    data = Combine(data, chunk.Take(dataReadLength).ToArray());
                }

                var response = !disposed && !_pipeClient.HasExited && !cts.IsCancellationRequested ? data : null;
                cts.Dispose();
                return response;
            };

            Func<int, Task<byte[]>> TryReadBytes = async (readBytesLength) =>
            {
                var task = ReadBytes(readBytesLength);
                var result = await Task.WhenAny(task, Task.Delay(timeout));
                if (result == task)
                {
                    return await task;
                }
                else
                {
                    _log.Warn($"Request timed out after {timeout} ms");
                    return null;
                }
            };

            if (writeBytes != null)
            {
                pipeInput.BaseStream.Write(writeBytes, 0, writeBytes.Length);
                pipeInput.BaseStream.Flush();
            }

            var readLength = await TryReadBytes(4);
            if (readLength == null) return (0, null);
            var length = BitConverter.ToUInt32(readLength, 0);

            if (length == 0)
            {
                return (0, null);
            }

            if (length == uint.MaxValue) // Try a different trailing slash logic
            {
                trailingSlashTry++;
            }
            else if (length > 0) // Trailing slash attempt worked and keep that logic for future requests
            {
                trailingSlash = trailingSlashTry;
            }

            string json = null;
            JObject jsonObj;
            try
            {
                _log.Info($"Reading {length} bytes from {_procName}");
                var readJson = await TryReadBytes((int)length);
                if (readJson == null) return (0, null);
                json = Encoding.UTF8.GetString(readJson);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return (length, null);
                }
                jsonObj = JObject.Parse(json);
            }
            catch (Exception e)
            {
                _log.Error(e);
                _log.Error(e, "Unable to parse JSON data from map server.");
                if (!string.IsNullOrWhiteSpace(json))
                {
                    _log.Error(json);
                }

                return (length, null);
            }

            if (jsonObj.ContainsKey("error"))
            {
                _log.Error(jsonObj["error"].ToString());
                return (length, null);
            }

            return (length, json);
        }

        public MapApi(Difficulty difficulty, uint mapSeed)
        {
            _difficulty = difficulty;
            _mapSeed = mapSeed;

            // Cache for pre-fetching maps for the surrounding areas.
            _cache = new ConcurrentDictionary<Area, AreaData>();
        }

        public AreaData GetMapData(Area area)
        {
            if (!_cache.TryGetValue(area, out AreaData areaData))
            {
                _log.Info($"Requesting map data for {area} ({_mapSeed} seed, {_difficulty} difficulty)");
                areaData = GetMapDataInternal(area);
                _cache[area] = areaData;
            }
            else
            {
                _log.Info($"Cache found for {area}");
            }

            if (areaData != null)
            {
                Area[] adjacentAreas = areaData.AdjacentLevels.Keys.ToArray();

                var additionalAreas = GetAdjacentLevelsForWideArea(areaData.Area);
                adjacentAreas = adjacentAreas.Concat(additionalAreas).ToArray();

                if (adjacentAreas.Length > 0)
                {
                    _log.Info($"{adjacentAreas.Length} adjacent areas to {area} found");

                    foreach (var adjacentArea in adjacentAreas)
                    {
                        if (!_cache.TryGetValue(adjacentArea, out AreaData adjAreaData))
                        {
                            _log.Info($"Requesting map data for {adjacentArea} ({_mapSeed} seed, {_difficulty} difficulty)");
                            _cache[adjacentArea] = GetMapDataInternal(adjacentArea);
                            areaData.AdjacentAreas[adjacentArea] = _cache[adjacentArea];
                        }
                        else
                        {
                            _log.Info($"Cache found for {adjacentArea}");
                            areaData.AdjacentAreas[adjacentArea] = adjAreaData;
                        }
                    }
                }
                else
                {
                    _log.Info($"No adjacent areas to {area} found");
                }
            }
            else
            {
                _log.Info($"areaData was null on {area}");
            }

            return areaData;
        }

        private Area[] GetAdjacentLevelsForWideArea(Area area)
        {
            // Improve stitching by rendering more areas than directly adjacent levels
            // Sometimes render areas 2 maps away to get a better picture
            switch (area)
            {
                case Area.BlackMarsh:
                    return new Area[] {
                        Area.MonasteryGate,
                        Area.OuterCloister,
                    };
                case Area.TamoeHighland:
                    return new Area[] {
                        Area.OuterCloister,
                        Area.Barracks,
                    };
                case Area.MonasteryGate:
                    return new Area[] {
                        Area.BlackMarsh,
                        Area.Barracks,
                    };
                case Area.OuterCloister:
                    return new Area[] {
                        Area.BlackMarsh,
                        Area.TamoeHighland,
                        Area.Barracks, // Missing adjacent area
                    };
                case Area.Barracks:
                    return new Area[] {
                        Area.TamoeHighland,
                        Area.MonasteryGate,
                        Area.OuterCloister, // Missing adjacent area
                    };
                case Area.InnerCloister:
                    return new Area[] {
                        Area.Cathedral, // Missing adjacent area
                    };
                case Area.Cathedral:
                    return new Area[] {
                        Area.InnerCloister, // Missing adjacent area
                    };
                case Area.LutGholein:
                    return new Area[] {
                        Area.DryHills,
                    };
                case Area.DryHills:
                    return new Area[] {
                        Area.LutGholein,
                        Area.LostCity,
                    };
                case Area.RockyWaste:
                    return new Area[] {
                        Area.FarOasis,
                    };
                case Area.LostCity:
                    return new Area[] {
                        Area.DryHills,
                    };
                case Area.FarOasis:
                    return new Area[] {
                        Area.RockyWaste,
                    };
                case Area.GreatMarsh:
                    return new Area[] {
                        Area.FlayerJungle,
                    };
                case Area.FlayerJungle:
                    return new Area[] {
                        Area.GreatMarsh,
                    };
                case Area.UpperKurast:
                    return new Area[] {
                        Area.Travincal,
                    };
                case Area.Travincal:
                    return new Area[] {
                        Area.UpperKurast,
                    };
                default:
                    return new Area[] { };
            }
        }

        private AreaData GetMapDataInternal(Area area)
        {
            var req = new Req();
            req.seed = _mapSeed;
            req.difficulty = (uint)_difficulty;
            req.levelId = (uint)area;

            lock (_pipeRequestLock)
            {
                uint length = 0;
                string json = null;
                var retry = false;

                do
                {
                    retry = false;

                    (length, json) = MapApiRequest(ToBytes(req)).Result;

                    if (json == null)
                    {
                        _log.Error($"Unable to load data for {area} from {_procName}, retrying after restarting {_procName}");
                        StartPipedChild();
                        retry = true;
                    }
                } while (retry);

                var rawAreaData = JsonConvert.DeserializeObject<RawAreaData>(json);
                return rawAreaData.ToInternal(area);
            }
        }

        public static byte[] Combine(byte[] first, byte[] second)
        {
            var ret = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, ret, 0, first.Length);
            Buffer.BlockCopy(second, 0, ret, first.Length, second.Length);
            return ret;
        }

        private static byte[] ToBytes(Req req)
        {
            var size = Marshal.SizeOf(req);
            var arr = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(req, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);
            return arr;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct Req
        {
            public uint seed;
            public uint difficulty;
            public uint levelId;
        }

        private static bool disposed = false;
        public static void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                DisposePipe();
            }
        }

        private static void DisposePipe()
        {
            if (_pipeClient == null)
            {
                return;
            }

            _log.Info("Closing map server");
            if (!_pipeClient.HasExited)
            {
                try { _pipeClient.Kill(); } catch (Exception) { }
                try { _pipeClient.Close(); } catch (Exception) { }
            }
            try { _pipeClient.Dispose(); } catch (Exception) { }

            _pipeClient = null;
        }

        private static void StopPipeServers()
        {
            DisposePipe();

            // Shutdown old running versions of the map server
            var procs = Process.GetProcessesByName(_procName);
            foreach (var proc in procs)
            {
                proc.Kill();
            }
        }
    }
}
