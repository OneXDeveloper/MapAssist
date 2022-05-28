using MapAssist.Settings;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Runtime.InteropServices;

namespace MapAssist.Helpers
{
    public class AudioPlayer
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        private static DateTime _itemAlertLastPlayed = DateTime.MinValue;
        private static Dictionary<string, SoundPlayer> soundPlayers = new Dictionary<string, SoundPlayer>();
        private static SoundPlayer lastItemSoundPlayer;

        public static void PlayItemAlert(string soundFile, bool stopPreviousAlert = false)
        {
            var itemSoundPlayer = GetSoundPlayer(soundFile);

            if (itemSoundPlayer != null)
            {
                var now = DateTime.Now;
                if (now - _itemAlertLastPlayed >= TimeSpan.FromSeconds(1) || stopPreviousAlert)
                {
                    if (lastItemSoundPlayer != null) lastItemSoundPlayer.Stop();

                    SetSoundVolume();
                    _itemAlertLastPlayed = now;

                    itemSoundPlayer.Play();
                    lastItemSoundPlayer = itemSoundPlayer;
                }
            }
        }

        public static SoundPlayer GetSoundPlayer(string soundFile)
        {
            if (string.IsNullOrEmpty(soundFile)) return null;
            if (soundPlayers.TryGetValue(soundFile, out var soundPlayer)) return soundPlayer;

            var directory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var soundPath = Path.Combine(directory, "Sounds", soundFile);

            if (File.Exists(soundPath))
            {
                var newSoundPlayer = new SoundPlayer(soundPath);
                soundPlayers[soundFile] = newSoundPlayer;

                _log.Info($"Loaded new sound file: {soundFile}");

                return newSoundPlayer;
            }
            else
            {
                _log.Info($"Sound file not found: {soundFile}");

                return null;
            }
        }

        public static void Dispose()
        {
            foreach (var soundPlayer in soundPlayers.Values)
            {
                soundPlayer.Dispose();
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
