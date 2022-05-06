using MapAssist.Helpers;
using MapAssist.Interfaces;
using System;

namespace MapAssist.Types
{
    public class RoomEx : IUpdatable<RoomEx>
    {
        private readonly IntPtr _pRoomEx = IntPtr.Zero;
        private Structs.RoomEx _roomEx;

        public RoomEx(IntPtr pRoomEx)
        {
            _pRoomEx = pRoomEx;
            Update();
        }

        public RoomEx Update()
        {
            using (var processContext = GameManager.GetProcessContext())
            {
                _roomEx = processContext.Read<Structs.RoomEx>(_pRoomEx);
            }

            return this;
        }

        public Level Level => new Level(_roomEx.pLevel);
    }
}
