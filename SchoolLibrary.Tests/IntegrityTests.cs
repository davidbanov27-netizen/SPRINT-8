using System.IO;
using System.Text.Json;
using SchoolLibrary.Models;
using SchoolLibrary.Services;

namespace SchoolLibrary.Tests;

/// <summary>Проверка за вътрешни противоречия в заредените данни.</summary>
public class IntegrityTests
{
    private static LibraryData Build()
    {
        var repo = new InMemoryRepository(new LibraryData { SchemaVersion = LibraryData.CurrentSchemaVersion });
        var s = new LibraryService(repo, () => TestLibrary.Today);
        var book = s.AddBookOk("Книга", 2);
        var reader = s.AddReaderOk();
        s.Lend(reader, book);
        return repo.Data;
    }

    [Fact]
    public void SeedData_HasNoProblems()
    {
        Assert.Empty(DataIntegrity.Check(SeedData.Create(TestLibrary.Today)));
    }

    [Fact]
    public void DataBuiltByTheService_HasNoProblems()
    {
        Assert.Empty(DataIntegrity.Check(Build()));
    }

    [Fact]
    public void EmptyData_HasNoProblems()
    {
        Assert.Empty(DataIntegrity.Check(new LibraryData()));
    }

    [Fact]
    public void DuplicateInventoryNumber_IsReported()
    {
        var d = Build();
        d.Copies[1].InventoryNumber = d.Copies[0].InventoryNumber.ToLowerInvariant();

        Assert.Contains(DataIntegrity.Check(d), p => p.Contains("Инвентарният номер"));
    }

    [Fact]
    public void DuplicateCardNumber_IsReported()
    {
        var d = Build();
        d.Readers.Add(new Reader { Id = 77, Name = "Друг Читател", CardNumber = d.Readers[0].CardNumber, ClassName = "8А" });

        Assert.Contains(DataIntegrity.Check(d), p => p.Contains("Читателският номер"));
    }

    [Fact]
    public void DuplicateIds_AreReported()
    {
        var d = Build();
        d.Books.Add(new Book { Id = d.Books[0].Id, Title = "Копие", Author = "А", Genre = "Г" });

        Assert.Contains(DataIntegrity.Check(d), p => p.Contains("се повтаря при книги"));
    }

    [Fact]
    public void LoanWithMissingCopy_IsReported()
    {
        var d = Build();
        d.Loans[0].CopyId = 9999;

        Assert.Contains(DataIntegrity.Check(d), p => p.Contains("липсващ екземпляр"));
    }

    [Fact]
    public void LoanWithMissingReader_IsReported()
    {
        var d = Build();
        d.Loans[0].ReaderId = 9999;

        Assert.Contains(DataIntegrity.Check(d), p => p.Contains("липсващ читател"));
    }

    [Fact]
    public void CopyWithMissingBook_IsReported()
    {
        var d = Build();
        d.Copies[0].BookId = 9999;

        Assert.Contains(DataIntegrity.Check(d), p => p.Contains("липсваща книга"));
    }

    [Fact]
    public void TwoOpenLoansForOneCopy_AreReported()
    {
        var d = Build();
        var first = d.Loans[0];
        d.Loans.Add(new Loan { Id = 500, CopyId = first.CopyId, BookId = first.BookId, ReaderId = first.ReaderId, LoanedOn = first.LoanedOn, DueDate = first.DueDate });

        Assert.Contains(DataIntegrity.Check(d), p => p.Contains("повече от един отворен заем"));
    }

    [Fact]
    public void OpenLoan_WithAvailableCopy_IsReported()
    {
        var d = Build();
        d.Copies.Single(c => c.Id == d.Loans[0].CopyId).Status = CopyStatus.Available;

        Assert.Contains(DataIntegrity.Check(d), p => p.Contains("не е отбелязан като зает"));
    }

    [Fact]
    public void OnLoanCopy_WithoutOpenLoan_IsReported()
    {
        var d = Build();
        d.Loans[0].ReturnedOn = TestLibrary.Today;

        Assert.Contains(DataIntegrity.Check(d), p => p.Contains("няма отворен заем"));
    }

    [Fact]
    public void LoadingAnInconsistentFile_StillLoads_ButWarns()
    {
        var dir = Path.Combine(Path.GetTempPath(), "SchoolLibraryIntegrity_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var d = Build();
            d.Loans[0].CopyId = 9999;
            var path = Path.Combine(dir, "library.json");
            File.WriteAllText(path, JsonSerializer.Serialize(d));
            var store = new DataStore(path);

            var loaded = store.Load();

            Assert.Equal(d.Books.Count, loaded.Books.Count);
            Assert.Contains("несъответствия", store.LastError);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
