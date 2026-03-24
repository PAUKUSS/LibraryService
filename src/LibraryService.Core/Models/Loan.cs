using System.Runtime.Serialization;

namespace LibraryService.Core.Models;

/// <summary>
/// Запись о выдаче книги читателю.
/// </summary>
[DataContract]
public class Loan
{
    [DataMember] public int Id { get; set; }
    [DataMember] public int BookId { get; set; }
    [DataMember] public string BookTitle { get; set; } = string.Empty;
    [DataMember] public int ReaderId { get; set; }
    [DataMember] public string ReaderName { get; set; } = string.Empty;
    [DataMember] public DateTime LoanDate { get; set; }
    [DataMember] public DateTime DueDate { get; set; }
    [DataMember] public DateTime? ReturnDate { get; set; }
    [DataMember] public bool IsOverdue { get; set; }
}
