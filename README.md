# Library Service — Система управления библиотекой (CoreWCF)

## Описание предметной области

Система управления библиотечным фондом — реальная потребность любой библиотеки. Позволяет вести учёт книг, читателей, выдач и возвратов. Включает поиск по каталогу, статистику и ролевую модель доступа.

**Потенциал для расширения:** система штрафов за просрочку, бронирование книг, интеграция с внешними каталогами (OPAC), уведомления по email, рекомендательная система.

## Структура проекта

```
LibraryService.sln
├── src/
│   ├── LibraryService.Core/          # Контракты, модели, DTO
│   │   ├── Contracts/
│   │   │   └── ILibraryService.cs    # ServiceContract (12 операций)
│   │   ├── Models/
│   │   │   ├── Book.cs               # Книга
│   │   │   ├── Reader.cs             # Читатель
│   │   │   └── Loan.cs               # Запись о выдаче
│   │   └── DTOs/
│   │       ├── AuthToken.cs          # Токен аутентификации
│   │       ├── SearchCriteria.cs     # Критерии поиска
│   │       └── LibraryStats.cs       # Статистика библиотеки
│   │
│   ├── LibraryService.Service/       # Реализация сервиса
│   │   ├── Services/
│   │   │   └── LibraryServiceImpl.cs # Реализация с авторизацией
│   │   ├── Security/
│   │   │   └── JwtHelper.cs          # JWT + UserStore
│   │   └── Program.cs               # Хост (HTTP + TCP)
│   │
│   └── LibraryService.Client/        # Тестовый клиент
│       └── Program.cs               # 12 тестовых сценариев
│
├── .gitignore
├── README.md
└── LibraryService.sln
```

## Контракт службы (12 операций)

```csharp
[ServiceContract]
public interface ILibraryService
{
    // Аутентификация
    AuthToken Authenticate(string username, string password);

    // Книги (CRUD + поиск) — 5 операций
    Book AddBook(token, title, author, isbn, year, genre, copies);
    Book GetBook(token, bookId);
    bool UpdateBook(token, bookId, title, author, genre);
    bool DeleteBook(token, bookId);               // Admin only
    List<Book> SearchBooks(token, SearchCriteria);

    // Читатели — 3 операции
    Reader RegisterReader(token, fullName, email, phone);
    Reader GetReader(token, readerId);
    List<Reader> GetAllReaders(token);

    // Выдача/возврат — 3 операции
    Loan LendBook(token, bookId, readerId, daysToReturn);
    bool ReturnBook(token, loanId);
    List<Loan> GetReaderLoans(token, readerId);

    // Статистика
    LibraryStats GetStatistics(token);
}
```

## Сложные типы данных

| Тип | Описание | Поля |
|-----|----------|------|
| `Book` | Книга | Id, Title, Author, ISBN, Year, Genre, TotalCopies, AvailableCopies |
| `Reader` | Читатель | Id, FullName, Email, Phone, RegisteredDate, IsActive |
| `Loan` | Выдача | Id, BookId, BookTitle, ReaderId, ReaderName, LoanDate, DueDate, ReturnDate, IsOverdue |
| `SearchCriteria` | Поиск | TitleContains, Author, Genre, OnlyAvailable |
| `LibraryStats` | Статистика | TotalBooks, TotalReaders, ActiveLoans, OverdueLoans, MostPopularBook, MostActiveReader |
| `AuthToken` | Токен | Token, Expires, Role |

## Ролевая модель

| Операция | Reader | Librarian | Admin |
|----------|:------:|:---------:|:-----:|
| Authenticate | + | + | + |
| SearchBooks, GetBook | + | + | + |
| GetReaderLoans | + | + | + |
| AddBook, UpdateBook | - | + | + |
| RegisterReader, GetReader, GetAllReaders | - | + | + |
| LendBook, ReturnBook | - | + | + |
| GetStatistics | - | + | + |
| DeleteBook | - | - | + |

### Пользователи

