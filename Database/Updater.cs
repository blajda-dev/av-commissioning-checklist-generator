using CommissioningChecklistGenerator.Checklist;
using CommissioningChecklistGenerator.Extensions;
using CommissioningChecklistGenerator.UI;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Wordprocessing;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;

namespace CommissioningChecklistGenerator.Database
{
    public static class Updater
    {
        private const string Prefix = "[Updater]";
        private static readonly HttpClient client = new HttpClient();
        private const int maximumMoveAttepts = 5;
        private const int moveAttemptInterval = 500;
        private static Timer? databaseUpdateTimer;
        private static int progress;

        private static bool _isDownloading = false;
        public static bool IsDownloading 
        {
            get { return _isDownloading; }
            private set 
            {
                bool _isDownloadingPrevious = _isDownloading;
                bool _isIdlePrevious = IsIdle;

                _isDownloading = value;
                IsIdle = !value;

                if (_isDownloadingPrevious != _isDownloading) { StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(nameof(IsDownloading))); }
                if (_isIdlePrevious != IsIdle) { StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(nameof(IsIdle))); }
            }
        }

        public static bool IsIdle { get; private set; }

        public static event EventHandler<PropertyChangedEventArgs>? StaticPropertyChanged;

        /// <summary>
        /// configures the automatic update of the database
        /// </summary>
        public static void Initialize()
        {
            IsDownloading = false;

            if (!CommissioningChecklistGenerator.Configuration.ApplicationConfiguration.ServerURLValid) { Log.Warning($"{Prefix} server url not configured, database update requests will fail!"); }

            Log.Information($"{Prefix} get latest database @ server {CommissioningChecklistGenerator.Configuration.ApplicationConfiguration.ServerURL} every {DatabaseConstants.ServerUpdateInterval / (1000 * 60 * 60)} hours");
            databaseUpdateTimer = new Timer(OnDatabaseUpdateTimerExpired, null, 0, DatabaseConstants.ServerUpdateInterval);
            Log.Debug($"{Prefix} timer started");
        }

        /// <summary>
        /// generates the final url of the database based on the provided server url
        /// </summary>
        /// <returns></returns>
        private static string GenerateRemoteDatabaseLocation()
        {
            return CommissioningChecklistGenerator.Configuration.ApplicationConfiguration.ServerURL + DatabaseConstants.ServerDatabaseFilePath;
        }

        /// <summary>
        /// a callback timer that gets fired when the timer expires
        /// </summary>
        /// <param name="sender">a user object passed to the timer</param>
        private static void OnDatabaseUpdateTimerExpired(object? obj)
        {
            Log.Debug($"{Prefix} database download timer expired");

            DownloadDatabase();
        }

        /// <summary>
        /// event handler called when the database download is completed, prompting a reconnection to the database
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static async void DatabaseDownloadCompleted()
        {
            IsDownloading = false;

            await Querier.ConnectToDatabase();
        }

        /// <summary>
        /// downloads the latest database from the remote server defined by the settings, called by the auto-download timer, and manual download button
        /// </summary>
        public static void DownloadDatabase()
        {
            try {
                IsDownloading = true;
                PrepareForDatabaseDownload();
            }
            catch (Exception e) {
                Log.Warning(e, $"{Prefix} downloading new database"); 
            }
            finally { IsDownloading = false; }
        }

        /// <summary>
        /// configures the database download, and dispatches the operation to the ui thread for proper reporting context
        /// </summary>
        private static void PrepareForDatabaseDownload()
        {
            //reset the progress value
            progress = 0;

            if (Configuration.ApplicationConfiguration.ServerURLValid)
            {
                App.Current.Dispatcher.Invoke(async () => {
                    ProgressWindow window = new ProgressWindow(App.Window, "Auto-Download Database", $"Auto-Downloading Database from {Configuration.ApplicationConfiguration.ServerURL}", "Download Database");
                    Progress<ProgressUpdate> progress = new Progress<ProgressUpdate>(status => { window.UpdateProgress(status); });
                    window.Show();
                    await Task.Run(async () =>
                    {
                        Log.Information($"{Prefix} attempting to get latest database from server @ {Configuration.ApplicationConfiguration.ServerURL}");
                        await PerformDatabaseUpdate(progress);
                        DatabaseDownloadCompleted();
                    });
                    window.Close();
                });
            }
            else { Log.Warning($"{Prefix} server url is not valid, cannot download remote database"); }
        }

        /// <summary>
        /// connects to the database, downloads the database to a temp file, deletes the old database, and renames the temp file
        /// </summary>
        /// <param name="reporter">the progress reporter to update the ui thread</param>
        private static async Task PerformDatabaseUpdate(IProgress<ProgressUpdate> reporter)
        {
            try
            {
                using (HttpResponseMessage response = await client.GetAsync(GenerateRemoteDatabaseLocation(), HttpCompletionOption.ResponseHeadersRead))
                {
                    progress += 5;
                    reporter.Report(new ProgressUpdate(progress, $"Contacted Server @ {Configuration.ApplicationConfiguration.ServerURL}"));

                    Log.Debug($"{Prefix} able to contact server @ {CommissioningChecklistGenerator.Configuration.ApplicationConfiguration.ServerURL}");

                    if (response.IsSuccessStatusCode)
                    {
                        progress += 5;
                        reporter.Report(new ProgressUpdate(progress, "Success Response Requesting Database File"));
                        Log.Information($"{Prefix} succeeded in request database file from server @ {GenerateRemoteDatabaseLocation()}");
                        //wait for the checklist generator to be complete if running so we dont corrupt the database by writing to the file while in use
                        Log.Information($"{Prefix} waiting for generator to finish using database before beginning update");

                        await Generator.Idle.WaitAsync(new TimeSpan(0, 0, 5));

                        string? tempFile = await DownloadDatabaseToTemporaryFile(reporter, response);

                        bool renamed = false;

                        if (tempFile != null) { renamed = await RenameTemporaryDatabase(reporter, tempFile); }
                        else { Log.Fatal($"{Prefix} unable to move temporary database!"); }

                        Log.Information($"{Prefix} database file move/rename operations {(renamed ? "succeeded" : "failed")}");
                    }
                    else
                    {
                        Log.Error($"{Prefix} server responded with error: {(int)response.StatusCode} ({response.StatusCode}) when attempting to retrieve database @ {GenerateRemoteDatabaseLocation()}");

                        App.Current.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show($"Unable to download database from server: {Configuration.ApplicationConfiguration.ServerURL}\r\rThe server responded with {(int)response.StatusCode}: {response.ReasonPhrase}. The application was shipped with an embedded database which will be used as a last resort, but it may be limited or out of date.", "Download Failed");
                        });
                    }

                    Log.Information($"{Prefix} database update {(response.IsSuccessStatusCode ? "completed" : "failed")}");

                    progress = 100;
                    reporter.Report(new ProgressUpdate(progress, response.IsSuccessStatusCode ? "Completed" : "Failed"));
                }
            }
            catch(Exception e) { Log.Fatal(e, $"{Prefix} requesting database from server @ {GenerateRemoteDatabaseLocation()}"); }
        }

        /// <summary>
        /// writes the file from the remote server to a temp file
        /// </summary>
        /// <param name="reporter">the reporter for providing updates to the ui thread</param>
        /// <param name="response">the http response from the server</param>
        /// <returns></returns>
        private static async Task<string?> DownloadDatabaseToTemporaryFile(IProgress<ProgressUpdate> reporter, HttpResponseMessage response)
        {
            string? result = null;
            int startingProgress = progress;
            double availableProgress = 95.0 - progress;

            try
            {
                long? length = response.Content.Headers.ContentLength;

                if (length.HasValue)
                {
                    Stream stream = await response.Content.ReadAsStreamAsync();

                    long totalBytesRead = 0;
                    //the amount of bytes read in the current loop
                    int bytesRead = 0;
                    //a buffer to store small portions of the database as its downloaded
                    //stupid small buffer for testing
                    //byte[] dataBuffer = new byte[50];
                    //normal buffer
                    byte[] dataBuffer = new byte[8196];
                    double totalKilobytes = length.Value / 1024.0;

                    string filename = String.Format("download-{0}.db", DateTime.Now.ToString("dd-mm-yyyy_hh-mm-ss-tt"));

                    string temp = Path.Combine(Constants.ApplicationDataDirectory, filename);
                    FileStream fileStream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

                    try
                    {
                        Log.Debug($"{Prefix} allocating file stream object to write temp database to disk");
                        //create a file stream to allow us to write as we download

                        Log.Information($"{Prefix} starting download of latest database to temp file @ {temp}");
                        //download database to temp file and report the progress
                        while ((bytesRead = await stream.ReadAsync(dataBuffer, 0, dataBuffer.Length)) != 0)
                        {
                            await fileStream.WriteAsync(dataBuffer, 0, bytesRead);
                            //add the just read bytes to the total amount
                            totalBytesRead += bytesRead;
                            //scale the progress using our offset of the current progress and the number of steps left to get to the cap of 95
                            double currentRawProgress = (double)totalBytesRead / length.Value;
                            double currentScaledProgress = startingProgress + (currentRawProgress * availableProgress);

                            double totalKiloBytesRead = totalBytesRead / 1024.0;
                            Log.Debug($"{Prefix} {totalBytesRead:F2}kb read");
                            Log.Debug($"{Prefix} raw progress: {currentRawProgress} scaled progress: {currentScaledProgress} scaled progress int: {(int)currentScaledProgress}");

                            reporter.Report(new ProgressUpdate((int)currentScaledProgress, $"Downloading Database:\r{totalKiloBytesRead:F2}kB / {totalKilobytes:F2}kB bytes remaining"));

                            //uncommented to slow download for debugging purposes
                            //await Task.Delay(500);
                        }

                        result = temp;
                    }
                    catch (Exception e) { Log.Warning(e, $"{Prefix} attempting to download the temp database"); }
                    finally { fileStream.Close(); }
                }
            }
            catch (Exception e) { Log.Error(e, $"{Prefix} unable to get content stream"); }

            return result;
        }

        /// <summary>
        /// performs the deletion operation on the current database, and renames the temp database to the correct name
        /// </summary>
        /// <param name="reporter">the reporter for updating the ui thread</param>
        /// <param name="temp">the temp database filename</param>
        /// <returns></returns>
        private static async Task<bool> RenameTemporaryDatabase(IProgress<ProgressUpdate> reporter, string temp)
        {
            Querier.DisconnectFromDatabase();

            bool result = false;
            for (int attempt = 0; attempt < maximumMoveAttepts; attempt++)
            {
                //delete the old file first
                if (File.Exists(DatabaseConstants.LatestDatabaseFilePath))
                {
                    try {
                        File.Delete(DatabaseConstants.LatestDatabaseFilePath);
                        Log.Information($"{Prefix} deleted existing database @ {DatabaseConstants.LatestDatabaseFilePath}");
                    }
                    catch (Exception e) { Log.Fatal(e, $"{Prefix} unable to delete database @ {DatabaseConstants.LatestDatabaseFilePath}"); }
                }

                if (!File.Exists(DatabaseConstants.LatestDatabaseFilePath))
                {
                    try
                    {
                        File.Move(temp, DatabaseConstants.LatestDatabaseFilePath);
                        Log.Information($"{Prefix} renamed temp database @ {temp} -> {DatabaseConstants.LatestDatabaseFilePath}");
                        result = true;
                        progress += 5;
                        reporter.Report(new ProgressUpdate(progress, "Moved Temporary Database"));
                    }
                    catch (Exception e) { Log.Fatal(e, $"{Prefix} unable to rename/move database @ {temp} -> {DatabaseConstants.LatestDatabaseFileName}"); }
                }
                //if we succeed, exit the loop early
                if (result) { break; }
                else
                {
                    Log.Warning($"{Prefix} failed to migrate database @ {temp} -> {DatabaseConstants.LatestDatabaseFileName} attempt [{attempt} / {maximumMoveAttepts}]");
                    await Task.Delay(moveAttemptInterval);
                }
            }
            return result;
        }
    }
}
