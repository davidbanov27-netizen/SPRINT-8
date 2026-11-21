using SchoolLibrary.Models;
using SchoolLibrary.Services.Validation;

namespace SchoolLibrary.Services;

/// <summary>
/// Заемане и връщане (BR-01 … BR-15).
/// Двата входа са <c>Lend</c> и <c>Return</c>: те не хвърлят изключения, а връщат <see cref="OperationResult{T}"/>
/// с разбираеми причини за отказ. <c>CreateLoan</c> и <c>ReturnLoan</c> са по-старите варианти с изключение.
/// </summary>
public partial class LibraryService
{
    public const string LoanField = "Loan";

    public int LimitOf(Reader reader) =>
        reader.Role == ReaderRole.Teacher ? Settings.TeacherLimit : Settings.StudentLimit;

    // ===== Търсене на екземпляр и заем =====

    /// <summary>Екземпляр по инвентарен номер (без значение от регистъра и интервалите).</summary>
    public BookCopy? FindCopy(string? inventoryNumber)
    {
        var number = CopyValidator.Normalize(inventoryNumber);
        return number.Length == 0
            ? null
            : _data.Copies.FirstOrDefault(c => string.Equals(c.InventoryNumber, number, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Незатвореният заем на екземпляра (най-много един), иначе null.</summary>
    public Loan? ActiveLoanOf(BookCopy copy) =>
        _data.Loans.FirstOrDefault(l => l.IsActive && l.CopyId == copy.Id);

    /// <summary>Намира заема, чийто екземпляр се връща – за връщане по инвентарен номер.</summary>
    public OperationResult<Loan> FindActiveLoan(string? inventoryNumber)
    {
        if (string.IsNullOrWhiteSpace(inventoryNumber))
        {
            return OperationResult<Loan>.Fail(LoanField, "Въведете инвентарен номер.");
        }

        var copy = FindCopy(inventoryNumber);
        if (copy == null)
        {
            return OperationResult<Loan>.Fail(LoanField,
                $"Няма екземпляр с инвентарен номер „{CopyValidator.Normalize(inventoryNumber)}“.");
        }

        var loan = ActiveLoanOf(copy);
        return loan == null
            ? OperationResult<Loan>.Fail(LoanField, $"Екземпляр {copy.InventoryNumber} не е зает – няма какво да се връща.")
            : OperationResult<Loan>.Ok(loan);
    }

    // ===== Проверки преди заемане =====

    /// <summary>Причините, поради които читателят не може да заеме заглавието. Празен списък = може.</summary>
    public IReadOnlyList<string> CheckLoan(Reader reader, Book book)
    {
        var problems = new List<string>();
        AddReaderProblems(problems, reader, book, Available(book) < 1 ? "Няма налично копие." : null);
        return problems;
    }

    /// <summary>Причините, поради които читателят не може да заеме точно този екземпляр.</summary>
    public IReadOnlyList<string> CheckLoan(Reader reader, BookCopy copy)
    {
        var problems = new List<string>();
        var book = GetBook(copy.BookId);
        if (book == null)
        {
            problems.Add("Книгата на този екземпляр не е намерена.");
            return problems;
        }

        AddReaderProblems(problems, reader, book, CopyProblem(copy));
        return problems;
    }

    private string? CopyProblem(BookCopy copy)
    {
        var no = copy.InventoryNumber;
        if (copy.Status == CopyStatus.Available && ActiveLoanOf(copy) == null)
        {
            return null;
        }

        return copy.Status switch
        {
            CopyStatus.Damaged => $"Екземпляр {no} е повреден и не може да се заема.",
            CopyStatus.Lost => $"Екземпляр {no} е записан като загубен.",
            CopyStatus.Withdrawn => $"Екземпляр {no} е списан от фонда.",
            // OnLoan, а също и „Наличен“ със забравен незатворен заем – и в двата случая е зает.
            _ => $"Екземпляр {no} вече е зает от друг читател.",
        };
    }

    private void AddReaderProblems(List<string> problems, Reader reader, Book book, string? copyProblem)
    {
        if (!reader.IsActive)
        {
            problems.Add("Читателят е деактивиран и не може да заема книги.");
        }

        if (!book.IsActive)
        {
            problems.Add("Книгата е деактивирана.");
        }

        if (book.ReferenceOnly)
        {
            problems.Add("Книгата е само за четене в библиотеката и не може да се изнася.");
        }

        if (copyProblem != null)
        {
            problems.Add(copyProblem);
        }

        var loans = LoansOf(reader);
        var limit = LimitOf(reader);
        if (loans.Count >= limit)
        {
            problems.Add($"Достигнат лимит от {limit} книги.");
        }

        var overdue = OverdueLoansOf(reader).FirstOrDefault();
        if (overdue != null)
        {
            problems.Add($"Читателят има просрочена книга: „{GetBook(overdue.BookId)?.Title}“ " +
                         $"(срок {overdue.DueDate:dd.MM.yyyy}). Върнете я, за да заемете нова.");
        }

        if (loans.Any(l => l.BookId == book.Id))
        {
            problems.Add("Читателят вече има това заглавие.");
        }
    }

    // ===== Заемане =====

    /// <summary>Заема конкретен екземпляр. Крайният срок се изчислява автоматично, екземплярът става „Зает“.</summary>
    public OperationResult<Loan> Lend(Reader reader, BookCopy copy)
    {
        // Работим със записаните обекти, не с копия, които екранът може да държи отдавна.
        var storedReader = GetReader(reader.Id);
        if (storedReader == null)
        {
            return OperationResult<Loan>.Fail(LoanField, "Читателят не е намерен.");
        }

        var storedCopy = GetCopy(copy.Id);
        if (storedCopy == null)
        {
            return OperationResult<Loan>.Fail(LoanField, "Екземплярът не е намерен.");
        }

        var problems = CheckLoan(storedReader, storedCopy);
        if (problems.Count > 0)
        {
            return OperationResult<Loan>.Fail(problems.Select(p => new ValidationError(LoanField, p)));
        }

        storedCopy.Status = CopyStatus.OnLoan;
        var loan = new Loan
        {
            Id = NextId(_data.Loans.Select(l => l.Id)),
            CopyId = storedCopy.Id,
            BookId = storedCopy.BookId,
            ReaderId = storedReader.Id,
            LoanedOn = Today,
            DueDate = Today.AddDays(Settings.LoanDays),
        };
        _data.Loans.Add(loan);
        Save();
        return OperationResult<Loan>.Ok(loan);
    }

    /// <summary>Заема първия свободен екземпляр на заглавието (по инвентарен номер).</summary>
    public OperationResult<Loan> Lend(Reader reader, Book book)
    {
        var copy = _data.Copies
            .Where(c => c.BookId == book.Id && c.Status == CopyStatus.Available && ActiveLoanOf(c) == null)
            .OrderBy(c => c.InventoryNumber, StringComparer.Ordinal)
            .FirstOrDefault();

        if (copy == null)
        {
            // Показваме и останалите причини (лимит, просрочие …), не само липсата на копие.
            var storedReader = GetReader(reader.Id) ?? reader;
            var reasons = CheckLoan(storedReader, book).ToList();
            if (reasons.Count == 0)
            {
                reasons.Add("Няма налично копие.");
            }

            return OperationResult<Loan>.Fail(reasons.Select(p => new ValidationError(LoanField, p)));
        }

        return Lend(reader, copy);
    }

    /// <summary>По-стар вариант на <see cref="Lend(Reader, Book)"/>: хвърля изключение при отказ.</summary>
    public Loan CreateLoan(Reader reader, Book book)
    {
        var result = Lend(reader, book);
        return result.Success ? result.Value! : throw new InvalidOperationException(result.Summary);
    }

    // ===== Връщане =====

    /// <summary>Приключва заема и освобождава екземпляра (или го отбелязва повреден/загубен).</summary>
    public OperationResult<ReturnResult> Return(Loan loan, BookCondition condition)
    {
        var stored = _data.Loans.FirstOrDefault(l => l.Id == loan.Id);
        if (stored == null)
        {
            return OperationResult<ReturnResult>.Fail(LoanField, "Заемането не е намерено.");
        }

        if (!stored.IsActive)
        {
            return OperationResult<ReturnResult>.Fail(LoanField, "Заемането вече е затворено.");
        }

        var book = GetBook(stored.BookId);
        var reader = GetReader(stored.ReaderId);
        var copy = GetCopy(stored.CopyId);
        if (book == null || reader == null || copy == null)
        {
            return OperationResult<ReturnResult>.Fail(LoanField, "Данните за заемането са непълни: липсва книга, читател или екземпляр.");
        }

        var late = DaysOverdue(stored); // преди да затворим заема

        stored.ReturnedOn = Today;
        stored.Condition = condition;

        // Повреденият или загубеният екземпляр не се връща в наличността (обезщетението е извън системата).
        copy.Status = condition switch
        {
            BookCondition.Damaged => CopyStatus.Damaged,
            BookCondition.Lost => CopyStatus.Lost,
            _ => CopyStatus.Available,
        };

        var lifted = late > 0 && !LoansOf(reader).Any(IsOverdue);
        Save();
        return OperationResult<ReturnResult>.Ok(new ReturnResult(book, reader, condition, late, lifted, copy.InventoryNumber));
    }

    /// <summary>Връщане по инвентарен номер (като при четец на баркод).</summary>
    public OperationResult<ReturnResult> Return(string? inventoryNumber, BookCondition condition)
    {
        var found = FindActiveLoan(inventoryNumber);
        return found.Success
            ? Return(found.Value!, condition)
            : OperationResult<ReturnResult>.Fail(found.Errors);
    }

    /// <summary>По-стар вариант на <see cref="Return(Loan, BookCondition)"/>: хвърля изключение при отказ.</summary>
    public ReturnResult ReturnLoan(Loan loan, BookCondition condition)
    {
        var result = Return(loan, condition);
        return result.Success ? result.Value! : throw new InvalidOperationException(result.Summary);
    }
}
