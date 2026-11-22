using SchoolLibrary.Models;
using SchoolLibrary.Services;
using SchoolLibrary.Services.Validation;

namespace SchoolLibrary.Tests;

/// <summary>Читатели и уникален читателски номер.</summary>
public class ReaderTests
{
    [Fact]
    public void AddReader_Student_GetsAutoCardNumber()
    {
        var s = TestLibrary.Empty();

        var first = s.AddReaderOk("Иван Петров", "7Б");
        var second = s.AddReaderOk("Мария Георгиева", "8А");

        Assert.Equal("RD-0001", first.CardNumber);
        Assert.Equal("RD-0002", second.CardNumber);
        Assert.True(first.IsActive);
        Assert.Equal(ReaderRole.Student, first.Role);
    }

    [Fact]
    public void AddReader_AfterSeed_ContinuesNumbering()
    {
        var s = TestLibrary.Seeded();

        var highest = s.AllReaders.Max(r => int.Parse(r.CardNumber.Substring(3)));

        var reader = s.AddReaderOk("Нов Ученик", "9В");

        Assert.Equal($"RD-{highest + 1:D4}", reader.CardNumber);
        Assert.Equal(1, s.AllReaders.Count(r => r.CardNumber == reader.CardNumber));
    }

    [Fact]
    public void AddReader_Teacher_ClassIsForcedToTeacher()
    {
        var s = TestLibrary.Empty();

        var teacher = s.AddTeacherOk("Елена Стоянова");

        Assert.Equal(ReaderValidator.TeacherClassName, teacher.ClassName);
        Assert.Equal(ReaderRole.Teacher, teacher.Role);
        Assert.Equal(5, s.LimitOf(teacher));
    }

    [Theory]
    [InlineData("7 б", "7Б")]
    [InlineData(" 12а ", "12А")]
    [InlineData("10 В", "10В")]
    public void AddReader_ClassIsNormalized(string input, string expected)
    {
        var s = TestLibrary.Empty();

        var reader = s.AddReaderOk("Иван Петров", input);

        Assert.Equal(expected, reader.ClassName);
    }

