﻿using System;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Windows;

using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;

using Microsoft.Practices.Prism.MefExtensions;

namespace Torshify.Radio
{
    public class RadioBootstrapper : MefBootstrapper
    {
        #region Fields

        private ILog _bootLogger;

        #endregion Fields

        #region Methods

        protected override void ConfigureAggregateCatalog()
        {
            base.ConfigureAggregateCatalog();

            AggregateCatalog.Catalogs.Add(new AssemblyCatalog(typeof(RadioBootstrapper).Assembly));
            AggregateCatalog.Catalogs.Add(new DirectoryCatalog(Environment.CurrentDirectory, "Torshify.Radio*.dll"));
        }

        protected override Microsoft.Practices.Prism.Logging.ILoggerFacade CreateLogger()
        {
            InitializeLogging();
            return new Log4NetFacade();
        }

        protected override DependencyObject CreateShell()
        {
            return Container.GetExportedValue<MainWindow>();
        }

        protected override void InitializeShell()
        {
            base.InitializeShell();

            Application.Current.MainWindow = (MainWindow)Shell;
            Application.Current.MainWindow.Show();
        }

        private void InitializeLogging()
        {
            var fileAppender = new RollingFileAppender();
            fileAppender.File = Path.Combine(AppConstants.LogFolder, "Torshify.Radio.log");
            fileAppender.AppendToFile = true;
            fileAppender.MaxSizeRollBackups = 10;
            fileAppender.MaxFileSize = 1024 * 1024;
            fileAppender.RollingStyle = RollingFileAppender.RollingMode.Size;
            fileAppender.StaticLogFileName = true;
            fileAppender.Layout = new PatternLayout("%date{dd MMM yyyy HH:mm} [%thread] %-5level %logger - %message%newline");
            fileAppender.Threshold = Level.Info;
            fileAppender.ActivateOptions();

            var consoleAppender = new CustomConsoleColorAppender();
            consoleAppender.AddMapping(
                new CustomConsoleColorAppender.LevelColors
                {
                    ForeColor = ColoredConsoleAppender.Colors.White | ColoredConsoleAppender.Colors.HighIntensity,
                    BackColor = ColoredConsoleAppender.Colors.Red | ColoredConsoleAppender.Colors.HighIntensity,
                    Level = Level.Fatal
                });
            consoleAppender.AddMapping(
                new CustomConsoleColorAppender.LevelColors
                {
                    ForeColor = ColoredConsoleAppender.Colors.Red | ColoredConsoleAppender.Colors.HighIntensity,
                    Level = Level.Error
                });
            consoleAppender.AddMapping(
                new CustomConsoleColorAppender.LevelColors
                {
                    ForeColor = ColoredConsoleAppender.Colors.Yellow | ColoredConsoleAppender.Colors.HighIntensity,
                    Level = Level.Warn
                });
            consoleAppender.AddMapping(
                new CustomConsoleColorAppender.LevelColors
                {
                    ForeColor = ColoredConsoleAppender.Colors.Green | ColoredConsoleAppender.Colors.HighIntensity,
                    Level = Level.Info
                });
            consoleAppender.AddMapping(
                new CustomConsoleColorAppender.LevelColors
                {
                    ForeColor = ColoredConsoleAppender.Colors.White | ColoredConsoleAppender.Colors.HighIntensity,
                    Level = Level.Info
                });
            consoleAppender.Layout = new PatternLayout("%date{dd MM HH:mm} %-5level - %message%newline");
            #if DEBUG
            consoleAppender.Threshold = Level.All;
            #else
            consoleAppender.Threshold = Level.Info;
            #endif
            consoleAppender.ActivateOptions();

            Logger root;
            root = ((Hierarchy)LogManager.GetRepository()).Root;
            root.AddAppender(consoleAppender);
            root.AddAppender(fileAppender);
            root.Repository.Configured = true;

            _bootLogger = LogManager.GetLogger("Bootstrapper");

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                Exception exception = (Exception)e.ExceptionObject;
                _bootLogger.Fatal(exception);
            };

            Application.Current.Dispatcher.UnhandledException += (s, e) =>
            {
                Exception exception = e.Exception;
                _bootLogger.Fatal(exception);
            };
        }

        #endregion Methods
    }
}