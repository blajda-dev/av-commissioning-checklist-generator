using System;
using System.ComponentModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CommissioningChecklistGenerator.AVSystem
{
    public class Device : INotifyPropertyChanged
    {
        ControlType _controlType;
        string? _controlMethodDescription;
        MediaType _capability;
        string? _name;
        string? _input;
        string? _output;


        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty("control_type", NullValueHandling = NullValueHandling.Ignore)]
        public ControlType ControlMethod { get { return _controlType; } set { _controlType = value; OnPropertyChanged("ControlMethod"); } }
        
        [JsonProperty("control_connector", NullValueHandling = NullValueHandling.Ignore)]
        public string ControlMethodDescription { get { return _controlMethodDescription; } set { _controlMethodDescription = value; OnPropertyChanged("ControlMethodDescription"); } }
        
        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get { return _name; } set { _name = value; OnPropertyChanged("Name"); } }

        [JsonProperty("input", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Input { get { return _input; } set { _input = value; OnPropertyChanged("Input"); } }

        [JsonProperty("output", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Output { get { return _output; } set { _output = value; OnPropertyChanged("Output"); } }

        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty("media_type", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public MediaType Capability { get { return _capability; } set { _capability = value; OnPropertyChanged("Capability"); } }

        public event PropertyChangedEventHandler? PropertyChanged;
        public Device() 
        {
            ControlMethod = ControlType.None;
            ControlMethodDescription = String.Empty;
            Name = "Generic Device";
            Input = "None";
            Output = "None";
            Capability = MediaType.AudioVideo;
        }

        public void CopyTo(Device recipient)
        {
            recipient.Input = Input;
            recipient.Output = Output;
            recipient.Capability = Capability;  
            recipient.Name = Name;
            recipient.ControlMethod = ControlMethod;
            recipient.ControlMethodDescription = ControlMethodDescription;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object? obj)
        {
            //return base.Equals(obj);
            if (obj != null)
            {
                if (obj.GetType() == typeof(Device))
                {
                    Device device = (Device)obj;

                    if (Name == device.Name && Input == device.Input && Output == device.Output && ControlMethod == device.ControlMethod && ControlMethodDescription == device.ControlMethodDescription && Capability == device.Capability) return true;
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
