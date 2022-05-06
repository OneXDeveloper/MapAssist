using System;
using MapAssist.Helpers;
using MapAssist.Interfaces;

namespace MapAssist.Types
{
    public class Act : IUpdatable<Act>
    {
        private readonly IntPtr _pAct = IntPtr.Zero;
        private Structs.Act _act;

        public Act(IntPtr pAct)
        {
            _pAct = pAct;
            Update();
        }

        public Act Update()
        {
            using (var processContext = GameManager.GetProcessContext())
            {
                _act = processContext.Read<Structs.Act>(_pAct);
            }

            return this;
        }

        public uint MapSeed => _act.MapSeed;
        public uint ActId => _act.ActId;
        public ActMisc ActMisc => new ActMisc(_act.pActMisc);
    }
}
