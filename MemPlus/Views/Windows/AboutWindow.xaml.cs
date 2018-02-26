﻿using System;
using System.Diagnostics;
using System.Windows;
using MemPlus.Business.Classes.GUI;
using MemPlus.Business.Classes.LOG;

namespace MemPlus.Views.Windows
{
    /// <inheritdoc cref="Syncfusion.Windows.Shared.ChromelessWindow" />
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow
    {
        #region Variables
        /// <summary>
        /// The LogController object that can be used to add logs
        /// </summary>
        private readonly LogController _logController;
        #endregion

        /// <inheritdoc />
        /// <summary>
        /// Initialize a new AboutWindow
        /// </summary>
        /// <param name="logController">The LogController that can be used to add logs</param>
        public AboutWindow(LogController logController)
        {
            _logController = logController;
            _logController.AddLog(new ApplicationLog("Initializing AboutWindow"));

            InitializeComponent();
            ChangeVisualStyle();
            LoadProperties();

            _logController.AddLog(new ApplicationLog("Done initializing AboutWindow"));
        }

        /// <summary>
        /// Load the properties of the application
        /// </summary>
        private void LoadProperties()
        {
            _logController.AddLog(new ApplicationLog("Loading AboutWindow properties"));
            try
            {
                Topmost = Properties.Settings.Default.Topmost;
            }
            catch (Exception ex)
            {
                _logController.AddLog(new ApplicationLog(ex.Message));
                MessageBox.Show(ex.Message, "MemPlus", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            _logController.AddLog(new ApplicationLog("Done loading AboutWindow properties"));
        }

        /// <summary>
        /// Change the visual style of the controls, depending on the settings.
        /// </summary>
        private void ChangeVisualStyle()
        {
            _logController.AddLog(new ApplicationLog("Changing AboutWindow theme style"));
            StyleManager.ChangeStyle(this);
            _logController.AddLog(new ApplicationLog("Done changing AboutWindow theme style"));
        }

        /// <summary>
        /// Close the current window
        /// </summary>
        /// <param name="sender">The object that has initialized the method</param>
        /// <param name="e">The routed event arguments</param>
        private void BtnClose_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Open the file containing the license for MemPlus
        /// </summary>
        /// <param name="sender">The object that has initialized the method</param>
        /// <param name="e">The routed event arguments</param>
        private void BtnLicense_OnClick(object sender, RoutedEventArgs e)
        {
            _logController.AddLog(new ApplicationLog("Opening MemPlus license file"));
            try
            {
                Process.Start(AppDomain.CurrentDomain.BaseDirectory + "\\gpl.pdf");
            }
            catch (Exception ex)
            {
                _logController.AddLog(new ApplicationLog(ex.Message));
                MessageBox.Show(ex.Message, "MemPlus", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Open the CodeDead website
        /// </summary>
        /// <param name="sender">The object that has initialized the method</param>
        /// <param name="e">The routed event arguments</param>
        private void BtnCodeDead_OnClick(object sender, RoutedEventArgs e)
        {
            _logController.AddLog(new ApplicationLog("Opening CodeDead website"));
            try
            {
                Process.Start("https://codedead.com/");
            }
            catch (Exception ex)
            {
                _logController.AddLog(new ApplicationLog(ex.Message));
                MessageBox.Show(ex.Message, "MemPlus", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}