using System.Runtime.Serialization;

namespace CommissioningChecklistGenerator.AVSystem
{
    public enum ControlType
    {
        [EnumMember(Value = "none")]
        None,
        [EnumMember(Value = "other")]
        Other,
        [EnumMember(Value = "network")]
        Network,
        [EnumMember(Value = "gpio")]
        GPIO,
        [EnumMember(Value = "serial")]
        Serial,
        [EnumMember(Value = "relay")]
        Relay,
        [EnumMember(Value = "ir")]
        IR,
        [EnumMember(Value = "ebus")]
        EBus,
        [EnumMember(Value = "cresnet")]
        Cresnet
    }
}
