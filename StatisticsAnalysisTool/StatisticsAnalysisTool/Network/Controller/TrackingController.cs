﻿using log4net;
using PcapDotNet.Base;
using StatisticsAnalysisTool.Common;
using StatisticsAnalysisTool.Enumerations;
using StatisticsAnalysisTool.GameData;
using StatisticsAnalysisTool.Models;
using StatisticsAnalysisTool.Network.Notification;
using StatisticsAnalysisTool.Network.Time;
using StatisticsAnalysisTool.ViewModels;
using StatisticsAnalysisTool.Views;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace StatisticsAnalysisTool.Network.Controller
{
    public class TrackingController
    {
        public EntityController EntityController;
        public DungeonController DungeonController;
        public CombatController CombatController;

        private const int _maxNotifications = 50;

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly MainWindowViewModel _mainWindowViewModel;
        private readonly MainWindow _mainWindow;
        private string _lastClusterHash;

        public ClusterInfo CurrentCluster {
            get;
            private set;
        }

        public TrackingController(MainWindowViewModel mainWindowViewModel, MainWindow mainWindow)
        {
            _mainWindowViewModel = mainWindowViewModel;
            _mainWindow = mainWindow;
            EntityController = new EntityController(_mainWindow, mainWindowViewModel);
            DungeonController = new DungeonController(this, _mainWindow, mainWindowViewModel);
            CombatController = new CombatController(this, _mainWindow, mainWindowViewModel);
        }

        public void RegisterEvents()
        {
            EntityController.OnHealthUpdate += DamageMeterUpdate;
        }

        public void UnregisterEvents()
        {
            EntityController.OnHealthUpdate -= DamageMeterUpdate;
        }
        
        #region Cluster
        
        public event Action<ClusterInfo> OnChangeCluster;

        public void SetNewCluster(MapType mapType, Guid? mapGuid, string clusterIndex, string mainClusterIndex)
        {
            CurrentCluster = WorldData.GetClusterInfoByIndex(clusterIndex, mainClusterIndex, mapType, mapGuid);

            if (!TryChangeCluster(CurrentCluster.Index, CurrentCluster.UniqueName))
            {
                return;
            }

            if (_mainWindowViewModel.IsDamageMeterResetByMapChangeActive)
            {
                CombatController.ResetDamageMeter();
            }

            Debug.Print($"[StateHandler] Changed cluster to: Index: '{CurrentCluster.Index}' UniqueName: '{CurrentCluster.UniqueName}' ClusterType: '{CurrentCluster.ClusterType}' MapType: '{CurrentCluster.MapType}'");
            OnChangeCluster?.Invoke(CurrentCluster);
        }

        private bool TryChangeCluster(string index, string mapName)
        {
            var newClusterHash = index + mapName;

            if (_lastClusterHash == newClusterHash)
            {
                return false;
            }

            _lastClusterHash = newClusterHash;
            return true;
        }

        #endregion
        
        #region Set Main Window values

        public void SetTotalPlayerFame(double value)
        {
            _mainWindowViewModel.TotalPlayerFame = value.ToString("N0", LanguageController.CurrentCultureInfo);
        }

        public void SetTotalPlayerSilver(double value)
        {
            _mainWindowViewModel.TotalPlayerSilver = value.ToString("N0", LanguageController.CurrentCultureInfo);
        }

        public void SetTotalPlayerReSpecPoints(double value)
        {
            _mainWindowViewModel.TotalPlayerReSpecPoints = value.ToString("N0", LanguageController.CurrentCultureInfo);
        }

        #endregion
        
        #region Notifications

        public void AddNotification(TrackingNotification item)
        {
            if (IsMainWindowNull() || _mainWindowViewModel.TrackingNotifications == null)
            {
                return;
            }

            if (_mainWindow.Dispatcher.CheckAccess())
            {
                _mainWindowViewModel.TrackingNotifications.Insert(0, item);
            }
            else
            {
                _mainWindow.Dispatcher.Invoke(delegate
                {
                    _mainWindowViewModel.TrackingNotifications.Insert(0, item);
                });
            }

            RemovesUnnecessaryNotifications();
        }

        public void RemovesUnnecessaryNotifications()
        {
            if (IsMainWindowNull() || _mainWindowViewModel.TrackingNotifications == null)
            {
                return;
            }

            try
            {
                while (true)
                {
                    if (_mainWindowViewModel.TrackingNotifications?.Count <= _maxNotifications)
                    {
                        break;
                    }

                    var dateTime = GetLowestDate(_mainWindowViewModel.TrackingNotifications);
                    if (dateTime != null)
                    {
                        var removableItem = _mainWindowViewModel.TrackingNotifications?.FirstOrDefault(x => x.DateTime == dateTime);
                        if (removableItem != null)
                        {
                            if (_mainWindow.Dispatcher.CheckAccess())
                            {
                                _mainWindowViewModel.TrackingNotifications.Remove(removableItem);
                            }
                            else
                            {
                                _mainWindow.Dispatcher.Invoke(delegate
                                {
                                    _mainWindowViewModel.TrackingNotifications.Remove(removableItem);
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(nameof(RemovesUnnecessaryNotifications), e);
            }
        }

        private static DateTime? GetLowestDate(ObservableCollection<TrackingNotification> items)
        {
            if (items.IsNullOrEmpty())
            {
                return null;
            }

            try
            {
                var lowestDate = items.Select(x => x.DateTime).Min();
                return lowestDate;
            }
            catch (ArgumentNullException e)
            {
                Log.Error(nameof(GetLowestDate), e);
                return null;
            }
        }
        
        #endregion

        #region Dungeon
        
        

        #endregion

        #region Trigger events

        public void DamageMeterUpdate(long objectId, GameTimeStamp timeStamp, double healthChange, double newHealthValue, EffectType effectType, EffectOrigin effectOrigin, long causerId, int causingSpellType)
        {
            CombatController.AddDamage(causerId, healthChange);
        }

        #endregion

        public bool IsMainWindowNull()
        {
            if (_mainWindow != null)
            {
                return false;
            }

            Log.Error($"{nameof(AddNotification)}: _mainWindow is null.");
            return true;
        }
    }
}