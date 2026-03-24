using System.Collections.Concurrent;
using System.Security.Claims;
using CoreWCF;
using LibraryService.Core.Contracts;
using LibraryService.Core.DTOs;
using LibraryService.Core.Models;
using LibraryService.Service.Security;

namespace LibraryService.Service.Services;

[ServiceBehavior(IncludeExceptionDetailInFaults = true)]
public class LibraryServiceImpl : ILibraryService
{
    private static int _nextBookId = 100;
    private static int _nextReaderId = 100;
    private static int _nextLoanId = 100;

    private static readonly ConcurrentDictionary<int, Book> _books = new(new Dictionary<int, Book>
    {
        [1] = new() { Id = 1, Title = "Война и мир", Author = "Л.Н. Толстой", ISBN = "978-5-17-090000-1", Year = 1869, Genre = "Роман", TotalCopies = 5, AvailableCopies = 3 },
        [2] = new() { Id = 2, Title = "Преступление и наказание", Author = "Ф.М. Достоевский", ISBN = "978-5-17-090000-2", Year = 1866, Genre = "Роман", TotalCopies = 3, AvailableCopies = 2 },
        [3] = new() { Id = 3, Title = "Мастер и Маргарита", Author = "М.А. Булгаков", ISBN = "978-5-17-090000-3", Year = 1967, Genre = "Фантастика", TotalCopies = 4, AvailableCopies = 4 },
        [4] = new() { Id = 4, Title = "Евгений Онегин", Author = "А.С. Пушкин", ISBN = "978-5-17-090000-4", Year = 1833, Genre = "Поэзия", TotalCopies = 6, AvailableCopies = 5 },
        [5] = new() { Id = 5, Title = "Идиот", Author = "Ф.М. Достоевский", ISBN = "978-5-17-090000-5", Year = 1869, Genre = "Роман", TotalCopies = 2, AvailableCopies = 1 },
    });

    private static readonly ConcurrentDictionary<int, Reader> _readers = new(new Dictionary<int, Reader>
    {
        [1] = new() { Id = 1, FullName = "Иванов Иван", Email = "ivanov@mail.ru", Phone = "+7-900-111-1111", RegisteredDate = DateTime.UtcNow.AddMonths(-6), IsActive = true },
        [2] = new() { Id = 2, FullName = "Петрова Мария", Email = "petrova@mail.ru", Phone = "+7-900-222-2222", RegisteredDate = DateTime.UtcNow.AddMonths(-3), IsActive = true },
        [3] = new() { Id = 3, FullName = "Сидоров Алексей", Email = "sidorov@mail.ru", Phone = "+7-900-333-3333", RegisteredDate = DateTime.UtcNow.AddMonths(-1), IsActive = true },
    });

    private static readonly ConcurrentDictionary<int, Loan> _loans = new(new Dictionary<int, Loan>
    {
        [1] = new() { Id = 1, BookId = 1, BookTitle = "Война и мир", ReaderId = 1, ReaderName = "Иванов Иван", LoanDate = DateTime.UtcNow.AddDays(-20), DueDate = DateTime.UtcNow.AddDays(-6), ReturnDate = null, IsOverdue = true },
        [2] = new() { Id = 2, BookId = 2, BookTitle = "Преступление и наказание", ReaderId = 2, ReaderName = "Петрова Мария", LoanDate = DateTime.UtcNow.AddDays(-5), DueDate = DateTime.UtcNow.AddDays(9), ReturnDate = null, IsOverdue = false },
        [3] = new() { Id = 3, BookId = 1, BookTitle = "Война и мир", ReaderId = 3, ReaderName = "Сидоров Алексей", LoanDate = DateTime.UtcNow.AddDays(-30), DueDate = DateTime.UtcNow.AddDays(-16), ReturnDate = DateTime.UtcNow.AddDays(-17), IsOverdue = false },
    });

    private static void Audit(string msg) => Console.WriteLine($"  [{DateTime.UtcNow:HH:mm:ss}] {msg}");

    private static ClaimsPrincipal Auth(string token, params string[] roles)
    {
        if (string.IsNullOrEmpty(token))
            throw new FaultException("Authentication required.");
        var p = JwtHelper.Validate(token) ?? throw new FaultException("Invalid or expired token.");
        if (roles.Length > 0 && !roles.Any(r => p.IsInRole(r)))
            throw new FaultException($"Access denied. Required: {string.Join("/", roles)}. Your role: {JwtHelper.GetRole(p)}");
        return p;
    }

    // === Auth ===

    public AuthToken Authenticate(string username, string password)
    {
        if (!UserStore.Users.TryGetValue(username, out var user) || user.Password != password)
        {
            Audit($"AUTH FAILED: {username}");
            throw new FaultException("Invalid credentials.");
        }
        var token = JwtHelper.GenerateToken(username, user.Role, TimeSpan.FromHours(2));
        Audit($"AUTH OK: {username} [{user.Role}]");
        return new AuthToken { Token = token, Expires = DateTime.UtcNow.AddHours(2), Role = user.Role };
    }

    // === Books CRUD ===

    public Book AddBook(string token, string title, string author, string isbn, int year, string genre, int copies)
    {
        var p = Auth(token, "Librarian", "Admin");
        var id = Interlocked.Increment(ref _nextBookId);
        var book = new Book { Id = id, Title = title, Author = author, ISBN = isbn, Year = year, Genre = genre, TotalCopies = copies, AvailableCopies = copies };
        _books[id] = book;
        Audit($"{JwtHelper.GetUser(p)} -> AddBook({title})  id={id}");
        return book;
    }

