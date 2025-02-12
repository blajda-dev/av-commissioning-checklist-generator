using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using Newtonsoft.Json;

namespace CommissioningChecklistGenerator.AVSystem
{
    public class AVSystem : INotifyPropertyChanged
    {
        bool _softConf;
        bool _videoConf;
        bool _audioConf;
        bool _roomCombine;

        BindingList<Device> _sources;
        BindingList<Device> _destinations;
        BindingList<Device> _devices;
        BindingList<Device> _interfaces;

        [JsonProperty("soft_conferencing")]
        public bool SoftConferencing { get { return _softConf; } set { _softConf = value; OnPropertyChanged("SoftConferencing"); } }
        [JsonProperty("video_conferencing")]
        public bool VideoConferencing { get { return _videoConf; } set { _videoConf = value; OnPropertyChanged("VideoConferencing"); } }
        [JsonProperty("audio_conferencing")]
        public bool AudioConferencing { get { return _audioConf; } set { _audioConf = value; OnPropertyChanged("AudioConferencing"); } }
        [JsonProperty("room_combining")]
        public bool RoomCombining { get { return _roomCombine; } set { _roomCombine = value; OnPropertyChanged("RoomCombining"); } }

        [JsonProperty("sources")]
        public BindingList<Device> Sources { get { return _sources; } set { _sources = value; OnPropertyChanged("Sources"); } }
        [JsonProperty("destinations")]
        public BindingList<Device> Destinations { get { return _destinations; } set { _destinations = value; OnPropertyChanged("Destinations"); } }
        [JsonProperty("user_interfaces")]
        public BindingList<Device> UserInterfaces { get { return _interfaces; } set { _interfaces = value; OnPropertyChanged("UserInterfaces"); } }
        [JsonProperty("controlled_devices")]
        public BindingList<Device> ControlledDevices { get { return _devices; } set { _devices = value; OnPropertyChanged("ControlledDevices"); } }

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
        }

        public void CopyTo(AVSystem recipient)
        {
            foreach(Device device in Sources) { if(!recipient.Sources.Contains(device)) {  recipient.Sources.Add(device); } }
            foreach (Device device in Destinations) { if (!recipient.Destinations.Contains(device)) { recipient.Destinations.Add(device); } }
            foreach (Device device in ControlledDevices) { if (!recipient.ControlledDevices.Contains(device)) { recipient.ControlledDevices.Add(device); } }
            foreach (Device device in UserInterfaces) { if (!recipient.UserInterfaces.Contains(device)) { recipient.UserInterfaces.Add(device); } }

            recipient.SoftConferencing = SoftConferencing;
            recipient.AudioConferencing = AudioConferencing;
            recipient.VideoConferencing = VideoConferencing;
            recipient.RoomCombining = RoomCombining;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public override string ToString()
        {
            return String.Format($"System Features:\r\n\tSoftConf: {SoftConferencing} | VideoConf: {VideoConferencing}\r\n\tAudioConf: {AudioConferencing} | Combine/Divide: {RoomCombining}\r\n\tSources: {Sources.Count} | Destinations: {Destinations.Count}\r\n\tUser Interfaces: {UserInterfaces.Count} | Controlled Devices: {ControlledDevices.Count}");
        }
    }
}
