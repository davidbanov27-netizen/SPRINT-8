using SchoolLibrary.Models;
using SchoolLibrary.Services.Validation;

namespace SchoolLibrary.Tests;

/// <summary>Създаване, редактиране и премахване на книги (заглавия).</summary>
public class BookTests
{
    // ===== Създаване =====

    [Fact]
    public void AddBook_with_valid_data_creates_book_and_numbered_copies()
    {
        var s = TestLibrary.Empty();

        var result = s.AddBook(TestLibrary.Draft("Под игото", "Иван Вазов"), copies: 3);

        Assert.True(result.Success);
        var copies = s.CopiesOf(result.Value!);
        Assert.Equal(3, copies.Count);
        Assert.Equal(new[] { "BK-00001", "BK-00002", "BK-00003" }, copies.Select(c => c.InventoryNumber));
        Assert.All(copies, c => Assert.Equal(CopyStatus.Available, c.Status));
        Assert.Equal(3, s.Available(result.Value!));
    }

    [Fact]
    public void AddBook_trims_whitespace()
    {
        var s = TestLibrary.Empty();

        var result = s.AddBook(TestLibrary.Draft("  Хобитът  ", "  Дж. Р. Р. Толкин "));

        Assert.True(result.Success);
        Assert.Equal("Хобитът", result.Value!.Title);
        Assert.Equal("Дж. Р. Р. Толкин", result.Value.Author);
    }

