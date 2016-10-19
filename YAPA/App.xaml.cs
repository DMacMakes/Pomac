﻿using Autofac;
using Microsoft.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using YAPA.Contracts;
using YAPA.Shared;
using YAPA.WPF;

namespace YAPA
{
    public partial class App : Application, ISingleInstanceApp
    {
        private static IContainer Container { get; set; }
        private static IPluginManager PluginManager { get; set; }
        private static IThemeManager ThemeManager { get; set; }

        [STAThread]
        public static void Main()
        {
            if (SingleInstance<App>.InitializeAsFirstInstance("AdvancedJumpList"))
            {
                var application = new App();

                Container = ConfigureContainer();

                //Themes
                ThemeManager = new ThemeManager(Container, GetThemeMetas(), (ThemeManagerSettings)Container.Resolve(typeof(ThemeManagerSettings)));
                var themeUpdater = new ContainerBuilder();
                themeUpdater.RegisterInstance(ThemeManager).As<IThemeManager>().SingleInstance();
                themeUpdater.RegisterType(ThemeManager.GetActiveTheme()).As<IApplication>().SingleInstance();
                themeUpdater.Update(Container);

                //Plugins
                PluginManager = new PluginManager(Container, GetPluginMetas(), (PluginManagerSettings)Container.Resolve(typeof(PluginManagerSettings)));
                var updater = new ContainerBuilder();
                updater.RegisterInstance(PluginManager).As<IPluginManager>().SingleInstance();
                updater.Update(Container);
                PluginManager.InitPlugins();

                //Load theme
                Current.MainWindow = (Window)Container.Resolve<IApplication>();

                Current.MainWindow.Show();
                Current.MainWindow.Closed += MainWindow_Closed;

                application.Init();
                application.Run();

                SingleInstance<App>.Cleanup();
            }
        }

        private static void MainWindow_Closed(object sender, EventArgs e)
        {
            Current.Shutdown();
        }

        private static IContainer ConfigureContainer()
        {
            var builder = new ContainerBuilder();
            builder.RegisterInstance(new JsonYapaSettings()).As<ISettings>().SingleInstance();

            builder.RegisterType(typeof(PomodoroEngine)).As<IPomodoroEngine>().SingleInstance();

            builder.RegisterType(typeof(Timer)).As<ITimer>();
            builder.RegisterType(typeof(MusicPlayer)).As<IMusicPlayer>();

            builder.RegisterType(typeof(MainViewModel)).As<IMainViewModel>();

            builder.RegisterType(typeof(ThemeManagerSettings));
            builder.RegisterType(typeof(ThemeManagerSettingWindow));

            builder.RegisterType(typeof(PluginManagerSettings));

            builder.RegisterType(typeof(SettingManager)).As<ISettingManager>();

            var container = builder.Build();

            var updater = new ContainerBuilder();
            updater.RegisterInstance(container).As<IContainer>();
            updater.Update(container);

            return container;
        }

        private static IEnumerable<IPluginMeta> GetPluginMetas()
        {
            return from plugin in Assembly.GetExecutingAssembly().GetTypes()
                   where plugin.GetInterfaces().Contains(typeof(IPluginMeta))
                   select (IPluginMeta)Activator.CreateInstance(plugin);
        }

        private static IEnumerable<IThemeMeta> GetThemeMetas()
        {
            return from plugin in Assembly.GetExecutingAssembly().GetTypes()
                   where plugin.GetInterfaces().Contains(typeof(IThemeMeta))
                   select (IThemeMeta)Activator.CreateInstance(plugin);
        }

        public void Init()
        {
            InitializeComponent();
        }

        #region ISingleInstanceApp Members

        public bool SignalExternalCommandLineArgs(IList<string> args)
        {
            if (args == null || args.Count == 0)
            {
                return true;
            }
            return ((MainWindow)MainWindow).ProcessCommandLineArgs(args.ToArray());
        }

        #endregion
    }
}
