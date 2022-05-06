using System;
using MapAssist.Helpers;
using MapAssist.Interfaces;

namespace MapAssist.Types
{
    public class ActMisc : IUpdatable<ActMisc>
    {
        private readonly IntPtr _pActMisc = IntPtr.Zero;
        private Structs.ActMisc _actMisc;

        public ActMisc(IntPtr pActMisc)
        {
            _pActMisc = pActMisc;
            Update();
        }

        public ActMisc Update()
        {
            using (var processContext = GameManager.GetProcessContext())
            {
                _actMisc = processContext.Read<Structs.ActMisc>(_pActMisc);
            }

            return this;
        }

        public Difficulty GameDifficulty => _actMisc.GameDifficulty;
        public Act Act => new Act(_actMisc.pAct);
        public Level LevelFirst => new Level(_actMisc.pLevelFirst);
        public Area RealTombArea => _actMisc.RealTombArea;
    }
}