    public Book GetBook(string token, int bookId)
    {
        Auth(token);
        return _books.TryGetValue(bookId, out var b) ? b : throw new FaultException($"Book {bookId} not found.");
    }

    public bool UpdateBook(string token, int bookId, string title, string author, string genre)
    {
        var p = Auth(token, "Librarian", "Admin");
        if (!_books.TryGetValue(bookId, out var b)) throw new FaultException($"Book {bookId} not found.");
        b.Title = title; b.Author = author; b.Genre = genre;
        Audit($"{JwtHelper.GetUser(p)} -> UpdateBook({bookId})");
        return true;
    }

    public bool DeleteBook(string token, int bookId)
    {
        var p = Auth(token, "Admin");
        if (!_books.TryRemove(bookId, out _)) throw new FaultException($"Book {bookId} not found.");
        Audit($"{JwtHelper.GetUser(p)} -> DeleteBook({bookId})");
        return true;
    }

    public List<Book> SearchBooks(string token, SearchCriteria criteria)
    {
        Auth(token);
        var q = _books.Values.AsEnumerable();
        if (!string.IsNullOrEmpty(criteria.TitleContains))
            q = q.Where(b => b.Title.Contains(criteria.TitleContains, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(criteria.Author))
            q = q.Where(b => b.Author.Contains(criteria.Author, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(criteria.Genre))
            q = q.Where(b => b.Genre.Equals(criteria.Genre, StringComparison.OrdinalIgnoreCase));
        if (criteria.OnlyAvailable)
            q = q.Where(b => b.AvailableCopies > 0);
        return q.ToList();
    }

    // === Readers ===

    public Reader RegisterReader(string token, string fullName, string email, string phone)
    {
        var p = Auth(token, "Librarian", "Admin");
        var id = Interlocked.Increment(ref _nextReaderId);
        var reader = new Reader { Id = id, FullName = fullName, Email = email, Phone = phone, RegisteredDate = DateTime.UtcNow, IsActive = true };
        _readers[id] = reader;
        Audit($"{JwtHelper.GetUser(p)} -> RegisterReader({fullName})  id={id}");
        return reader;
    }

    public Reader GetReader(string token, int readerId)
    {
        Auth(token, "Librarian", "Admin");
        return _readers.TryGetValue(readerId, out var r) ? r : throw new FaultException($"Reader {readerId} not found.");
    }

    public List<Reader> GetAllReaders(string token)
    {
        Auth(token, "Librarian", "Admin");
        return _readers.Values.ToList();
    }

    // === Loans ===

    public Loan LendBook(string token, int bookId, int readerId, int daysToReturn)
    {
        var p = Auth(token, "Librarian", "Admin");
        if (!_books.TryGetValue(bookId, out var book)) throw new FaultException($"Book {bookId} not found.");
        if (book.AvailableCopies <= 0) throw new FaultException($"No copies available for '{book.Title}'.");
        if (!_readers.TryGetValue(readerId, out var reader)) throw new FaultException($"Reader {readerId} not found.");

        book.AvailableCopies--;
        var id = Interlocked.Increment(ref _nextLoanId);
        var loan = new Loan
        {
            Id = id, BookId = bookId, BookTitle = book.Title,
            ReaderId = readerId, ReaderName = reader.FullName,
            LoanDate = DateTime.UtcNow, DueDate = DateTime.UtcNow.AddDays(daysToReturn)
        };
        _loans[id] = loan;
        Audit($"{JwtHelper.GetUser(p)} -> LendBook(book={bookId}, reader={readerId})  loan={id}");
        return loan;
    }

    public bool ReturnBook(string token, int loanId)
    {
        var p = Auth(token, "Librarian", "Admin");
        if (!_loans.TryGetValue(loanId, out var loan)) throw new FaultException($"Loan {loanId} not found.");
        if (loan.ReturnDate != null) throw new FaultException($"Loan {loanId} already returned.");

        loan.ReturnDate = DateTime.UtcNow;
        loan.IsOverdue = DateTime.UtcNow > loan.DueDate;
        if (_books.TryGetValue(loan.BookId, out var book)) book.AvailableCopies++;

        Audit($"{JwtHelper.GetUser(p)} -> ReturnBook(loan={loanId})");
        return true;
    }

    public List<Loan> GetReaderLoans(string token, int readerId)
    {
        Auth(token);
        return _loans.Values.Where(l => l.ReaderId == readerId && l.ReturnDate == null).ToList();
    }

    // === Stats ===

    public LibraryStats GetStatistics(string token)
    {
        Auth(token, "Librarian", "Admin");
        var activeLoans = _loans.Values.Where(l => l.ReturnDate == null).ToList();
        var overdue = activeLoans.Count(l => l.DueDate < DateTime.UtcNow);

        var popularBook = _loans.Values.GroupBy(l => l.BookTitle).OrderByDescending(g => g.Count()).FirstOrDefault();
        var activeReader = _loans.Values.GroupBy(l => l.ReaderName).OrderByDescending(g => g.Count()).FirstOrDefault();

        return new LibraryStats
        {
            TotalBooks = _books.Count,
            TotalReaders = _readers.Count,
            ActiveLoans = activeLoans.Count,
            OverdueLoans = overdue,
            MostPopularBook = popularBook?.Key ?? "N/A",
            MostActiveReader = activeReader?.Key ?? "N/A"
        };
    }
}
