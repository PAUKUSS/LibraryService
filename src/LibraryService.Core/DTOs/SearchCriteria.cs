using System.Runtime.Serialization;

namespace LibraryService.Core.DTOs;

/// <summary>
/// Критерии поиска книг.
/// </summary>
[DataContract]
public class SearchCriteria
{
    [DataMember] public string? TitleContains { get; set; }
    [DataMember] public string? Author { get; set; }
    [DataMember] public string? Genre { get; set; }
    [DataMember] public bool OnlyAvailable { get; set; }
}
