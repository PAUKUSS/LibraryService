using System.Runtime.Serialization;

namespace LibraryService.Core.DTOs;

/// <summary>
/// Статистика библиотеки.
/// </summary>
[DataContract]
public class LibraryStats
{
    [DataMember] public int TotalBooks { get; set; }
    [DataMember] public int TotalReaders { get; set; }
    [DataMember] public int ActiveLoans { get; set; }
    [DataMember] public int OverdueLoans { get; set; }
    [DataMember] public string MostPopularBook { get; set; } = string.Empty;
    [DataMember] public string MostActiveReader { get; set; } = string.Empty;
}
