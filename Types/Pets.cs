using MapAssist.Helpers;
using MapAssist.Interfaces;
using MapAssist.Structs;
using System;
using System.Collections.Generic;

namespace MapAssist.Types
{
    public class PetEntry
    {
        public IntPtr PtrUnit { get; set; }
        public Pet Struct { get; set; }
    
        public uint UnitId => Struct.UnitId;
        public uint OwnerId => Struct.OwnerId;
        public bool IsMerc => new List<Npc> { Npc.Rogue2, Npc.Guard, Npc.IronWolf, Npc.Act5Hireling1Hand, Npc.Act5Hireling2Hand }.Contains(Struct.TxtFileNo);
        public bool IsPlayerOwned { get; set; }
    }

    public class Pets : IUpdatable<Pets>
    {
        private readonly IntPtr _pFirst;
        public List<PetEntry> List { get; } = new List<PetEntry>();

        public Pets(IntPtr pFirst)
        {
            _pFirst = pFirst;
            Update();
        }

        public Pets Update()
        {
            using (var processContext = GameManager.GetProcessContext())
            {
                var firstMember = processContext.Read<IntPtr>(_pFirst);
                var entry = GetNewEntry(firstMember);
                List.Add(entry);
                while (entry.Struct.pNext != IntPtr.Zero)
                {
                    entry = GetNewEntry(entry.Struct.pNext);
                    List.Add(entry);
                }
            }
            return this;
        }

        private PetEntry GetNewEntry(IntPtr pAddress)
        {
            using (var processContext = GameManager.GetProcessContext())
            {
                var pet = processContext.Read<Pet>(pAddress);
                var entry = new PetEntry
                {
                    PtrUnit = pAddress,
                    Struct = pet
                };
                return entry;
            }
        }
    }
}
