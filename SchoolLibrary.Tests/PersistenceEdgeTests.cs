using System.IO;
using System.Text.Json;
using SchoolLibrary.Models;
using SchoolLibrary.Services;

namespace SchoolLibrary.Tests;

/// <summary>Етап 7: данните остават след рестартиране – и какво става, когато файлът е в беда.</summary>
public sealed class PersistenceEdgeTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "SchoolLibraryEdge_" + Guid.NewGuid().ToString("N"));

    public PersistenceEdgeTests()
    {
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_dir, recursive: true);
        }
        catch (IOException)
        {
            // Временна папка – не е проблем, ако не се изтрие.
        }
    }

    private string PathFor(string name) => Path.Combine(_dir, name);

    /// <summary>
    /// „Стартира програмата“ върху файла. Ако файлът още го няма, започва с празна библиотека
    /// (а не с примерните данни, чиито читатели биха се сблъскали с тестовите).
    /// </summary>
    private static LibraryService Start(string path)
    {
        if (!File.Exists(path))
        {
            File.WriteAllText(path, JsonSerializer.Serialize(new LibraryData { SchemaVersion = LibraryData.CurrentSchemaVersion }));
        }

        return new LibraryService(new DataStore(path), () => TestLibrary.Today);
    }

    // ===== Рестартиране =====

    [Fact]
    public void FirstRun_StartsWithSeed_AndCreatesTheFileOnFirstChange()
    {
        var path = PathFor("library.json");
        var s = new LibraryService(new DataStore(path), () => TestLibrary.Today);
        Assert.False(File.Exists(path));
        Assert.NotEmpty(s.Books);

        s.AddBookOk("Първа промяна", 1, "Автор Промяна");

        Assert.True(File.Exists(path));
    }

    [Fact]
    public void Restart_KeepsEverything_IncludingReturnsAndCopyStates()
    {
        var path = PathFor("library.json");
        var s = Start(path);
        var book = s.AddBookOk("Българска книга", copies: 2);
        var reader = s.AddReaderOk("Петър Иванов", "9А");
        var loan = s.Lend(reader, book).Value!;
        s.Return(loan, BookCondition.Damaged);
        var second = s.Lend(reader, book).Value!;
        var openCopyId = second.CopyId;

        var after = Start(path); // „рестарт“

        var b = after.Books.Single(x => x.Title == "Българска книга");
        Assert.Equal(2, after.TotalCopies(b));
        Assert.Equal(0, after.Available(b)); // едното е повредено, другото е заето
        var closed = after.AllLoans.Single(l => l.Id == loan.Id);
        Assert.False(closed.IsActive);
        Assert.Equal(BookCondition.Damaged, closed.Condition);
        Assert.Equal(CopyStatus.OnLoan, after.GetCopy(openCopyId)!.Status);
        Assert.Single(after.ActiveLoans, l => l.Id == second.Id);
    }

    [Fact]
    public void Restart_KeepsIdsAndInventoryNumbersUnique()
    {
        var path = PathFor("library.json");
        var s = Start(path);
        s.AddBookOk("Първа", 1, "Автор Първи");
        s.AddReaderOk("Иван Петров", "7Б");

        var after = Start(path);
        var book = after.AddBookOk("Втора", 1, "Автор Втори");
        var reader = after.AddReaderOk("Мария Георгиева", "7Б");

        var numbers = after.Books.SelectMany(b => after.CopiesOf(b)).Select(c => c.InventoryNumber).ToList();
        Assert.Equal(numbers.Count, numbers.Distinct().Count());
        Assert.Equal(after.Books.Count, after.Books.Select(b => b.Id).Distinct().Count());
        Assert.Equal(after.AllReaders.Count, after.AllReaders.Select(r => r.CardNumber).Distinct().Count());
        Assert.NotEqual(0, book.Id);
        Assert.NotEqual(0, reader.Id);
    }

    [Fact]
    public void Restart_KeepsOverdueCalculation()
    {
        var path = PathFor("library.json");
        var s = Start(path);
        var reader = s.AddReaderOk();
        s.Lend(reader, s.AddBookOk());

        var later = new LibraryService(new DataStore(path), () => TestLibrary.Today.AddDays(20));

        Assert.Single(later.OverdueLoans());
        Assert.Equal(6, later.DaysOverdue(later.OverdueLoans()[0]));
    }

    [Fact]
    public void Restart_KeepsChangedSettings()
    {
        var path = PathFor("library.json");
        var s = Start(path);
        s.Settings.LoanDays = 21;
        s.AddBookOk(); // всяка промяна записва целия файл

        Assert.Equal(21, Start(path).Settings.LoanDays);
    }

    [Fact]
    public void EmptyLibrary_IsNotReplacedBySeed()
    {
        var path = PathFor("library.json");
        File.WriteAllText(path, JsonSerializer.Serialize(new LibraryData { SchemaVersion = LibraryData.CurrentSchemaVersion }));

        var data = new DataStore(path).Load();

        Assert.Empty(data.Books);
    }

    [Fact]
    public void NullListsInFile_AreTreatedAsEmpty()
    {
        var path = PathFor("library.json");
        File.WriteAllText(path, "{ \"SchemaVersion\": 2, \"Books\": null, \"Copies\": null, \"Readers\": null, \"Loans\": null }");

        var data = new DataStore(path).Load();

        Assert.Empty(data.Books);
        Assert.Empty(data.Loans);
    }

    // ===== Безопасен запис =====

    [Fact]
    public void Save_LeavesNoTemporaryFile()
    {
        var path = PathFor("library.json");
        var s = Start(path);
        s.AddBookOk();
        s.AddBookOk("Втора", 1, "Друг");

        Assert.False(File.Exists(path + ".tmp"));
    }

    [Fact]
    public void Save_KeepsThePreviousVersionAsBackup()
    {
        var path = PathFor("library.json");
        var s = Start(path);
        s.AddBookOk("Първа", 1, "Автор Първи");
        var firstVersion = File.ReadAllText(path);

        s.AddBookOk("Втора", 1, "Автор Втори");

        Assert.True(File.Exists(path + ".bak"));
        Assert.Equal(firstVersion, File.ReadAllText(path + ".bak"));
        Assert.NotEqual(firstVersion, File.ReadAllText(path));
    }

    [Fact]
    public void Save_StampsTheCurrentSchemaVersion()
    {
        var path = PathFor("library.json");
        new DataStore(path).Save(new LibraryData { SchemaVersion = 0 });

        Assert.Equal(LibraryData.CurrentSchemaVersion, new DataStore(path).Load().SchemaVersion);
    }

    // ===== Повреден файл =====

    [Fact]
    public void CorruptMainFile_IsRecoveredFromBackup()
    {
        var path = PathFor("library.json");
        var s = Start(path);
        s.AddBookOk("Първа", 1, "Автор Първи");
        s.AddBookOk("Втора", 1, "Автор Втори");   // след този запис .bak пази състоянието с „Първа“
        File.WriteAllText(path, "{ счупен файл");
        var store = new DataStore(path);

        var data = store.Load();

        Assert.Contains(data.Books, b => b.Title == "Първа");
        Assert.Contains("предишния запис", store.LastError);
    }

    [Fact]
    public void CorruptMainFile_IsKeptForInspection()
    {
        var path = PathFor("library.json");
        File.WriteAllText(path, "{ счупен файл");

        new DataStore(path).Load();

        var kept = Directory.GetFiles(_dir, "library.corrupt-*.json");
        Assert.Single(kept);
        Assert.Equal("{ счупен файл", File.ReadAllText(kept[0]));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("null")]
    [InlineData("[1, 2, 3]")]
    [InlineData("{ \"Books\": ")]
    public void UnusableContent_FallsBackToSeed_WithoutException(string content)
    {
        var path = PathFor("library.json");
        File.WriteAllText(path, content);
        var store = new DataStore(path);

        var data = store.Load();

        Assert.NotEmpty(data.Books);
        Assert.NotNull(store.LastError);
    }

    [Fact]
    public void BothFilesCorrupt_FallsBackToSeed()
    {
        var path = PathFor("library.json");
        File.WriteAllText(path, "{ x");
        File.WriteAllText(path + ".bak", "{ y");
        var store = new DataStore(path);

        var data = store.Load();

        Assert.NotEmpty(data.Books);
        Assert.Contains("не може да се прочете", store.LastError);
    }

    [Fact]
    public void NewerSchemaFile_IsBackedUp_NotSilentlyOverwritten()
    {
        var path = PathFor("library.json");
        File.WriteAllText(path, "{ \"SchemaVersion\": 99, \"Books\": [] }");
        var store = new DataStore(path);

        var data = store.Load();

        Assert.True(File.Exists(PathFor("library.v99.bak")));
        Assert.Contains("по-нова", store.LastError);
        Assert.NotEmpty(data.Books); // примерни данни
    }

    [Fact]
    public void Load_AfterCorruption_NextSaveWritesAHealthyFile()
    {
        var path = PathFor("library.json");
        File.WriteAllText(path, "{ x");
        var s = Start(path);

        s.AddBookOk("Нова");

        Assert.Contains(Start(path).Books, b => b.Title == "Нова");
    }

    // ===== Грешки при запис =====

    [Fact]
    public void SaveFailure_DoesNotThrow_AndIsReported()
    {
        var blocker = PathFor("blocker");
        File.WriteAllText(blocker, "това е файл, не папка");
        var store = new DataStore(Path.Combine(blocker, "library.json")); // папката не може да се създаде
        string? reported = null;
        var s = new LibraryService(store, () => TestLibrary.Today);
        s.StorageStatusChanged += e => reported = e;

        var result = s.AddBook(TestLibrary.Draft("Не се записва"), 1);

        Assert.True(result.Success, result.Summary); // работата в паметта продължава
        Assert.NotNull(store.LastError);
        Assert.Contains("не са записани", reported);
    }

    [Fact]
    public void StorageStatus_IsRaisedAfterEveryWrite()
    {
        var repo = new InMemoryRepository(new LibraryData { SchemaVersion = LibraryData.CurrentSchemaVersion });
        var s = new LibraryService(repo, () => TestLibrary.Today);
        var calls = new List<string?>();
        s.StorageStatusChanged += calls.Add;

        s.AddBookOk();
        repo.LastError = "диск пълен";
        s.AddReaderOk();

        Assert.Equal(new string?[] { null, "диск пълен" }, calls);
    }

    [Fact]
    public void LoadNotice_CarriesTheStartupWarning()
    {
        var repo = new InMemoryRepository(new LibraryData { SchemaVersion = LibraryData.CurrentSchemaVersion })
        {
            LastError = "Данните са възстановени от копие.",
        };

        var s = new LibraryService(repo, () => TestLibrary.Today);

        Assert.Equal("Данните са възстановени от копие.", s.LoadNotice);
    }

    [Fact]
    public void RejectedOperation_RaisesNoStorageEvent()
    {
        var repo = new InMemoryRepository(new LibraryData { SchemaVersion = LibraryData.CurrentSchemaVersion });
        var s = new LibraryService(repo, () => TestLibrary.Today);
        var raised = 0;
        s.StorageStatusChanged += _ => raised++;

        s.AddReader("", "", ReaderRole.Student);

        Assert.Equal(0, raised);
    }
}
