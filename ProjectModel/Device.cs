using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows.Markup;
using CommissioningChecklistGenerator.Checklist;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CommissioningChecklistGenerator.ProjectModel
{
    public class Device : INotifyPropertyChanged
    {
        private const string DefaultDeviceName = "Generic Device";
        private const string DefaultDeviceManufacturer = "Device Manufacturer";
        private const string DefaultDeviceModel = "Device Model";
        private const string DefaultControlConnector = "";
        private const string DevicePrefixPattern = @"(?<prefix>[\w]+)-[\d]+";

        private BindingList<int> _controltype = new BindingList<int>();
        //[JsonProperty("control_type", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [JsonIgnore]
        public BindingList<int> ControlType { get { return _controltype; } set { _controltype = value; OnPropertyChanged("ControlType"); } }

        string _prefix = string.Empty;

        [JsonProperty("prefix", NullValueHandling = NullValueHandling.Ignore)]
        public string Prefix { get { return _prefix; } set { _prefix = value.Trim().ToUpper(); OnPropertyChanged("Prefix"); } }

        string _name = DefaultDeviceName;
        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name 
        { 
            get { return _name; } 
            set { 
                _name = value.Trim().ToUpper(); 
                OnPropertyChanged("Name");
                if (Regex.Match(_name, DevicePrefixPattern)?.Groups["prefix"]?.Value != null) { this.Prefix = Regex.Match(_name, DevicePrefixPattern).Groups["prefix"].Value; }
            } 
        }

        string _manufacturer = DefaultDeviceManufacturer;
        [JsonProperty("manufacturer", NullValueHandling = NullValueHandling.Ignore)]
        public string Manufacturer { get { return _manufacturer; } set { _manufacturer = value.Trim().ToUpper(); OnPropertyChanged("Manufacturer"); } }

        string _model = DefaultDeviceManufacturer;
        [JsonProperty("model", NullValueHandling = NullValueHandling.Ignore)]
        public string Model { get { return _model; } set { _model = value.Trim().ToUpper(); OnPropertyChanged("Model"); } }

        string _description = DefaultDeviceManufacturer;
        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        public string Description { get { return _description; } set { _description = value.Trim().ToUpper(); OnPropertyChanged("Description"); } }

        private BindingList<Capability> _capabilities = new BindingList<Capability>();
        [JsonProperty("capabilities", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public BindingList<Capability> Capabilities { get { return _capabilities; } set { _capabilities = value; OnPropertyChanged("Capabilities"); } }

        private BindingList<CommissioningTask> _tasks = new BindingList<CommissioningTask>();
        [JsonProperty("tasks", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public BindingList<CommissioningTask> Tasks { get { return _tasks; } set { _tasks = value; OnPropertyChanged("Tasks"); } }

        public event PropertyChangedEventHandler? PropertyChanged;
        public Device() 
        {
            Name = DefaultDeviceName;
            Capabilities = new BindingList<Capability>();
            ControlType = new BindingList<int>();
        }

        public Device(string name, string? make, string? model, string? description)
        {
            Name = name;
            if (make != null) Manufacturer = make;
            if (model != null) Model = model;
            if (description != null) Description = description;
        }

        public void CopyTo(Device recipient)
        {
            recipient.ControlType = this.ControlType;
            recipient.Capabilities = this.Capabilities;
            recipient.Name = this.Name;
            recipient.Manufacturer = this.Manufacturer;
            recipient.Model = this.Model;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object? obj)
        {
            if (obj != null)
            {
                if (obj.GetType() == typeof(Device))
                {
                    Device device = (Device)obj;

                    if (Name == device.Name) return true;
                    else return false;
                }
                else {  return false; }
            }
            else { return false; }
        }

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
