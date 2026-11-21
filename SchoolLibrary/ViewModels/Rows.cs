using SchoolLibrary.Models;
using SchoolLibrary.Services;

namespace SchoolLibrary.ViewModels;

// Редовете са прости „само за показване“ обекти, подготвени за DataGrid-овете.

public sealed record BookRow(Book Book, int Available, int Total)
{
    public int Id => Book.Id;
    public string Title => Book.Title;
    public string Author => Book.Author;
    public string Genre => Book.Genre;
    public string Isbn => Book.Isbn.Length > 0 ? Book.Isbn : "—";
    public string AgeText => Book.AgeText;
    public string Shelf => Book.Shelf.Length > 0 ? Book.Shelf : "—";
    public string Description => Book.Description;
    public bool ReferenceOnly => Book.ReferenceOnly;
    public string Availability => $"{Available} / {Total}";
    public bool IsAvailable => Available > 0;
    public string Label => $"{Title} (налични {Available})";
}

public static class StatusLabels
{
    public static string For(CopyStatus status) => status switch
    {
        CopyStatus.Available => "Наличен",
        CopyStatus.OnLoan => "Зает",
        CopyStatus.Damaged => "Повреден",
        CopyStatus.Lost => "Загубен",
        CopyStatus.Withdrawn => "Списан",
        _ => status.ToString(),
    };
}

public sealed record CopyStatusOption(string Label, CopyStatus Value);

public sealed record CopyRow(BookCopy Copy)
{
    public int Id => Copy.Id;
    public string InventoryNumber => Copy.InventoryNumber;
    public CopyStatus Status => Copy.Status;
    public string StatusText => StatusLabels.For(Copy.Status);
    public bool IsOnLoan => Copy.Status == CopyStatus.OnLoan;
}

/// <summary>Избор на екземпляр при заемане: „автоматично“ (Copy = null) или конкретен свободен екземпляр.</summary>
public sealed record CopyChoice(BookCopy? Copy, string Label);

public sealed record LoanRow(Loan Loan, string BookTitle, string ReaderName, string ClassName, int DaysOverdue, bool NeedsReview, string InventoryNumber = "")
{
    public DateTime DueDate => Loan.DueDate;
    public bool IsOverdue => DaysOverdue > 0;

    public string StatusText =>
        !IsOverdue ? "В срок"
        : NeedsReview ? $"Просрочена ({DaysOverdue} дни) – за разглеждане"
        : $"Просрочена ({DaysOverdue} дни)";
}

public sealed record ReaderRow(Reader Reader, int LoanCount, int Limit, bool HasOverdue)
{
    public int Id => Reader.Id;
    public string CardNumber => Reader.CardNumber;
    public string Name => Reader.Name;
    public string ClassName => Reader.ClassName;
    public string Contact => Reader.Contact.Length > 0 ? Reader.Contact : "—";
    public bool IsActive => Reader.IsActive;
    public string LoansText => $"{LoanCount} / {Limit}";

    /// <summary>Червено оцветяване в таблицата: просрочена книга или неактивен читател.</summary>
    public bool IsProblem => HasOverdue || !IsActive;

    public string StatusText => !IsActive ? "Неактивен" : HasOverdue ? "Има просрочена" : "В ред";
}

public sealed record BarRow(string Label, int Value, double BarWidth);

public sealed record ConditionOption(string Label, BookCondition Value);

public sealed record ResultRow(Book Book, string Why, int Available, int Total)
{
    public bool IsAvailable => Available > 0;
    public string Title => Book.Title;
    public string Subtitle => $"{Book.Author} · {Book.AgeText}";
    public string Shelf => Book.Shelf.Length > 0 ? $"Рафт {Book.Shelf}" : "";
    public bool HasWhy => Why.Length > 0;
    public string WhyText => $"Защо: {Why}";
    public string AvailabilityText =>
        Available > 0 ? $"Налични: {Available} от {Total} ✔" : Total > 0 ? "Всички са заети ✖" : "Няма екземпляри ✖";
}

public static class RowFactory
{
    public static BookRow ToBookRow(LibraryService s, Book b) => new(b, s.Available(b), s.TotalCopies(b));

    public static LoanRow ToLoanRow(LibraryService s, Loan l)
    {
        var book = s.GetBook(l.BookId);
        var reader = s.GetReader(l.ReaderId);
        var days = s.DaysOverdue(l);
        return new LoanRow(l, book?.Title ?? "?", reader?.Name ?? "?", reader?.ClassName ?? "", days,
            s.NeedsReview(l), s.GetCopy(l.CopyId)?.InventoryNumber ?? "");
    }

    public static ReaderRow ToReaderRow(LibraryService s, Reader r)
    {
        var loans = s.LoansOf(r);
        return new ReaderRow(r, loans.Count, s.LimitOf(r), s.IsBlocked(r));
    }

    public static ResultRow ToResultRow(LibraryService s, Book b, string why) =>
        new(b, why, s.Available(b), s.TotalCopies(b));
}
