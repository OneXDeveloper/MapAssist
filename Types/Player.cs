using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapAssist.Types
{
    enum PlayerClasses : uint
    {
        PCLASS_AMAZON,
        PCLASS_SORCERESS,
        PCLASS_NECROMANCER,
        PCLASS_PALADIN,
        PCLASS_BARBARIAN,
        PCLASS_DRUID,
        PCLASS_ASSASSIN,
        PCLASS_EVILFORCE,
        PCLASS_INVALID = 7,
        NUMBER_OF_PLAYERCLASSES = 7,
    };
}
