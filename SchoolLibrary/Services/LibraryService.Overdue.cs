using SchoolLibrary.Models;

namespace SchoolLibrary.Services;

/// <summary>
/// Просрочие. Заемът е просрочен, когато срокът му е СЛЕД ПО-РАНО от днешната дата:
/// на самия краен ден още не е просрочен, на следващия е просрочен с 1 ден.
/// Над <see cref="LibrarySettings.ReviewAfterDays"/> дни просрочие редът се маркира „за разглеждане“.
/// </summary>
public partial class LibraryService
{
    public bool IsOverdue(Loan loan) => loan.IsActive && loan.DueDate.Date < Today;

    public int DaysOverdue(Loan loan) => IsOverdue(loan) ? (Today - loan.DueDate.Date).Days : 0;

    /// <summary>Дни до крайния срок: 0 = срокът е днес, отрицателно = просрочен.</summary>
    public int DaysUntilDue(Loan loan) => (loan.DueDate.Date - Today).Days;

    /// <summary>Просрочие над 30 дни (по настройка): отива „за разглеждане“ от библиотекаря.</summary>
    public bool NeedsReview(Loan loan) => DaysOverdue(loan) > Settings.ReviewAfterDays;

    /// <summary>Всички просрочени заеми: най-старият срок първи.</summary>
    public IReadOnlyList<Loan> OverdueLoans() =>
        ActiveLoans.Where(IsOverdue).OrderBy(l => l.DueDate).ThenBy(l => l.Id).ToList();

    public IReadOnlyList<Loan> OverdueLoansOf(Reader reader) =>
        LoansOf(reader).Where(IsOverdue).OrderBy(l => l.DueDate).ToList();

    /// <summary>Читател с просрочена книга не може да заема нова, докато не я върне.</summary>
    public bool IsBlocked(Reader reader) => OverdueLoansOf(reader).Count > 0;
}
