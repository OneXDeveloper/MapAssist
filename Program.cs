using Gma.System.MouseKeyHook;
using MapAssist.Helpers;
using MapAssist.Settings;
using NLog;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace MapAssist
{
    internal static class Program
    {
        private static readonly string githubSha = "GITHUB_SHA";
        private static readonly string githubRunNumber = "GITHUB_RUN_NUMBER";
        private static readonly string appName = "MapAssist";
        private static string messageBoxTitle = $"{appName} v1.0.0";
        private static Mutex mutex = null;

        private static ConfigEditor configEditor;
        private static NotifyIcon trayIcon;
        private static Overlay overlay;
        private static BackgroundWorker backWorkOverlay = new BackgroundWorker();
        private static IKeyboardMouseEvents globalHook = Hook.GlobalEvents();
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            try
            {
                if (githubSha.Length == 40)
                {
                    messageBoxTitle += $".{githubRunNumber}";
                }

                bool createdNew;
                mutex = new Mutex(true, appName, out createdNew);

                if (!createdNew)
                {
                    var rand = new Random();
                    var isGemActive = rand.NextDouble() < 0.05;

                    MessageBox.Show("An instance of " + appName + " is already running." + (isGemActive ? " Better go catch it!" : ""), messageBoxTitle, MessageBoxButtons.OK);
                    return;
                }

                var configurationOk = LoadLoggingConfiguration() && LoadMainConfiguration() && LoadLootLogConfiguration();
                if (configurationOk)
                {
                    if (githubSha.Length == 40)
                    {
                        _log.Info($"Running from commit {githubSha}");
                    }

                    if (MapAssistConfiguration.Loaded.DPIAware)
                    {
                        SetProcessDPIAware();
                    }

                    Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                    Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);
                    AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);

                    try
                    {
                        if (!MapApi.StartPipedChild())
                        {
                            MessageBox.Show($"{messageBoxTitle}: Unable to start d2mapapi pipe", messageBoxTitle, MessageBoxButtons.OK);
                            return;
                        }
                    }
                    catch (Exception e)
                    {
                        _log.Fatal(e);
                        _log.Fatal(e, "Unable to start d2mapapi pipe.");

                        var message = e.Message + Environment.NewLine + Environment.NewLine + e.StackTrace;
                        MessageBox.Show(message, $"{messageBoxTitle}: Unable to start d2mapapi pipe", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    var contextMenu = new ContextMenuStrip();

                    var configMenuItem = new ToolStripMenuItem("Config", null, ShowConfigEditor);
                    var lootFilterMenuItem = new ToolStripMenuItem("Loot Filter", null, LootFilter);
                    var restartMenuItem = new ToolStripMenuItem("Restart", null, TrayRestart);
                    var exitMenuItem = new ToolStripMenuItem("Exit", null, TrayExit);
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

                    globalHook.KeyDown += (sender, args) =>
                    {
                        if (overlay != null)
                        {
                            overlay.KeyDownHandler(sender, args);
                        }
                    };

                    backWorkOverlay.DoWork += new DoWorkEventHandler(RunOverlay);
                    backWorkOverlay.WorkerSupportsCancellation = true;
                    backWorkOverlay.RunWorkerAsync();

                    GameManager.OnGameAccessDenied += (_, __) =>
                    {
                        var message = $"MapAssist could not read {GameManager.ProcessName} memory. Please reopen MapAssist as an administrator.";
                        MessageBox.Show(message, $"{messageBoxTitle}: Error opening handle to {GameManager.ProcessName}", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                        Dispose();
                        Application.Exit();
                        Environment.Exit(0);
                    };

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
            using (overlay = new Overlay())
            {
                overlay.Run();
            }
        }

        private static void ProcessException(Exception e)
        {
            _log.Fatal(e);

            var message = e.Message + Environment.NewLine + Environment.NewLine + e.StackTrace;
            MessageBox.Show(message, $"{messageBoxTitle}: Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            Application.Exit();
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ProcessException((Exception)e.ExceptionObject);
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
                MapAssistConfiguration.Loaded.RenderingConfiguration.InitialSize = MapAssistConfiguration.Loaded.RenderingConfiguration.Size;
                configurationOk = true;
            }
            catch (YamlDotNet.Core.YamlException e)
            {
                _log.Fatal(e);
                _log.Fatal(e, "Invalid yaml for configuration file");

                var message = e.InnerException != null ? e.InnerException.Message : e.Message;
                MessageBox.Show(message, $"{messageBoxTitle}: MapAssist configuration yaml parsing error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception e)
            {
                _log.Fatal(e, "Unknown error loading main configuration");
                MessageBox.Show(e.Message, $"{messageBoxTitle}: General error occurred", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return configurationOk;
        }

        private static bool LoadLootLogConfiguration()
        {
            var configurationOk = false;
            try
            {
                LootLogConfiguration.Load();
                configurationOk = true;
            }
            catch (YamlDotNet.Core.YamlException e)
            {
                _log.Fatal(e);
                _log.Fatal("Invalid item log yaml file");

                var message = e.InnerException != null ? e.InnerException.Message : e.Message;
                MessageBox.Show(message, $"{messageBoxTitle}: Item filter yaml parsing error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception e)
            {
                _log.Fatal(e, $"Unable to initialize Loot Log configuration");
                MessageBox.Show(e.Message, $"{messageBoxTitle}: General error occurred", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    CreateDirs = true,
                    ArchiveNumbering = NLog.Targets.ArchiveNumberingMode.DateAndSequence,
                    ArchiveOldFileOnStartup = true,
                    MaxArchiveFiles = 5
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
                MessageBox.Show(e.Message, $"{messageBoxTitle}: General error occurred", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return configurationOk;
        }

        private static void ShowConfigEditor(object sender, EventArgs e)
        {
            if (configEditor == null)
            {
                configEditor = new ConfigEditor();
            }

            if (configEditor.Visible)
            {
                configEditor.Activate();
            }
            else
            {
                configEditor.ShowDialog();
            }
        }

        private static void LootFilter(object sender, EventArgs e)
        {
            var _path = AppDomain.CurrentDomain.BaseDirectory;
            Process.Start(_path + "\\" + MapAssistConfiguration.Loaded.ItemLog.FilterFileName);
        }

        private static void Dispose()
        {
            _log.Info("Disposing");

            overlay.Dispose();
            GameManager.Dispose();
            MapApi.Dispose();
            globalHook.Dispose();
            trayIcon.Dispose();

            if (backWorkOverlay.IsBusy)
            {
                backWorkOverlay.CancelAsync();
            }

            mutex.Dispose();

            _log.Info("Finished disposing");
            LogManager.Flush();
        }

        private static void TrayRestart(object sender, EventArgs e)
        {
            _log.Info("Restarting from tray icon");
            Dispose();

            Application.Restart();
        }

        private static void TrayExit(object sender, EventArgs e)
        {
            _log.Info("Exiting from tray icon");
            Dispose();

            Application.Exit();
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetProcessDPIAware();
    }
}
