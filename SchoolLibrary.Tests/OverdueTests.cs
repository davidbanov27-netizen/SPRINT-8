using SchoolLibrary.Models;
using SchoolLibrary.Services;

namespace SchoolLibrary.Tests;

/// <summary>Етап 7: просрочие спрямо текущата дата – гранични случаи и блокиране на заемане.</summary>
public class OverdueTests
{
    /// <summary>Библиотека с часовник, който тестът превърта; всички дати са спрямо 20.11.2026.</summary>
    private sealed class Fixture
    {
        public DateTime Now = TestLibrary.Today;
        public LibraryService S { get; }

        public Fixture()
        {
            var repo = new InMemoryRepository(new LibraryData { SchemaVersion = LibraryData.CurrentSchemaVersion });
            S = new LibraryService(repo, () => Now);
        }

        public Loan LendOne(Reader reader, string title = "Книга")
        {
            var book = S.AddBookOk(title, 1, "Автор " + title);
            return S.Lend(reader, book).Value!;
        }
    }

    // ===== Изчисляване =====

    [Fact]
    public void Loan_IsNotOverdue_OnTheDueDate()
    {
        var f = new Fixture();
        var loan = f.LendOne(f.S.AddReaderOk());

        f.Now = loan.DueDate; // последният ден

        Assert.False(f.S.IsOverdue(loan));
        Assert.Equal(0, f.S.DaysOverdue(loan));
        Assert.Equal(0, f.S.DaysUntilDue(loan));
    }

    [Fact]
    public void Loan_IsOverdueByOneDay_TheDayAfter()
    {
        var f = new Fixture();
        var loan = f.LendOne(f.S.AddReaderOk());

        f.Now = loan.DueDate.AddDays(1);

        Assert.True(f.S.IsOverdue(loan));
        Assert.Equal(1, f.S.DaysOverdue(loan));
        Assert.Equal(-1, f.S.DaysUntilDue(loan));
    }

    [Fact]
    public void Loan_BeforeDueDate_ShowsDaysLeft()
    {
        var f = new Fixture();
        var loan = f.LendOne(f.S.AddReaderOk());

        Assert.False(f.S.IsOverdue(loan));
        Assert.Equal(14, f.S.DaysUntilDue(loan));
    }

    [Fact]
    public void Overdue_IgnoresTimeOfDay()
    {
        var f = new Fixture();
        var loan = f.LendOne(f.S.AddReaderOk());
        loan.DueDate = new DateTime(2026, 11, 20, 23, 59, 0); // в края на днешния ден

        Assert.False(f.S.IsOverdue(loan));
        loan.DueDate = new DateTime(2026, 11, 19, 0, 1, 0);
        Assert.True(f.S.IsOverdue(loan));
    }

    [Fact]
    public void ReturnedLoan_IsNeverOverdue()
    {
        var f = new Fixture();
        var loan = f.LendOne(f.S.AddReaderOk());
        f.Now = loan.DueDate.AddDays(10);
        f.S.Return(loan, BookCondition.Good);

        Assert.False(f.S.IsOverdue(loan));
        Assert.Equal(0, f.S.DaysOverdue(loan));
    }

    [Fact]
    public void Overdue_GrowsWithTheCalendar()
    {
        var f = new Fixture();
        var loan = f.LendOne(f.S.AddReaderOk());

        f.Now = loan.DueDate.AddDays(5);
        Assert.Equal(5, f.S.DaysOverdue(loan));

        f.Now = loan.DueDate.AddDays(12);
        Assert.Equal(12, f.S.DaysOverdue(loan));
    }

    [Theory]
    [InlineData(29, false)]
    [InlineData(30, false)] // „над 30 дни“ – 30 още не е за разглеждане
    [InlineData(31, true)]
    public void NeedsReview_OnlyAfterThirtyDays(int daysLate, bool expected)
    {
        var f = new Fixture();
        var loan = f.LendOne(f.S.AddReaderOk());

        f.Now = loan.DueDate.AddDays(daysLate);

        Assert.Equal(expected, f.S.NeedsReview(loan));
    }

    [Fact]
    public void NeedsReview_FollowsSettings()
    {
        var f = new Fixture();
        f.S.Settings.ReviewAfterDays = 5;
        var loan = f.LendOne(f.S.AddReaderOk());

        f.Now = loan.DueDate.AddDays(6);

        Assert.True(f.S.NeedsReview(loan));
    }

    // ===== Списъци =====

    [Fact]
    public void OverdueLoans_ListsOnlyOverdue_OldestFirst()
    {
        var f = new Fixture();
        var a = f.S.AddReaderOk("Иван Петров", "7Б");
        var b = f.S.AddReaderOk("Мария Георгиева", "7Б");
        var first = f.LendOne(a, "Първа");
        f.Now = f.Now.AddDays(3);
        var second = f.LendOne(b, "Втора");
        f.Now = f.Now.AddDays(13); // първата: срок +14 -> вече е просрочена; втората още не е

        Assert.Equal(new[] { first.Id }, f.S.OverdueLoans().Select(l => l.Id).ToArray());

        f.Now = f.Now.AddDays(5);

        Assert.Equal(new[] { first.Id, second.Id }, f.S.OverdueLoans().Select(l => l.Id).ToArray());
    }

