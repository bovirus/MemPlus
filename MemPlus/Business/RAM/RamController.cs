﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using MemPlus.Business.LOG;
using MemPlus.Views.Windows;
using Microsoft.VisualBasic.Devices;

namespace MemPlus.Business.RAM
{
    /// <summary>
    /// Sealed class containing methods and interaction logic in terms of RAM
    /// </summary>
    internal sealed class RamController
    {
        #region Variables
        /// <summary>
        /// The RamOptimizer object that can be called to clear memory
        /// </summary>
        private readonly RamOptimizer _ramOptimizer;
        /// <summary>
        /// The Timer object that will periodically update RAM usage statistics
        /// </summary>
        private readonly Timer _ramTimer;
        /// <summary>
        /// The Timer object that will automatically Optimize the RAM after a certain interval has passed
        /// </summary>
        private Timer _ramAutoOptimizeTimer;
        /// <summary>
        /// The LogController object that can be used to add logs
        /// </summary>
        private readonly LogController _logController;
        /// <summary>
        /// The ComputerInfo object that can be used to retrieve RAM usage statistics
        /// </summary>
        private readonly ComputerInfo _info;
        /// <summary>
        /// The list of processes that should be excluded from memory optimisation
        /// </summary>
        private List<string> _processExceptionList;
        /// <summary>
        /// An integer value representative of the percentage of RAM usage that should be reached before RAM optimisation should be called
        /// </summary>
        private double _autoOptimizeRamThreshold;
        /// <summary>
        /// The MainWindow object that called this class
        /// </summary>
        private readonly MainWindow _mainWindow;
        #endregion

        #region Properties
        /// <summary>
        /// Property containing how much RAM is being used
        /// </summary>
        internal double RamUsage { get; private set; }
        /// <summary>
        /// Property containing the percentage of RAM that is being used
        /// </summary>
        internal double RamUsagePercentage { get; private set; }
        /// <summary>
        /// Property containing the total amount of RAM available
        /// </summary>
        internal double RamTotal { get; private set; }
        /// <summary>
        /// Property containing how much RAM was saved during the last optimisation
        /// </summary>
        internal double RamSavings { get; private set; }
        /// <summary>
        /// Property displaying whether the RAM monitor is enabled or not
        /// </summary>
        internal bool RamMonitorEnabled { get; private set; }
        /// <summary>
        /// Property displaying whether the working set of processes should be emptied
        /// </summary>
        internal bool EmptyWorkingSets { get; set; }
        /// <summary>
        /// Property displaying whether the FileSystem cache should be cleared or not during memory optimisation
        /// </summary>
        internal bool ClearFileSystemCache { get; set; }
        /// <summary>
        /// Property displaying whether the standby cache should be cleared or not during memory optimisation
        /// </summary>
        internal bool ClearStandbyCache { get; set; }
        /// <summary>
        /// Property displaying whether automatic RAM optimisation should occur after a certain RAM usage percentage was reached
        /// </summary>
        internal bool AutoOptimizePercentage { get; set; }
        /// <summary>
        /// Property displaying whether or not RAM clearing statistics should be displayed
        /// </summary>
        internal bool ShowStatistics { get; set; }
        /// <summary>
        /// Property displaying whether RAM usage statistics should be displayed in the notifyicon
        /// </summary>
        internal bool ShowNotifyIconStatistics { get; set; }
        /// <summary>
        /// The last time automatic RAM optimisation was called in terms of RAM percentage threshold settings
        /// </summary>
        private DateTime _lastAutoOptimizeTime;
        #endregion

        /// <summary>
        /// Initialize a new RamController object
        /// </summary>
        /// <param name="mainWindow">The MainWindow object that called this initializer</param>
        /// <param name="ramUpdateTimerInterval">The interval for which RAM usage statistics should be updated</param>
        /// <param name="logController">The LogController object that can be used to add logs</param>
        internal RamController(MainWindow mainWindow, int ramUpdateTimerInterval, bool showStatistics, LogController logController)
        {
            _logController = logController ?? throw new ArgumentNullException(nameof(logController));
            _logController.AddLog(new ApplicationLog("Initializing RamController"));

            if (ramUpdateTimerInterval <= 0) throw new ArgumentException("Timer interval cannot be less than or equal to zero!");
            _mainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));

            RamSavings = 0;

            _info = new ComputerInfo();

            _ramOptimizer = new RamOptimizer(_logController);
            EmptyWorkingSets = true;
            ClearStandbyCache = true;
            ClearFileSystemCache = true;
            ShowStatistics = true;
            ShowNotifyIconStatistics = showStatistics;

