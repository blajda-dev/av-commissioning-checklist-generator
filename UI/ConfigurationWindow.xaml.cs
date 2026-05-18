using System;
using System.Windows;
using System.Linq;
using System.ComponentModel;
using CommissioningChecklistGenerator.AVSystem;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using CommissioningChecklistGenerator.Checklist;
using ClosedXML.Excel;
using System.Windows.Controls;
using CommissioningChecklistGenerator.Drawings;
using Serilog;
using Serilog.Sinks.File;
using Microsoft.VisualBasic.FileIO;
using System.Threading.Tasks;
using CommissioningChecklistGenerator.Database;
using CommissioningChecklistGenerator.Extensions;
using System.Security.Policy;
using DocumentFormat.OpenXml.Presentation;
using System.Windows.Navigation;

namespace CommissioningChecklistGenerator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ConfigurationWindow : Window, INotifyPropertyChanged, IDataErrorInfo
    {
        private const string Prefix = "[ConfigurationWindow]";

        private string _url = CommissioningChecklistGenerator.Configuration.ApplicationConfiguration.ServerURL;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string URL
        {
            get { return this._url; }
            set {
                _url = value; 
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(URL))); 
            }
        }

        public string Error =>  String.Empty; 

        public string this[string columnName]
        {
            get
            {
                string result = String.Empty;

                if (columnName == nameof(URL)) { result = CommissioningChecklistGenerator.Configuration.ValidateServerURL(URL); }

                return result;
            }
        }


        public ConfigurationWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            this.ServerURL.Text = CommissioningChecklistGenerator.Configuration.ApplicationConfiguration.ServerURL;
            this.ServerURL.TextChanged += OnServerURLChanged;
            this.SaveConfiguration.IsEnabled = !Validation.GetHasError(this.ServerURL);
            this.ServerURLComplete.Content = CommissioningChecklistGenerator.Configuration.ApplicationConfiguration.ServerURL + CommissioningChecklistGenerator.Database.DatabaseConstants.ServerDatabaseFilePath;
        }

        private void OnServerURLChanged(object sender, TextChangedEventArgs e)
        {
            this.SaveConfiguration.IsEnabled = !Validation.GetHasError(this.ServerURL);
            this.ServerURLComplete.Content = CommissioningChecklistGenerator.Configuration.ApplicationConfiguration.ServerURL + CommissioningChecklistGenerator.Database.DatabaseConstants.ServerDatabaseFilePath;
        }

        private void OnSaveConfigurationClick(object sender, RoutedEventArgs e)
        {
            CommissioningChecklistGenerator.ConfigurationReader.WriteConfiguration();
            this.DialogResult = true;
            this.Close();
        }
    }
}
