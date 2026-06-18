using CommissioningChecklistGenerator.ProjectModel;
using CommissioningChecklistGenerator.Checklist;
using CommissioningChecklistGenerator.Settings;
using Microsoft.Data.Sqlite;
using Dapper;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Windows;
using System.Linq;
using System.Collections.Immutable;
using Newtonsoft.Json.Bson;
using System.Threading.Tasks;
using System.Reflection;
using System.Windows.Documents;
using System.Xml.XPath;

namespace CommissioningChecklistGenerator.Database
{ 
    static class Querier
    {
        private static SqliteConnection Connection = new SqliteConnection();
        private const string Prefix = "[Querier]";

        public static void Initialize()
        {
            if (!Directory.Exists(Constants.ApplicationDataDirectory))
            {
                Log.Warning($"{Prefix} appdata directory @ {Constants.ApplicationDataDirectory} does not exist! creating it now...");
                try { Directory.CreateDirectory(Constants.ApplicationDataDirectory); }
                catch (Exception e) { Log.Fatal(e, $"{Prefix} could not create appdata directory @ {Constants.ApplicationDataDirectory} thats a big problem"); }
            }
            else { Log.Information($"{Prefix} appdata directory @ {Constants.ApplicationDataDirectory} exists! woo. dont need to create it."); }
        }

        private static void ExportEmbeddedDatabase()
        {
            Log.Warning($"{Prefix} export embedded database as last resort");
            Assembly assembly = Assembly.GetExecutingAssembly();
            using var resource = assembly.GetManifestResourceStream("CommissioningChecklistGenerator.Database." + DatabaseConstants.DesignTimeDatabaseFilename);
            using var file = new FileStream(DatabaseConstants.EmbeddedDatabaseFilePath, FileMode.Create, FileAccess.Write);

            resource?.CopyTo(file);
        }

        /// <summary>
        /// determines which database should be used
        /// </summary>
        /// <returns>the source of the database that should be used</returns>
        private static string DetermineDesiredDatabase()
        {
            string result = DatabaseConstants.EmbeddedDatabaseFilePath;
            //check the app data location to see if we have a file
            if (File.Exists(DatabaseConstants.LatestDatabaseFilePath)) { result = DatabaseConstants.LatestDatabaseFilePath; }
            //if not, see if the embedded database was previously exported
            else
            {
                //export the database that was embedded into the application if required
                if (!File.Exists(DatabaseConstants.EmbeddedDatabaseFilePath)) { ExportEmbeddedDatabase(); }
            }

            Log.Information($"{Prefix} use database @ {result}");

            return result;
        }

        public static async Task<bool> GetDatabaseConnectionState()
        {
            
            (bool result, string reason) = await ValidateDatabase(Connection);
            Log.Information($"{Prefix} retrieve database @ {DetermineDesiredDatabase()} connection state -> {(result ? Connection.State.ToString().ToLower() : "Invalid")}");
            return result;
        }

        public static async Task<(bool, string)> ValidateTemporaryDatabase(string path)
        {
            string source = GenerateConnectionString(path);

            SqliteConnection conn = new SqliteConnection(source);

            (bool result, string reason) = await Querier.ConnectToDatabase(conn);

            await Querier.DisconnectFromDatabase(conn);

            return (result, reason);
        }

        public static async Task<(bool, string)> ValidateDatabase(SqliteConnection connection)
        {
            bool valid = false;
            string reason = "unknown validation error";

            try
            {
                using(SqliteCommand cmd = new SqliteCommand("SELECT COUNT(*) FROM sqlite_master WHERE type-'table'", connection))
                {
                    object? result = await cmd.ExecuteScalarAsync();
                    if (result != null)
                    {
                        if (result != null) {
                            valid = true;
                            Log.Debug($"{Prefix} database file is a valid sqlite database");
                        }
                    }
                }
            }
            catch (Exception e) { 
                Log.Fatal(e, $"{Prefix} failed to get count from sqlite master table from database file @ {connection.DataSource}");
                reason = "exception attempting to query database for master tables";
            }
            
            if (valid) { Log.Information($"{Prefix} sqlite database verification successful"); }
            else 
            {
                reason = "no tables found within database, assuming file has been corrupted";
                Log.Fatal($"{Prefix} sqlite database @ {connection.DataSource} verification failed"); 
            }
            
            return (valid, reason);
        }

        public static string GenerateConnectionString(string path)
        {
            SqliteConnectionStringBuilder builder = new SqliteConnectionStringBuilder();
            builder.DataSource = path;
            builder.Mode = SqliteOpenMode.ReadOnly;

            return builder.ToString();
        }

