using SchoolLibrary.Models;
using SchoolLibrary.Services;
using SchoolLibrary.ViewModels;

namespace SchoolLibrary.Tests;

internal sealed class TestNavigator : INavigator
{
    public List<string> Keys { get; } = new();

    public void Navigate(string key) => Keys.Add(key);
}

/// <summary>
/// Интеграционни тестове на ниво екрани: ViewModel-ите работят заедно съсservice-а, валидацията и хранилището
/// точно както ги ползва потребителят (без да се отваря прозорец).
/// </summary>
public class ViewModelFlowTests
{
    private static MainViewModel Main(LibraryService s) => new(s, new LocalAiSearchService());

    private static LoginViewModel LoginOf(MainViewModel m) => (LoginViewModel)m.CurrentPage!;

    // ===== Каталог =====

    [Fact]
    public void Catalog_AddBook_ThroughTheForm()
    {
        var s = TestLibrary.Empty();
        var vm = new CatalogViewModel(s);

        vm.AddCommand.Execute(null);
        vm.BookTitle = "Нова книга";
        vm.BookAuthor = "Автор Нов";
        vm.BookGenre = "Роман";
        vm.BookCopies = "2";
        vm.SaveCommand.Execute(null);

        Assert.Equal(MessageKind.Success, vm.Kind);
        Assert.Single(s.Books);
        Assert.Equal(2, s.CopiesOf(s.Books[0]).Count);
    }

    [Fact]
    public void Catalog_NonNumericCopies_ShowsAnError_AndSavesNothing()
    {
        var s = TestLibrary.Empty();
        var vm = new CatalogViewModel(s);

        vm.AddCommand.Execute(null);
        vm.BookTitle = "Нова";
        vm.BookAuthor = "Автор Нов";
        vm.BookGenre = "Роман";
        vm.BookCopies = "abc";
        vm.SaveCommand.Execute(null);

        Assert.Equal(MessageKind.Error, vm.Kind);
        Assert.Empty(s.Books);
    }

    [Fact]
    public void Catalog_EditBook_ChangesTheTitle()
    {
        var s = TestLibrary.Seeded();
        var vm = new CatalogViewModel(s);
        vm.SelectedBook = vm.Books.First();
        var id = vm.SelectedBook!.Book.Id;

        vm.EditCommand.Execute(null);
        vm.BookTitle = "Променено заглавие";
        vm.SaveCommand.Execute(null);

        Assert.Equal(MessageKind.Success, vm.Kind);
        Assert.Equal("Променено заглавие", s.GetBook(id)!.Title);
    }

    [Fact]
    public void Catalog_DuplicateInventoryNumber_ShowsAnError()
    {
        var s = TestLibrary.Seeded();
        var vm = new CatalogViewModel(s);
        vm.SelectedBook = vm.Books.First();

        vm.NewInventoryNumber = "BK-00002"; // вече съществува в примерните данни
        vm.AddCopyCommand.Execute(null);

        Assert.Equal(MessageKind.Error, vm.Kind);
    }

    [Fact]
    public void Catalog_CannotDeactivate_BookWithACopyOnLoan()
    {
        var s = TestLibrary.Seeded();
        var vm = new CatalogViewModel(s);
        vm.SelectedBook = vm.Books.First(b => b.Book.Id == 2); // и двата екземпляра са заети

        vm.DeactivateCommand.Execute(null);

        Assert.Equal(MessageKind.Error, vm.Kind);
        Assert.Contains(s.GetBook(2)!, s.Books);
    }

    [Fact]
    public void Catalog_Search_UsesTitleAndAuthor()
    {
        var s = TestLibrary.Seeded();
        var vm = new CatalogViewModel(s);

        vm.SearchText = "йовков";

        Assert.True(vm.Books.Count >= 4);
        Assert.All(vm.Books, b => Assert.Equal("Йордан Йовков", b.Book.Author));
    }

    // ===== Читатели =====

