using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Windows;
using CommissioningChecklistGenerator.Checklist;
using CommissioningChecklistGenerator.Database;
using DocumentFormat.OpenXml.Office2021.DocumentTasks;
using DocumentFormat.OpenXml.Wordprocessing;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Serilog;

namespace CommissioningChecklistGenerator.AVSystem
{
    public class AVSystem : INotifyPropertyChanged
    {
        private const string Prefix = "[AVSystem]";

        bool _softConf;
        [JsonProperty("soft_conferencing")]
        public bool SoftConferencing { get { return _softConf; } set { _softConf = value; OnPropertyChanged("SoftConferencing"); } }

        bool _videoConf;
        [JsonProperty("video_conferencing")]
        public bool VideoConferencing { get { return _videoConf; } set { _videoConf = value; OnPropertyChanged("VideoConferencing"); } }

        bool _audioConf;
        [JsonProperty("audio_conferencing")]
        public bool AudioConferencing { get { return _audioConf; } set { _audioConf = value; OnPropertyChanged("AudioConferencing"); } }

        bool _roomCombine;
        [JsonProperty("room_combining")]
        public bool RoomCombining { get { return _roomCombine; } set { _roomCombine = value; OnPropertyChanged("RoomCombining"); } }

        BindingList<Device> _sources = new BindingList<Device>();
        [JsonIgnore]
        public BindingList<Device> Sources { get { return _sources; } private set { _sources = value; OnPropertyChanged("Sources"); } }

        BindingList<Device> _destinations = new BindingList<Device>();
        [JsonIgnore]
        public BindingList<Device> Destinations { get { return _destinations; } private set { _destinations = value; OnPropertyChanged("Destinations"); } }

        BindingList<Device> _interfaces = new BindingList<Device>();
        [JsonIgnore]
        public BindingList<Device> UserInterfaces { get { return _interfaces; } private set { _interfaces = value; OnPropertyChanged("UserInterfaces"); } }

        BindingList<Device> _controlled = new BindingList<Device>();
        [JsonIgnore]
        public BindingList<Device> ControlledDevices { get { return _controlled; } private set { _controlled = value; OnPropertyChanged("ControlledDevices"); } }

        BindingList<Device> _devices = new BindingList<Device>();
        [JsonProperty("devices")]
        public BindingList<Device> Devices { get { return _devices; } private set { _devices = value; OnPropertyChanged("Devices"); } }

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// the default constructor for an avsystem
        /// </summary>
        public AVSystem()
        {
            SoftConferencing = false;
            VideoConferencing = false;
            AudioConferencing = false;
            RoomCombining = false;
            Sources = new BindingList<Device>();
            Destinations = new BindingList<Device>();
            UserInterfaces = new BindingList<Device>();
            ControlledDevices = new BindingList<Device>();
            Devices = new BindingList<Device>();
        }

        /// <summary>
        /// clears all devices from the system and resets any flags
        /// </summary>
        public void Clear()
        { 
            this.ControlledDevices.Clear();
            this.Sources.Clear();
            this.Destinations.Clear();
            this.UserInterfaces.Clear();
            this.Devices.Clear();
        }

        /// <summary>
        /// copies an av system from one to another, without overwriting the bindinglist object itself and requiring re-subsribing to the lists
        /// </summary>
        /// <param name="recipient">the av system that we should copy to</param>
        public void CopyTo(AVSystem recipient)
        {
            foreach (Device device in Devices) { if (!recipient.Devices.Contains(device)) { recipient.Devices.Add(device); } }

            recipient.SoftConferencing = SoftConferencing;
            recipient.AudioConferencing = AudioConferencing;
            recipient.VideoConferencing = VideoConferencing;
            recipient.RoomCombining = RoomCombining;
        }

        /// <summary>
        /// retrieves the tasks for the system
        /// </summary>
        /// <returns>whether the operation was successful or not</returns>
        public bool GetCommissioningTasks()
        {
            return GetDevicesCommissioningTasks();
        }

        /// <summary>
        /// get the system wide commissioning tasks
        /// </summary>
        /// <returns>the system commissioning tasks</returns>
        public Dictionary<string, List<CommissioningTask>> GetSystemCommissioningTasks()
        {
            Dictionary<string, List<CommissioningTask>> result = new Dictionary<string, List<CommissioningTask>>();

            if (this.AudioConferencing)
            {
                //audio conference
                List<CommissioningTask> tasks = Querier.GetTasksByCapability(new List<Capability> { Capability.Conference, Capability.DTMF });
                //add the tasks
                result.Add("Audio Conference", tasks);
            }

            if (this.VideoConferencing)
            {
                //video conference
                List<CommissioningTask> tasks = Querier.GetTasksByCapability(new List<Capability> { Capability.Conference, Capability.DTMF, Capability.Camera });
                //add the tasks
                result.Add("Video Conference", tasks);
            }

            if (this.SoftConferencing)
            {
                //soft conference tasks
                List<CommissioningTask> tasks = Querier.GetTasksByCapability(new List<Capability> { Capability.Conference, Capability.Camera, Capability.USB });
                //add the tasks
                result.Add("Soft Conference", tasks);
            }

            if (this.RoomCombining)
            {
                //room combining tasks
                List<CommissioningTask> tasks = Querier.GetTasksByCapability(new List<Capability> { Capability.Combine });
                //add the tasks
                result.Add("Room Combine", tasks);
            }

            return result;
        }

