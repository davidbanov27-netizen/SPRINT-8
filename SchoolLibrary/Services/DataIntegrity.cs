using SchoolLibrary.Models;

namespace SchoolLibrary.Services;

/// <summary>
/// Проверка на заредените данни за вътрешни противоречия (ръчно редактиран или повреден файл).
/// Не поправя нищо – връща разбираеми описания, за да може екранът да предупреди потребителя.
/// </summary>
public static class DataIntegrity
{
    public static List<string> Check(LibraryData data)
    {
        var problems = new List<string>();

        Duplicates(problems, "книги", data.Books.Select(b => b.Id));
        Duplicates(problems, "екземпляри", data.Copies.Select(c => c.Id));
        Duplicates(problems, "читатели", data.Readers.Select(r => r.Id));
        Duplicates(problems, "заеми", data.Loans.Select(l => l.Id));

        foreach (var group in data.Copies.GroupBy(c => c.InventoryNumber, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
        {
            problems.Add($"Инвентарният номер „{group.Key}“ се повтаря.");
        }

        foreach (var group in data.Readers.GroupBy(r => r.CardNumber, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
        {
            problems.Add($"Читателският номер „{group.Key}“ се повтаря.");
        }

        var bookIds = data.Books.Select(b => b.Id).ToHashSet();
        var readerIds = data.Readers.Select(r => r.Id).ToHashSet();
        var copiesById = data.Copies.GroupBy(c => c.Id).ToDictionary(g => g.Key, g => g.First());

        foreach (var copy in data.Copies.Where(c => !bookIds.Contains(c.BookId)))
        {
            problems.Add($"Екземпляр {copy.InventoryNumber} сочи към липсваща книга.");
        }

        foreach (var loan in data.Loans)
        {
            if (!copiesById.TryGetValue(loan.CopyId, out var copy))
            {
                problems.Add($"Заем №{loan.Id} сочи към липсващ екземпляр.");
                continue;
            }

            if (!bookIds.Contains(loan.BookId))
            {
                problems.Add($"Заем №{loan.Id} сочи към липсваща книга.");
            }

            if (!readerIds.Contains(loan.ReaderId))
            {
                problems.Add($"Заем №{loan.Id} сочи към липсващ читател.");
            }

            if (loan.IsActive && copy.Status != CopyStatus.OnLoan)
            {
                problems.Add($"Заем №{loan.Id} е отворен, но екземпляр {copy.InventoryNumber} не е отбелязан като зает.");
            }
        }

        foreach (var group in data.Loans.Where(l => l.IsActive).GroupBy(l => l.CopyId).Where(g => g.Count() > 1))
        {
            var number = copiesById.TryGetValue(group.Key, out var c) ? c.InventoryNumber : "№" + group.Key;
            problems.Add($"Екземпляр {number} има повече от един отворен заем.");
        }

        var openCopyIds = data.Loans.Where(l => l.IsActive).Select(l => l.CopyId).ToHashSet();
        foreach (var copy in data.Copies.Where(c => c.Status == CopyStatus.OnLoan && !openCopyIds.Contains(c.Id)))
        {
            problems.Add($"Екземпляр {copy.InventoryNumber} е отбелязан като зает, но няма отворен заем.");
        }

        return problems;
    }

    private static void Duplicates(List<string> problems, string what, IEnumerable<int> ids)
    {
        foreach (var id in ids.GroupBy(i => i).Where(g => g.Count() > 1).Select(g => g.Key))
        {
            problems.Add($"Номер {id} се повтаря при {what}.");
        }
    }
}