    [Fact]
    public void OverdueLoans_IsEmpty_WhenNothingIsLate()
    {
        var f = new Fixture();
        f.LendOne(f.S.AddReaderOk());

        Assert.Empty(f.S.OverdueLoans());
    }

    [Fact]
    public void Seeded_OverdueLoans_MatchTheKnownReaders()
    {
        var s = TestLibrary.Seeded();

        var readers = s.OverdueLoans().Select(l => l.ReaderId).Distinct().OrderBy(i => i).ToArray();

        Assert.Equal(new[] { 101, 103, 110, 117, 121, 125 }, readers);
        Assert.Equal(7, s.DaysOverdue(s.LoansOf(s.GetReader(101)!).Single(s.IsOverdue)));
        Assert.Equal(20, s.DaysOverdue(s.LoansOf(s.GetReader(103)!).Single(s.IsOverdue)));
    }

    // ===== Блокиране =====

    [Fact]
    public void Reader_IsBlocked_OnlyWhileHavingAnOverdueLoan()
    {
        var f = new Fixture();
        var reader = f.S.AddReaderOk();
        var loan = f.LendOne(reader);
        Assert.False(f.S.IsBlocked(reader));

        f.Now = loan.DueDate.AddDays(1);
        Assert.True(f.S.IsBlocked(reader));

        f.S.Return(loan, BookCondition.Good);
        Assert.False(f.S.IsBlocked(reader));
    }

    [Fact]
    public void NewLoan_IsRefused_WhileOverdue_AndAllowedAfterReturn()
    {
        var f = new Fixture();
        var reader = f.S.AddReaderOk();
        var old = f.LendOne(reader, "Стара");
        var next = f.S.AddBookOk("Нова", 1, "Автор Нова");

        f.Now = old.DueDate.AddDays(1);
        var refused = f.S.Lend(reader, next);
        Assert.False(refused.Success);
        Assert.Contains("просрочена", refused.Summary);
        Assert.Contains("Стара", refused.Summary);

        f.S.Return(old, BookCondition.Good);
        Assert.True(f.S.Lend(reader, next).Success);
    }

    [Fact]
    public void NewLoan_IsAllowed_OnTheDueDate()
    {
        var f = new Fixture();
        var reader = f.S.AddReaderOk();
        var old = f.LendOne(reader, "Стара");
        var next = f.S.AddBookOk("Нова", 1, "Автор Нова");

        f.Now = old.DueDate; // още не е просрочена

        Assert.True(f.S.Lend(reader, next).Success);
    }

    [Fact]
    public void OneOverdueBlocks_EvenWithOtherLoansOnTime()
    {
        var f = new Fixture();
        var reader = f.S.AddReaderOk();
        var late = f.LendOne(reader, "Закъсняла");
        f.Now = f.Now.AddDays(10);
        f.LendOne(reader, "В срок");
        f.Now = late.DueDate.AddDays(1);

        Assert.True(f.S.IsBlocked(reader));
        Assert.Single(f.S.OverdueLoansOf(reader));
    }

    [Fact]
    public void ReturningOneOfTwoOverdue_KeepsTheBlock()
    {
        var f = new Fixture();
        var reader = f.S.AddReaderOk();
        var one = f.LendOne(reader, "Едно");
        var two = f.LendOne(reader, "Две");
        f.Now = one.DueDate.AddDays(3);

        var result = f.S.Return(one, BookCondition.Good).Value!;

        Assert.False(result.BlockLifted);
        Assert.True(f.S.IsBlocked(reader));

        Assert.True(f.S.Return(two, BookCondition.Good).Value!.BlockLifted);
    }

    [Fact]
    public void Block_AppliesToTeachersToo()
    {
        var f = new Fixture();
        var teacher = f.S.AddTeacherOk();
        var loan = f.LendOne(teacher);
        f.Now = loan.DueDate.AddDays(1);

        Assert.True(f.S.IsBlocked(teacher));
        Assert.False(f.S.Lend(teacher, f.S.AddBookOk("Друга", 1, "Автор Друг")).Success);
    }

    [Fact]
    public void Seeded_OverdueReaderIsBlocked_OthersAreNot()
    {
        var s = TestLibrary.Seeded();

        Assert.True(s.IsBlocked(s.GetReader(101)!));
        Assert.True(s.IsBlocked(s.GetReader(103)!));
        Assert.False(s.IsBlocked(s.GetReader(102)!));
        Assert.False(s.IsBlocked(s.GetReader(104)!));
    }
}
