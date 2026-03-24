using System.ServiceModel;
using LibraryService.Core.Contracts;
using LibraryService.Core.DTOs;

Console.WriteLine("=== Library Service Test Client ===");
Console.WriteLine();

// HTTP connection
var httpFactory = new ChannelFactory<ILibraryService>(
    new BasicHttpBinding(),
    new EndpointAddress("http://localhost:5000/LibraryService/http"));
var http = httpFactory.CreateChannel();

// TCP connection
var tcpFactory = new ChannelFactory<ILibraryService>(
    new NetTcpBinding(SecurityMode.None),
    new EndpointAddress("net.tcp://localhost:8090/LibraryService/tcp"));
var tcp = tcpFactory.CreateChannel();

try
{
    // ===== 1. AUTH =====
    Console.WriteLine("=== 1. AUTHENTICATION ===");

    Console.WriteLine("  Authenticating as 'librarian'...");
    var libToken = http.Authenticate("librarian", "lib123");
    Console.WriteLine($"  OK! Role: {libToken.Role}, Expires: {libToken.Expires:HH:mm:ss}");

    Console.WriteLine("  Authenticating as 'reader'...");
    var readToken = tcp.Authenticate("reader", "read123");
    Console.WriteLine($"  OK! Role: {readToken.Role}");

    Console.WriteLine("  Trying wrong password...");
    try { http.Authenticate("librarian", "wrong"); }
    catch (FaultException ex) { Console.WriteLine($"  Expected error: {ex.Message}"); }
    Console.WriteLine();

    // ===== 2. SEARCH BOOKS =====
    Console.WriteLine("=== 2. SEARCH BOOKS (via HTTP) ===");
    var allBooks = http.SearchBooks(libToken.Token, new SearchCriteria());
    Console.WriteLine($"  All books ({allBooks.Count}):");
    foreach (var b in allBooks)
        Console.WriteLine($"    [{b.Id}] {b.Title} - {b.Author} ({b.Year}) | {b.Genre} | Available: {b.AvailableCopies}/{b.TotalCopies}");
    Console.WriteLine();

    Console.WriteLine("  Search by author 'Достоевский':");
    var dostoevsky = http.SearchBooks(libToken.Token, new SearchCriteria { Author = "Достоевский" });
    foreach (var b in dostoevsky)
        Console.WriteLine($"    [{b.Id}] {b.Title}");
    Console.WriteLine();

    // ===== 3. GET BOOK =====
    Console.WriteLine("=== 3. GET BOOK (via TCP) ===");
    var book1 = tcp.GetBook(readToken.Token, 1);
    Console.WriteLine($"  Book #1: {book1.Title} by {book1.Author}, ISBN: {book1.ISBN}");
    Console.WriteLine();

    // ===== 4. ADD BOOK =====
    Console.WriteLine("=== 4. ADD BOOK (librarian via HTTP) ===");
    var newBook = http.AddBook(libToken.Token, "Анна Каренина", "Л.Н. Толстой", "978-5-17-090000-6", 1877, "Роман", 3);
    Console.WriteLine($"  Added: [{newBook.Id}] {newBook.Title}");

    Console.WriteLine("  Reader trying to add book (should fail)...");
    try { tcp.AddBook(readToken.Token, "Test", "Test", "000", 2024, "Test", 1); }
    catch (FaultException ex) { Console.WriteLine($"  ACCESS DENIED: {ex.Message}"); }
    Console.WriteLine();

    // ===== 5. UPDATE BOOK =====
    Console.WriteLine("=== 5. UPDATE BOOK ===");
    http.UpdateBook(libToken.Token, newBook.Id, "Анна Каренина (доп. тираж)", "Л.Н. Толстой", "Классика");
    var updated = http.GetBook(libToken.Token, newBook.Id);
    Console.WriteLine($"  Updated: [{updated.Id}] {updated.Title} | Genre: {updated.Genre}");
    Console.WriteLine();

    // ===== 6. REGISTER READER =====
    Console.WriteLine("=== 6. REGISTER READER ===");
    var newReader = http.RegisterReader(libToken.Token, "Козлов Дмитрий", "kozlov@mail.ru", "+7-900-444-4444");
    Console.WriteLine($"  Registered: [{newReader.Id}] {newReader.FullName}, {newReader.Email}");
    Console.WriteLine();

    // ===== 7. LIST READERS =====
    Console.WriteLine("=== 7. ALL READERS (via TCP) ===");
    var readers = tcp.GetAllReaders(libToken.Token);
    foreach (var r in readers)
        Console.WriteLine($"  [{r.Id}] {r.FullName} | {r.Email} | Since: {r.RegisteredDate:yyyy-MM-dd}");
    Console.WriteLine();

    // ===== 8. LEND BOOK =====
    Console.WriteLine("=== 8. LEND BOOK ===");
    var loan = http.LendBook(libToken.Token, 3, 1, 14);
    Console.WriteLine($"  Loan #{loan.Id}: '{loan.BookTitle}' -> {loan.ReaderName}");
    Console.WriteLine($"  Due: {loan.DueDate:yyyy-MM-dd}");

    var loan2 = tcp.LendBook(libToken.Token, 4, newReader.Id, 21);
    Console.WriteLine($"  Loan #{loan2.Id}: '{loan2.BookTitle}' -> {loan2.ReaderName}");
    Console.WriteLine();

    // ===== 9. READER LOANS =====
    Console.WriteLine("=== 9. READER LOANS (reader #1) ===");
    var loans = http.GetReaderLoans(readToken.Token, 1);
    foreach (var l in loans)
        Console.WriteLine($"  Loan #{l.Id}: '{l.BookTitle}' | Due: {l.DueDate:yyyy-MM-dd} | Overdue: {l.IsOverdue}");
    Console.WriteLine();

    // ===== 10. RETURN BOOK =====
    Console.WriteLine("=== 10. RETURN BOOK ===");
    http.ReturnBook(libToken.Token, loan.Id);
    Console.WriteLine($"  Loan #{loan.Id} returned successfully.");
    var bookAfter = http.GetBook(libToken.Token, 3);
    Console.WriteLine($"  Book '{bookAfter.Title}' available copies: {bookAfter.AvailableCopies}/{bookAfter.TotalCopies}");
    Console.WriteLine();

    // ===== 11. STATISTICS =====
    Console.WriteLine("=== 11. LIBRARY STATISTICS (via TCP) ===");
    var stats = tcp.GetStatistics(libToken.Token);
    Console.WriteLine($"  Total books:       {stats.TotalBooks}");
    Console.WriteLine($"  Total readers:     {stats.TotalReaders}");
    Console.WriteLine($"  Active loans:      {stats.ActiveLoans}");
    Console.WriteLine($"  Overdue loans:     {stats.OverdueLoans}");
    Console.WriteLine($"  Most popular book: {stats.MostPopularBook}");
    Console.WriteLine($"  Most active reader:{stats.MostActiveReader}");
    Console.WriteLine();

    // ===== 12. DELETE BOOK (admin only) =====
    Console.WriteLine("=== 12. DELETE BOOK (admin only) ===");
    Console.WriteLine("  Librarian trying to delete (should fail)...");
    try { http.DeleteBook(libToken.Token, newBook.Id); }
    catch (FaultException ex) { Console.WriteLine($"  ACCESS DENIED: {ex.Message}"); }

    var adminToken = http.Authenticate("admin", "admin123");
    http.DeleteBook(adminToken.Token, newBook.Id);
    Console.WriteLine($"  Admin deleted book #{newBook.Id} successfully.");

    ((IClientChannel)http).Close(); httpFactory.Close();
    ((IClientChannel)tcp).Close(); tcpFactory.Close();
}
catch (Exception ex)
{
    Console.WriteLine($"\nERROR: {ex.Message}");
}

Console.WriteLine();
Console.WriteLine("All tests passed! Press any key to exit...");
Console.ReadKey();
