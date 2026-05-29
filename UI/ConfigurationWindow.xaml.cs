using System;
using System.Windows;
using System.Linq;
using System.ComponentModel;
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

namespace CommissioningChecklistGenerator.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ConfigurationWindow : Window, INotifyPropertyChanged, IDataErrorInfo
    {
        private const string Prefix = "[ConfigurationWindow]";

        private string _url = Settings.Configuration.ApplicationConfiguration.ServerURL;
        private string _authURL = Settings.Configuration.ApplicationConfiguration.AuthenticationURL;
        private string _clientID = Settings.Configuration.ApplicationConfiguration.ClientID;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string URL
        {
            get { return this._url; }
            set {
                _url = value; 
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(URL))); 
            }
        }

        public string AuthenticationURL
        {
            get { return this._authURL; }
            set
            {
                _authURL = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AuthenticationURL)));
            }
        }

        public string ClientID
        {
            get { return this._clientID; }
            set
            {
                _clientID = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ClientID)));
            }
        }

        public string Error =>  String.Empty; 

        public string this[string columnName]
        {
            get
            {
                string result = String.Empty;

                if (columnName == nameof(URL)) { result = Settings.Configuration.ValidateServerURL(URL); }

                return result;
            }
        }


        public ConfigurationWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            this.ServerURL.Text = Settings.Configuration.ApplicationConfiguration.ServerURL;
            this.ServerURL.TextChanged += OnServerURLChanged;
            this.AuthURL.TextChanged += OnServerURLChanged;
            this.ServerURLComplete.Content = Settings.Configuration.ApplicationConfiguration.ServerURL + CommissioningChecklistGenerator.Database.DatabaseConstants.ServerDatabaseFilePath;
            //assign the current configuration value for SSO to the checkbox
            this.EnableSSO.IsChecked = Settings.Configuration.ApplicationConfiguration.EnableSSO;
            //assign these afterwards so that bindings update correctly
            this.AuthenticationURL = Settings.Configuration.ApplicationConfiguration.AuthenticationURL;
            this.ClientID = Settings.Configuration.ApplicationConfiguration.ClientID;

            Log.Debug($"{Prefix} url -> {this.ServerURL.Text} sso -> {this.EnableSSO.IsChecked} auth url -> {this.AuthURL.Text} client -> {this.Client.Text}");

            //check the initial state because the checkbox won't trigger the event on initialization and we need to make sure the authentication members are in the correct state
            DisableAuthenticationMembers(this.EnableSSO.IsChecked);
        }

        private void OnServerURLChanged(object sender, TextChangedEventArgs e)
        {
            this.SaveConfiguration.IsEnabled = !Validation.GetHasError(this.ServerURL);
            this.ServerURLComplete.Content = Settings.Configuration.ApplicationConfiguration.ServerURL + CommissioningChecklistGenerator.Database.DatabaseConstants.ServerDatabaseFilePath;
        }

        private void OnSaveConfigurationClick(object sender, RoutedEventArgs e)
        {
            //update the static object after validation
            Settings.Configuration.ApplicationConfiguration.ServerURL = this.URL;
            Settings.Configuration.ApplicationConfiguration.AuthenticationURL = this.AuthenticationURL;
            Settings.Configuration.ApplicationConfiguration.ClientID = this.ClientID;
            Settings.Configuration.ApplicationConfiguration.EnableSSO = this.EnableSSO.IsChecked ?? false;

            //write to the configuration
            Settings.Configuration.WriteConfiguration();
            //return a positive result
            this.DialogResult = true;
            //close the window
            this.Close();
        }

        private void DisableAuthenticationMembers(bool? disable)
        {
            if (disable.HasValue) {
                this.AuthURLLabel.IsEnabled = disable.Value;
                this.AuthURL.IsEnabled = disable.Value;
                this.ClientLabel.IsEnabled = disable.Value;
                this.Client.IsEnabled = disable.Value;
            }
        }

        private void OnChecked(object sender, RoutedEventArgs e)
        {
            DisableAuthenticationMembers(((CheckBox)sender).IsChecked);
        }
    }
}
