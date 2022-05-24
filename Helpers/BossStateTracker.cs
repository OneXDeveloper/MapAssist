using MapAssist.Types;

namespace MapAssist.Helpers
{
    public class BossStateTracker
    {
        private NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();
        private UnitMonster _activeBoss;
        private readonly BossKillCountRepository _bossKillCountRepository;

        public BossStateTracker(BossKillCountRepository bossKillCountRepository)
        {
            _bossKillCountRepository = bossKillCountRepository;
        }

        public void Activate(UnitMonster unitMonster)
        {
            if (_activeBoss != null)
            {
                return;
            }
            
            _log.Info("Tracking boss {0}", unitMonster);
            _activeBoss = unitMonster;
        }
        
        public void Clear()
        {
            if (_activeBoss == null)
                return;
            
            switch (_activeBoss.Npc)
            {
                case Npc.Andariel:
                    _bossKillCountRepository.IncrementAndariel();
                    break;
                case Npc.Duriel:
                    _bossKillCountRepository.IncrementDuriel();
                    break;
                case Npc.Mephisto:
                    _bossKillCountRepository.IncrementMephisto();
                    break;
                case Npc.Diablo:
                    _bossKillCountRepository.IncrementDiablo();
                    break;
                case Npc.BaalCrab:
                    _bossKillCountRepository.IncrementBaal();
                    break;
                default:
                    _log.Warn("Unknown boss {0} for tracker", _activeBoss);
                    break;
            }
            _activeBoss = null;
        }
    }
}