        /// <summary>
        /// connects to the local database whether that be the embedded or latest
        /// </summary>
        /// <returns></returns>
        public static async Task<(bool, string)> ConnectToLocalDatabase()
        {
            Connection.ConnectionString = GenerateConnectionString(DetermineDesiredDatabase());
            return await ConnectToDatabase(Connection);
        }

        /// <summary>
        /// connects to the database from the connection provided
        /// </summary>
        public static async Task<(bool, string)> ConnectToDatabase(SqliteConnection conn)
        {
            bool result = await DisconnectFromDatabase(conn);

            bool open = false;
            string reason = "unknown reason";

            Log.Information($"{Prefix} disconnect from previous database {(result ? "succeeded" : "failed")}");

            Log.Information($"{Prefix} using database @ {conn.DataSource}");

            if (File.Exists(conn.DataSource))
            {
                try
                {
                    await conn.OpenAsync();

                    (open, reason) = await ValidateDatabase(conn);
                }
                catch (Exception e)
                {
                    Log.Fatal(e, $"{Prefix} failed to open database @ {conn.DataSource}");
                    MessageBox.Show(App.Window, $"Exception: {e.Message}\r\rHas caused a failure to open the desired database: {conn.DataSource}", "Failure To Open Database", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                if (open) { Log.Information($"{Prefix} database @ {conn.DataSource} is {Connection.State.ToString().ToLower()}!"); }
            }
            else {
                Log.Fatal($"{Prefix} database file @ {conn.DataSource} does not exist -> Cannot open a file that does not exist.");
                MessageBox.Show(App.Window, $"File Does Not Exist: {conn.DataSource}\r\rCannot open a file that does not exist.", "Failure To Open Database", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return (open, reason);
        }

        public static async Task<bool> DisconnectFromLocalDatabase()
        {
            return await DisconnectFromDatabase(Connection);
        }

        public static async Task<bool> DisconnectFromDatabase(SqliteConnection conn)
        {
            bool result = false;
            try
            {
                if (conn.State == ConnectionState.Open && conn.DataSource != String.Empty) { Log.Information($"{Prefix} disconnecting from database @ {conn.DataSource}"); }
                else { Log.Debug($"{Prefix} database source unavailable, must have already closed"); }
                
                await conn.CloseAsync();

                if (conn.State == ConnectionState.Closed && conn.DataSource != String.Empty) { Log.Information($"{Prefix} disconnected from database @ {conn.DataSource}"); }
                
                Log.Debug($"{Prefix} clearing connection pool @ {conn.DataSource}");
                
                SqliteConnection.ClearPool(conn);

                Log.Debug($"{Prefix} cleared connection pool @ {conn.DataSource}");
                
                result = true;
            }
            catch(Exception e) { Log.Error(e, $"{Prefix} while disconnecting from database"); }

            return result;
        }

        /// <summary>
        /// sends a command to a view within the database
        /// </summary>
        /// <param name="view">the targeted view</param>
        /// <param name="commandPart">conditions to get the desired rows from the view</param>
        /// <returns></returns>
        private static List<Dictionary<string, object>> QueryView(string view, string commandPart = "")
        {
            List<Dictionary<string, object>> results = new List<Dictionary<string, object>>();

            if (Connection.State == ConnectionState.Open) 
            {
                try
                {
                    string command = $"SELECT * FROM {view} {commandPart}";
                    Log.Debug($"{Prefix} executing: {command} on database @ {Connection.DataSource}");
                    results = Connection.Query(command).Select(row => new Dictionary<string, object>((IDictionary<string, object>)row)).ToList();
                }
                catch(Exception e) { Log.Error(e, $"{Prefix} while executing cmd: {commandPart} on view: {view}");  }
            }
            else { Log.Warning($"{Prefix} cannot query database @ {Connection.DataSource} -> connection: {Connection.State}");  }

            return results;
        }
        
        /// <summary>
        /// gets tasks from the required capability view and returns tasks that meet the required capabilities
        /// </summary>
        /// <param name="capabilities">the required capabilities for the desired tasks</param>
        /// <returns>a list of matching tasks</returns>
        public static List<CommissioningTask> GetTasksByCapability(List<Capability> requiredCapabilities)
        {
            List<CommissioningTask> gatheredTasks = new List<CommissioningTask>();

            List<string> args = new List<string>();
            requiredCapabilities.ForEach(capability => args.Add($"{DatabaseConstants.CapabilityIDColumnName} = {((int)capability)}"));

            List<Dictionary<string, object>> rows = QueryView(DatabaseConstants.TaskRequiredCapabilityView, String.Format("WHERE {0}", String.Join(" OR ", args)));

            rows.ForEach(row => {
                if (row.ContainsKey(DatabaseConstants.TaskNameColumnName) && row.ContainsKey(DatabaseConstants.TaskDescriptionColumnName))
                {
                    string? name = row[DatabaseConstants.TaskNameColumnName].ToString();
                    string? description = row[DatabaseConstants.TaskDescriptionColumnName].ToString();
                    if (name != null && description != null) { gatheredTasks.Add(new CommissioningTask(name, description)); }
                    else { Log.Warning($"cannot add a task that has no name or description! {name} - {description}"); }
                }
                else { Log.Information($"row does not have a valid task name or description"); }
            });

            return gatheredTasks;
        }

        /// <summary>
        /// retrieves the list of capabilities for the target device
        /// </summary>
        /// <param name="device">the device</param>
        /// <returns>a list of enums containing what the device is capable of<returns>
        public static List<Capability> GetDeviceCapabilities(Device device)
        {
            List<Capability> capabilities = new List<Capability>();

            try
            {
                string cmd = $"device_type_prefix = '{device.Prefix}' AND device_model_name IS NULL";

                if (device.Model != "") { cmd = $"device_model_name = '{device.Model}'"; }

                List<Dictionary<string, object>> rows = Querier.QueryView("device_effective_capability", $"WHERE {cmd}");

                if (rows.Count == 0)
                {
                    cmd = $"device_type_prefix = '{device.Prefix}' AND device_model_name IS NULL";
                    rows = Querier.QueryView("device_effective_capability", $"WHERE {cmd}");
                }

                Log.Debug($"{Prefix} found {rows.Count} capabilities for {device.Name} [{device.Prefix}] | {device.Model}");

                rows.ForEach(r =>
                {
                    if (r.ContainsKey(DatabaseConstants.CapabilityIDColumnName)) { capabilities.Add((Capability)r[DatabaseConstants.CapabilityIDColumnName]); }
                });
            }
            catch (Exception ex) { Log.Error(ex, $"{Prefix} getting capabilities for device: {device.Name} | {device.Prefix} | {device.Model} | {device.Manufacturer} | {device.Description}"); }

            return capabilities;
        }

        /// <summary>
        /// gets the available commissioning tasks for the device, if any are configured
        /// </summary>
        /// <param name="device">the device</param>
        /// <returns>a list of commissioning task objects</returns>
        public static List<CommissioningTask> GetCommissioningTasksForDevice(Device device)
        {
            List<CommissioningTask> tasks = new List<CommissioningTask>();
            try
            {           
                string view = DatabaseConstants.DeviceTypeTaskView;
                string desired = $"device_type_prefix = '{device.Prefix}'";
            
                if (device.Model != "") { 
                    view = DatabaseConstants.DeviceModelTaskView;
                    desired = $"device_model_name = '{device.Model}'";
                }

                Log.Debug($"{Prefix} get {desired} tasks from view: {view}");

                List<Dictionary<string, object>> rows = Querier.QueryView(view, $"WHERE {desired}");

                string type = "model";

                if (rows.Count == 0) {
                    type = "prefix";

                    Log.Debug($"{Prefix} failure to to find model specific tasks for {device.Name} [{device.Prefix}] -> {device.Model}");
                
                    desired = $"device_type_prefix = '{device.Prefix}'";
                    view = DatabaseConstants.DeviceTypeTaskView;

                    Log.Debug($"{Prefix} get {desired} tasks from view: {view}");
                
                   rows = Querier.QueryView(view, $"WHERE {desired}");
                }

                Log.Information($"{Prefix} found {rows.Count} {type} based tasks for device: {device.Name} [{device.Prefix}] -> ({device.Model} | {device.Manufacturer} | {device.Description})");

                rows.ForEach(r =>
                {
                    if (r.ContainsKey(DatabaseConstants.TaskNameColumnName) && r.ContainsKey(DatabaseConstants.TaskDescriptionColumnName)) 
                    {
                        tasks.Add(new CommissioningTask((string)r[DatabaseConstants.TaskNameColumnName], (string)r[DatabaseConstants.TaskDescriptionColumnName]));
                    }
                });

            }
            catch (Exception ex) { Log.Error(ex, $"{Prefix} getting commissioning tasks for device: {device.Name} | {device.Prefix} | {device.Model} | {device.Manufacturer} | {device.Description}"); }

            return tasks;
        }
    }
}
