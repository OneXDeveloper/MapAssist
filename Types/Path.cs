using System;
using MapAssist.Helpers;
using MapAssist.Interfaces;

namespace MapAssist.Types
{
    public class Path : IUpdatable<Path>
    {
        private readonly IntPtr _pPath = IntPtr.Zero;
        private Structs.Path _path;

        public Path(IntPtr pPath)
        {
            _pPath = pPath;
            Update();
        }

        public Path Update()
        {
            using (var processContext = GameManager.GetProcessContext())
            {
                _path = processContext.Read<Structs.Path>(_pPath);
            }

            return this;
        }

        public float CalcFloatPos(ushort DynamicVal, ushort OffsetVal)
        {
            return DynamicVal + ((float)OffsetVal / 65535);
        }

        public float DynamicX => CalcFloatPos(_path.DynamicX, _path.XOffset);
        public float DynamicY => CalcFloatPos(_path.DynamicY, _path.YOffset);
        public ushort StaticX => _path.StaticX;
        public ushort StaticY => _path.StaticY;
        public Room Room => new Room(_path.pRoom);
    }
}
