using MapAssist.Helpers;
using System;

namespace MapAssist.Types
{
    public class UnitMissile : UnitAny
    {
        public Missile Missile => (Missile)TxtFileNo;

        public UnitMissile(IntPtr ptrUnit) : base(ptrUnit)
        {
        }

        public override string HashString => Missile + "/" + Position.X + "/" + Position.Y;
    }
}
