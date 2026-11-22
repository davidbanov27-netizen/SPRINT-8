using System.Text.RegularExpressions;
using SchoolLibrary.Models;
using SchoolLibrary.Services.Validation;

namespace SchoolLibrary.Services;

// Книги и екземпляри: създаване, редактиране, деактивиране.
public partial class LibraryService
{
    public const int MaxInitialCopies = 50;

    /// <summary>
    /// Добавя книга и първоначален брой екземпляри с автоматични инвентарни номера.
    /// Ако данните са невалидни или има дубликат, нищо не се записва.
    /// </summary>
    public OperationResult<Book> AddBook(Book draft, int copies = 1)
    {
        var errors = BookValidator.Validate(draft, _data.Books.Where(b => b.IsActive));
        if (copies < 1 || copies > MaxInitialCopies)
        {
            errors.Add(new ValidationError("Copies", $"Броят екземпляри трябва да е число от 1 до {MaxInitialCopies}."));
        }

        if (errors.Count > 0)
        {
            return OperationResult<Book>.Fail(errors);
        }

        var book = new Book { Id = NextId(_data.Books.Select(b => b.Id)), IsActive = true };
        ApplyBookFields(book, draft);
        _data.Books.Add(book);

        for (var i = 0; i < copies; i++)
        {
            _data.Copies.Add(new BookCopy
            {
                Id = NextId(_data.Copies.Select(c => c.Id)),
                BookId = book.Id,
                InventoryNumber = NextInventoryNumber(),
                Status = CopyStatus.Available,
            });
        }

        Save();
        return OperationResult<Book>.Ok(book);
    }

    /// <summary>Променя данните на съществуваща книга. <paramref name="draft"/>.Id сочи коя книга.</summary>
    public OperationResult<Book> UpdateBook(Book draft)
    {
        var book = _data.Books.FirstOrDefault(b => b.Id == draft.Id && b.IsActive);
        if (book == null)
        {
            return OperationResult<Book>.Fail("Id", "Книгата не е намерена. Може вече да е премахната.");
        }

        var errors = BookValidator.Validate(draft, _data.Books.Where(b => b.IsActive && b.Id != book.Id));
        if (errors.Count > 0)
        {
            return OperationResult<Book>.Fail(errors);
        }

        ApplyBookFields(book, draft);
        Save();
        return OperationResult<Book>.Ok(book);
    }

    /// <summary>Премахва книгата от каталога. Не е позволено, докато някой екземпляр е зает.</summary>
    public OperationResult DeactivateBook(Book book)
    {
        var stored = _data.Books.FirstOrDefault(b => b.Id == book.Id && b.IsActive);
        if (stored == null)
        {
            return OperationResult.Fail("Id", "Книгата не е намерена.");
        }

        if (_data.Copies.Any(c => c.BookId == stored.Id && c.Status == CopyStatus.OnLoan))
        {
            return OperationResult.Fail("Id", "Книгата има неврънати екземпляри. Първо запишете връщането им.");
        }

        stored.IsActive = false;
        foreach (var copy in _data.Copies.Where(c => c.BookId == stored.Id))
        {
            copy.Status = CopyStatus.Withdrawn; // номерът остава запазен, за да не се повтаря
        }

        Save();
        return OperationResult.Ok();
    }

    /// <summary>Добавя екземпляр към книга. Празен номер = автоматичен.</summary>
    public OperationResult<BookCopy> AddCopy(Book book, string? inventoryNumber = null)
    {
        var stored = _data.Books.FirstOrDefault(b => b.Id == book.Id && b.IsActive);
        if (stored == null)
        {
            return OperationResult<BookCopy>.Fail("Id", "Книгата не е намерена.");
        }

        var number = CopyValidator.Normalize(inventoryNumber);
        if (number.Length == 0)
        {
            number = NextInventoryNumber();
        }

        var errors = CopyValidator.Validate(number, _data.Copies);
        if (errors.Count > 0)
        {
            return OperationResult<BookCopy>.Fail(errors);
        }

        var copy = new BookCopy
        {
            Id = NextId(_data.Copies.Select(c => c.Id)),
            BookId = stored.Id,
            InventoryNumber = number,
            Status = CopyStatus.Available,
        };
        _data.Copies.Add(copy);
        Save();
        return OperationResult<BookCopy>.Ok(copy);
    }

    /// <summary>Променя инвентарния номер и състоянието на екземпляр, който не е зает.</summary>
    public OperationResult<BookCopy> UpdateCopy(int copyId, string inventoryNumber, CopyStatus status)
    {
        var copy = GetCopy(copyId);
        if (copy == null)
        {
            return OperationResult<BookCopy>.Fail("Id", "Екземплярът не е намерен.");
        }

        if (copy.Status == CopyStatus.OnLoan)
        {
            return OperationResult<BookCopy>.Fail(nameof(BookCopy.Status),
                "Екземплярът е зает и не може да се променя. Първо запишете връщането.");
        }

        if (status == CopyStatus.OnLoan)
        {
            return OperationResult<BookCopy>.Fail(nameof(BookCopy.Status),
                "Състояние „Зает“ се задава само със заемане.");
        }

        // DEF-02: екземпляр на деактивирана книга не може да се „съживява“ – би останал свободен към неактивно заглавие.
        if (GetBook(copy.BookId) is { IsActive: false } && status is not (CopyStatus.Withdrawn or CopyStatus.Lost))
        {
            return OperationResult<BookCopy>.Fail(nameof(BookCopy.Status),
                "Книгата е деактивирана – екземплярите ѝ могат да са само „Отписани“ или „Загубени“.");
        }

        var number = CopyValidator.Normalize(inventoryNumber);
        var errors = CopyValidator.Validate(number, _data.Copies.Where(c => c.Id != copy.Id));
        if (errors.Count > 0)
        {
            return OperationResult<BookCopy>.Fail(errors);
        }

        copy.InventoryNumber = number;
        copy.Status = status;
        Save();
        return OperationResult<BookCopy>.Ok(copy);
    }

    /// <summary>Следващ свободен автоматичен номер: BK-00001, BK-00002 …</summary>
    public string NextInventoryNumber()
    {
        var max = _data.Copies
            .Select(c => Regex.Match(c.InventoryNumber, @"^BK-(\d{1,8})$"))
            .Where(m => m.Success)
            .Select(m => int.Parse(m.Groups[1].Value))
            .DefaultIfEmpty(0)
            .Max();
        return $"BK-{max + 1:D5}";
    }

    private static void ApplyBookFields(Book target, Book source)
    {
        target.Title = source.Title.Trim();
        target.Author = source.Author.Trim();
        target.Genre = source.Genre.Trim();
        target.Isbn = Isbn.Normalize(source.Isbn);
        target.MinGrade = source.MinGrade;
        target.MaxGrade = source.MaxGrade;
        target.Description = source.Description.Trim();
        target.Shelf = source.Shelf.Trim();
        target.ReferenceOnly = source.ReferenceOnly;
    }
}
