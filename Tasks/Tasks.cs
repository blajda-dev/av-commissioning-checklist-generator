using CommissioningChecklistGenerator.AVSystem;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace CommissioningChecklistGenerator.Tasks
{
    public enum TaskType
    {
        Source,
        Destination,
        Matrix,
        UserInterface,
        ControlledDevice,
        Conference,
        SoftConference,
        VideoConference,
        AudioConference,
        RoomCombining
    }

    public class Task
    {
        [JsonProperty("capability")]
        public MediaType Capability { get; set; }
        [JsonProperty("type")]
        public TaskType Type { get; set; }
        [JsonProperty("template_string")]
        public string? Template { get; set; }
        public string? Details { get; set; }

        public Task()
        {

        }
        public Task(string template, TaskType type, MediaType capability)
        {
            Capability = capability;
            Type = type;
            Template = template;
        }

        public void GenerateTaskDetails()
        {
            if (Template != null) Details = Template;
            else Details = "null";
        }

        public void GenerateTaskDetails(Dictionary<string, object> objects)
        {
            if (objects != null)
            {
                Regex namedParameterPattern = new Regex("(?<=\\{)(.*?)(?=\\})");

                string? temp = Template;

                if (Template != null) {
                    if (namedParameterPattern.IsMatch(Template)) {
                        MatchCollection matches = namedParameterPattern.Matches(Template);
                        matches.ToList().ForEach(match => {
                            if (match != null) {
                                //Debug.WriteLine(match.Groups[0]);
                                string contents = match.Groups[0].Value;
                                string[] args = contents.Split(".");
                                //Debug.WriteLine(String.Join(", ", args));
                                if (args.Length > 0) {  //index one should reflect the object associated with a key in the dictionary
                                    if (objects.Keys.Contains(args[0])) {
                                        object desiredObject = objects[args[0]];

                                        if (args.Length > 1) {
                                            foreach (string arg in args) {
                                                //Debug.WriteLine(arg);
                                                Type t = desiredObject.GetType();
                                                string? property = t.GetProperty(arg)?.GetValue(desiredObject)?.ToString();
                                                if (property != null) {
                                                    //Debug.WriteLine("{0} --> {1}", contents, property);
                                                    temp = temp?.Replace('{' + contents + '}', property);
                                                }
                                            }
                                        }
                                        else {
                                            //Debug.WriteLine("{0} --> {1}", contents, desiredObject);
                                            temp = temp?.Replace("{" + contents + "}", desiredObject.ToString());
                                        }
                                    }
                                }
                            }
                        });
                    }
                    Details = temp;
                    //Debug.WriteLine(temp);
                }
            }
        }
    }

    public class Tasks
    {
        public List<Task> FixedTasks { get; }
        public BindingList<Task> DynamicTasks { get; set; }

        public Tasks()
        {
            FixedTasks = new List<Task>();
            DynamicTasks = new BindingList<Task>();

            FixedTasks.Add(new Task("{Source.Input}: {Source.Name} has the correct edid applied to it", TaskType.Source, MediaType.Video));
            FixedTasks.Add(new Task("{Source.Input}: {Source.Name}'s input gain has been adjusted so that {Source.Name}'s playback level is equal to that of other sources if possible", TaskType.Source, MediaType.Video));

            FixedTasks.Add(new Task("{Source.Input}: {Source.Name} signal's gain has been set to acheive a nominal level of -15dBFS or 0dBU", TaskType.Source, MediaType.Audio));
            FixedTasks.Add(new Task("{Source.Input}: {Source.Name} if the volume level can be controlled independently, it is reset upon system startup and/or shutdown", TaskType.Source, MediaType.Audio));
            FixedTasks.Add(new Task("{Source.Input}: {Source.Name} if the volume level can be controlled independently, any controls provided on {UserInterfaces} function as expected", TaskType.Source, MediaType.Audio));

            FixedTasks.Add(new Task("{Source.Name} signal can be heard on {Destination.Name} | IN: {Source.Input} -> OUT: {Destination.Output}", TaskType.Matrix, MediaType.Audio));
            FixedTasks.Add(new Task("{Source.Name} signal can be seen on {Destination.Name} | IN: {Source.Input} -> OUT: {Destination.Output}", TaskType.Matrix, MediaType.Video));

            FixedTasks.Add(new Task("the incoming conference volume can be controlled independently", TaskType.Conference, MediaType.None));
            FixedTasks.Add(new Task("the system attempts to end any calls currently active when the shutdown routine is called", TaskType.Conference, MediaType.None));
            FixedTasks.Add(new Task("any microphone reinforced in the room when muted, is also muted at the far end", TaskType.Conference, MediaType.None));
            FixedTasks.Add(new Task("any privacy mute buttons provided on the {UserInterfaces} user interfaces completely mute audio to the far end", TaskType.Conference, MediaType.None));
            FixedTasks.Add(new Task("the privacy mute status between any provided user device and privacy mute buttons provided on {UserInterfaces} are synchronized", TaskType.SoftConference, MediaType.None));
            FixedTasks.Add(new Task("camera control buttons provided on the {UserInterfaces} user interfaces function as intended", TaskType.SoftConference, MediaType.None));

            FixedTasks.Add(new Task("when the system is not in a call [on hook], any dial keypad applicable appends the pressed digit or value to dialstrings displayed on {UserInterfaces}", TaskType.VideoConference, MediaType.None));
            FixedTasks.Add(new Task("when the system is in a call [off hook] any dial keypad applicable sends the appropriate dtmf tone to remote participants", TaskType.VideoConference, MediaType.None));
            FixedTasks.Add(new Task("camera control buttons provided on the {UserInterfaces} user interfaces function as intended", TaskType.VideoConference, MediaType.None));

            FixedTasks.Add(new Task("when the system is not in a call [on hook], any dial keypad applicable appends the pressed digit or value to dialstrings displayed on {UserInterfaces}", TaskType.AudioConference, MediaType.None));
            FixedTasks.Add(new Task("when the system is in a call [off hook] any dial keypad applicable sends the appropriate dtmf tone to remote participants", TaskType.AudioConference, MediaType.None));

            FixedTasks.Add(new Task("{Destination.Output}: {Destination.Name} reproduces audio without distortion throughout the range of volume any controllers may provide", TaskType.Destination, MediaType.Audio));
            FixedTasks.Add(new Task("{Destination.Output}: {Destination.Name} is muted when the appropriate command is sent by the control system", TaskType.Destination, MediaType.Audio));
            FixedTasks.Add(new Task("{Destination.Output}: {Destination.Name} is un-muted when the appropriate command is sent by the control system", TaskType.Destination, MediaType.Audio));
            FixedTasks.Add(new Task("{Destination.Output}: {Destination.Name} if the volume can be controlled independently, it is modified when the appropriate command is sent by the control system", TaskType.Destination, MediaType.Audio));
            FixedTasks.Add(new Task("{Destination.Output}: {Destination.Name} if the volume can be controlled independently, it is reset upon system startup and/or shutdown", TaskType.Destination, MediaType.Audio));

            FixedTasks.Add(new Task("{Destination.Output}: {Destination.Name} reproduces video without unexpected interruption", TaskType.Destination, MediaType.Video));
            FixedTasks.Add(new Task("{Destination.Output}: {Destination.Name} reproduces video with no visible distortion or defects", TaskType.Destination, MediaType.Video));
            FixedTasks.Add(new Task("{Destination.Output}: {Destination.Name} turns off when the appropriate command is sent by the control system", TaskType.Destination, MediaType.Video));
            FixedTasks.Add(new Task("{Destination.Output}: {Destination.Name} turns on when the appropriate command is sent by the control system", TaskType.Destination, MediaType.Video));
            FixedTasks.Add(new Task("{Destination.Output}: {Destination.Name} switches to the required input when the appropriate command is sent by the control system", TaskType.Destination, MediaType.Video));

            FixedTasks.Add(new Task("{Device.Name} can be controlled via {Device.ControlMethod} @ {Device.ControlMethodDescription}", TaskType.ControlledDevice, MediaType.None));
            FixedTasks.Add(new Task("{Device.Name}'s connection via {Device.ControlMethod} @ {Device.ControlMethodDescription} is stable", TaskType.ControlledDevice, MediaType.None));
            FixedTasks.Add(new Task("{Device.Name} responds to actions and controls provided on the {UserInterfaces} user interfaces", TaskType.ControlledDevice, MediaType.None));
            FixedTasks.Add(new Task("{Device.Name} acts in accordance with known automation events to the best of your current understanding of the SOW", TaskType.ControlledDevice, MediaType.None));

            FixedTasks.Add(new Task("{Device.Name} turns off when the appropriate command is sent", TaskType.ControlledDevice, MediaType.None));
            FixedTasks.Add(new Task("{Device.Name} turns off when the appropriate command is sent", TaskType.ControlledDevice, MediaType.None));

            FixedTasks.Add(new Task("{UserInterface.Name} ui connects to the appropriate controller via {UserInterface.ControlMethod} @ {UserInterface.ControlMethodDescription}", TaskType.UserInterface, MediaType.None));
            FixedTasks.Add(new Task("{UserInterface.Name}'s connection via {UserInterface.ControlMethod} @ {UserInterface.ControlMethodDescription} to controller is stable", TaskType.UserInterface, MediaType.None));
            FixedTasks.Add(new Task("{UserInterface.Name}'s page navigation is independent of other user interfaces where applicable", TaskType.UserInterface, MediaType.None));
            FixedTasks.Add(new Task("{UserInterface.Name} displays live & accurate feedback where applicable", TaskType.UserInterface, MediaType.None));
            FixedTasks.Add(new Task("{UserInterface.Name} returns to the splash screen and hides any applicable subpages when the system is shutdown", TaskType.UserInterface, MediaType.None));
            FixedTasks.Add(new Task("{UserInterface.Name} navigates to the appropriate page & subpage combination when applicable automation events occur", TaskType.UserInterface, MediaType.None));

            FixedTasks.Add(new Task("if applicable, audio/video sources from all combined rooms are available to touchpanels located in affected areas", TaskType.RoomCombining, MediaType.None));
            FixedTasks.Add(new Task("if provided, audio controls for levels controlling the audio volume in combined areas are synchronized across all applicable user interfaces", TaskType.RoomCombining, MediaType.None));
            FixedTasks.Add(new Task("if applicable, the touchpanel designated as the \"Master\" touchpanel is available, and all others are disabled", TaskType.RoomCombining, MediaType.None));
            FixedTasks.Add(new Task("if applicable, any available touchpanels have access to microphones from all combined areas", TaskType.RoomCombining, MediaType.None));
            FixedTasks.Add(new Task("if applicable, any available touchpanels show synchronized feedback between combined areas", TaskType.RoomCombining, MediaType.None));
            FixedTasks.Add(new Task("when combined, all affected touchpanels reflect the combined state", TaskType.RoomCombining, MediaType.None));
            FixedTasks.Add(new Task("when separated, all affected touchpanels reflect the separated state, and can only access controls within the area it is located", TaskType.RoomCombining, MediaType.None));
            FixedTasks.Add(new Task("if applicable, the control system responds when any sensors detect a change in combination status", TaskType.RoomCombining, MediaType.None));
        }
    }
}