using AutoUpdaterDotNET;
using Gma.System.MouseKeyHook;
using MapAssist.Helpers;
using MapAssist.Settings;
using NLog;
using NLog.Config;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using YamlDotNet.Core;

namespace MapAssist
{
    internal static class Program
    {
        private static readonly string githubSha = "GITHUB_SHA";
        private static readonly string githubRepo = @"GITHUB_REPO";
        private static readonly string githubReleaseTag = "GITHUB_RELEASE_TAG";
        public static readonly bool isPrecompiled = githubSha.Length == 40;

        private static readonly string appName = "MapAssist";
        private static string messageBoxTitle = $"{appName} v{typeof(Program).Assembly.GetName().Version}";
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
                bool createdNew;
                mutex = new Mutex(true, appName, out createdNew);

                if (!createdNew)
                {
                    var rand = new Random();
                    var isGemActive = rand.NextDouble() < 0.05;

                    MessageBox.Show("An instance of " + appName + " is already running." + (isGemActive ? " Better go catch it!" : ""), messageBoxTitle, MessageBoxButtons.OK);
                    return;
                }

                LoadLoggingConfiguration();
                if (isPrecompiled)
                {
                    _log.Info($"Running from commit {githubSha} ({githubRepo} repo, {githubReleaseTag} release)");

                    CheckForUpdates();
                }
                else
                {
                    _log.Info($"Running a self-compiled build");
                }

                LoadMainConfiguration();
                LoadLootLogConfiguration();

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

                contextMenu.Items.AddRange(new ToolStripItem[]
                {
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

                configEditor = new ConfigEditor();
                backWorkOverlay.DoWork += new DoWorkEventHandler(RunOverlay);
                backWorkOverlay.WorkerSupportsCancellation = true;
                backWorkOverlay.RunWorkerAsync();

                GameManager.OnGameAccessDenied += (_, __) =>
                {
                    var message = $"MapAssist could not read {GameManager.ProcessName} memory. Please reopen MapAssist as an administrator.";
                    Dispose();
                    MessageBox.Show(message, $"{messageBoxTitle}: Error opening handle to {GameManager.ProcessName}", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                    Application.Exit();
                    Environment.Exit(0);
                };

                GameManager.MonitorForegroundWindow();

                Application.Run();
            }
            catch (Exception e)
            {
                ProcessException(e);
            }
        }

        private static void CheckForUpdates()
        {
            var xmlUrl = $"https://raw.githubusercontent.com/{githubRepo}/releases/{githubReleaseTag}.xml";

            void AutoUpdater_CheckForUpdateEvent(UpdateInfoEventArgs args)
            {
                if (args.Error == null && args.IsUpdateAvailable && args.Mandatory.Value && args.InstalledVersion < new System.Version(args.Mandatory.MinimumVersion))
                {
                    if (Thread.CurrentThread.GetApartmentState().Equals(ApartmentState.STA))
                    {
                        AutoUpdater.ShowUpdateForm(args);
                    }
                    else
                    {
                        var thread = new Thread(new ThreadStart(delegate { AutoUpdater.ShowUpdateForm(args); }));
                        thread.CurrentCulture = thread.CurrentUICulture = CultureInfo.CurrentCulture;
                        thread.SetApartmentState(ApartmentState.STA);
                        thread.Start();
                        thread.Join();
                    }
                }
            }

            AutoUpdater.OpenDownloadPage = true;
            AutoUpdater.CheckForUpdateEvent += AutoUpdater_CheckForUpdateEvent;
            AutoUpdater.ApplicationExitEvent += AutoUpdaterExit;
            AutoUpdater.Start(xmlUrl);
        }

        public static void RunOverlay(object sender, DoWorkEventArgs e)
        {
            using (overlay = new Overlay(configEditor))
            {
                overlay.Run();
            }
        }

        private static void ProcessException(Exception e)
        {
            var message = e.Message + Environment.NewLine + Environment.NewLine + e.StackTrace;

            if (e.GetType() == typeof(YamlException) && e.InnerException != null)
            {
                _log.Fatal(e.InnerException);

                message = e.Message + Environment.NewLine + Environment.NewLine +
                    e.InnerException.Message + Environment.NewLine + Environment.NewLine +
                    e.InnerException.StackTrace;
            }

            _log.Fatal(e);

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

        private static void LoadMainConfiguration()
        {
            MapAssistConfiguration.Load();
            MapAssistConfiguration.Loaded.RenderingConfiguration.InitialSize = MapAssistConfiguration.Loaded.RenderingConfiguration.Size;
        }

        private static void LoadLootLogConfiguration()
        {
            LootLogConfiguration.Load();
        }

        private static void LoadLoggingConfiguration()
        {
            ConfigurationItemFactory.Default.LayoutRenderers.RegisterDefinition("InvariantCulture", typeof(InvariantCultureLayoutRendererWrapper));
            var config = new LoggingConfiguration();

            var logfile = new NLog.Targets.FileTarget("logfile")
            {
                FileName = "logs\\log.txt",
                CreateDirs = true,
                ArchiveNumbering = NLog.Targets.ArchiveNumberingMode.DateAndSequence,
                ArchiveOldFileOnStartup = true,
                MaxArchiveFiles = 5,
                Encoding = System.Text.Encoding.UTF8,
                // Default layout with forcing invariant culture for messages, especially for stacktrace
                Layout = NLog.Layouts.Layout.FromString("${longdate}|${level:uppercase=true}|${logger}|${InvariantCulture:${message:withexception=true}}")
            };
            var logconsole = new NLog.Targets.ConsoleTarget("logconsole");

            // Rules for mapping loggers to targets
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, logconsole);
            config.AddRule(LogLevel.Info, LogLevel.Fatal, logfile);

            // Apply config
            LogManager.Configuration = config;
        }

        private static void ShowConfigEditor(object sender, EventArgs e)
        {
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

            AudioPlayer.Dispose();
            _log.Info("Disposed sound files");

            overlay.Dispose();
            _log.Info("Disposed Overlay");

            GameManager.Dispose();
            _log.Info("Disposed GameManager");

            MapApi.Dispose();
            _log.Info("Disposed MapApi");

            globalHook.Dispose();
            _log.Info("Disposed keyboard hook");

            trayIcon.Dispose();
            _log.Info("Disposed tray icon");

            if (backWorkOverlay.IsBusy)
            {
                backWorkOverlay.CancelAsync();
                _log.Info("Cancelled overlay background worker");
            }

            mutex.Dispose();
            _log.Info("Disposed mutex");

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

        private static void AutoUpdaterExit()
        {
            _log.Info("Exiting from outdated version");
            Dispose();

            Application.Exit();
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetProcessDPIAware();
    }
}