    [Fact]
    public void Readers_AddReader_NormalizesTheClass()
    {
        var s = TestLibrary.Empty();
        var vm = new ReadersViewModel(s);

        vm.AddCommand.Execute(null);
        vm.Name = "Петър Петров";
        vm.ClassName = "7 б";
        vm.SaveCommand.Execute(null);

        Assert.Equal(MessageKind.Success, vm.Kind);
        Assert.Equal("7Б", s.AllReaders[0].ClassName);
    }

    [Fact]
    public void Readers_DuplicateReader_ShowsAnError()
    {
        var s = TestLibrary.Seeded();
        var vm = new ReadersViewModel(s);

        vm.AddCommand.Execute(null);
        vm.Name = "Иван Петров";
        vm.ClassName = "7Б";
        vm.SaveCommand.Execute(null);

        Assert.Equal(MessageKind.Error, vm.Kind);
    }

    [Fact]
    public void Readers_ToggleActive_DeactivatesAReaderWithoutLoans()
    {
        var s = TestLibrary.Seeded();
        var vm = new ReadersViewModel(s);
        var free = vm.Readers.First(r => r.IsActive && s.LoansOf(r.Reader).Count == 0);
        vm.SelectedReader = free;

        vm.ToggleActiveCommand.Execute(null);

        Assert.False(s.GetReader(free.Reader.Id)!.IsActive);
    }

    // ===== Заемане и връщане през екраните =====

    [Fact]
    public void Loan_ThenReturn_ThroughTheScreens()
    {
        var s = TestLibrary.Empty();
        var book = s.AddBookOk("Книга", 2, "Автор");
        var reader = s.AddReaderOk();

        var loan = new LoanViewModel(s, new TestNavigator());
        loan.SelectedReader = reader;
        loan.SelectedBook = loan.Books.Single(b => b.Book.Id == book.Id);
        Assert.True(loan.CanConfirm, string.Join("; ", loan.Problems));
        loan.ConfirmCommand.Execute(null);

        Assert.Equal(MessageKind.Success, loan.Kind);
        Assert.Single(s.ActiveLoans);

        var ret = new ReturnViewModel(s);
        ret.SelectedReader = reader;
        ret.SelectedLoan = ret.ActiveLoans[0];
        ret.ConfirmCommand.Execute(null);

        Assert.Equal(MessageKind.Success, ret.Kind);
        Assert.Empty(s.ActiveLoans);
        Assert.Equal(2, s.Available(book));
    }

    [Fact]
    public void Loan_BlockedReader_CannotConfirm()
    {
        var s = TestLibrary.Seeded();
        var loan = new LoanViewModel(s, new TestNavigator());

        loan.SelectedReader = s.GetReader(101); // има просрочена книга
        loan.SelectedBook = loan.Books.First(b => b.Available > 0 && !b.Book.ReferenceOnly);

        Assert.False(loan.CanConfirm);
        Assert.True(loan.HasProblems);
        Assert.Contains(loan.Problems, p => p.Contains("просрочена"));
    }

    [Fact]
    public void Loan_ChosenCopy_IsTheOneThatGetsLent()
    {
        var s = TestLibrary.Empty();
        s.AddBookOk("Книга", 3, "Автор");
        var loan = new LoanViewModel(s, new TestNavigator());
        loan.SelectedReader = s.AddReaderOk();
        loan.SelectedBook = loan.Books.Single();
        var third = loan.CopyChoices.Last();

        loan.SelectedCopyChoice = third;
        loan.ConfirmCommand.Execute(null);

        Assert.Equal(third.Copy!.Id, s.ActiveLoans.Single().CopyId);
    }

