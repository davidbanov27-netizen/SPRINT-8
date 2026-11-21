using SchoolLibrary.Models;

namespace SchoolLibrary.Services;

public sealed record ReturnResult(
    Book Book, Reader Reader, BookCondition Condition, int LateDays, bool BlockLifted, string InventoryNumber = "");

/// <summary>
/// Цялата бизнес логика на библиотеката (BR-01 … BR-15).
/// Класът е разделен на шест файла: този (четене), <c>LibraryService.Search.cs</c> (търсене), <c>LibraryService.Overdue.cs</c> (просрочие),
/// <c>LibraryService.Loans.cs</c> (заемане и връщане),
/// <c>LibraryService.Books.cs</c> (книги и екземпляри) и <c>LibraryService.Readers.cs</c> (читатели).
/// Екраните не пазят правила – те само извикват този клас.
/// </summary>
public partial class LibraryService
{
    private static readonly char[] Separators =
        { ' ', '\t', ',', '.', ';', ':', '-', '–', '"', '„', '“', '(', ')', '!', '?', '/', '_', '«', '»', '…' };

    private readonly ILibraryRepository _store;
    private readonly LibraryData _data;
    private readonly Func<DateTime> _clock;

    /// <param name="clock">Часовник. По подразбиране е днешната дата; в тестове се подава фиксирана.</param>
    public LibraryService(ILibraryRepository store, Func<DateTime>? clock = null)
    {
        _store = store;
        _clock = clock ?? (() => DateTime.Today);
        _data = store.Load();
        LoadNotice = store.LastError;
    }

    /// <summary>Забележка от зареждането на данните (повреден файл, стара версия, несъответствия). Null = всичко е наред.</summary>
    public string? LoadNotice { get; }

    public DateTime Today => _clock().Date;
    public LibrarySettings Settings => _data.Settings;
    public string? LastSaveError => _store.LastError;

    // ===== Четене =====

    public IReadOnlyList<Book> Books => _data.Books.Where(b => b.IsActive).OrderBy(b => b.Title).ToList();

    /// <summary>Активните читатели – за избор при заемане, връщане и вход.</summary>
    public IReadOnlyList<Reader> Readers => _data.Readers.Where(r => r.IsActive).OrderBy(r => r.CardNumber).ToList();

    /// <summary>Всички читатели, включително деактивираните – за екрана „Читатели“.</summary>
    public IReadOnlyList<Reader> AllReaders => _data.Readers.OrderBy(r => r.CardNumber).ToList();

    public IReadOnlyList<Loan> AllLoans => _data.Loans;
    public IEnumerable<Loan> ActiveLoans => _data.Loans.Where(l => l.IsActive);

    public Book? GetBook(int id) => _data.Books.FirstOrDefault(b => b.Id == id);
    public Reader? GetReader(int id) => _data.Readers.FirstOrDefault(r => r.Id == id);
    public BookCopy? GetCopy(int id) => _data.Copies.FirstOrDefault(c => c.Id == id);

    public IReadOnlyList<BookCopy> CopiesOf(Book book) =>
        _data.Copies.Where(c => c.BookId == book.Id).OrderBy(c => c.InventoryNumber).ToList();

    /// <summary>Екземпляри, които могат да се заемат в момента.</summary>
    public int Available(Book book) =>
        _data.Copies.Count(c => c.BookId == book.Id && c.Status == CopyStatus.Available);

    /// <summary>Екземпляри, с които библиотеката разполага (без загубените и списаните).</summary>
    public int TotalCopies(Book book) =>
        _data.Copies.Count(c => c.BookId == book.Id && c.Status is not (CopyStatus.Lost or CopyStatus.Withdrawn));

    public IReadOnlyList<Loan> LoansOf(Reader reader) => ActiveLoans.Where(l => l.ReaderId == reader.Id).ToList();


    // ===== Търсене по думи (FR-18) =====

    /// <summary>Търсене по заглавие, автор, жанр и описание с толерантност към една правописна грешка.</summary>
    public IReadOnlyList<Book> SearchKeyword(string query)
    {
        var terms = Tokenize(query);
        if (terms.Count == 0)
        {
            return Books;
        }

        return Books.Where(book =>
        {
            var words = Tokenize($"{book.Title} {book.Author} {book.Genre} {book.Description}");
            return terms.All(term => words.Any(word => Matches(word, term)));
        }).ToList();
    }

    private static bool Matches(string word, string term) =>
        word.Contains(term) || (term.Length >= 4 && Levenshtein(word, term) <= 1);

    private static List<string> Tokenize(string text) =>
        text.ToLowerInvariant().Split(Separators, StringSplitOptions.RemoveEmptyEntries).ToList();

    /// <summary>Разстояние на Левенщайн: колко единични промени делят две думи.</summary>
    public static int Levenshtein(string a, string b)
    {
        var d = new int[a.Length + 1, b.Length + 1];
        for (var i = 0; i <= a.Length; i++) d[i, 0] = i;
        for (var j = 0; j <= b.Length; j++) d[0, j] = j;

        for (var i = 1; i <= a.Length; i++)
        {
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
            }
        }

        return d[a.Length, b.Length];
    }

    // ===== Помощни =====

    private static int NextId(IEnumerable<int> ids) => ids.DefaultIfEmpty(0).Max() + 1;

    /// <summary>Съобщава на екрана, че записът е бил неуспешен (или че е успял след предишна грешка).</summary>
    public event Action<string?>? StorageStatusChanged;

    private void Save()
    {
        _store.Save(_data);
        StorageStatusChanged?.Invoke(_store.LastError);
    }
}
