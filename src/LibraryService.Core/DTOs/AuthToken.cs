using System.Runtime.Serialization;

namespace LibraryService.Core.DTOs;

[DataContract]
public class AuthToken
{
    [DataMember] public string Token { get; set; } = string.Empty;
    [DataMember] public DateTime Expires { get; set; }
    [DataMember] public string Role { get; set; } = string.Empty;
}