    [Fact]
    public void Loan_NoFreeCopy_ShowsAReason_AndNoChoices()
    {
        var s = TestLibrary.Empty();
        var book = s.AddBookOk("Книга", 1, "Автор");
        s.Lend(s.AddReaderOk("Иван Петров", "7Б"), book);
        var loan = new LoanViewModel(s, new TestNavigator());

        loan.SelectedReader = s.AddReaderOk("Мария Георгиева", "7Б");
        loan.SelectedBook = loan.Books.Single();

        Assert.False(loan.CanConfirm);
        Assert.Contains("Няма налично копие.", loan.Problems);
        Assert.Single(loan.CopyChoices); // само „автоматично“
    }

    [Fact]
    public void Return_ByInventoryNumber_FindsTheLoan()
    {
        var s = TestLibrary.Empty();
        var book = s.AddBookOk("Книга", 1, "Автор");
        s.Lend(s.AddReaderOk(), book);
        var ret = new ReturnViewModel(s);

        ret.InventoryInput = "bk-00001";
        ret.FindByNumberCommand.Execute(null);

        Assert.NotNull(ret.SelectedLoan);
        ret.ConfirmCommand.Execute(null);
        Assert.Empty(s.ActiveLoans);
    }

    [Fact]
    public void Return_UnknownInventoryNumber_ShowsAnError()
    {
        var ret = new ReturnViewModel(TestLibrary.Seeded());

        ret.InventoryInput = "ZZ-999";
        ret.FindByNumberCommand.Execute(null);

        Assert.Equal(MessageKind.Error, ret.Kind);
        Assert.Null(ret.SelectedLoan);
    }

    [Fact]
    public void Return_Lost_RemovesTheCopyFromTotals()
    {
        var s = TestLibrary.Empty();
        var book = s.AddBookOk("Книга", 2, "Автор");
        var reader = s.AddReaderOk();
        s.Lend(reader, book);
        var ret = new ReturnViewModel(s);
        ret.SelectedReader = reader;
        ret.SelectedLoan = ret.ActiveLoans[0];

        ret.SelectedCondition = ret.Conditions.First(c => c.Value == BookCondition.Lost);
        ret.ConfirmCommand.Execute(null);

        Assert.Equal(1, s.TotalCopies(book));
        Assert.Contains("изключено", ret.Message);
    }

    // ===== Читателски екрани =====

    [Fact]
    public void ReaderSearch_Keyword_ShowsResultsWithAvailability()
    {
        var s = TestLibrary.Seeded();
        var vm = new SearchViewModel(s, new LocalAiSearchService());

        vm.Query = "вазов";
        vm.KeywordCommand.Execute(null);

        Assert.True(vm.Results.Count >= 4);
        Assert.All(vm.Results, r => Assert.Contains("от", r.AvailabilityText.Replace("Всички са заети ✖", "от").Replace("Няма екземпляри ✖", "от")));
    }

    [Fact]
    public void ReaderSearch_EmptyQuery_Warns()
    {
        var vm = new SearchViewModel(TestLibrary.Seeded(), new LocalAiSearchService());

        vm.Query = "   ";
        vm.KeywordCommand.Execute(null);

        Assert.Equal(MessageKind.Warning, vm.Kind);
    }

    [Fact]
    public void Dashboard_And_Overdue_AgreeWithTheService()
    {
        var s = TestLibrary.Seeded();

        var dash = new DashboardViewModel(s, new TestNavigator());
        var overdue = new OverdueViewModel(s);

        Assert.Equal(s.ActiveLoans.Count(), dash.ActiveLoans);
        Assert.Equal(s.OverdueLoans().Count, dash.OverdueCount);
        Assert.Equal(s.OverdueLoans().Count, overdue.Rows.Count);
        Assert.Equal(s.Books.Sum(b => s.TotalCopies(b)), dash.TotalCopies);
    }

    [Fact]
    public void Dashboard_Shortcut_Navigates()
    {
        var m = Main(TestLibrary.Seeded());
        LoginOf(m).LoginCommand.Execute(null);

        ((DashboardViewModel)m.CurrentPage!).LoanCommand.Execute(null);

        Assert.True(m.CurrentPage is LoanViewModel);
    }