            _ramTimer = new Timer();
            _ramTimer.Elapsed += OnTimedEvent;
            _ramTimer.Interval = ramUpdateTimerInterval;
            _ramTimer.Enabled = false;

            _logController.AddLog(new ApplicationLog("Done initializing RamController"));
        }

        /// <summary>
        /// Set the threshold percentage for automatic RAM optimisation
        /// </summary>
        /// <param name="threshold">The percentage threshold</param>
        internal void SetAutoOptimizeThreshold(double threshold)
        {
            if (threshold < 25) throw new ArgumentException("Threshold is dangerously low!");
            _autoOptimizeRamThreshold = threshold;
        }

        /// <summary>
        /// Enable or disable automatic timed RAM optimisation
        /// </summary>
        /// <param name="enabled">A boolean to indicate whether automatic RAM optimisation should occur or not</param>
        /// <param name="interval">The interval for automatic RAM optimisation</param>
        internal void AutoOptimizeTimed(bool enabled, int interval)
        {
            if (_ramAutoOptimizeTimer == null)
            {
                _ramAutoOptimizeTimer = new Timer();
                _ramAutoOptimizeTimer.Elapsed += RamAutoOptimizeTimerOnElapsed;
            }

            _ramAutoOptimizeTimer.Interval = interval;
            _ramAutoOptimizeTimer.Enabled = enabled;
        }

        /// <summary>
        /// Event that will be called when the timer interval was reached
        /// </summary>
        /// <param name="sender">The object that called this method</param>
        /// <param name="elapsedEventArgs">The ElapsedEventArgs</param>
        private async void RamAutoOptimizeTimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            await ClearMemory();
        }

        /// <summary>
        /// Set the list of processes that should excluded from RAM optimisation
        /// </summary>
        /// <param name="processExceptionList">The list of processes that should be excluded from RAM optimisation</param>
        internal void SetProcessExceptionList(List<string> processExceptionList)
        {
            _processExceptionList = processExceptionList;
        }

        /// <summary>
        /// Set the interval for the RAM Monitor updates
        /// </summary>
        /// <param name="interval">The amount of miliseconds before an update should occur</param>
        internal void SetRamUpdateTimerInterval(int interval)
        {
            _ramTimer.Interval = interval;
        }

        /// <summary>
        /// Enable RAM usage monitoring
        /// </summary>
        internal void EnableMonitor()
        {
            if (_ramTimer.Enabled) return;

            _ramTimer.Enabled = true;
            RamMonitorEnabled = true;

            UpdateRamUsage();
            UpdateGuiControls();

            _logController.AddLog(new ApplicationLog("The RAM monitor has been enabled"));
        }

        /// <summary>
        /// Disable RAM usage monitoring
        /// </summary>
        internal void DisableMonitor()
        {
            _ramTimer.Enabled = false;
            RamMonitorEnabled = false;

            _logController.AddLog(new ApplicationLog("The RAM monitor has been disabled"));
        }

        /// <summary>
        /// Update the GUI controls with the available RAM usage statistics
        /// </summary>
        private void UpdateGuiControls()
        {
            _mainWindow.Dispatcher.Invoke(() =>
            {
                string ramTotal = (RamTotal / 1024 / 1024 / 1024).ToString("F2") + " GB";
                string ramAvailable = (RamUsage / 1024 / 1024 / 1024).ToString("F2") + " GB";
                _mainWindow.CgRamUsage.Scales[0].Pointers[0].Value = RamUsagePercentage;
                _mainWindow.CgRamUsage.GaugeHeader = "RAM usage (" + RamUsagePercentage.ToString("F2") + "%)";
                _mainWindow.LblTotalPhysicalMemory.Content = ramTotal;
                _mainWindow.LblAvailablePhysicalMemory.Content = ramAvailable;

                if (ShowNotifyIconStatistics)
                {
                    string tooltipText = "MemPlus";
                    tooltipText += Environment.NewLine;
                    tooltipText += "Total physical memory: " + ramTotal;
                    tooltipText += Environment.NewLine;
                    tooltipText += "Available physical memory: " + ramAvailable;

                    _mainWindow.TbiIcon.ToolTipText = tooltipText;
                }
            });
        }

        /// <summary>
        /// Event that will be called when the timer interval was reached
        /// </summary>
        /// <param name="source">The calling object</param>
        /// <param name="e">The ElapsedEventArgs</param>
        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            _logController.AddLog(new ApplicationLog("RAM monitor timer has been called"));

            UpdateRamUsage();
            UpdateGuiControls();

            _logController.AddLog(new ApplicationLog("Finished RAM monitor timer"));
        }

        /// <summary>
        /// Clear all non-essential RAM
        /// </summary>
        /// <returns>A Task</returns>
        internal async Task ClearMemory()
        {
            _lastAutoOptimizeTime = DateTime.Now;
            _logController.AddLog(new ApplicationLog("Clearing RAM memory"));

            await Task.Run(async () =>
            {
                UpdateRamUsage();

                double oldUsage = RamUsage;

                if (EmptyWorkingSets)
                {
                    _ramOptimizer.EmptyWorkingSetFunction(_processExceptionList);
                    await Task.Delay(10000);
                }

                if (ClearFileSystemCache)
                {
                    _ramOptimizer.ClearFileSystemCache(ClearStandbyCache);
                }

                UpdateRamUsage();
                UpdateGuiControls();

                double newUsage = RamUsage;

                RamSavings = oldUsage - newUsage;
            });

            ClearingStatistcs();

            _logController.AddLog(new ApplicationLog("Done clearing RAM memory"));
        }

        /// <summary>
        /// Clear the working set of all processes, excluding the exclusion list
        /// </summary>
        /// <returns>A Task</returns>
        internal async Task ClearWorkingSets()
        {
            _logController.AddLog(new ApplicationLog("Clearing process working sets"));

            await Task.Run(async () =>
            {
                UpdateRamUsage();

                double oldUsage = RamUsage;

                _ramOptimizer.EmptyWorkingSetFunction(_processExceptionList);

                await Task.Delay(10000);

                UpdateRamUsage();
                UpdateGuiControls();

                double newUsage = RamUsage;

                RamSavings = oldUsage - newUsage;
            });

            ClearingStatistcs();

            _logController.AddLog(new ApplicationLog("Done clearing process working sets"));
        }

        /// <summary>
        /// Clear the FileSystem cache
        /// </summary>
        /// <returns>A Task</returns>
        internal async Task ClearFileSystemCaches()
        {
            _logController.AddLog(new ApplicationLog("Clearing FileSystem cache"));

            await Task.Run(() =>
            {
                UpdateRamUsage();

                double oldUsage = RamUsage;

                _ramOptimizer.ClearFileSystemCache(ClearStandbyCache);

                UpdateRamUsage();
                UpdateGuiControls();

                double newUsage = RamUsage;

                RamSavings = oldUsage - newUsage;
            });

            ClearingStatistcs();

            _logController.AddLog(new ApplicationLog("Done clearing FileSystem cache"));
        }

        /// <summary>
        /// Display a message about the last RAM Optimizer clearing statistics
        /// </summary>
        private void ClearingStatistcs()
        {
            double ramSavings = RamSavings / 1024 / 1024;
            string message;
            if (ramSavings < 0)
            {
                ramSavings = Math.Abs(ramSavings);
                _logController.AddLog(new RamLog("RAM usage increase: " + ramSavings.ToString("F2") + " MB"));
                message = "Looks like your RAM usage has increased with " + ramSavings.ToString("F2") + " MB!";
            }
            else
            {
                _logController.AddLog(new RamLog("RAM usage decrease: " + ramSavings.ToString("F2") + " MB"));
                message = "You saved " + ramSavings.ToString("F2") + " MB of RAM!";
            }

            if (!ShowStatistics) return;
            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (_mainWindow.Visibility)
            {
                default:
                    MessageBox.Show(message, "MemPlus", MessageBoxButton.OK, MessageBoxImage.Information);
                    break;
                case Visibility.Hidden when _mainWindow.TbiIcon.Visibility == Visibility.Visible:
                    _mainWindow.TbiIcon.ShowBalloonTip("MemPlus", message, BalloonIcon.Info);
                    break;
            }
        }

        /// <summary>
        /// Update RAM usage statistics
        /// </summary>
        private void UpdateRamUsage()
        {
            _logController.AddLog(new ApplicationLog("Updating RAM usage"));

            double total = Convert.ToDouble(_info.TotalPhysicalMemory);
            double usage = total - Convert.ToDouble(_info.AvailablePhysicalMemory);
            double perc = usage / total * 100;

            RamUsage = usage;
            RamUsagePercentage = perc;
            RamTotal = total;

            if (RamUsagePercentage >= _autoOptimizeRamThreshold && AutoOptimizePercentage)
            {
                double diff = (DateTime.Now - _lastAutoOptimizeTime).TotalSeconds;
                if (diff > 10)
                {
#pragma warning disable 4014
                    ClearMemory();
#pragma warning restore 4014
                }
            }

            _logController.AddLog(new ApplicationLog("Finished updating RAM usage"));
        }
    }
}
