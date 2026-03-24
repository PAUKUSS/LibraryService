using System.Runtime.Serialization;

namespace LibraryService.Core.Models;

/// <summary>
/// Читатель библиотеки.
/// </summary>
[DataContract]
public class Reader
{
    [DataMember] public int Id { get; set; }
    [DataMember] public string FullName { get; set; } = string.Empty;
    [DataMember] public string Email { get; set; } = string.Empty;
    [DataMember] public string Phone { get; set; } = string.Empty;
    [DataMember] public DateTime RegisteredDate { get; set; }
    [DataMember] public bool IsActive { get; set; } = true;
}
