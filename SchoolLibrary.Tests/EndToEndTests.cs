using System.IO;
using System.Text.Json;
using SchoolLibrary.Models;
using SchoolLibrary.Services;
using SchoolLibrary.ViewModels;

namespace SchoolLibrary.Tests;

/// <summary>
/// Основният end-to-end сценарий (E2E-01): от регистрирането на книга и читател до просрочие, връщане и рестарт –
/// през екраните, услугата и истински файл, като нов „ден“ се симулира с часовника.
/// </summary>
public sealed class EndToEndTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "SchoolLibraryE2E_" + Guid.NewGuid().ToString("N"));
    private DateTime _now = TestLibrary.Today;

    public EndToEndTests()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(Path.Combine(_dir, "library.json"),
            JsonSerializer.Serialize(new LibraryData { SchemaVersion = LibraryData.CurrentSchemaVersion }));
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

    /// <summary>„Стартира“ програмата: нова услуга върху същия файл.</summary>
    private LibraryService StartApp() => new(new DataStore(Path.Combine(_dir, "library.json")), () => _now);

    [Fact]
    public void MainScenario_FromRegistrationToLateReturn_AndRestart()
    {
        var s = StartApp();

        // 1. Библиотекарят регистрира книга с 2 екземпляра.
        var catalog = new CatalogViewModel(s);
        catalog.AddCommand.Execute(null);
        catalog.BookTitle = "Железният светилник";
        catalog.BookAuthor = "Димитър Талев";
        catalog.BookGenre = "Роман";
        catalog.BookCopies = "2";
        catalog.SaveCommand.Execute(null);
        Assert.Equal(MessageKind.Success, catalog.Kind);

        // 2. Регистрира читател.
        var readers = new ReadersViewModel(s);
        readers.AddCommand.Execute(null);
        readers.Name = "Мария Георгиева";
        readers.ClassName = "10 б";
        readers.SaveCommand.Execute(null);
        Assert.Equal(MessageKind.Success, readers.Kind);
        var reader = s.AllReaders.Single();
        Assert.Equal("10Б", reader.ClassName);

        // 3. Читателят търси книгата и вижда наличността.
        var search = new SearchViewModel(s, new LocalAiSearchService());
        search.Query = "талев светилник";
        search.KeywordCommand.Execute(null);
        var found = Assert.Single(search.Results);
        Assert.Equal("Налични: 2 от 2 ✔", found.AvailabilityText);

        // 4. Заемане през екрана „Заемане“.
        var loan = new LoanViewModel(s, new TestNavigator());
        loan.SelectedReader = reader;
        loan.SelectedBook = loan.Books.Single();
        Assert.True(loan.CanConfirm, string.Join("; ", loan.Problems));
        loan.ConfirmCommand.Execute(null);
        Assert.Equal(MessageKind.Success, loan.Kind);
        var taken = s.ActiveLoans.Single();
        Assert.Equal(_now.AddDays(14), taken.DueDate);

        // 5. Наличността се е променила.
        search.KeywordCommand.Execute(null);
        Assert.Equal("Налични: 1 от 2 ✔", search.Results.Single().AvailabilityText);

        // 6. Същото заглавие втори път се отказва.
        var again = new LoanViewModel(s, new TestNavigator());
        again.SelectedReader = reader;
        again.SelectedBook = again.Books.Single();
        Assert.False(again.CanConfirm);
        Assert.Contains("Читателят вече има това заглавие.", again.Problems);

        // 7. Минават 15 дни: заемът е просрочен, читателят е блокиран, таблото го показва.
        _now = _now.AddDays(15);
        Assert.True(s.IsOverdue(taken));
        Assert.True(s.IsBlocked(reader));
        Assert.Equal(1, new DashboardViewModel(s, new TestNavigator()).OverdueCount);
        Assert.Single(new OverdueViewModel(s).Rows);

        // 8. Просрочен читател не може да заеме друга книга.
        var other = s.AddBookOk("Тютюн", 1, "Димитър Димов");
        var blocked = new LoanViewModel(s, new TestNavigator());
        blocked.SelectedReader = reader;
        blocked.SelectedBook = blocked.Books.Single(b => b.Book.Id == other.Id);
        Assert.False(blocked.CanConfirm);

        // 9. Връщане по инвентарен номер: закъснение 1 ден, блокировката се сваля.
        var ret = new ReturnViewModel(s);
        ret.InventoryInput = s.GetCopy(taken.CopyId)!.InventoryNumber;
        ret.FindByNumberCommand.Execute(null);
        ret.ConfirmCommand.Execute(null);
        Assert.Contains("Закъснение: 1 дни", ret.Message);
        Assert.False(s.IsBlocked(reader));
        Assert.Equal(2, s.Available(s.GetBook(taken.BookId)!));

        // 10. Вече може да заеме другата книга.
        Assert.True(s.Lend(reader, other).Success);

        // 11. Рестарт: всичко е запазено и няма противоречия.
        var restarted = StartApp();
        Assert.Single(restarted.ActiveLoans);
        Assert.Equal(2, restarted.AllLoans.Count);
        Assert.False(restarted.AllLoans.Single(l => l.Id == taken.Id).IsActive);
        Assert.Equal(other.Id, restarted.ActiveLoans.Single().BookId);
        Assert.Equal(2, restarted.Books.Count);
        Assert.Empty(DataIntegrity.Check(new DataStore(Path.Combine(_dir, "library.json")).Load()));
    }

    [Fact]
    public void Scenario_WithSeedData_DoesNotBreakTheRules()
    {
        var path = Path.Combine(_dir, "seed.json");
        var s = new LibraryService(new DataStore(path), () => _now);

        // Всички просрочени читатели са блокирани и връщането им отблокира.
        foreach (var loan in s.OverdueLoans().ToList())
        {
            var reader = s.GetReader(loan.ReaderId)!;
            var book = s.Books.First(b => !b.ReferenceOnly && s.Available(b) > 0 && s.LoansOf(reader).All(l => l.BookId != b.Id));
            Assert.False(s.Lend(reader, book).Success);

            Assert.True(s.Return(loan, BookCondition.Good).Success);
        }

        Assert.Empty(s.OverdueLoans());

        // След рестарт данните са същите и без противоречия.
        var restarted = new LibraryService(new DataStore(path), () => _now);
        Assert.Empty(restarted.OverdueLoans());
        Assert.Equal(s.ActiveLoans.Count(), restarted.ActiveLoans.Count());
        Assert.Empty(DataIntegrity.Check(new DataStore(path).Load()));
    }

    [Fact]
    public void Scenario_ManyOperations_KeepTheDataConsistent()
    {
        var path = Path.Combine(_dir, "stress.json");
        var s = new LibraryService(new DataStore(path), () => _now);
        var rnd = new Random(8);

        for (var i = 0; i < 300; i++)
        {
            var reader = s.Readers[rnd.Next(s.Readers.Count)];
            var book = s.Books[rnd.Next(s.Books.Count)];
            if (rnd.Next(3) == 0 && s.LoansOf(reader).Count > 0)
            {
                s.Return(s.LoansOf(reader)[0], rnd.Next(10) == 0 ? BookCondition.Damaged : BookCondition.Good);
            }
            else
            {
                s.Lend(reader, book); // успехът не е важен – важно е правилата да не се нарушават
            }

            if (i % 25 == 0)
            {
                _now = _now.AddDays(3);
            }
        }

        Assert.Empty(DataIntegrity.Check(new DataStore(path).Load()));
        foreach (var reader in s.AllReaders)
        {
            Assert.True(s.LoansOf(reader).Count <= s.LimitOf(reader), reader.Name);
            var titles = s.LoansOf(reader).Select(l => l.BookId).ToList();
            Assert.Equal(titles.Count, titles.Distinct().Count());
        }
    }
}
