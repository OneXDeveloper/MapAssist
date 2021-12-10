/**
 *   Copyright (C) 2021 okaygo
 *   
 *   https://github.com/misterokaygo/MapAssist/
 *
 *  This program is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program.  If not, see <https://www.gnu.org/licenses/>.
 **/

using System;
using System.Threading;
using System.Windows.Forms;
using Gma.System.MouseKeyHook;
using MapAssist.Settings;
using System.ComponentModel;
using System.Diagnostics;
using NLog;
using MapAssist.Helpers;
using MapAssist.Types;

namespace MapAssist
{
    static class Program
    {
        private static string appName = "MapAssist";
        private static Mutex mutex = null; 
        
        private static NotifyIcon trayIcon;
        private static Overlay overlay;
        private static BackgroundWorker backWorkOverlay = new BackgroundWorker();
        private static IKeyboardMouseEvents globalHook = Hook.GlobalEvents();
        private static readonly Logger _log = LogManager.GetCurrentClassLogger(); 

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                bool createdNew;
                mutex = new Mutex(true, appName, out createdNew);

                if (!createdNew)
                {
                    var rand = new Random();
                    var isGemActive = rand.NextDouble() < 0.05;

                    MessageBox.Show("An instance of " + appName + " is already running." + (isGemActive ? " Better go catch it!" : ""), appName, MessageBoxButtons.OK);
                    return;
                }


                var configurationOk = LoadLoggingConfiguration() && LoadMainConfiguration() && LoadLootLogConfiguration();
                if (configurationOk)
                {
                    Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                    Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);
                    AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);

                    try
                    {
                        if (!MapApi.StartPipedChild())
                        {
                            MessageBox.Show("Unable to start d2mapapi pipe.", appName, MessageBoxButtons.OK);
                            return;
                        }
                    }
                    catch (Exception e)
                    {
                        _log.Fatal(e);
                        _log.Fatal(e, "Unable to start d2mapapi pipe.");

                        var message = e.Message + Environment.NewLine + Environment.NewLine + e.StackTrace;
                        MessageBox.Show(message, "Unable to start d2mapapi pipe.", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    var contextMenu = new ContextMenuStrip();

                    var configMenuItem = new ToolStripMenuItem("Config", null, Config);
                    var lootFilterMenuItem = new ToolStripMenuItem("Loot Filter", null, LootFilter);
                    var restartMenuItem = new ToolStripMenuItem("Restart", null, Restart);
                    var exitMenuItem = new ToolStripMenuItem("Exit", null, Exit);
                    contextMenu.Items.Add(exitMenuItem);

                    contextMenu.Items.AddRange(new ToolStripItem[] {
                        configMenuItem,
                        lootFilterMenuItem,
                        new ToolStripSeparator(),
                        restartMenuItem,
                        exitMenuItem
                    });

                    trayIcon = new NotifyIcon()
                    {
                        Icon = Properties.Resources.Icon1,
                        ContextMenuStrip = contextMenu,
                        Text = appName,
                        Visible = true
                    };

                    globalHook.KeyPress += (sender, args) =>
                    {
                        if (overlay != null)
                        {
                            overlay.KeyPressHandler(sender, args);
                        }
                    };
                    
                    backWorkOverlay.DoWork += new DoWorkEventHandler(RunOverlay);
                    backWorkOverlay.WorkerSupportsCancellation = true;
                    backWorkOverlay.RunWorkerAsync();

                    GameManager.MonitorForegroundWindow();

                    Application.Run();
                }
            }
            catch (Exception e)
            {
                ProcessException(e);
            }
        }

        public static void RunOverlay(object sender, DoWorkEventArgs e)
        {
            using (overlay = new Overlay(globalHook))
            {
                overlay.Run();
            }
        }

        private static void ProcessException(Exception e)
        {
            var message = e.Message + Environment.NewLine + Environment.NewLine + e.StackTrace;

            MessageBox.Show(message, "MapAssist Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            Application.Exit();
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ProcessException((Exception) e.ExceptionObject);
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            ProcessException(e.Exception);
        }

        private static bool LoadMainConfiguration()
        {
            var configurationOk = false;
            try
            {
                MapAssistConfiguration.Load();
                configurationOk = true;
            }
            catch (YamlDotNet.Core.YamlException e)
            {
                _log.Fatal(e);
                _log.Fatal(e, "Invalid yaml for configuration file");

                MessageBox.Show(e.Message, "Yaml parsing error occurred. Invalid MapAssist configuration.",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception e)
            {
                _log.Fatal(e, "Unknown error loading main configuration");

                MessageBox.Show(e.Message, "General error occurred!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return configurationOk;
        }

        private static bool LoadLootLogConfiguration()
        {
            var configurationOk = false;
            try
            {
                LootLogConfiguration.Load();
                Items.LoadLocalization();
                configurationOk = true;
            }
            catch (YamlDotNet.Core.YamlException e)
            {
                _log.Fatal("Invalid loot log yaml file");
                MessageBox.Show(e.Message, "Yaml parsing error occurred. Invalid loot filter configuration.",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception e)
            {
                _log.Fatal(e, $"Unable to initialize Loot Log configuration");
                MessageBox.Show(e.Message, "General error occurred!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return configurationOk;
        }

        private static bool LoadLoggingConfiguration()
        {
            var configurationOk = false;

            try
            {
                var config = new NLog.Config.LoggingConfiguration();

                var logfile = new NLog.Targets.FileTarget("logfile")
                {
                    FileName = "logs\\log.txt",
                    ArchiveNumbering = NLog.Targets.ArchiveNumberingMode.DateAndSequence,
                    ArchiveOldFileOnStartup = true,
                    MaxArchiveFiles = 20
                };
                var logconsole = new NLog.Targets.ConsoleTarget("logconsole");

                // Rules for mapping loggers to targets
                config.AddRule(LogLevel.Debug, LogLevel.Fatal, logconsole);
                config.AddRule(LogLevel.Info, LogLevel.Fatal, logfile);

                // Apply config
                LogManager.Configuration = config;

                configurationOk = true;
            }
            catch (Exception e)
            {
                
                MessageBox.Show(e.Message, "General error occurred!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return configurationOk;
        }

        private static void Config(object sender, EventArgs e)
        {
            var _path = AppDomain.CurrentDomain.BaseDirectory;
            Process.Start(_path + "\\Config.yaml");
        }

        private static void LootFilter(object sender, EventArgs e)
        {
            var _path = AppDomain.CurrentDomain.BaseDirectory;
            Process.Start(_path + "\\ItemFilter.yaml");
        }

        private static void Dispose()
        {
            trayIcon.Visible = false;

            GameManager.Dispose();
            MapApi.Dispose();
            globalHook.Dispose();
            overlay.Dispose();

            if (backWorkOverlay.IsBusy)
            {
                backWorkOverlay.CancelAsync();
            }

            mutex.Dispose();
        }

        private static void Restart(object sender, EventArgs e)
        {
            Dispose();

            Application.Restart();
        }

        private static void Exit(object sender, EventArgs e)
        {
            Dispose();

            Application.Exit();
        }
    }
}
