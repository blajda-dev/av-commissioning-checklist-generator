using System.Runtime.Serialization;
namespace CommissioningChecklistGenerator.AVSystem
{
    public enum MediaType
    {
        [EnumMember(Value = "none")]
        None,
        [EnumMember(Value = "av")]
        AudioVideo,
        [EnumMember(Value = "audio")]
        Audio,
        [EnumMember(Value = "video")]
        Video
    }
}