    // ===== Главен прозорец =====

    [Fact]
    public void Main_Librarian_CanOpenEveryScreen()
    {
        var m = Main(TestLibrary.Seeded());
        LoginOf(m).LoginCommand.Execute(null);

        foreach (var item in m.NavItems.ToList())
        {
            m.SelectedNav = item;
            Assert.NotNull(m.CurrentPage);
        }

        Assert.Equal(7, m.NavItems.Count);
    }

    [Fact]
    public void Main_EveryReader_CanOpenEveryScreen()
    {
        var s = TestLibrary.Seeded();

        foreach (var reader in s.Readers)
        {
            var m = Main(s);
            var login = LoginOf(m);
            login.RoleIndex = 1;
            login.SelectedReader = reader;
            login.LoginCommand.Execute(null);

            foreach (var item in m.NavItems.ToList())
            {
                m.SelectedNav = item;
                Assert.NotNull(m.CurrentPage);
            }
        }
    }

    [Fact]
    public void Main_Logout_ReturnsToLogin()
    {
        var m = Main(TestLibrary.Seeded());
        LoginOf(m).LoginCommand.Execute(null);

        m.LogoutCommand.Execute(null);

        Assert.False(m.IsSignedIn);
        Assert.True(m.CurrentPage is LoginViewModel);
    }

    [Fact]
    public void Main_EmptyLibrary_ReaderLoginIsDisabled_LibrarianWorks()
    {
        var m = Main(TestLibrary.Empty());
        var login = LoginOf(m);

        login.RoleIndex = 1;
        Assert.False(login.LoginCommand.CanExecute(null));

        login.RoleIndex = 0;
        Assert.True(login.LoginCommand.CanExecute(null));
        login.LoginCommand.Execute(null);
        foreach (var item in m.NavItems.ToList())
        {
            m.SelectedNav = item; // всички екрани трябва да се отварят и върху празна библиотека
            Assert.NotNull(m.CurrentPage);
        }
    }

    [Fact]
    public void Main_ShowsStorageWarnings_AndClearsThemAfterASuccessfulSave()
    {
        var repo = new InMemoryRepository(new LibraryData { SchemaVersion = LibraryData.CurrentSchemaVersion })
        {
            LastError = "Повреден файл",
        };
        var s = new LibraryService(repo, () => TestLibrary.Today);
        var m = Main(s);
        Assert.True(m.HasStorageNotice);

        m.DismissNoticeCommand.Execute(null);
        Assert.False(m.HasStorageNotice);

        repo.LastError = "диск пълен";
        s.AddBookOk();
        Assert.True(m.HasStorageNotice);

        repo.LastError = null;
        s.AddBookOk("Втора", 1, "Друг");
        Assert.False(m.HasStorageNotice);
    }

    [Fact]
    public void Recommendations_WorkForTeachers_AndReadersWithoutHistory()
    {
        var s = TestLibrary.Seeded();
        var fresh = s.AddReaderOk("Нов Ученик", "5Б");

        _ = new RecsViewModel(s, s.GetReader(104)!);
        _ = new RecsViewModel(s, fresh);
        var mine = new MyBooksViewModel(s, fresh);

        Assert.True(mine.IsEmpty);
    }

    [Fact]
    public void AllScreens_Construct_OnAnEmptyLibrary()
    {
        var s = TestLibrary.Empty();
        var reader = s.AddReaderOk();

        _ = new DashboardViewModel(s, new TestNavigator());
        _ = new StatsViewModel(s);
        _ = new OverdueViewModel(s);
        _ = new CatalogViewModel(s);
        _ = new ReadersViewModel(s);
        _ = new LoanViewModel(s, new TestNavigator());
        _ = new ReturnViewModel(s);
        _ = new MyBooksViewModel(s, reader);
        _ = new RecsViewModel(s, reader);
        _ = new SearchViewModel(s, new LocalAiSearchService());
    }
}