    [Fact]
    public void AddReader_NameWhitespaceIsCleaned()
    {
        var s = TestLibrary.Empty();

        var reader = s.AddReaderOk("  Иван    Петров ", "7Б");

        Assert.Equal("Иван Петров", reader.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AddReader_EmptyName_IsRejected(string name)
    {
        var s = TestLibrary.Empty();

        var result = s.AddReader(name, "7Б", ReaderRole.Student);

        Assert.False(result.Success);
        Assert.True(result.HasError(nameof(Reader.Name)));
        Assert.Contains("Въведете име", result.Summary);
        Assert.Empty(s.AllReaders);
    }

    [Theory]
    [InlineData("И")]            // твърде късо
    [InlineData("Иван123")]      // цифри
    [InlineData("Иван @Петров")] // непозволен знак
    public void AddReader_InvalidName_IsRejected(string name)
    {
        var s = TestLibrary.Empty();

        var result = s.AddReader(name, "7Б", ReaderRole.Student);

        Assert.False(result.Success);
        Assert.True(result.HasError(nameof(Reader.Name)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("13А")]
    [InlineData("0Б")]
    [InlineData("7")]
    [InlineData("Б7")]
    [InlineData("77Б")]
    public void AddReader_InvalidClass_IsRejected(string className)
    {
        var s = TestLibrary.Empty();

        var result = s.AddReader("Иван Петров", className, ReaderRole.Student);

        Assert.False(result.Success);
        Assert.True(result.HasError(nameof(Reader.ClassName)));
        Assert.Contains("Класът", result.Summary);
    }

    [Theory]
    [InlineData("ivan@school.bg")]
    [InlineData("0888123456")]
    [InlineData("+359 888 123 456")]
    [InlineData("")]
    public void AddReader_ValidOrEmptyContact_IsAccepted(string contact)
    {
        var s = TestLibrary.Empty();

        var result = s.AddReader("Иван Петров", "7Б", ReaderRole.Student, contact);

        Assert.True(result.Success, result.Summary);
    }

    [Theory]
    [InlineData("не-е-контакт")]
    [InlineData("12345")]
    [InlineData("ivan@")]
    public void AddReader_InvalidContact_IsRejected(string contact)
    {
        var s = TestLibrary.Empty();

        var result = s.AddReader("Иван Петров", "7Б", ReaderRole.Student, contact);

        Assert.False(result.Success);
        Assert.True(result.HasError(nameof(Reader.Contact)));
    }

    [Fact]
    public void AddReader_DuplicateNameAndClass_IsRejected()
    {
        var s = TestLibrary.Empty();
        s.AddReaderOk("Иван Петров", "7Б");

        var result = s.AddReader("иван  петров", "7 б", ReaderRole.Student);

        Assert.False(result.Success);
        Assert.Contains("вече е регистриран", result.Summary);
        Assert.Single(s.AllReaders);
    }

    [Fact]
    public void AddReader_SameNameInDifferentClass_IsAllowed()
    {
        var s = TestLibrary.Empty();
        s.AddReaderOk("Иван Петров", "7Б");

        var result = s.AddReader("Иван Петров", "8А", ReaderRole.Student);

        Assert.True(result.Success, result.Summary);
        Assert.Equal(2, s.AllReaders.Count);
    }

    [Fact]
    public void AddReader_ManualCardNumber_IsNormalizedAndKept()
    {
        var s = TestLibrary.Empty();

        var result = s.AddReader("Иван Петров", "7Б", ReaderRole.Student, null, " rd-9001 ");

        Assert.True(result.Success, result.Summary);
        Assert.Equal("RD-9001", result.Value!.CardNumber);
    }

    [Fact]
    public void AddReader_DuplicateCardNumber_IsRejected()
    {
        var s = TestLibrary.Empty();
        var first = s.AddReaderOk("Иван Петров", "7Б");

        var result = s.AddReader("Мария Георгиева", "8А", ReaderRole.Student, null, first.CardNumber.ToLowerInvariant());

        Assert.False(result.Success);
        Assert.True(result.HasError(nameof(Reader.CardNumber)));
        Assert.Contains("вече е зает", result.Summary);
        Assert.Single(s.AllReaders);
    }

    [Theory]
    [InlineData("A")]
    [InlineData("RD 001")]
    [InlineData("РД-0001")] // кирилица не е позволена
    [InlineData("ABCDEFGHIJKLMNOP")]
    public void AddReader_InvalidCardNumber_IsRejected(string card)
    {
        var s = TestLibrary.Empty();

        var result = s.AddReader("Иван Петров", "7Б", ReaderRole.Student, null, card);

        Assert.False(result.Success);
        Assert.True(result.HasError(nameof(Reader.CardNumber)));
    }

    [Fact]
    public void AddReader_MultipleErrors_AreAllReported()
    {
        var s = TestLibrary.Empty();

        var result = s.AddReader("", "99", ReaderRole.Student, "???");

        Assert.False(result.Success);
        Assert.True(result.Errors.Count >= 3);
        Assert.StartsWith("•", result.Summary);
    }

    [Fact]
    public void UpdateReader_ChangesFields_ButKeepsCardNumber()
    {
        var s = TestLibrary.Empty();
        var reader = s.AddReaderOk("Иван Петров", "7Б");
        var card = reader.CardNumber;

        var result = s.UpdateReader(reader.Id, "Иван Петров-Иванов", "8Б", ReaderRole.Student, "ivan@school.bg");

        Assert.True(result.Success, result.Summary);
        Assert.Equal("Иван Петров-Иванов", reader.Name);
        Assert.Equal("8Б", reader.ClassName);
        Assert.Equal("ivan@school.bg", reader.Contact);
        Assert.Equal(card, reader.CardNumber);
    }

    [Fact]
    public void UpdateReader_ToExistingNameAndClass_IsRejected()
    {
        var s = TestLibrary.Empty();
        s.AddReaderOk("Иван Петров", "7Б");
        var other = s.AddReaderOk("Мария Георгиева", "7Б");

        var result = s.UpdateReader(other.Id, "Иван Петров", "7Б", ReaderRole.Student, null);

        Assert.False(result.Success);
        Assert.Equal("Мария Георгиева", other.Name);
    }

    [Fact]
    public void UpdateReader_WithoutChangingAnything_Succeeds()
    {
        var s = TestLibrary.Empty();
        var reader = s.AddReaderOk("Иван Петров", "7Б");

        var result = s.UpdateReader(reader.Id, reader.Name, reader.ClassName, reader.Role, reader.Contact);

        Assert.True(result.Success, result.Summary);
    }

    [Fact]
    public void UpdateReader_UnknownId_IsRejected()
    {
        var s = TestLibrary.Empty();

        var result = s.UpdateReader(999, "Иван Петров", "7Б", ReaderRole.Student, null);

        Assert.False(result.Success);
        Assert.True(result.HasError("Id"));
    }

    [Fact]
    public void SetReaderActive_Deactivate_HidesFromActiveList_AndBlocksLoans()
    {
        var s = TestLibrary.Empty();
        var book = s.AddBookOk(copies: 1);
        var reader = s.AddReaderOk();

        var result = s.SetReaderActive(reader.Id, false);

        Assert.True(result.Success, result.Summary);
        Assert.DoesNotContain(reader, s.Readers);
        Assert.Contains(reader, s.AllReaders);
        Assert.Contains(s.CheckLoan(reader, book), p => p.Contains("деактивиран"));
    }

    [Fact]
    public void SetReaderActive_WithActiveLoan_IsRefused()
    {
        var s = TestLibrary.Empty();
        var book = s.AddBookOk(copies: 1);
        var reader = s.AddReaderOk();
        s.CreateLoan(reader, book);

        var result = s.SetReaderActive(reader.Id, false);

        Assert.False(result.Success);
        Assert.True(reader.IsActive);
        Assert.Contains("неврънати", result.Summary);
    }

    [Fact]
    public void SetReaderActive_Reactivate_AllowsLoansAgain()
    {
        var s = TestLibrary.Empty();
        var book = s.AddBookOk(copies: 1);
        var reader = s.AddReaderOk();
        s.SetReaderActive(reader.Id, false);

        s.SetReaderActive(reader.Id, true);

        Assert.Empty(s.CheckLoan(reader, book));
    }

    [Fact]
    public void SearchReaders_ByNameCardAndClass_IncludesInactive()
    {
        var s = TestLibrary.Empty();
        var ivan = s.AddReaderOk("Иван Петров", "7Б");
        s.AddReaderOk("Мария Георгиева", "8А");
        s.SetReaderActive(ivan.Id, false);

        Assert.Single(s.SearchReaders("петров"));
        Assert.Single(s.SearchReaders("RD-0002"));
        Assert.Single(s.SearchReaders("8а"));
        Assert.Equal(2, s.SearchReaders("").Count);
        Assert.Empty(s.SearchReaders("несъществуващ"));
    }

    [Fact]
    public void Reader_DisplayName_ContainsCardNameAndClass()
    {
        var s = TestLibrary.Empty();
        var reader = s.AddReaderOk("Иван Петров", "7Б");

        Assert.Equal("RD-0001 – Иван Петров (7Б)", reader.DisplayName);
    }
}
