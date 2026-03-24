using System.Runtime.Serialization;

namespace LibraryService.Core.Models;

/// <summary>
/// Книга в библиотечном фонде.
/// </summary>
[DataContract]
public class Book
{
    [DataMember] public int Id { get; set; }
    [DataMember] public string Title { get; set; } = string.Empty;
    [DataMember] public string Author { get; set; } = string.Empty;
    [DataMember] public string ISBN { get; set; } = string.Empty;
    [DataMember] public int Year { get; set; }
    [DataMember] public string Genre { get; set; } = string.Empty;
    [DataMember] public int TotalCopies { get; set; }
    [DataMember] public int AvailableCopies { get; set; }
}
