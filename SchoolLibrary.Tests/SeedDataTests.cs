using SchoolLibrary.Models;
using SchoolLibrary.Services;
using SchoolLibrary.Services.Validation;

namespace SchoolLibrary.Tests;

/// <summary>Примерните данни трябва да изглеждат като истинска библиотека и да спазват собствените ѝ правила.</summary>
public class SeedDataTests
{
    private static readonly LibraryData Data = SeedData.Create(TestLibrary.Today);

    [Fact]
    public void Catalog_IsRealisticallyLarge()
    {
        Assert.True(Data.Books.Count >= 55, $"книги: {Data.Books.Count}");
        Assert.True(Data.Copies.Count >= 100, $"екземпляри: {Data.Copies.Count}");
    }

    [Theory]
    [InlineData("Иван Вазов")]
    [InlineData("Йордан Йовков")]
    [InlineData("Елин Пелин")]
    [InlineData("Димитър Талев")]
    [InlineData("Димитър Димов")]
    [InlineData("Христо Ботев")]
    [InlineData("Никола Вапцаров")]
    [InlineData("Алеко Константинов")]
    [InlineData("Емилиян Станев")]
    [InlineData("Георги Господинов")]
    public void Catalog_ContainsBulgarianAuthors(string author)
    {
        Assert.Contains(Data.Books, b => b.Author == author);
    }

    [Fact]
    public void Catalog_HasSeveralGenres()
    {
        var genres = Data.Books.Select(b => b.Genre).Distinct().ToList();

        Assert.True(genres.Count >= 8, string.Join(", ", genres));
        Assert.All(Data.Books, b => Assert.False(string.IsNullOrWhiteSpace(b.Genre)));
    }

    [Fact]
    public void EveryBook_PassesTheSameValidationAsNewBooks()
    {
        foreach (var book in Data.Books)
        {
            var errors = BookValidator.Validate(book, Data.Books.Where(b => b.Id != book.Id));
            Assert.True(errors.Count == 0, $"{book.Title}: {string.Join("; ", errors.Select(e => e.Message))}");
        }
    }

    [Fact]
    public void EveryReader_PassesTheSameValidationAsNewReaders()
    {
        foreach (var reader in Data.Readers)
        {
            var errors = ReaderValidator.Validate(reader, Data.Readers.Where(r => r.Id != reader.Id));
            Assert.True(errors.Count == 0, $"{reader.Name}: {string.Join("; ", errors.Select(e => e.Message))}");
        }
    }

    [Fact]
    public void Readers_CoverAllGrades_Teachers_AndInactive()
    {
        Assert.True(Data.Readers.Count >= 50, $"читатели: {Data.Readers.Count}");

        var grades = Data.Readers.Where(r => r.Role == ReaderRole.Student)
            .Select(r => r.Grade).Where(g => g != null).Distinct().ToList();
        Assert.Equal(Enumerable.Range(5, 8), grades.OrderBy(g => g).Select(g => g!.Value));

        Assert.True(Data.Readers.Count(r => r.Role == ReaderRole.Teacher) >= 5);
        Assert.Contains(Data.Readers, r => !r.IsActive);
    }

    [Fact]
    public void Inactive_Readers_HaveNoOpenLoans()
    {
        var inactive = Data.Readers.Where(r => !r.IsActive).Select(r => r.Id).ToHashSet();

        Assert.DoesNotContain(Data.Loans, l => l.IsActive && inactive.Contains(l.ReaderId));
    }

    [Fact]
    public void Data_HasNoInternalContradictions()
    {
        Assert.Empty(DataIntegrity.Check(Data));
    }

    [Fact]
    public void Nobody_ExceedsTheirLoanLimit()
    {
        var s = TestLibrary.Seeded();

        foreach (var reader in s.AllReaders)
        {
            Assert.True(s.LoansOf(reader).Count <= s.LimitOf(reader), reader.Name);
        }
    }

    [Fact]
    public void NobodyHolds_TheSameTitleTwice()
    {
        var s = TestLibrary.Seeded();

        foreach (var reader in s.AllReaders)
        {
            var titles = s.LoansOf(reader).Select(l => l.BookId).ToList();
            Assert.Equal(titles.Count, titles.Distinct().Count());
        }
    }

    [Fact]
    public void ReferenceBooks_AreNeverOnLoan()
    {
        var reference = Data.Books.Where(b => b.ReferenceOnly).Select(b => b.Id).ToHashSet();

        Assert.NotEmpty(reference);
        Assert.DoesNotContain(Data.Loans, l => l.IsActive && reference.Contains(l.BookId));
    }

    [Fact]
    public void Loans_LookLikeADailyLibrary()
    {
        var s = TestLibrary.Seeded();

        Assert.True(s.ActiveLoans.Count() >= 30, $"текущи: {s.ActiveLoans.Count()}");
        Assert.True(s.AllLoans.Count(l => !l.IsActive) >= 50);
        Assert.Equal(new[] { 101, 103, 110, 117, 121, 125 },
            s.OverdueLoans().Select(l => l.ReaderId).Distinct().OrderBy(i => i).ToArray());
        Assert.Contains(s.OverdueLoans(), s.NeedsReview);
        Assert.Contains(s.ActiveLoans, l => s.DaysUntilDue(l) is >= 0 and <= 2);
    }

    [Fact]
    public void History_SpansSeveralMonths()
    {
        var oldest = Data.Loans.Min(l => l.LoanedOn);

        Assert.True((TestLibrary.Today - oldest).Days >= 180);
    }

    [Fact]
    public void Library_HasDamagedAndWithdrawnCopies()
    {
        Assert.Contains(Data.Copies, c => c.Status == CopyStatus.Damaged);
        Assert.Contains(Data.Copies, c => c.Status == CopyStatus.Withdrawn);
    }

    [Fact]
    public void Search_FindsBulgarianClassicsWithAvailability()
    {
        var s = TestLibrary.Seeded();

        var result = s.FindBooks("йовков").ToList();

        Assert.True(result.Count >= 4);
        Assert.All(result, r => Assert.Equal("Йордан Йовков", r.Book.Author));
        Assert.All(result, r => Assert.Equal(s.Available(r.Book), r.Available));
    }

    [Fact]
    public void Search_ByTitle_FindsTheNovel()
    {
        var s = TestLibrary.Seeded();

        Assert.Equal("Тютюн", s.FindBooks("тютюн").First().Book.Title);
        Assert.Equal("Димитър Димов", s.FindBooks("тютюн").First().Book.Author);
    }

    [Fact]
    public void Seed_CreatedTwice_IsIdentical()
    {
        var other = SeedData.Create(TestLibrary.Today);

        Assert.Equal(Data.Books.Select(b => b.Title), other.Books.Select(b => b.Title));
        Assert.Equal(Data.Loans.Select(l => (l.Id, l.CopyId, l.ReaderId)), other.Loans.Select(l => (l.Id, l.CopyId, l.ReaderId)));
    }
}