    [Fact]
    public void Books_in_one_library_get_continuing_inventory_numbers()
    {
        var s = TestLibrary.Empty();
        s.AddBookOk("Първа", copies: 2);
        var second = s.AddBookOk("Втора", copies: 2);

        Assert.Equal(new[] { "BK-00003", "BK-00004" }, s.CopiesOf(second).Select(c => c.InventoryNumber));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AddBook_requires_title(string title)
    {
        var s = TestLibrary.Empty();

        var result = s.AddBook(TestLibrary.Draft(title: title));

        Assert.False(result.Success);
        Assert.True(result.HasError(nameof(Book.Title)));
        Assert.Contains("заглавие", result.Summary);
        Assert.Empty(s.Books);
    }

    [Fact]
    public void AddBook_requires_author_and_genre()
    {
        var s = TestLibrary.Empty();

        var result = s.AddBook(TestLibrary.Draft(author: " ", genre: ""));

        Assert.False(result.Success);
        Assert.True(result.HasError(nameof(Book.Author)));
        Assert.True(result.HasError(nameof(Book.Genre)));
        Assert.Empty(s.Books);
    }

    [Fact]
    public void AddBook_reports_all_problems_at_once()
    {
        var s = TestLibrary.Empty();

        var result = s.AddBook(TestLibrary.Draft(title: "", author: "", genre: ""));

        Assert.Equal(3, result.Errors.Count);
        Assert.StartsWith("•", result.Summary);
    }

    [Fact]
    public void AddBook_rejects_too_long_title()
    {
        var s = TestLibrary.Empty();

        var result = s.AddBook(TestLibrary.Draft(title: new string('а', BookValidator.MaxTitle + 1)));

        Assert.False(result.Success);
        Assert.True(result.HasError(nameof(Book.Title)));
    }

    [Theory]
    [InlineData(0, 5)]
    [InlineData(1, 13)]
    [InlineData(8, 5)]
    public void AddBook_rejects_invalid_grade_range(int min, int max)
    {
        var s = TestLibrary.Empty();
        var draft = TestLibrary.Draft();
        draft.MinGrade = min;
        draft.MaxGrade = max;

        var result = s.AddBook(draft);

        Assert.False(result.Success);
        Assert.True(result.HasError(nameof(Book.MinGrade)) || result.HasError(nameof(Book.MaxGrade)));
        Assert.Empty(s.Books);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(51)]
    public void AddBook_rejects_invalid_copy_count(int copies)
    {
        var s = TestLibrary.Empty();

        var result = s.AddBook(TestLibrary.Draft(), copies);

        Assert.False(result.Success);
        Assert.True(result.HasError("Copies"));
        Assert.Empty(s.Books);
    }

    // ===== ISBN =====

    [Fact]
    public void AddBook_accepts_valid_isbn_and_stores_it_without_hyphens()
    {
        var s = TestLibrary.Empty();
        var draft = TestLibrary.Draft();
        draft.Isbn = "978-0-306-40615-7";

        var result = s.AddBook(draft);

        Assert.True(result.Success);
        Assert.Equal("9780306406157", result.Value!.Isbn);
    }

    [Fact]
    public void AddBook_rejects_isbn_with_wrong_check_digit()
    {
        var s = TestLibrary.Empty();
        var draft = TestLibrary.Draft();
        draft.Isbn = "9780306406158";

        var result = s.AddBook(draft);

        Assert.False(result.Success);
        Assert.True(result.HasError(nameof(Book.Isbn)));
    }

    [Fact]
    public void AddBook_rejects_duplicate_isbn_even_when_written_differently()
    {
        var s = TestLibrary.Empty();
        var first = TestLibrary.Draft("Първа книга", "Автор Първи");
        first.Isbn = "9780306406157";
        Assert.True(s.AddBook(first).Success);

        var second = TestLibrary.Draft("Втора книга", "Автор Втори");
        second.Isbn = "978-0-306-40615-7";
        var result = s.AddBook(second);

        Assert.False(result.Success);
        Assert.True(result.HasError(nameof(Book.Isbn)));
        Assert.Contains("Първа книга", result.Summary);
        Assert.Single(s.Books);
    }

    // ===== Дубликати =====

    [Fact]
    public void AddBook_rejects_same_title_and_author_ignoring_case()
    {
        var s = TestLibrary.Empty();
        s.AddBookOk("Под игото", author: "Иван Вазов");

        var result = s.AddBook(TestLibrary.Draft("ПОД ИГОТО", "иван вазов"));

        Assert.False(result.Success);
        Assert.True(result.HasError(nameof(Book.Title)));
        Assert.Contains("вече съществува", result.Summary);
        Assert.Single(s.Books);
    }

    [Fact]
    public void AddBook_allows_same_title_by_different_author()
    {
        var s = TestLibrary.Empty();
        s.AddBookOk("Приказки", author: "Автор А");

        var result = s.AddBook(TestLibrary.Draft("Приказки", "Автор Б"));

        Assert.True(result.Success);
        Assert.Equal(2, s.Books.Count);
    }

    // ===== Редактиране =====

    [Fact]
    public void UpdateBook_changes_fields_but_keeps_copies()
    {
        var s = TestLibrary.Empty();
        var book = s.AddBookOk("Хобитът", copies: 2, author: "Толкин");

        var draft = new Book
        {
            Id = book.Id, Title = "Хобитът (ново издание)", Author = "Дж. Р. Р. Толкин",
            Genre = "Фентъзи", MinGrade = 6, MaxGrade = 9, Shelf = "Б-04",
        };
        var result = s.UpdateBook(draft);

        Assert.True(result.Success);
        var stored = s.GetBook(book.Id)!;
        Assert.Equal("Хобитът (ново издание)", stored.Title);
        Assert.Equal("Фентъзи", stored.Genre);
        Assert.Equal(6, stored.MinGrade);
        Assert.Equal(2, s.CopiesOf(stored).Count);
    }

    [Fact]
    public void UpdateBook_does_not_treat_itself_as_duplicate()
    {
        var s = TestLibrary.Empty();
        var book = s.AddBookOk("Хобитът", author: "Толкин");

        var draft = TestLibrary.Draft("Хобитът", "Толкин", genre: "Фентъзи");
        draft.Id = book.Id;
        var result = s.UpdateBook(draft);

        Assert.True(result.Success);
        Assert.Equal("Фентъзи", s.GetBook(book.Id)!.Genre);
    }

    [Fact]
    public void UpdateBook_rejects_title_and_author_of_another_book()
    {
        var s = TestLibrary.Empty();
        s.AddBookOk("Под игото", author: "Иван Вазов");
        var other = s.AddBookOk("Бай Ганьо", author: "Алеко Константинов");

        var draft = TestLibrary.Draft("Под игото", "Иван Вазов");
        draft.Id = other.Id;
        var result = s.UpdateBook(draft);

        Assert.False(result.Success);
        Assert.True(result.HasError(nameof(Book.Title)));
        Assert.Equal("Бай Ганьо", s.GetBook(other.Id)!.Title);
    }

    [Fact]
    public void UpdateBook_rejects_invalid_data_and_leaves_book_unchanged()
    {
        var s = TestLibrary.Empty();
        var book = s.AddBookOk("Хобитът");

        var draft = TestLibrary.Draft(title: "");
        draft.Id = book.Id;
        var result = s.UpdateBook(draft);

        Assert.False(result.Success);
        Assert.Equal("Хобитът", s.GetBook(book.Id)!.Title);
    }

    [Fact]
    public void UpdateBook_of_unknown_id_fails()
    {
        var s = TestLibrary.Empty();

        var draft = TestLibrary.Draft();
        draft.Id = 999;
        var result = s.UpdateBook(draft);

        Assert.False(result.Success);
        Assert.Contains("не е намерена", result.Summary);
    }

    // ===== Премахване =====

    [Fact]
    public void DeactivateBook_hides_it_and_withdraws_its_copies()
    {
        var s = TestLibrary.Empty();
        var book = s.AddBookOk("Стара книга", copies: 2);

        var result = s.DeactivateBook(book);

        Assert.True(result.Success);
        Assert.Empty(s.Books);
        Assert.All(s.CopiesOf(book), c => Assert.Equal(CopyStatus.Withdrawn, c.Status));
    }

    [Fact]
    public void DeactivateBook_is_refused_while_a_copy_is_on_loan()
    {
        var s = TestLibrary.Empty();
        var book = s.AddBookOk("Заета книга", copies: 2);
        var reader = s.AddReaderOk();
        s.CreateLoan(reader, book);

        var result = s.DeactivateBook(book);

        Assert.False(result.Success);
        Assert.Contains("неврънати", result.Summary);
        Assert.Single(s.Books);
    }

    [Fact]
    public void Deactivated_book_title_can_be_added_again()
    {
        var s = TestLibrary.Empty();
        var book = s.AddBookOk("Под игото", author: "Иван Вазов");
        s.DeactivateBook(book);

        var result = s.AddBook(TestLibrary.Draft("Под игото", "Иван Вазов"));

        Assert.True(result.Success);
    }

    // ===== Търсене =====

    [Fact]
    public void SearchKeyword_tolerates_one_spelling_mistake()
    {
        var s = TestLibrary.Seeded();

        var found = s.SearchKeyword("мускетери"); // правилно: мускетари

        Assert.Contains(found, b => b.Title == "Тримата мускетари");
    }

    [Fact]
    public void SearchKeyword_with_empty_query_returns_all_books()
    {
        var s = TestLibrary.Seeded();

        Assert.Equal(s.Books.Count, s.SearchKeyword("  ").Count);
    }

    [Fact]
    public void SearchKeyword_with_no_match_returns_empty_list()
    {
        var s = TestLibrary.Seeded();

        Assert.Empty(s.SearchKeyword("квантова механика"));
    }

    [Fact]
    public void Levenshtein_counts_single_edits()
    {
        Assert.Equal(0, SchoolLibrary.Services.LibraryService.Levenshtein("книга", "книга"));
        Assert.Equal(1, SchoolLibrary.Services.LibraryService.Levenshtein("мускетари", "мускетери"));
        Assert.Equal(3, SchoolLibrary.Services.LibraryService.Levenshtein("кат", "куче"));
    }

    // ===== Съобщения =====

    [Fact]
    public void OperationResult_summary_is_plain_text_for_one_error_and_bullets_for_many()
    {
        var one = OperationResult.Fail("X", "Една грешка.");
        var many = new OperationResult(new[] { new ValidationError("A", "Първа."), new ValidationError("B", "Втора.") });

        Assert.Equal("Една грешка.", one.Summary);
        Assert.Contains("• Първа.", many.Summary);
        Assert.Contains("• Втора.", many.Summary);
        Assert.True(OperationResult.Ok().Success);
    }
}
