using System.ServiceModel;
using LibraryService.Core.DTOs;
using LibraryService.Core.Models;

namespace LibraryService.Core.Contracts;

/// <summary>
/// Контракт службы управления библиотекой.
/// Поддерживает CRUD для книг и читателей, выдачу/возврат книг, поиск и статистику.
/// </summary>
[ServiceContract]
public interface ILibraryService
{
    // === Аутентификация ===

    /// <summary>Аутентификация пользователя. Возвращает токен для последующих вызовов.</summary>
    [OperationContract]
    AuthToken Authenticate(string username, string password);

    // === Книги (CRUD) ===

    /// <summary>Добавить новую книгу в фонд.</summary>
    [OperationContract]
    Book AddBook(string token, string title, string author, string isbn, int year, string genre, int copies);

    /// <summary>Получить книгу по ID.</summary>
    [OperationContract]
    Book GetBook(string token, int bookId);

    /// <summary>Обновить информацию о книге.</summary>
    [OperationContract]
    bool UpdateBook(string token, int bookId, string title, string author, string genre);

    /// <summary>Удалить книгу из фонда.</summary>
    [OperationContract]
    bool DeleteBook(string token, int bookId);

    /// <summary>Поиск книг по критериям.</summary>
    [OperationContract]
    List<Book> SearchBooks(string token, SearchCriteria criteria);

    // === Читатели ===

    /// <summary>Зарегистрировать нового читателя.</summary>
    [OperationContract]
    Reader RegisterReader(string token, string fullName, string email, string phone);

    /// <summary>Получить информацию о читателе.</summary>
    [OperationContract]
    Reader GetReader(string token, int readerId);

    /// <summary>Получить список всех читателей.</summary>
    [OperationContract]
    List<Reader> GetAllReaders(string token);

    // === Выдача и возврат книг ===

    /// <summary>Выдать книгу читателю. Возвращает запись о выдаче.</summary>
    [OperationContract]
    Loan LendBook(string token, int bookId, int readerId, int daysToReturn);

    /// <summary>Принять возврат книги.</summary>
    [OperationContract]
    bool ReturnBook(string token, int loanId);

    /// <summary>Получить активные выдачи читателя.</summary>
    [OperationContract]
    List<Loan> GetReaderLoans(string token, int readerId);

    // === Статистика ===

    /// <summary>Получить общую статистику библиотеки.</summary>
    [OperationContract]
    LibraryStats GetStatistics(string token);
}
