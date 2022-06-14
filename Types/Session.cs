using MapAssist.Helpers;
using MapAssist.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MapAssist.Types
{
    public class Session : IUpdatable<Session>
    {
        private readonly IntPtr _pSession;
        private string _gameName = "";
        private string _gamePass = "";

        public Session(IntPtr pSession)
        {
            _pSession = pSession;
            Update();
        }

        public Session Update()
        {
            using (var processContext = GameManager.GetProcessContext())
            {
                var sessionData = processContext.Read<Structs.Session>(_pSession);

                try
                {
                    _gameName = Encoding.UTF8.GetString(sessionData.GameName.Take(sessionData.GameNameLength).ToArray()).TrimEnd((char)0);
                    _gamePass = Encoding.UTF8.GetString(sessionData.GamePass.Take(sessionData.GamePassLength).ToArray()).TrimEnd((char)0);
                }
                catch (Exception) { }
            }
            return this;
        }

        public string GameName => _gameName;
        public string GamePass => _gamePass;

        public DateTime GameStartTime { get; set; } = DateTime.MinValue;

        public string GameStartTimeDisplay(DateTime now) => FormatTime(now.Subtract(GameStartTime).TotalSeconds);

        public Dictionary<uint, Area> PlayerArea { get; set; } = new Dictionary<uint, Area>();
        public Dictionary<uint, DateTime> PlayerAreaStart { get; set; } = new Dictionary<uint, DateTime>();
        public Dictionary<uint, Dictionary<Area, List<double>>> PlayerAreasTimes { get; set; } = new Dictionary<uint, Dictionary<Area, List<double>>>();

        public double AreaTimeElapsed(uint unitId, DateTime now, bool totalTime = true)
        {
            if (!PlayerArea.TryGetValue(unitId, out var area)) return 0;

            return (totalTime && PlayerAreasTimes.TryGetValue(unitId, out var playerAreasTimes) && playerAreasTimes.TryGetValue(area, out var areasTimes) ? areasTimes.Sum() : 0) +
                (PlayerAreaStart.TryGetValue(unitId, out var playerAreaStart) ? now.Subtract(playerAreaStart).TotalSeconds : 0);
        }

        public string AreaTimeDisplay(uint unitId, DateTime now) => FormatTime(AreaTimeElapsed(unitId, now));

        public string FormatTime(double seconds)
        {
            var t = TimeSpan.FromSeconds(seconds);
            if (t.Hours > 0)
            {
                return string.Format("{0:D1}h {1:D1}m {2:D1}s", t.Hours, t.Minutes, t.Seconds);
            }
            if (t.Minutes > 0)
            {
                return string.Format("{0:D1}m {1:D1}s", t.Minutes, t.Seconds);
            }
            return string.Format("{0:D1}s", t.Seconds);
        }
    }
}
