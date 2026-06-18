using System;
using System.Windows;
using System.Linq;
using System.ComponentModel;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using ClosedXML.Excel;
using System.Windows.Controls;
using System.Threading.Tasks;
using Serilog;
using Serilog.Sinks.File;
using Microsoft.VisualBasic.FileIO;
using CommissioningChecklistGenerator.ProjectModel;
using CommissioningChecklistGenerator.Drawings;
using CommissioningChecklistGenerator.Checklist;
using CommissioningChecklistGenerator.UI;
using CommissioningChecklistGenerator.Database;
using CommissioningChecklistGenerator.Extensions;
using CommissioningChecklistGenerator.Settings;

namespace CommissioningChecklistGenerator.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string Prefix = "[App]";
        public AVSystem Project { get; set; }

        public MainWindow()
        {
            ConfigureLogging();
            Log.Debug($"{Prefix} create new av system");
            Project = new AVSystem();
            Log.Debug($"{Prefix} set data context");
            DataContext = this;
            Log.Debug($"{Prefix} subscribe to window initialized event");
            this.ContentRendered += OnShown;
            this.Closing += OnClosing;
            Log.Debug($"{Prefix} initialize window");
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            /*
            //hide edit buttons by default
            EditSource.Visibility = Visibility.Hidden;
            EditDevice.Visibility = Visibility.Hidden;
            EditUserInterface.Visibility = Visibility.Hidden;
            EditDestination.Visibility = Visibility.Hidden;
            //hide delete buttons by default
            DeleteSource.Visibility = Visibility.Hidden;
            DeleteDevice.Visibility = Visibility.Hidden;
            DeleteUserInterface.Visibility = Visibility.Hidden;
            DeleteDestination.Visibility = Visibility.Hidden;
            */

            Log.Debug($"{Prefix} subscribe to list change events");
            Project.Sources.ListChanged += OnListSizeChanged;
            Project.Destinations.ListChanged += OnListSizeChanged;
            Project.ControlledDevices.ListChanged += OnListSizeChanged;
            Project.UserInterfaces.ListChanged += OnListSizeChanged;

            Log.Debug($"{Prefix} subscribe to drawing parsed event");
            Drawings.DrawingParser.DrawingParsed = OnParseSystemDrawingsCompleted;
        }

        private async Task Logout()
        {
            this.Hide();

            try
            {
                Log.Debug($"{Prefix} log out from authentication provider");
                await Authentication.OpenAuth.Logout();
            }
            catch (Exception err) { Log.Error(err, $"{Prefix} error logging out"); }
            finally
            {
                Log.Information($"{Prefix} calling shutdown");
                Application.Current.Shutdown();
            }
        }

        private async void OnClosing(object? sender, CancelEventArgs e)
        {
            if (Authentication.OpenAuth.IsAuthenticated)
            {
                MessageBoxResult result = MessageBox.Show(this, "You are currently logged in to the authentication server, would you like to log out?\r\rYou should do this if this machine is shared!", "Log Out?", MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    e.Cancel = true;
                    await Logout();
                }
                else { Log.Debug($"{Prefix} logout canceled"); }
            }
            else { Log.Information($"{Prefix} already logged out -> exiting"); }
        }

        private async void OnShown(object? sender, EventArgs e)
        {
            Log.Information($"{Prefix} started");
            //wait for the configuration file to be read before starting up the querier to get the latest database
            bool success = await Settings.Configuration.Initialize();

            if (!success) { 
                ConfigurationWindow configDialog = new ConfigurationWindow();
                configDialog.Owner = this;
                configDialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                configDialog.ShowDialog();
                
                CommissioningChecklistGenerator.Database.Updater.Initialize();
            }

            Log.Information($"{Prefix} initialized");
        }

        /// <summary>
        /// configures logging to the debug output and file on startup
        /// </summary>
        private void ConfigureLogging()
        {
            Log.Logger = new LoggerConfiguration()
            .WriteTo.Debug()
            .WriteTo.File(
                path: Path.Combine(Settings.Constants.ApplicationDataDirectory, "logs", "log-.log"),
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
            )
            #if DEBUG
            .MinimumLevel.Debug()
            #else
            .MinimumLevel.Information()
            #endif
            .CreateLogger();
            
            Log.Debug($"{Prefix} successfully configured logging");
        }

        /// <summary>
        /// shows a messagebox that the specified feature is not implemented
        /// </summary>
        private void ShowNotImplementedMessage()
        {
            string messageBoxText = "This feature is not available yet.\r\nGo bother Ryan about implementing it.";
            string caption = "Not Implemented";
            MessageBoxButton button = MessageBoxButton.OK;
            MessageBoxImage icon = MessageBoxImage.Exclamation;

            MessageBox.Show(this, messageBoxText, caption, button, icon, MessageBoxResult.Yes);
        }

        private void OnHelp(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(this, """
                This generator will use a remotely hosted Sqlite database to generate a commissioning checklist for a provided AV system.
                
                You need to provide the application a DXF or DWG drawing, and it will be automatically parsed. As long as your drawing uses the standard block and prefix system (see the engineering team standards handbook), the database will know how to retrieve tasks for any device present in the system that has been determined as "commissionable."

                The database will automatically be downloaded every time you start up the application, and once every hour while it is running. To adjust the server hosting the database, open the settings window; your changes will be written to disk.
                """, "Help", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// called when the server url button is clicked
        /// </summary>
        /// <param name="sender">the button that sent the event handler</param>
        /// <param name="e">the click event args</param>
        private void OnEditApplicationConfiguration(object sender, RoutedEventArgs e)
        {
            ConfigurationWindow configDialog = new ConfigurationWindow();
            configDialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            configDialog.Owner = this;
            configDialog.ShowDialog();
        }

        private void OnDownloadingDatabase(object? sender, EventArgs e)
        {
            if (sender != null)
            {
                this.DownloadDatabaseButton.IsEnabled = !((bool)sender);
            }
        }

        private async void OnDownloadDatabase(object sender, RoutedEventArgs e)
        {
            await Updater.DownloadDatabase();
        }

        /// <summary>
        /// an event handler called when any system list size changes to determine which buttons should be shown
        /// </summary>
        /// <param name="sender">the list that changed</param>
        /// <param name="e">the details of the change</param>
        private void OnListSizeChanged(object? sender, ListChangedEventArgs e) {
            if (sender != null) {
                if (((BindingList<Device>)sender).Count == 0) {
                    /*
                    if (SourceList.Items.Count == 0) { 
                        EditSource.Visibility = Visibility.Hidden;
                        DeleteSource.Visibility = Visibility.Hidden;
                    }
                    if (DestinationList.Items.Count == 0) { 
                        EditDestination.Visibility = Visibility.Hidden;
                        DeleteDestination.Visibility = Visibility.Hidden;
                    }
                    if (UserInterfaceList.Items.Count == 0) { 
                        EditUserInterface.Visibility = Visibility.Hidden;
                        DeleteUserInterface.Visibility = Visibility.Hidden;
                    }
                    if (ControlledDeviceList.Items.Count == 0) { 
                        EditDevice.Visibility = Visibility.Hidden;
                        DeleteDevice.Visibility = Visibility.Hidden;
                    }
                }
                else {
                    if (SourceList.Items.Count != 0) { 
                        EditSource.Visibility = Visibility.Visible;
                        DeleteSource.Visibility = Visibility.Visible;
                    }
                    if (DestinationList.Items.Count != 0) { 
                        EditDestination.Visibility = Visibility.Visible;
                        DeleteDestination.Visibility = Visibility.Visible;
                    }
                    if (UserInterfaceList.Items.Count != 0) { 
                        EditUserInterface.Visibility = Visibility.Visible;
                        DeleteUserInterface.Visibility = Visibility.Visible;
                    }
                    if (ControlledDeviceList.Items.Count != 0) { 
                        EditDevice.Visibility = Visibility.Visible;
                        DeleteDevice.Visibility = Visibility.Visible;
                    }
                    */
                }
            }
        }

        /*
        private void EditListItem(List<Device> list, AVSystem.DeviceType type, int selectedItem, Device itemToEdit)
        {
            if (list.Count != 0)
            {
                if (selectedItem >= 0 && selectedItem < list.Count)
                {
                    AddEditItem addEditWindow = new(type, itemToEdit);
                    //open add dialog
                    bool? result = addEditWindow.ShowDialog();
                    //changes were saved, and not discarded
                    if (result == true) addEditWindow.thisDevice.CopyTo(list[selectedItem]);
                }
                else { MessageBox.Show(this, $"Please Select A {type}", $"Error: Edit {type}", MessageBoxButton.OK, MessageBoxImage.Warning); }
            }
            else { MessageBox.Show(this, $"Add {type} Before Trying To Edit", $"Error: Edit {type}", MessageBoxButton.OK, MessageBoxImage.Warning); }
        }

        private void DeleteListItem(BindingList<Device> list, AVSystem.DeviceType type, int selectedItem)
        {
            if (list.Count != 0)
            {
                if (selectedItem >= 0 && selectedItem < list.Count)
                {
                    //open delete dialog & if delete confirmed
                    if (ShowDeleteItemConfirmationMessage(type, list[selectedItem]) == MessageBoxResult.Yes) list.RemoveAt(selectedItem);
                }
                else { MessageBox.Show(this, $"Please Select A {type}", $"Error: Edit {type}", MessageBoxButton.OK, MessageBoxImage.Warning); }
            }
            else { MessageBox.Show(this, $"Add {type} Before Trying To Edit", $"Error: Edit {type}", MessageBoxButton.OK, MessageBoxImage.Warning); }
        }


        private void OnAddSourceClicked(object sender, RoutedEventArgs e)
        {
            //open add dialog 
            AddEditItem addEditWindow = new (AVSystem.DeviceType.Source);
            bool? result = addEditWindow.ShowDialog();

            if (result == true) Project.Sources.Add(addEditWindow.thisDevice);
        }


        private void OnEditSourceClicked(object sender, RoutedEventArgs e)
        {
            EditListItem(Project.Sources.ToList(), DeviceType.Source, SourceList.SelectedIndex, (Device)SourceList.SelectedItem);
        }

        private void OnDeleteSourceClicked(object sender, RoutedEventArgs e) {
            DeleteListItem(Project.Sources, DeviceType.Source, SourceList.SelectedIndex);
        }

        private void OnAddDeviceClicked(object sender, RoutedEventArgs e)
        {
            //open add dialog 
            AddEditItem addEditWindow = new(AVSystem.DeviceType.ControlledDevice);
            bool? result = addEditWindow.ShowDialog();

            if (result == true) Project.ControlledDevices.Add(addEditWindow.thisDevice);
        }


        private void OnEditDeviceClicked(object sender, RoutedEventArgs e)
        {
            EditListItem(Project.ControlledDevices.ToList(), DeviceType.ControlledDevice, ControlledDeviceList.SelectedIndex, (Device)ControlledDeviceList.SelectedItem);
        }

        private void OnDeleteDeviceClicked(object sender, RoutedEventArgs e) 
        {
            DeleteListItem(Project.ControlledDevices, DeviceType.ControlledDevice, ControlledDeviceList.SelectedIndex);
        }

        private void OnAddDestinationClicked(object sender, RoutedEventArgs e)
        {
            //open add dialog 
            AddEditItem addEditWindow = new(AVSystem.DeviceType.Destination);
            bool? result = addEditWindow.ShowDialog();

            if (result == true) Project.Destinations.Add(addEditWindow.thisDevice);
        }

        private void OnEditDestinationClicked(object sender, RoutedEventArgs e)
        {
            EditListItem(Project.Destinations.ToList(), DeviceType.Destination, DestinationList.SelectedIndex, (Device)DestinationList.SelectedItem);
        }

        private void OnDeleteDestinationClicked(object sender, RoutedEventArgs e) 
        {
            DeleteListItem(Project.Destinations, DeviceType.Destination, DestinationList.SelectedIndex);
        }

        private void OnAddUserInterfaceClicked(object sender, RoutedEventArgs e)
        {
            //open add dialog 
            AddEditItem addEditWindow = new(AVSystem.DeviceType.UserInterface);
            bool? result = addEditWindow.ShowDialog();

            if (result == true) Project.UserInterfaces.Add(addEditWindow.thisDevice);
        }

        private void OnEditUserInterfaceClicked(object sender, RoutedEventArgs e)
        {
            EditListItem(Project.UserInterfaces.ToList(), DeviceType.UserInterface, UserInterfaceList.SelectedIndex, (Device)UserInterfaceList.SelectedItem);
        }

        private void OnDeleteUserInterfaceClicked(Object sender, RoutedEventArgs e) 
        {
            DeleteListItem(Project.UserInterfaces, DeviceType.UserInterface, UserInterfaceList.SelectedIndex);
        }

        private void OnClearSystemConfigurationClicked(object sender, RoutedEventArgs args)
        {
            MessageBoxResult result = MessageBox.Show(this, "Are you sure you want to clear the ENTIRE configuration?", "Clear Configuration?", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes) //if this is confirmed, we reset the entire config.
            {
                Project.UserInterfaces.Clear();
                Project.Sources.Clear();
                Project.Destinations.Clear();
                Project.ControlledDevices.Clear();

                Project.VideoConferencing = false;
                Project.AudioConferencing = false;
                Project.SoftConferencing = false;
                Project.RoomCombining = false;
            }
        }
        */

        /// <summary>
        /// shows a message box asking for confirmation before deleting the selected device from the system configuration
        /// </summary>
        /// <param name="deviceType">the devices type</param>
        /// <param name="device">the actual device object</param>
        /// <returns>a messagebox result containing the user response</returns>
        /*
        private static MessageBoxResult ShowDeleteItemConfirmationMessage(DeviceType deviceType, Device device) {
            string messageBoxText = $"Are you sure you want to delete the following {deviceType}?\r\n{device.Name} | {device.Capability}\r\nIN:{device.Input} / OUT: {device.Output}\r\n{device.ControlMethod}: {device.ControlMethodDescription}";
            return MessageBox.Show(this, messageBoxText, $"Confirm Delete {deviceType}", MessageBoxButton.YesNo, MessageBoxImage.Question);
        }
        */

        /// <summary>
        /// an event handler called when the checkbox is clicked
        /// </summary>
        /// <param name="sender">the checkbox that sent the event handler</param>
        /// <param name="e">the checkbox clicke event args</param>
        private void OnAudioConferencingChecked(object sender, RoutedEventArgs e) {
            bool? isChecked = ((global::System.Windows.Controls.CheckBox)sender).IsChecked;
            if (isChecked != null) Project.AudioConferencing = (bool)isChecked;
        }

        /// <summary>
        /// an event handler called when the checkbox is clicked
        /// </summary>
        /// <param name="sender">the checkbox that sent the event handler</param>
        /// <param name="e">the checkbox clicke event args</param>
        private void OnVideoConferencingChecked(object sender, RoutedEventArgs e)
        {
            bool? isChecked = ((global::System.Windows.Controls.CheckBox)sender).IsChecked;
            if (isChecked != null) Project.VideoConferencing = (bool)isChecked;
        }

        /// <summary>
        /// an event handler called when the checkbox is clicked
        /// </summary>
        /// <param name="sender">the checkbox that sent the event handler</param>
        /// <param name="e">the checkbox clicke event args</param>
        private void OnSoftConferencingChecked(object sender, RoutedEventArgs e)
        {
            bool? isChecked = ((global::System.Windows.Controls.CheckBox)sender).IsChecked;
            if (isChecked != null) Project.SoftConferencing = (bool)isChecked;
        }

        /// <summary>
        /// an event handler called when the checkbox is clicked
        /// </summary>
        /// <param name="sender">the checkbox that sent the event handler</param>
        /// <param name="e">the checkbox clicke event args</param>
        private void OnRoomCombiningChecked(object sender, RoutedEventArgs e)
        {
            bool? isChecked = ((global::System.Windows.Controls.CheckBox)sender).IsChecked;
            if (isChecked != null) Project.RoomCombining = (bool)isChecked;
        }

        /// <summary>
        /// toggles the visibility state of the manual system configuration pane
        /// </summary>
        /// <param name="sender">the object that called the event</param>
        /// <param name="args">args containing data regarding the event</param>
        private void OnToggleSystemConfigurationPanelClicked(object sender, RoutedEventArgs args)
        {
            ((Button)sender).Content = SystemConfiguration.Visibility == Visibility.Visible ? "Show System Configuration Panel" : "Hide System Configuration Panel";
            SystemConfiguration.Visibility = SystemConfiguration.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        /// <summary>
        /// an event handler called when the parse system button is clicked
        /// </summary>
        /// <param name="sender">the object that called the event</param>
        /// <param name="args">args containing data regarding the event</param>
        private async void OnParseSystemDrawingsClicked(object sender, RoutedEventArgs args)
        {
            ((Button)sender).IsEnabled = false;
            Log.Information($"{Prefix} attempting to open file dialog for selection of dxf document");
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.DefaultExt = ".dxf"; // Default file extension
            dialog.Filter = "AutoCAD Files (*.dwg,*.dxf)|*.dwg;*.dxf|AutoCAD DWG Files (*.dwg)|*.dwg|AutoCAD DXF Files (*.dxf)|*.dxf|All Files (*.*)|*.*"; // Filter files by extension

            // Show open file dialog box
            bool? open = dialog.ShowDialog();

            // Process open file dialog box results
            if (open == true)
            {
                Log.Information($"{Prefix} attempting to clear the current av system");
                Project.Clear();

                Log.Information($"{Prefix} attempting to create progress object to provide status of parsing dxf document @ {dialog.FileName}");
                ProgressWindow window = new ProgressWindow(this, UI.Icons.Resources.ConvertIcon, "Starting", $"Extracting commissionable devices from DXF drawing: {Path.GetFileName(dialog.FileName)}", "Parse DXF File");
                Progress<ProgressUpdate> progress = new Progress<ProgressUpdate>(status => { window.UpdateProgress(status); });
                window.Show();

                DrawingParsedEventArgs result = await DrawingParser.ParseSystemDrawing(progress, dialog.FileName);

                if (result.Success && result.System != null) { Project = result.System; }
                
                MessageBox.Show(this, $"{result.Reason}", $"Operation {(result.Success ? "Success" : "Failure")}", MessageBoxButton.OK, result.Success ? MessageBoxImage.Information : MessageBoxImage.Exclamation);
                window.Close();
            }
            ((Button)sender).IsEnabled = true;
        }

        /// <summary>
        /// an event handler that is called when the parse drawing operation is completed
        /// </summary>
        /// <param name="sender">the object that called the event</param>
        /// <param name="args">event args containing data regarding the parse operation</param>
        private void OnParseSystemDrawingsCompleted(DrawingParsedEventArgs args)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                if (args.Success && args.System != null)
                {
                    args.System.CopyTo(Project);
                    Log.Information($"{Prefix} successfully parsed dxf document!");
                    MessageBox.Show(this, "The CAD drawings were parsed, and an AV system has been created.\r\rYou may now save this configuration to a file for later use, or export the commissioning checklist immediately.", "Successfully Generated AV System!", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else {
                    Log.Information($"{Prefix} failure to parse dxf document!");
                    MessageBox.Show(this, $"An error occurred parsing the system drawings. We were unable to generate an AV system object.\r\rReason: {args.Reason}", "Failed To Parse CAD Drawings", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });
        }

        /// <summary>
        /// event handler thats triggered when import system from json is clicked. parses the json file and creates a system configuration from it
        /// </summary>
        /// <param name="sender">the sender of the event</param>
        /// <param name="args">event args</param>
        private void OnImportSystemConfigurationClicked(object sender, RoutedEventArgs args)
        {
            ((Button)sender).IsEnabled = false;
            //open file dialog
            //ShowNotImplementedMessage();
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.FileName = "config.json"; // Default file name
            dialog.DefaultExt = ".json"; // Default file extension
            dialog.Filter = "JSON Files (.json)|*.json"; // Filter files by extension

            // Show open file dialog box
            bool? result = dialog.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                // Open document
                try
                {
                    StreamReader reader = new StreamReader(dialog.FileName);

                    try
                    {
                        string fileContents = reader.ReadToEnd();

                        if (fileContents != null)
                        {
                            //reset the entire project so that we dont add devices to an existing system
                            Project.Clear();

                            AVSystem? project = JsonConvert.DeserializeObject<AVSystem>(fileContents);

                            if (project != null) {
                                //copy read in data to this object
                                project.CopyTo(Project);
                                //re-subscribe to list
                            }
                            else {
                                Log.Error($"{Prefix} failure to generate av system object from json file @ {dialog.FileName}");
                                MessageBox.Show(this, $"An error occured creating an AVSystem object to represent the configuration. Something must be wrong with the file.", "Failure Creating AVSystem", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                        else {
                            Log.Warning($"{Prefix} failure to read json file @ {dialog.FileName} -> file contents empty!");
                            MessageBox.Show(this, $"Unable to Parse Provided File:\r\r{dialog.FileName}\r\rFile Cannot Be Empty!!", "JSON File Empty", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                    catch(Exception e) {
                        Log.Error(e, $"{Prefix} failure to read json file @ {dialog.FileName}");
                        MessageBox.Show(this, e.Message, $"Exception {e.Source}", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    finally { 
                        reader.Close(); 
                        reader.Dispose(); 
                    }
                }
                catch(Exception e) {
                    Log.Error(e, $"{Prefix} failure to create stream reader for json file @ {dialog.FileName}");
                    MessageBox.Show(this, e.Message, $"Exception {e.Source}", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            
            ((Button)sender).IsEnabled = true;
        }

        /// <summary>
        /// saves the generated checklist file to disk
        /// </summary>
        /// <param name="wb">the excel workbook that was generated</param>
        private void SaveGeneratedChecklist(XLWorkbook wb)
        {
            if (wb.Worksheets.Count == 0)
            {
                MessageBox.Show(this, "An error has occurred while attempting to generate the checklist, resulting in no worksheets being generated.", "Cannot Save Checklist", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                var dialog = new Microsoft.Win32.SaveFileDialog();
                dialog.FileName = "PR-X_CommissioningChecklist.xlsx"; // Default file name
                dialog.DefaultExt = ".xlsx"; // Default file extension
                dialog.Filter = "Excel FIles (.xlsx)|*.xlsx"; // Filter files by extension

                // Show open file dialog box
                bool? result = dialog.ShowDialog();

                // Process open file dialog box results
                if (result == true)
                {
                    try { 
                        wb.SaveAs(dialog.FileName);
                        Log.Information($"{Prefix} exported excel checklist to disk @ {dialog.FileName}");
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(this, e.Message, $"Exception Encountered: Saving Checklist", MessageBoxButton.OK, MessageBoxImage.Error);
                        Log.Error(e, $"{Prefix} saving checklist to disk @ {dialog.FileName}");
                    }
                }
            }
        }

        /// <summary>
        /// an event handler called when the export checklist button is clicked, causing a background worker to start
        /// </summary>
        /// <param name="sender">the object that called the event</param>
        /// <param name="args">args that contain data related to the event handler</param>
        private async void OnExportChecklistClicked(object sender, RoutedEventArgs args)
        {
            ((Button)sender).IsEnabled = false;
            //need to update this to function with the new local database
            ProgressWindow window = new ProgressWindow(this, UI.Icons.Resources.BlackChecklistIcon, "Generate Commissioning Tasks For Devices", "Generating Formatted Excel Comissioning Checklist", "Generate Checklist");
            Progress<ProgressUpdate> progress = new Progress<ProgressUpdate>(status => { window.UpdateProgress(status); });
            window.Show();
            XLWorkbook? generationResult = await Generator.GenerateChecklist(progress, Project);
            if (generationResult != null) { SaveGeneratedChecklist(generationResult); }
            window.Close();
            ((Button)sender).IsEnabled = true;
        }

        /// <summary>
        /// an event handler called by the export system configuration button to save the configuration to a file for later use
        /// </summary>
        /// <param name="sender">the object that sent the event</param>
        /// <param name="args">event args related to the sender and event</param>
        private void OnExportSystemConfigurationClicked(object sender, RoutedEventArgs args)
        {
            ((Button)sender).IsEnabled = false;
            var dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.FileName = "config.json"; // Default file name
            dialog.DefaultExt = ".json"; // Default file extension
            dialog.Filter = "JSON FIles (.json)|*.json"; // Filter files by extension

            // Show open file dialog box
            bool? result = dialog.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                // Open document
                try
                {
                    StreamWriter writer = new StreamWriter(dialog.FileName);
                    try
                    {
                        string json = JsonConvert.SerializeObject(Project, Newtonsoft.Json.Formatting.Indented);
                        writer.Write(json);
                        Log.Information($"{Prefix} exported system config to disk @ {dialog.FileName}");
                    }
                    catch (Exception e) {
                        Log.Error(e, $"{Prefix} writing configuration to disk @ {dialog.FileName}");
                        MessageBox.Show(this, e.Message, $"Exception {e.Source}", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    finally { 
                        writer.Close(); 
                        writer.Dispose(); 
                    }
                }
                catch (Exception e) {
                    Log.Error(e, $"{Prefix} creating stream writer to facilitate configuration to disk @ {dialog.FileName}");
                    MessageBox.Show(this, e.Message, $"Exception {e.Source}", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            ((Button)sender).IsEnabled = true;
        }
    }
}
