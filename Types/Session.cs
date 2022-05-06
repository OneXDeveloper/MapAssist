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

        public DateTime GameTimerStart { get; private set; } = DateTime.Now;
        public string GameTimerDisplay => FormatTime(DateTime.Now.Subtract(GameTimerStart).TotalSeconds);

        public DateTime LastAreaChange { get; set; } = DateTime.Now;
        public double PreviousAreaTime { get; set; } = 0;
        public double AreaTimeElapsed => PreviousAreaTime + DateTime.Now.Subtract(LastAreaChange).TotalSeconds;
        public Dictionary<Area, double> TotalAreaTimeElapsed { get; set; } = new Dictionary<Area, double>();
        public string AreaTimerDisplay => FormatTime(AreaTimeElapsed);

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
