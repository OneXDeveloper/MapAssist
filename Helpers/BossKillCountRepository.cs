using LiteDB;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MapAssist.Helpers
{
    public class BossKillCount
    {
        public ObjectId Id { get; set; }
        public uint Andariel { get; set; }
        public uint Duriel { get; set; }
        public uint Mephisto { get; set; }
        public uint Diablo { get; set; }
        public uint Baal { get; set; }
    }
    
    public class BossKillCountRepository
    {
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        private ILiteDatabase db;
        
        private static string _dbFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MapAssist");
        
        private static string _dbPath =  Path.Combine(_dbFolder,"bosskillcount.db");

        private BossKillCount _lastFetched { get; set; } = new BossKillCount();
        
        private SemaphoreSlim _semaphore { get; }

        public BossKillCountRepository()
        {
            if (!Directory.Exists(_dbFolder))
            {
                Directory.CreateDirectory(_dbFolder);
            }

            db = new LiteDatabase(_dbPath);
            
            _semaphore = new SemaphoreSlim(1, 1);
        }
        
        public async Task<BossKillCount> Get()
        {
            if (_semaphore.CurrentCount == 0)
            {
                return _lastFetched;
            }

            await _semaphore.WaitAsync();
            try
            {
                var collection = db.GetCollection<BossKillCount>("bosskillcount");
                var result = collection.FindOne(x => true);
                if (result == null)
                {
                    result = new BossKillCount();
                    collection.Insert(result);
                }

                _lastFetched = result;
                return result;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        //Increment Andariel kill count
        public void IncrementAndariel()
        {
            var collection = db.GetCollection<BossKillCount>("bosskillcount");
            var result = collection.FindOne(x => true);
            result.Andariel++;
            _logger.Info("Andariel killed: " + result.Andariel);
            collection.Update(result);
        }
        
        //Increment Duriel kill count
        public void IncrementDuriel()
        {
            var collection = db.GetCollection<BossKillCount>("bosskillcount");
            var result = collection.FindOne(x => true);
            result.Duriel++;
            _logger.Info("Duriel killed: " + result.Duriel);
            collection.Update(result);
        }
        
        //Increment Mephisto kill count
        public void IncrementMephisto()
        {
            var collection = db.GetCollection<BossKillCount>("bosskillcount");
            var result = collection.FindOne(x => true);
            result.Mephisto++;
            _logger.Info("Mephisto killed: " + result.Mephisto);
            collection.Update(result);
        }
        
        //Increment Diablo kill count
        public void IncrementDiablo()
        {
            var collection = db.GetCollection<BossKillCount>("bosskillcount");
            var result = collection.FindOne(x => true);
            result.Diablo++;
            _logger.Info("Diablo killed: " + result.Diablo);
            collection.Update(result);
        }
        
        //Increment Baal kill count
        public void IncrementBaal()
        {
            var collection = db.GetCollection<BossKillCount>("bosskillcount");
            var result = collection.FindOne(x => true);
            result.Baal++;
            _logger.Info("Baal killed: " + result.Baal);
            collection.Update(result);
        }
        
        ~BossKillCountRepository()
        {
            db.Dispose();
        }
    }
}