| Логин | Пароль | Роль |
|-------|--------|------|
| librarian | lib123 | Librarian |
| reader | read123 | Reader |
| admin | admin123 | Admin |

## Эндпоинты

| Протокол | Адрес |
|----------|-------|
| HTTP | `http://localhost:5000/LibraryService/http` |
| TCP | `net.tcp://localhost:8090/LibraryService/tcp` |
| WSDL | `http://localhost:5000/LibraryService/http?wsdl` |

## Как запустить

```bash
# Сборка
dotnet build

# Запуск сервиса
dotnet run --project src/LibraryService.Service

# Запуск тестового клиента (в другом терминале)
dotnet run --project src/LibraryService.Client
```

## Скриншоты работы

### Запуск сервиса
```
=== Library Service ===
HTTP: http://localhost:5000/LibraryService/http
TCP:  net.tcp://localhost:8090/LibraryService/tcp
Users: librarian/lib123, reader/read123, admin/admin123
```

### Тестовый клиент (полный вывод)
```
=== Library Service Test Client ===

=== 1. AUTHENTICATION ===
  Authenticating as 'librarian'...
  OK! Role: Librarian, Expires: 13:43:10
  Authenticating as 'reader'...
  OK! Role: Reader
  Trying wrong password...
  Expected error: Invalid credentials.

=== 2. SEARCH BOOKS (via HTTP) ===
  All books (5):
    [1] Война и мир - Л.Н. Толстой (1869) | Роман | Available: 3/5
    [2] Преступление и наказание - Ф.М. Достоевский (1866) | Роман | Available: 2/3
    [3] Мастер и Маргарита - М.А. Булгаков (1967) | Фантастика | Available: 4/4
    [4] Евгений Онегин - А.С. Пушкин (1833) | Поэзия | Available: 5/6
    [5] Идиот - Ф.М. Достоевский (1869) | Роман | Available: 1/2

  Search by author 'Достоевский':
    [2] Преступление и наказание
    [5] Идиот

=== 3. GET BOOK (via TCP) ===
  Book #1: Война и мир by Л.Н. Толстой, ISBN: 978-5-17-090000-1

=== 4. ADD BOOK (librarian via HTTP) ===
  Added: [101] Анна Каренина
  Reader trying to add book (should fail)...
  ACCESS DENIED: Access denied. Required: Librarian/Admin. Your role: Reader

=== 5. UPDATE BOOK ===
  Updated: [101] Анна Каренина (доп. тираж) | Genre: Классика

=== 6. REGISTER READER ===
  Registered: [101] Козлов Дмитрий, kozlov@mail.ru

=== 7. ALL READERS (via TCP) ===
  [1] Иванов Иван | ivanov@mail.ru | Since: 2025-09-23
  [2] Петрова Мария | petrova@mail.ru | Since: 2025-12-23
  [3] Сидоров Алексей | sidorov@mail.ru | Since: 2026-02-23
  [101] Козлов Дмитрий | kozlov@mail.ru | Since: 2026-03-23

=== 8. LEND BOOK ===
  Loan #101: 'Мастер и Маргарита' -> Иванов Иван
  Due: 2026-04-06
  Loan #102: 'Евгений Онегин' -> Козлов Дмитрий

=== 9. READER LOANS (reader #1) ===
  Loan #1: 'Война и мир' | Due: 2026-03-17 | Overdue: True
  Loan #101: 'Мастер и Маргарита' | Due: 2026-04-06 | Overdue: False

=== 10. RETURN BOOK ===
  Loan #101 returned successfully.
  Book 'Мастер и Маргарита' available copies: 4/4

=== 11. LIBRARY STATISTICS (via TCP) ===
  Total books:       6
  Total readers:     4
  Active loans:      3
  Overdue loans:     1
  Most popular book: Война и мир
  Most active reader:Иванов Иван

=== 12. DELETE BOOK (admin only) ===
  Librarian trying to delete (should fail)...
  ACCESS DENIED: Access denied. Required: Admin. Your role: Librarian
  Admin deleted book #101 successfully.

All tests passed!
```
