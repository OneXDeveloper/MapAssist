using System;
using System.Media;
using MapAssist.Settings;

namespace MapAssist.Helpers
{
    public class AudioPlayer
    {
        private static DateTime _itemAlertLastPlayed = DateTime.MinValue;
        private static SoundPlayer _itemAlertPlayer = null;
        public static void PlayItemAlert()
        {
            if((MapAssistConfiguration.Loaded.ItemLog.SoundFile != null && MapAssistConfiguration.Loaded.ItemLog.SoundFile != "") && _itemAlertPlayer == null)
            {
                var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                var directory = System.IO.Path.GetDirectoryName(exePath);
                var soundPath = directory + @"\" + MapAssistConfiguration.Loaded.ItemLog.SoundFile;
                _itemAlertPlayer = new SoundPlayer(soundPath);
            }
            if (_itemAlertPlayer == null) { _itemAlertPlayer = new SoundPlayer(Properties.Resources.ching); }
            var now = DateTime.Now;
            if (now - _itemAlertLastPlayed >= TimeSpan.FromSeconds(1))
            {
                _itemAlertLastPlayed = now;
                try
                {
                    _itemAlertPlayer.Play();
                }
                catch
                {
                    _itemAlertPlayer = new SoundPlayer(Properties.Resources.ching);
                    _itemAlertPlayer.Play();
                }
            }
        }
    }
}
