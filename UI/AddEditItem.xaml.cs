using CommissioningChecklistGenerator.ProjectModel;
using System.Windows;

namespace CommissioningChecklistGenerator.UI
{
    /// <summary>
    /// Interaction logic for AddEditItem.xaml
    /// </summary>
    public partial class AddEditItem : Window
    {
        /*
        public AVSystem.Device thisDevice { get; set; }

        
        public AddEditItem(AVSystem.DeviceType type)
        {
            InitializeComponent();
            DataContext = this;
            Complete.Content = $"Add {type}";
            thisDevice = new();
            switch (type)
            {
                case AVSystem.DeviceType.Source:
                    Output.Visibility = Visibility.Hidden;
                    OutputLabel.Visibility = Visibility.Hidden;
                    break;
                case AVSystem.DeviceType.Destination:
                    Input.Visibility = Visibility.Hidden;
                    InputLabel.Visibility = Visibility.Hidden;
                    break;
                case AVSystem.DeviceType.UserInterface:
                    MediaTypeGroup.Visibility = Visibility.Hidden;
                    Input.Visibility = Visibility.Hidden;
                    InputLabel.Visibility = Visibility.Hidden;
                    Output.Visibility = Visibility.Hidden;
                    OutputLabel.Visibility = Visibility.Hidden;
                    break;
                case AVSystem.DeviceType.ControlledDevice:
                    MediaTypeGroup.Visibility = Visibility.Hidden;
                    Input.Visibility = Visibility.Hidden;
                    InputLabel.Visibility = Visibility.Hidden;
                    Output.Visibility = Visibility.Hidden;
                    OutputLabel.Visibility = Visibility.Hidden;
                    break;
            }
        }

        public AddEditItem(AVSystem.DeviceType type, AVSystem.Device device) : this(type)
        {
            if (device != null)
            {
                thisDevice.Name = device.Name;
                thisDevice.Input = device.Input;
                thisDevice.Output= device.Output;
                thisDevice.ControlMethod = device.ControlMethod;
                thisDevice.ControlMethodDescription = device.ControlMethodDescription;
                thisDevice.Capability = device.Capability;

                Complete.Content = "Save Changes";
            }

            switch (thisDevice.Capability)
            {
                case AVSystem.MediaType.Audio:
                    Audio.IsChecked = true;
                    break;
                case AVSystem.MediaType.Video:
                    Video.IsChecked = true;
                    break;
                case AVSystem.MediaType.AudioVideo:
                    AudioVideo.IsChecked = true;
                    break;
            }
        }

        private void OnAudioVideoIsChecked(object sender, RoutedEventArgs e)
        {
            thisDevice.Capability = MediaType.AudioVideo;
        }
        private void OnVideoIsChecked(object sender, RoutedEventArgs e)
        {
            thisDevice.Capability = MediaType.Video;
        }
        private void OnAudioIsChecked(object sender, RoutedEventArgs e) { 
            thisDevice.Capability = MediaType.Audio;
        }
        private void OnAddEditCompleted(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
        */
    }
}
