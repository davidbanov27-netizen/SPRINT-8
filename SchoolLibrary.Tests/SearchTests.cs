using SchoolLibrary.Models;
using SchoolLibrary.Services;

namespace SchoolLibrary.Tests;

/// <summary>Етап 7: търсене по заглавие и автор с наличност – и гранични случаи.</summary>
public class SearchTests
{
    private static LibraryService Library()
    {
        var s = TestLibrary.Empty();
        s.AddBookOk("Под игото", 2, "Иван Вазов");
        s.AddBookOk("Бай Ганьо", 1, "Алеко Константинов");
        s.AddBookOk("Хари Потър и философският камък", 3, "Дж. К. Роулинг");
        return s;
    }

    private static List<string> Titles(IEnumerable<BookSearchResult> results) => results.Select(r => r.Book.Title).ToList();

    // ===== Правилни резултати =====

    [Fact]
    public void ByTitle_FindsTheBook()
    {
        Assert.Equal(new[] { "Под игото" }, Titles(Library().FindBooks("Под игото")));
    }

    [Fact]
    public void ByAuthor_FullNameAndSurname()
    {
        var s = Library();

        Assert.Equal(new[] { "Под игото" }, Titles(s.FindBooks("Иван Вазов")));
        Assert.Equal(new[] { "Под игото" }, Titles(s.FindBooks("Вазов")));
    }

    [Theory]
    [InlineData("ВАЗОВ")]
    [InlineData("вазов")]
    [InlineData("  вАзОв  ")]
    public void IsCaseAndSpaceInsensitive(string query)
    {
        Assert.Single(Library().FindBooks(query));
    }

    [Fact]
    public void WhileTyping_PrefixOfWordsIsEnough()
    {
        var s = Library();

        Assert.Equal(new[] { "Под игото" }, Titles(s.FindBooks("под иг")));
        Assert.Equal(new[] { "Бай Ганьо" }, Titles(s.FindBooks("ган")));
    }

    [Fact]
    public void OneTypoIsTolerated_InWordsOfFourOrMoreLetters()
    {
        Assert.Equal(new[] { "Под игото" }, Titles(Library().FindBooks("Вазав")));
    }

    [Fact]
    public void ShortWordsAreNotFuzzy()
    {
        var s = TestLibrary.Empty();
        s.AddBookOk("Кат", 1, "Автор Един");

        Assert.Empty(s.FindBooks("кар")); // „кар“ е на 1 буква от „кат“, но думата е само 3 букви
    }

    [Fact]
    public void AllWordsMustMatch()
    {
        var s = Library();

        Assert.Single(s.FindBooks("Под игото Вазов"));
        Assert.Empty(s.FindBooks("Под Роулинг"));
    }

    [Fact]
    public void TitleAndAuthorWordsCanBeMixed()
    {
        Assert.Equal(new[] { "Бай Ганьо" }, Titles(Library().FindBooks("ганьо алеко")));
    }

    [Fact]
    public void PunctuationDoesNotMatter()
    {
        var s = Library();

        Assert.Single(s.FindBooks("хари, потър!"));
        Assert.Single(s.FindBooks("Дж. К. Роулинг"));
        Assert.Single(s.FindBooks("дж к роулинг"));
        Assert.Single(s.FindBooks("„Под игото“"));
    }

    [Fact]
    public void NothingFound_ReturnsEmptyList()
    {
        Assert.Empty(Library().FindBooks("Хемингуей"));
    }

    [Fact]
    public void ByIsbn_WithOrWithoutHyphens()
    {
        var s = TestLibrary.Empty();
        var draft = TestLibrary.Draft("Книга с ISBN", "Някой Автор");
        draft.Isbn = "978-0-306-40615-7";
        Assert.True(s.AddBook(draft, 1).Success);
        s.AddBookOk("Друга книга", 1, "Друг Автор");

        Assert.Equal(new[] { "Книга с ISBN" }, Titles(s.FindBooks("978-0-306-40615-7")));
        Assert.Equal(new[] { "Книга с ISBN" }, Titles(s.FindBooks("9780306406157")));
    }

    // ===== Подредба =====

    [Fact]
    public void TitleMatchRanksAboveAuthorMatch()
    {
        var s = TestLibrary.Empty();
        s.AddBookOk("Под игото", 1, "Иван Вазов");
        s.AddBookOk("Вазов и приятели", 1, "Друг Автор");

        Assert.Equal(new[] { "Вазов и приятели", "Под игото" }, Titles(s.FindBooks("вазов")));
    }

    [Fact]
    public void ExactTitleRanksAbovePartialTitle()
    {
        var s = TestLibrary.Empty();
        s.AddBookOk("Лятото на баба", 1, "Автор Първи");
        s.AddBookOk("Лято", 1, "Автор Втори");

        Assert.Equal(new[] { "Лято", "Лятото на баба" }, Titles(s.FindBooks("лято")));
    }

