﻿using log4net;
using Notification.Wpf;
using StatisticsAnalysisTool.Common;
using StatisticsAnalysisTool.Common.UserSettings;
using StatisticsAnalysisTool.Enumerations;
using StatisticsAnalysisTool.Network.Manager;
using StatisticsAnalysisTool.Notification;
using StatisticsAnalysisTool.ViewModels;
using StatisticsAnalysisTool.Views;
using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace StatisticsAnalysisTool;

public partial class App
{
    private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType);
    private MainWindowViewModel _mainWindowViewModel;
    private TrackingController _trackingController;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        var services = new ServiceCollection();
        
        ConfigureServices(services);

        // Erstelle den Dienstanbieter
        var serviceProvider = services.BuildServiceProvider();

        // Erstelle das Hauptfenster deiner Anwendung und übergebe den Dienstanbieter
        var mainWindow = new MainWindow(serviceProvider);
        mainWindow.Show();


        //////////////////////////////////////

        //base.OnStartup(e);

        //log4net.Config.XmlConfigurator.Configure();
        //Log.InfoFormat(LanguageController.CurrentCultureInfo, $"Tool started with v{Assembly.GetExecutingAssembly().GetName().Version}");

        //AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        //SettingsController.LoadSettings();
        //InitializeLanguage();

        //AutoUpdateController.RemoveUpdateFiles();
        //AutoUpdateController.AutoUpdate();

        //RegisterServices();

        //var mainWindow = new MainWindow(_mainWindowViewModel);
        //mainWindow.Show();
        //_mainWindowViewModel.InitMainWindowData();
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<MainWindowViewModel, MainWindowViewModel>();
        services.AddSingleton<ISatNotificationManager, SatNotificationManager>();
    }

    private void RegisterServices()
    {
        _mainWindowViewModel = new MainWindowViewModel();
        ServiceLocator.Register<MainWindowViewModel>(_mainWindowViewModel);

        var satNotifications = new SatNotificationManager(new NotificationManager(Current.Dispatcher));
        ServiceLocator.Register<SatNotificationManager>(satNotifications);

        _trackingController = new TrackingController(_mainWindowViewModel);
        ServiceLocator.Register<TrackingController>(_trackingController);
    }

    private static void InitializeLanguage()
    {
        if (LanguageController.InitializeLanguage())
        {
            return;
        }

        var dialogWindow = new DialogWindow(
            "LANGUAGE FILE NOT FOUND",
            "No language file was found, please add one and restart the tool!",
            DialogType.Error);
        var dialogResult = dialogWindow.ShowDialog();

        if (dialogResult is not true)
        {
            Current.Shutdown();
        }
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        try
        {
            Log.Fatal(nameof(OnUnhandledException), (Exception) e.ExceptionObject);
        }
        catch (Exception ex)
        {
            Log.Fatal(nameof(OnUnhandledException), ex);
        }
    }

    // Fixes a issue in the WPF clipboard handler.
    // It is necessary to handle the unhandled exception in the Application.DispatcherUnhandledException event.
    private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        if (e.Exception is COMException { ErrorCode: -2147221040 })
        {
            e.Handled = true;
        }
    }

    private void Application_SessionEnding(object sender, SessionEndingCancelEventArgs e)
    {
        _mainWindowViewModel.SaveLootLogger();
        SettingsController.SaveSettings();

        if (_mainWindowViewModel.IsTrackingActive)
        {
            _ = _trackingController.StopTrackingAsync();
        }
    }

    private void Application_Exit(object sender, ExitEventArgs e)
    {
        _mainWindowViewModel.SaveLootLogger();
        SettingsController.SaveSettings();

        if (_mainWindowViewModel.IsTrackingActive)
        {
            _ = _trackingController.StopTrackingAsync();
        }
    }
}