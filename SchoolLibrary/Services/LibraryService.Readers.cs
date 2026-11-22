using System.Text.RegularExpressions;
using SchoolLibrary.Models;
using SchoolLibrary.Services.Validation;

namespace SchoolLibrary.Services;

// Читатели: регистриране, редактиране, активиране и деактивиране, търсене.
public partial class LibraryService
{
    /// <summary>
    /// Регистрира читател. Читателският номер е автоматичен (RD-0105 …),
    /// освен ако изрично не е подаден друг – тогава се проверява за уникалност.
    /// </summary>
    public OperationResult<Reader> AddReader(
        string name, string className, ReaderRole role, string? contact = null, string? cardNumber = null)
    {
        var reader = new Reader
        {
            Name = ReaderValidator.CleanName(name),
            Role = role,
            ClassName = role == ReaderRole.Teacher ? ReaderValidator.TeacherClassName : ReaderValidator.NormalizeClass(className),
            Contact = (contact ?? "").Trim(),
            CardNumber = string.IsNullOrWhiteSpace(cardNumber) ? NextCardNumber() : ReaderValidator.NormalizeCard(cardNumber),
            IsActive = true,
        };

        var errors = ReaderValidator.Validate(reader, _data.Readers);
        if (errors.Count > 0)
        {
            return OperationResult<Reader>.Fail(errors);
        }

        reader.Id = NextId(_data.Readers.Select(r => r.Id));
        _data.Readers.Add(reader);
        Save();
        return OperationResult<Reader>.Ok(reader);
    }

    /// <summary>Променя данните на читател. Читателският номер не се променя.</summary>
    public OperationResult<Reader> UpdateReader(
        int id, string name, string className, ReaderRole role, string? contact)
    {
        var reader = GetReader(id);
        if (reader == null)
        {
            return OperationResult<Reader>.Fail("Id", "Читателят не е намерен.");
        }

        var candidate = new Reader
        {
            Id = reader.Id,
            CardNumber = reader.CardNumber,
            Name = ReaderValidator.CleanName(name),
            Role = role,
            ClassName = role == ReaderRole.Teacher ? ReaderValidator.TeacherClassName : ReaderValidator.NormalizeClass(className),
            Contact = (contact ?? "").Trim(),
            IsActive = reader.IsActive,
        };

        var errors = ReaderValidator.Validate(candidate, _data.Readers.Where(r => r.Id != reader.Id));
        if (errors.Count > 0)
        {
            return OperationResult<Reader>.Fail(errors);
        }

        // DEF-03: смяна на ролята не бива да оставя читателя над лимита на новата роля.
        var newLimit = role == ReaderRole.Teacher ? Settings.TeacherLimit : Settings.StudentLimit;
        var open = LoansOf(reader).Count;
        if (open > newLimit)
        {
            var roleName = role == ReaderRole.Teacher ? "учител" : "ученик";
            return OperationResult<Reader>.Fail(nameof(Reader.Role),
                $"Читателят има {open} неврънати книги, а лимитът за {roleName} е {newLimit}. Върнете книги преди тази промяна.");
        }

        reader.Name = candidate.Name;
        reader.Role = candidate.Role;
        reader.ClassName = candidate.ClassName;
        reader.Contact = candidate.Contact;
        Save();
        return OperationResult<Reader>.Ok(reader);
    }

    /// <summary>Деактивира или връща читател. Деактивиране не е позволено, докато има неврънати книги.</summary>
    public OperationResult SetReaderActive(int id, bool active)
    {
        var reader = GetReader(id);
        if (reader == null)
        {
            return OperationResult.Fail("Id", "Читателят не е намерен.");
        }

        if (!active && LoansOf(reader).Count > 0)
        {
            return OperationResult.Fail("Id",
                $"{reader.Name} има неврънати книги. Първо запишете връщането им.");
        }

        reader.IsActive = active;
        Save();
        return OperationResult.Ok();
    }

    /// <summary>Търсене по име, читателски номер или клас.</summary>
    public IReadOnlyList<Reader> SearchReaders(string? query)
    {
        var q = (query ?? "").Trim();
        if (q.Length == 0)
        {
            return AllReaders;
        }

        return AllReaders.Where(r =>
            r.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            r.CardNumber.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            r.ClassName.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    /// <summary>Следващ свободен читателски номер: RD-0105, RD-0106 …</summary>
    public string NextCardNumber()
    {
        var max = _data.Readers
            .Select(r => Regex.Match(r.CardNumber, @"^RD-(\d{1,6})$"))
            .Where(m => m.Success)
            .Select(m => int.Parse(m.Groups[1].Value))
            .DefaultIfEmpty(0)
            .Max();
        return $"RD-{max + 1:D4}";
    }
}