    [Fact]
    public void EqualScores_AreSortedByTitle()
    {
        var s = TestLibrary.Empty();
        s.AddBookOk("Бета", 1, "Иван Иванов");
        s.AddBookOk("Алфа", 1, "Иван Иванов");

        Assert.Equal(new[] { "Алфа", "Бета" }, Titles(s.FindBooks("иванов")));
    }

    // ===== Празна заявка и особени знаци =====

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyQuery_ReturnsWholeCatalog_SortedByTitle(string? query)
    {
        var titles = Titles(Library().FindBooks(query));

        Assert.Equal(new[] { "Бай Ганьо", "Под игото", "Хари Потър и философският камък" }, titles);
    }

    [Theory]
    [InlineData("---")]
    [InlineData("!!!")]
    [InlineData(" . , ; ")]
    public void OnlyPunctuation_FindsNothing(string query)
    {
        Assert.Empty(Library().FindBooks(query));
    }

    [Fact]
    public void VeryLongQuery_DoesNotCrash()
    {
        Assert.Empty(Library().FindBooks(new string('а', 5000)));
    }

    [Fact]
    public void SingleLetter_DoesNotCrash()
    {
        Assert.NotEmpty(Library().FindBooks("п"));
    }

    [Fact]
    public void DeactivatedBook_IsNotFound()
    {
        var s = Library();
        var book = s.Books.Single(b => b.Title == "Бай Ганьо");
        Assert.True(s.DeactivateBook(book).Success);

        Assert.Empty(s.FindBooks("Ганьо"));
        Assert.DoesNotContain("Бай Ганьо", Titles(s.FindBooks("")));
    }

    // ===== Наличност =====

    [Fact]
    public void Availability_AllCopiesFree()
    {
        var result = Library().FindBooks("Под игото").Single();

        Assert.Equal(2, result.Available);
        Assert.Equal(2, result.Total);
        Assert.True(result.IsAvailable);
        Assert.Equal("Налични: 2 от 2", result.AvailabilityText);
    }

    [Fact]
    public void Availability_OneCopyOnLoan()
    {
        var s = Library();
        var book = s.Books.Single(b => b.Title == "Хари Потър и философският камък");
        s.Lend(s.AddReaderOk(), book);

        var result = s.FindBooks("Потър").Single();

        Assert.Equal(2, result.Available);
        Assert.Equal(3, result.Total);
    }

    [Fact]
    public void Availability_AllCopiesOnLoan()
    {
        var s = Library();
        s.Lend(s.AddReaderOk(), s.Books.Single(b => b.Title == "Бай Ганьо"));

        var result = s.FindBooks("Ганьо").Single();

        Assert.Equal(0, result.Available);
        Assert.False(result.IsAvailable);
        Assert.Equal("Всички екземпляри са заети", result.AvailabilityText);
    }

    [Fact]
    public void Availability_BookWithoutCopies()
    {
        var s = TestLibrary.Empty();
        s.AddBookOk("Без копия", 0, "Автор Нула");

        var result = s.FindBooks("копия").Single();

        Assert.Equal(0, result.Total);
        Assert.Equal("Няма екземпляри", result.AvailabilityText);
    }

    [Fact]
    public void Availability_DamagedCopyIsTotalButNotAvailable()
    {
        var s = TestLibrary.Empty();
        var book = s.AddBookOk("Повредена", 2, "Автор Два");
        var copy = s.CopiesOf(book)[0];
        s.UpdateCopy(copy.Id, copy.InventoryNumber, CopyStatus.Damaged);

        var result = s.FindBooks("повредена").Single();

        Assert.Equal(1, result.Available);
        Assert.Equal(2, result.Total);
    }

    [Fact]
    public void Availability_ChangesAfterReturn()
    {
        var s = TestLibrary.Empty();
        var book = s.AddBookOk("Връщана", 1, "Автор Три");
        var loan = s.Lend(s.AddReaderOk(), book).Value!;
        Assert.False(s.FindBooks("връщана").Single().IsAvailable);

        s.Return(loan, BookCondition.Good);

        Assert.True(s.FindBooks("връщана").Single().IsAvailable);
    }

    [Fact]
    public void Seeded_EveryResultShowsTheRealAvailability()
    {
        var s = TestLibrary.Seeded();

        var all = s.FindBooks("");

        Assert.Equal(s.Books.Count, all.Count);
        foreach (var r in all)
        {
            Assert.Equal(s.Available(r.Book), r.Available);
            Assert.Equal(s.TotalCopies(r.Book), r.Total);
        }
    }
}
