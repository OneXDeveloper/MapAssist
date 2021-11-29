using System;
using System.Media;

namespace MapAssist.Helpers
{
    public class AudioPlayer
    {
        private static DateTime _itemAlertLastPlayed = DateTime.MinValue;
        private static readonly SoundPlayer _itemAlertPlayer = new SoundPlayer(Properties.Resources.ching);
        public static void PlayItemAlert()
        {
            var now = DateTime.Now;
            if (now - _itemAlertLastPlayed >= TimeSpan.FromSeconds(1))
            {
                _itemAlertLastPlayed = now;
                _itemAlertPlayer.Play();
            }
        }
    }
}
