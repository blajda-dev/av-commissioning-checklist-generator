using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Serilog;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CommissioningChecklistGenerator
{
    public static class ConfigurationReader
    {
        private const string Prefix = "[ConfigurationReader]";

        private static readonly string ConfigurationLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Constants.ApplicationName, Constants.ConfigurationFileName);
        private static readonly string ConfigurationTemporaryLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Constants.ApplicationName, String.Format("{0}.tmp", Constants.ConfigurationFileName));

        internal static async Task<bool> Initialize()
        {
            bool result = await ReadConfiguration(ConfigurationLocation);
            return result;
        }

        internal static async Task<bool> ReadConfiguration(string filepath) 
        {
            bool result = false;

            if (File.Exists(filepath)) {
                CancellationToken token = new CancellationToken();
                
                string? content = null;
                
                try { content = await File.ReadAllTextAsync(filepath, token); }
                catch (Exception e) { Log.Fatal(e, $"{Prefix} attempting to read data from config file @ {filepath}"); }

                if (content != null) {
                    Configuration? config = null;
                    try { config = JsonConvert.DeserializeObject<Configuration>(content); }
                    catch (Exception e) { Log.Fatal(e, $"{Prefix} attempting to deserialize json content -> {content}"); }

                    if (config != null) { 
                        Configuration.ApplicationConfiguration.ServerURL = config.ServerURL;
                        Log.Information($"{Prefix} successfully retrieved server url {Configuration.ApplicationConfiguration.ServerURL} from config file @ {filepath}");
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

        internal static async void WriteConfiguration() {
            try {
                string? json = null;
                try { json = JsonConvert.SerializeObject(Configuration.ApplicationConfiguration); }
                catch (Exception e) { Log.Fatal(e, $"{Prefix} attempting to serialize configuration"); }

                if (File.Exists(ConfigurationLocation)) {
                    try {
                        Log.Information($"{Prefix} writing to temp config file");
                        await File.WriteAllTextAsync(ConfigurationTemporaryLocation, json);
                        Log.Information($"{Prefix} completed writing to temp config file");
                        bool success = false;
                        try {
                            File.Move(ConfigurationTemporaryLocation, ConfigurationLocation, true);
                            success = true;
                        }
                        catch (Exception e) { Log.Fatal(e, $"{Prefix} attmempting to move temp config to permanent config"); }

                        if (success) { File.Delete(ConfigurationTemporaryLocation); }
                        
                        Log.Information($"{Prefix} {(success ? "deleting" : "not deleting")} temp config @ {ConfigurationTemporaryLocation}");
                    }
                    catch (Exception e) { Log.Fatal(e, $"{Prefix} attempting to write to temp config file"); }
                }
                else {
                    try {
                        Log.Information($"{Prefix} writing to new config file");
                        await File.WriteAllTextAsync(ConfigurationLocation, json);
                        Log.Information($"{Prefix} completed writing to new config file");
                    }
                    catch(Exception e) { Log.Fatal(e, $"{Prefix} attempting to write to new config file"); }
                }
            }
            catch(Exception e) { Log.Fatal(e, $"{Prefix} writing configuration to file"); }
        }
    }
}
