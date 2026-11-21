using SchoolLibrary.Models;
using SchoolLibrary.Services;

namespace SchoolLibrary.Tests;

/// <summary>Хранилище в паметта: тестовете не пипат файлове.</summary>
internal sealed class InMemoryRepository : ILibraryRepository
{
    public InMemoryRepository(LibraryData data)
    {
        Data = data;
    }

    public LibraryData Data { get; private set; }
    public int SaveCount { get; private set; }
    public string? LastError { get; set; }

    public LibraryData Load() => Data;

    public void Save(LibraryData data)
    {
        Data = data;
        SaveCount++;
    }
}

internal static class TestLibrary
{
    /// <summary>Фиксирана „днешна“ дата, за да не зависят тестовете от календара.</summary>
    public static readonly DateTime Today = new(2026, 11, 20);

    /// <summary>Празна библиотека без книги и читатели.</summary>
    public static LibraryService Empty() =>
        new(new InMemoryRepository(new LibraryData { SchemaVersion = LibraryData.CurrentSchemaVersion }), () => Today);

    /// <summary>Библиотека с примерните данни (9 книги, 4 читатели, заемания).</summary>
    public static LibraryService Seeded() =>
        new(new InMemoryRepository(SeedData.Create(Today)), () => Today);

    public static Book Draft(string title = "Тест книга", string author = "Тест Автор", string genre = "Роман") =>
        new() { Title = title, Author = author, Genre = genre, MinGrade = 1, MaxGrade = 12 };

    /// <summary>Добавя книга и проверява, че е успешно.</summary>
    public static Book AddBookOk(this LibraryService s, string title = "Тест книга", int copies = 1,
        string author = "Тест Автор", bool referenceOnly = false)
    {
        var draft = Draft(title, author);
        draft.ReferenceOnly = referenceOnly;
        var result = s.AddBook(draft, copies);
        Assert.True(result.Success, result.Summary);
        return result.Value!;
    }

    /// <summary>Регистрира ученик и проверява, че е успешно.</summary>
    public static Reader AddReaderOk(this LibraryService s, string name = "Иван Петров", string className = "7Б")
    {
        var result = s.AddReader(name, className, ReaderRole.Student);
        Assert.True(result.Success, result.Summary);
        return result.Value!;
    }

    public static Reader AddTeacherOk(this LibraryService s, string name = "Елена Стоянова")
    {
        var result = s.AddReader(name, "", ReaderRole.Teacher);
        Assert.True(result.Success, result.Summary);
        return result.Value!;
    }
}
