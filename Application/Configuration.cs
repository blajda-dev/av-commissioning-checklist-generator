using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace CommissioningChecklistGenerator
{
    public class Configuration : IDataErrorInfo, INotifyPropertyChanged
    {
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
                    if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) { valid = true; }
                }
            }

            if (!valid) { result = "please provide a valid http or https url"; }

            return result;
        }
    }
}
