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

namespace CommissioningChecklistGenerator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        public AVSystem.AVSystem Project { get; set; }
        public Tasks.Tasks Tasks { get; set; }

        public MainWindow()
        {
            Project = new AVSystem.AVSystem();
            Tasks = new Tasks.Tasks();
            MinHeight = 500;
            MinWidth = 600;
            DataContext = this;
            InitializeComponent();
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

            Project.Sources.ListChanged += OnListSizeChanged;
            Project.Destinations.ListChanged += OnListSizeChanged;
            Project.ControlledDevices.ListChanged += OnListSizeChanged;
            Project.UserInterfaces.ListChanged += OnListSizeChanged;
        }

        private void OnListSizeChanged(object? sender, ListChangedEventArgs e) {
            if (sender != null) {
                if (((BindingList<AVSystem.Device>)sender).Count == 0) {
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
                }
            }
        }

        private static void ShowNotImplementedMessage() {
            string messageBoxText = "This feature is not available yet.\r\nGo bother Ryan about implementing it.";
            string caption = "Not Implemented";
            MessageBoxButton button = MessageBoxButton.OK;
            MessageBoxImage icon = MessageBoxImage.Exclamation;

            MessageBox.Show(messageBoxText, caption, button, icon, MessageBoxResult.Yes);
        }

        private static MessageBoxResult ShowDeleteItemConfirmationMessage(DeviceType deviceType, Device device) {
            string messageBoxText = $"Are you sure you want to delete the following {deviceType}?\r\n{device.Name} | {device.Capability}\r\nIN:{device.Input} / OUT: {device.Output}\r\n{device.ControlMethod}: {device.ControlMethodDescription}";
            return MessageBox.Show(messageBoxText, $"Confirm Delete {deviceType}", MessageBoxButton.YesNo, MessageBoxImage.Question);
        }

        private void OnAudioConferencingChecked(object sender, RoutedEventArgs e) {
            bool? isChecked = ((System.Windows.Controls.CheckBox)sender).IsChecked;
            if (isChecked != null) Project.AudioConferencing = (bool)isChecked;
        }

        private void OnVideoConferencingChecked(object sender, RoutedEventArgs e)
        {
            bool? isChecked = ((System.Windows.Controls.CheckBox)sender).IsChecked;
            if (isChecked != null) Project.VideoConferencing = (bool)isChecked;
        }

        private void OnSoftConferencingChecked(object sender, RoutedEventArgs e)
        {
            bool? isChecked = ((System.Windows.Controls.CheckBox)sender).IsChecked;
            if (isChecked != null) Project.SoftConferencing = (bool)isChecked;
        }

        private void OnRoomCombiningChecked(object sender, RoutedEventArgs e)
        {
            bool? isChecked = ((System.Windows.Controls.CheckBox)sender).IsChecked;
            if (isChecked != null) Project.RoomCombining = (bool)isChecked;
        }

        private void OnEditChecklistClicked(object sender, RoutedEventArgs e)
        {
            //open checklist
            TaskList TaskListWindow = new TaskList(Tasks.DynamicTasks);
            TaskListWindow.ShowDialog();
        }

        private void OnImportSystemConfigurationClicked(object sender, RoutedEventArgs args)
        {
            //open file dialog
            //ShowNotImplementedMessage();
            var dialog = new Microsoft.Win32.OpenFileDialog();
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
                    StreamReader reader = new StreamReader(dialog.FileName);

                    try
                    {
                        string fileContents = reader.ReadToEnd();

                        if (fileContents != null)
                        {
                            AVSystem.AVSystem? project = JsonConvert.DeserializeObject<AVSystem.AVSystem>(fileContents);

                            if (project != null) {
                                //copy read in data to this object
                                project.CopyTo(Project);
                                //re-subscribe to list
                                Project.Sources.ListChanged += OnListSizeChanged;
                                Project.Destinations.ListChanged += OnListSizeChanged;
                                Project.ControlledDevices.ListChanged += OnListSizeChanged;
                                Project.UserInterfaces.ListChanged += OnListSizeChanged;
                                //string message = $"Parsed Project Assigned\r\n\r\n{project}";
                                //MessageBox.Show(message);
                            }
                            else { MessageBox.Show("Parsed Project Null"); }
                        }
                        else { MessageBox.Show("File Contents Null");  }
                    }
                    catch(Exception e) { MessageBox.Show(e.Message, $"Exception {e.Source}"); }
                    finally { reader.Close(); reader.Dispose(); }
                }
                catch(Exception e) { MessageBox.Show(e.Message, $"Exception {e.Source}"); }
            }
        }

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
                else { MessageBox.Show($"Please Select A {type}", $"Error: Edit {type}", MessageBoxButton.OK, MessageBoxImage.Warning); }
            }
            else { MessageBox.Show($"Add {type} Before Trying To Edit", $"Error: Edit {type}", MessageBoxButton.OK, MessageBoxImage.Warning); }
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
                else { MessageBox.Show($"Please Select A {type}", $"Error: Edit {type}", MessageBoxButton.OK, MessageBoxImage.Warning); }
            }
            else { MessageBox.Show($"Add {type} Before Trying To Edit", $"Error: Edit {type}", MessageBoxButton.OK, MessageBoxImage.Warning); }
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
            MessageBoxResult result = MessageBox.Show("Are you sure you want to clear the ENTIRE configuration?", "Clear Configuration?", MessageBoxButton.YesNo, MessageBoxImage.Question);
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

        private void OnWorkerCompleted(object? sender, RunWorkerCompletedEventArgs args, ProgressWindow dialog)
        {
            dialog.Close();
        }

        private void SaveGeneratedChecklist(XLWorkbook wb)
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
                wb.SaveAs(dialog.FileName);
            }
        }

        private void OnGenerateChecklist(object? sender, DoWorkEventArgs args, BackgroundWorker worker)
        {
           XLWorkbook? generationResult = Generator.GenerateChecklist(worker, Project, Tasks);
            if(generationResult != null) { SaveGeneratedChecklist(generationResult); }
        }

        private void OnProgressChanged(object? sender,  ProgressChangedEventArgs args, ProgressWindow dialog)
        {
            dialog.CurrentProgress.Value = args.ProgressPercentage;
            dialog.CurrentTask.Text = (string?)args.UserState;
        }

        private void OnExportChecklistClicked(object sender, RoutedEventArgs args)
        {
            ProgressWindow progressDialog = new ProgressWindow();
            
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (obj, e) => OnGenerateChecklist(obj, e, worker);
            worker.ProgressChanged += (obj, e) => OnProgressChanged(obj, e, progressDialog);
            worker.RunWorkerCompleted += (obj, e) => OnWorkerCompleted(obj, e, progressDialog);
            worker.WorkerReportsProgress = true;
            progressDialog.Show();
            worker.RunWorkerAsync();
        }

        private void OnExportSystemConfigurationClicked(object sender, RoutedEventArgs args)
        {
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
                        string json = JsonConvert.SerializeObject(Project, Formatting.Indented);
                        writer.Write(json);
                    }
                    catch (Exception e) { MessageBox.Show(e.Message, $"Exception {e.Source}"); }
                    finally { writer.Close(); writer.Dispose(); }
                }
                catch (Exception e) { MessageBox.Show(e.Message, $"Exception {e.Source}"); }
            }
        }
    }
}
