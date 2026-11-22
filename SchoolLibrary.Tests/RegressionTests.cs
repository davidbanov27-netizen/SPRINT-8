using SchoolLibrary.Models;
using SchoolLibrary.Services;

namespace SchoolLibrary.Tests;

/// <summary>
/// Регресионни тестове: по един (или повече) за всеки дефект от регистъра (docs/09_defect_register.md).
/// Целият пакет се пуска наново след всяка поправка.
/// </summary>
public class RegressionTests
{
    // DEF-01: документацията обещаваше 0 екземпляра при създаване, а кодът изисква 1–50.
    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public void DEF01_CopiesAtCreation_AreBetweenOneAndFifty(int copies)
    {
        var s = TestLibrary.Empty();

        var result = s.AddBook(TestLibrary.Draft(), copies);

        Assert.False(result.Success);
        Assert.Contains("от 1 до 50", result.Summary);
    }

    [Fact]
    public void DEF01_OneAndFiftyAreAccepted()
    {
        var s = TestLibrary.Empty();

        Assert.True(s.AddBook(TestLibrary.Draft("Една", "А"), 1).Success);
        Assert.True(s.AddBook(TestLibrary.Draft("Петдесет", "Б"), 50).Success);
    }

    // DEF-02: екземпляр на деактивирана книга можеше да стане „Наличен“.
    [Theory]
    [InlineData(CopyStatus.Available)]
    [InlineData(CopyStatus.Damaged)]
    public void DEF02_CopyOfDeactivatedBook_CannotBeRevived(CopyStatus status)
    {
        var s = TestLibrary.Empty();
        var book = s.AddBookOk(copies: 1);
        var copy = s.CopiesOf(book)[0];
        s.DeactivateBook(book);

        var result = s.UpdateCopy(copy.Id, copy.InventoryNumber, status);

        Assert.False(result.Success);
        Assert.True(result.HasError(nameof(BookCopy.Status)));
        Assert.Equal(CopyStatus.Withdrawn, copy.Status);
    }

    [Theory]
    [InlineData(CopyStatus.Withdrawn)]
    [InlineData(CopyStatus.Lost)]
    public void DEF02_CopyOfDeactivatedBook_CanStillBeWithdrawnOrMarkedLost(CopyStatus status)
    {
        var s = TestLibrary.Empty();
        var book = s.AddBookOk(copies: 1);
        var copy = s.CopiesOf(book)[0];
        s.DeactivateBook(book);

        Assert.True(s.UpdateCopy(copy.Id, copy.InventoryNumber, status).Success);
    }

    [Fact]
    public void DEF02_ActiveBook_StillAllowsAllChanges()
    {
        var s = TestLibrary.Empty();
        var book = s.AddBookOk(copies: 1);
        var copy = s.CopiesOf(book)[0];

        Assert.True(s.UpdateCopy(copy.Id, copy.InventoryNumber, CopyStatus.Damaged).Success);
        Assert.True(s.UpdateCopy(copy.Id, copy.InventoryNumber, CopyStatus.Available).Success);
    }

    // DEF-03: учител с 5 книги можеше да стане ученик (лимит 3) и да остане над лимита.
    [Fact]
    public void DEF03_RoleChange_CannotPutAReaderOverTheNewLimit()
    {
        var s = TestLibrary.Empty();
        var teacher = s.AddTeacherOk();
        for (var i = 1; i <= 5; i++)
        {
            Assert.True(s.Lend(teacher, s.AddBookOk("Книга " + i, 1, "Автор " + i)).Success);
        }

        var result = s.UpdateReader(teacher.Id, teacher.Name, "9А", ReaderRole.Student, null);

        Assert.False(result.Success);
        Assert.True(result.HasError(nameof(Reader.Role)));
        Assert.Contains("лимитът за ученик е 3", result.Summary);
        Assert.Equal(ReaderRole.Teacher, teacher.Role);
        Assert.Equal(ReaderValidator_TeacherClass, teacher.ClassName);
    }

    private const string ReaderValidator_TeacherClass = "Учител";

    [Fact]
    public void DEF03_RoleChange_IsAllowed_WhenLoansFitTheNewLimit()
    {
        var s = TestLibrary.Empty();
        var teacher = s.AddTeacherOk();
        for (var i = 1; i <= 3; i++)
        {
            s.Lend(teacher, s.AddBookOk("Книга " + i, 1, "Автор " + i));
        }

        var result = s.UpdateReader(teacher.Id, teacher.Name, "9А", ReaderRole.Student, null);

        Assert.True(result.Success, result.Summary);
        Assert.Equal(ReaderRole.Student, teacher.Role);
    }

    [Fact]
    public void DEF03_StudentToTeacher_IsAlwaysAllowed()
    {
        var s = TestLibrary.Empty();
        var student = s.AddReaderOk();
        s.Lend(student, s.AddBookOk());

        Assert.True(s.UpdateReader(student.Id, student.Name, "", ReaderRole.Teacher, null).Success);
        Assert.Equal(5, s.LimitOf(student));
    }
}