        /// <summary>
        /// get the commissioning tasks for the devices in the system
        /// </summary>
        /// <returns>whether or not the operation succeeded</returns>
        private bool GetDevicesCommissioningTasks()
        {
            bool result = false;

            try
            {
                this.Devices.ToList()?.ForEach(d =>
                {
                    //get the capabilities for the device
                    List<Capability> capabilities = Querier.GetDeviceCapabilities(d);
                    //clear any previously discovered capabilities
                    d.Capabilities.Clear();
                    //assign those capabilities to the device
                    capabilities.ForEach(c => d.Capabilities.Add(c));
                    //get the tasks the device requires to be commissioned
                    List<CommissioningTask> tasks = Querier.GetCommissioningTasksForDevice(d);
                    //clear any previously discovered tasks
                    d.Tasks.Clear();
                    //add the tasks to the device
                    tasks.ForEach(t => d.Tasks.Add(t));
                    capabilities.ForEach(c => d.Capabilities.Add(c));

                    //sources
                    if (capabilities.Contains(Capability.Endpoint) && capabilities.Contains(Capability.Input))
                    {
                        if (!this.Sources.Contains(d))
                        {
                            Application.Current.Dispatcher.Invoke(() => { this.Sources.Add(d); });
                            Log.Debug($"{Prefix} device: {d.Name} -> {d.Manufacturer} : {d.Model} : {d.Description} | adding device to source list!");
                        }
                    }
                    //destinations
                    else if (capabilities.Contains(Capability.Endpoint) && capabilities.Contains(Capability.Output))
                    {
                        if (!this.Destinations.Contains(d))
                        {
                            Application.Current.Dispatcher.Invoke(() => { this.Destinations.Add(d); });
                            Log.Debug($"{Prefix} device: {d.Name} -> {d.Manufacturer} : {d.Model} : {d.Description} | adding device to destination list!");
                        }
                    }
                    //controlled devices
                    else if (capabilities.Contains(Capability.Controllable) && (!capabilities.Contains(Capability.Endpoint) && !capabilities.Contains(Capability.UserInterface)))
                    {
                        if (!this.ControlledDevices.Contains(d))
                        {
                            Application.Current.Dispatcher.Invoke(() => { this.ControlledDevices.Add(d); });
                            Log.Debug($"{Prefix} device: {d.Name} -> {d.Manufacturer} : {d.Model} : {d.Description} | adding device to generic controlled device list!");
                        }
                    }
                    //user interfaces
                    else if (capabilities.Contains(Capability.UserInterface))
                    {
                        if (!this.UserInterfaces.Contains(d))
                        {
                            Application.Current.Dispatcher.Invoke(() => { this.UserInterfaces.Add(d); });
                            Log.Debug($"{Prefix} device: {d.Name} -> {d.Manufacturer} : {d.Model} : {d.Description} | adding device to user interface list!");
                        }
                    }
                    //ignore
                    else { Log.Information($"{Prefix} device: {d.Name} -> {d.Manufacturer} : {d.Model} : {d.Description} | there are no commissionable tasks for this device!"); }

                    #if DEBUG
                    //print capabilities
                    Log.Debug($"{Prefix} device: {d.Name} is capable of: {String.Join(", ", capabilities)}");
                    //print tasks
                    Log.Debug($"{Prefix} device: {d.Name} is requires the following commisioning tasks:");
                    tasks.ForEach(t => Log.Debug($"{Prefix} {t.Name} -> {t.Description}"));
                    #endif
                });
                result = true;
            }
            catch (Exception e) { Log.Error(e, $"{Prefix} could not get commissoning tasks for device!"); }
        
            return result;
        }

        /// <summary>
        /// the event handler to notify subscribers when a property changes
        /// </summary>
        /// <param name="propertyName">the property that changed</param>
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// override the to string method to print the details of the system
        /// </summary>
        /// <returns>a string representing the system members</returns>
        public override string ToString()
        {
            return String.Format($"Features -> SoftConf: {SoftConferencing} | VideoConf: {VideoConferencing} | AudioConf: {AudioConferencing} | Combine/Divide: {RoomCombining} | Devices: {Devices.Count}");
        }
    }
}
