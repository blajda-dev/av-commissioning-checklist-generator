using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CommissioningChecklistGenerator.Settings
{
    public class Configuration : IDataErrorInfo, INotifyPropertyChanged
    {
        private static readonly string ConfigurationLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Constants.ApplicationName, Constants.ConfigurationFileName);
        private static readonly string ConfigurationTemporaryLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Constants.ApplicationName, String.Format("{0}.tmp", Constants.ConfigurationFileName));

        public static Configuration ApplicationConfiguration { get; private set; }

        static Configuration()
        {
            ApplicationConfiguration = new Configuration();
        }

        private const string Prefix = "[Configuration]";

        [JsonIgnore]
        public string Error => String.Empty;

        [JsonIgnore]
        public bool ServerURLValid { get; private set; } = false;

        [JsonIgnore]
        private string _serverURL = String.Empty;
        
        [JsonProperty("url")]
        public string ServerURL
        {
            get { return _serverURL; }
            set
            {
                if (value != null && value != String.Empty)
                {
                    string previous = _serverURL;
                    _serverURL = value;
                    if (ValidateServerURL(_serverURL) == String.Empty) {
                        if (previous != _serverURL) { Log.Information($"{Prefix} configured server url changed -> {_serverURL}"); }
                        this.ServerURLValid = true;
                    }
                    else {
                        Log.Warning($"{Prefix} configured server url is invalid -> {_serverURL}");
                        this.ServerURLValid = false;
                    }
                    OnPropertyChanged(nameof(ServerURL));
                }
            }
        }

        [JsonIgnore] 
        private string _authenticationUrl = String.Empty;
        
        [JsonProperty("authenticator_url")]
        public string AuthenticationURL
        {
            get { return _authenticationUrl; }
            set
            {
                if (value != null && value != String.Empty)
                {
                    string previous = _authenticationUrl;
                    _authenticationUrl = value;
                    if (ValidateServerURL(_authenticationUrl) == String.Empty)
                    {
                        if (previous != _authenticationUrl) { Log.Information($"{Prefix} configured server url changed -> {_authenticationUrl}"); }
                    }
                    else
                    {
                        Log.Warning($"{Prefix} configured server url is invalid -> {_authenticationUrl}");
                    }
                    OnPropertyChanged(nameof(AuthenticationURL));
                }
            }
        }

        [JsonIgnore]
        private string _clientID = String.Empty;

        [JsonProperty("client_id")]
        public string ClientID {
            get { return _clientID; }
            set
            {
                if (value != null && value != String.Empty)
                {
                    string previous = _clientID;
                    _clientID = value;
                    if (previous != _clientID)
                    {
                        Log.Debug($"{Prefix} configured client id changed -> {_clientID}");
                        OnPropertyChanged(nameof(ClientID));
                    }
                }
            }
        }

        [JsonIgnore]
        private bool _enableSSO = false;

        [JsonProperty("use_sso")]
        public bool EnableSSO
        {
            get { return _enableSSO; }
            set
            {
                _enableSSO = value;
                Log.Debug($"{Prefix} sso authentication -> {(_enableSSO ? "enabled" : "disabled")}");
                OnPropertyChanged(nameof(EnableSSO));
            }
        }

        public string this[string columnName]
        {
            get
            {
                string result = String.Empty;

                if (columnName == nameof(ServerURL)) { result = ValidateServerURL(ServerURL); }

                return result;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string propertyName) { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }

        public Configuration() { this.ServerURL = String.Empty; }

        public static string ValidateServerURL(string url)
        {
            string result = String.Empty;

            bool valid = false;

            if (Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
            {
                if (uri != null)
                {
                    if (uri.Scheme == Uri.UriSchemeHttps) { valid = true; }
                }
            }

            if (!valid) { result = "please provide a valid https url"; }

            return result;
        }

        internal static async Task<bool> Initialize()
        {
            bool result = await ReadConfiguration(ConfigurationLocation);
            return result;
        }

        private static void UpdateApplicationConfiguration(Configuration config, string filepath)
        {
            Configuration.ApplicationConfiguration.ServerURL = config.ServerURL;
            Log.Debug($"{Prefix} {(config.ServerURL != String.Empty ? "successfully" : "failed to")} retrieved server url -> {config.ServerURL} from config file @ {filepath}");
            ApplicationConfiguration.AuthenticationURL = config.AuthenticationURL;
            Log.Debug($"{Prefix} {(config.AuthenticationURL != String.Empty ? "successfully" : "failed to")} retrieved auth url -> {config.AuthenticationURL} from config file @ {filepath}");
            ApplicationConfiguration.ClientID = config.ClientID;
            Log.Debug($"{Prefix} {(config.ClientID != String.Empty ? "successfully" : "failed to")} retrieved client id -> {config.ClientID} from config file @ {filepath}");
            ApplicationConfiguration.EnableSSO = config.EnableSSO;
            Log.Debug($"{Prefix} retrieved enable sso -> {config.EnableSSO} from config file @ {filepath}");
        }

        internal static async Task<bool> ReadConfiguration(string filepath)
        {
            bool result = false;

            if (File.Exists(filepath))
            {
                CancellationToken token = new CancellationToken();

                string? content = null;

                try { content = await File.ReadAllTextAsync(filepath, token); }
                catch (Exception e) { Log.Fatal(e, $"{Prefix} attempting to read data from config file @ {filepath}"); }

                if (content != null)
                {
                    Configuration? config = null;
                    try { config = JsonConvert.DeserializeObject<Configuration>(content); }
                    catch (Exception e) { Log.Fatal(e, $"{Prefix} attempting to deserialize json content -> {content}"); }

                    if (config != null)
                    {
                        UpdateApplicationConfiguration(config, filepath);
                        result = true;
                    }
                    else { Log.Error($"{Prefix} unable to assign new server url to configuration"); }
                }
                else { Log.Error($"{Prefix} cannot deserialize a null string, unable to update configuration"); }
            }
            else { Log.Warning($"{Prefix} no configuration file exists @ {filepath}; either it has been deleted, or this is the apps first startup on this device"); }

            if (result)
            {
                //get the latest database from the interwebs if possible
                CommissioningChecklistGenerator.Database.Updater.Initialize();
            }

            return result;
        }

        internal static async void WriteConfiguration()
        {
            try
            {
                string? json = null;
                try { json = JsonConvert.SerializeObject(Configuration.ApplicationConfiguration); }
                catch (Exception e) { Log.Fatal(e, $"{Prefix} attempting to serialize configuration"); }

                if (File.Exists(ConfigurationLocation))
                {
                    try
                    {
                        Log.Information($"{Prefix} writing to temp config file");
                        await File.WriteAllTextAsync(ConfigurationTemporaryLocation, json);
                        Log.Information($"{Prefix} completed writing to temp config file");
                        bool success = false;
                        try
                        {
                            File.Move(ConfigurationTemporaryLocation, ConfigurationLocation, true);
                            success = true;
                        }
                        catch (Exception e) { Log.Fatal(e, $"{Prefix} attmempting to move temp config to permanent config"); }

                        if (success) { File.Delete(ConfigurationTemporaryLocation); }

                        Log.Information($"{Prefix} {(success ? "deleting" : "not deleting")} temp config @ {ConfigurationTemporaryLocation}");
                    }
                    catch (Exception e) { Log.Fatal(e, $"{Prefix} attempting to write to temp config file"); }
                }
                else
                {
                    try
                    {
                        Log.Information($"{Prefix} writing to new config file");
                        await File.WriteAllTextAsync(ConfigurationLocation, json);
                        Log.Information($"{Prefix} completed writing to new config file");
                    }
                    catch (Exception e) { Log.Fatal(e, $"{Prefix} attempting to write to new config file"); }
                }
            }
            catch (Exception e) { Log.Fatal(e, $"{Prefix} writing configuration to file"); }
        }
    }
}
