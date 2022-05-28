using MapAssist.Settings;
using NLog;
using System;
using System.IO;
using System.Media;
using System.Runtime.InteropServices;

namespace MapAssist.Helpers
{
    public class AudioPlayer
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();
        
        private static DateTime _itemAlertLastPlayed = DateTime.MinValue;
        private static SoundPlayer _itemAlertPlayer = null;

        public static void PlayItemAlert(bool stopPreviousAlert = false)
        {
            LoadNewSound();

            var now = DateTime.Now;
            if (now - _itemAlertLastPlayed >= TimeSpan.FromSeconds(1) || stopPreviousAlert)
            {
                SetSoundVolume();
                _itemAlertLastPlayed = now;

                if (_itemAlertPlayer != null)
                {
                    _itemAlertPlayer.Stop();
                    _itemAlertPlayer.Play();
                }
            }
        }

        public static void LoadNewSound(bool ignoreIfAlreadyLoaded = false)
        {
            if (!string.IsNullOrEmpty(MapAssistConfiguration.Loaded.ItemLog.SoundFile) && (_itemAlertPlayer == null || ignoreIfAlreadyLoaded))
            {
                var directory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                var soundPath = Path.Combine(directory, "Sounds", MapAssistConfiguration.Loaded.ItemLog.SoundFile);

                if (File.Exists(soundPath))
                {
                    _itemAlertPlayer = new SoundPlayer(soundPath);
                    _log.Info($"Loaded new sound file: {MapAssistConfiguration.Loaded.ItemLog.SoundFile}");
                }
                else
                {
                    _log.Info($"Sound file not found: {MapAssistConfiguration.Loaded.ItemLog.SoundFile}");
                }
            }
        }

        private static void SetSoundVolume()
        {
            var NewVolume = (ushort.MaxValue * Math.Max(Math.Min(MapAssistConfiguration.Loaded.ItemLog.SoundVolume, 100), 0) / 100);
            var NewVolumeAllChannels = (((uint)NewVolume & 0x0000ffff) | ((uint)NewVolume << 16));
            waveOutSetVolume(IntPtr.Zero, NewVolumeAllChannels);
        }

        [DllImport("winmm.dll")]
        public static extern int waveOutSetVolume(IntPtr hwo, uint dwVolume);
    }
}
