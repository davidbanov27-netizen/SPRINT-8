using System.IO;
using System.Text.Json;
using SchoolLibrary.Models;

namespace SchoolLibrary.Services;

/// <summary>
/// Записва данните в JSON файл в %LocalAppData%\SchoolLibrary\library.json.
///
/// Надеждност:
/// • Записът е на два етапа – първо във временен файл, после замяна. Прекъснат запис не поврежда основния файл.
/// • Преди замяната предишният файл се пази като library.json.bak. Ако основният файл се повреди, данните се връщат от него.
/// • Повреден файл не се губи: пази се като library.corrupt-дата.json.
/// • Файл от по-стара или по-нова версия се архивира като library.vN.bak и не се презаписва мълчаливо.
/// • Грешките не хвърлят изключения към екрана – те остават в <see cref="LastError"/>.
/// По-късно може да се замени със SQLite само чрез нов клас по <see cref="ILibraryRepository"/>.
/// </summary>
public class DataStore : ILibraryRepository
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    private readonly string _path;

    public DataStore(string? path = null)
    {
        _path = path ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SchoolLibrary",
            "library.json");
    }

    public string FilePath => _path;

    /// <summary>Файлът с предишния успешен запис.</summary>
    public string BackupPath => _path + ".bak";

    private string TempPath => _path + ".tmp";

    /// <summary>Съобщение за последната грешка или важна забележка (null = всичко е наред).</summary>
    public string? LastError { get; private set; }

    private enum ReadKind { Ok, Missing, Corrupt, Unreadable, OldSchema, NewSchema }

    private sealed record ReadOutcome(ReadKind Kind, LibraryData? Data, int Version, string Detail);

    public LibraryData Load()
    {
        LastError = null;
        var main = Read(_path);

        switch (main.Kind)
        {
            case ReadKind.Ok:
                WarnAboutInconsistencies(main.Data!);
                return main.Data!;

            case ReadKind.Missing:
                return SeedData.Create(DateTime.Today);

            case ReadKind.OldSchema:
            case ReadKind.NewSchema:
                var backup = Path.ChangeExtension(_path, ".v" + main.Version + ".bak");
                var kept = TryCopy(_path, backup);
                var which = main.Kind == ReadKind.OldSchema ? "по-стара" : "по-нова";
                LastError = $"Файлът с данни е от {which} версия на програмата. " +
                            (kept ? $"Копие е запазено като {backup}. " : "Копие не можа да се запази. ") +
                            "Заредени са примерни данни.";
                return SeedData.Create(DateTime.Today);

            default:
                return RecoverFromBackup(main);
        }
    }

    private LibraryData RecoverFromBackup(ReadOutcome failed)
    {
        var corrupt = Path.Combine(
            Path.GetDirectoryName(_path) ?? "",
            Path.GetFileNameWithoutExtension(_path) + ".corrupt-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".json");
        var kept = TryCopy(_path, corrupt);

        var previous = Read(BackupPath);
        if (previous.Kind == ReadKind.Ok)
        {
            LastError = "Основният файл с данни е повреден. Върнати са данните от предишния запис" +
                        (kept ? $"; повреденият файл е запазен като {corrupt}." : ".");
            return previous.Data!;
        }

        LastError = "Файлът с данни не може да се прочете. Заредени са примерни данни" +
                    (kept ? $"; повреденият файл е запазен като {corrupt}. " : ". ") + failed.Detail;
        return SeedData.Create(DateTime.Today);
    }

    private ReadOutcome Read(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return new ReadOutcome(ReadKind.Missing, null, 0, "");
            }

            var data = JsonSerializer.Deserialize<LibraryData>(File.ReadAllText(path), Options);
            if (data == null)
            {
                return new ReadOutcome(ReadKind.Corrupt, null, 0, "Файлът е празен.");
            }

            if (data.SchemaVersion < LibraryData.CurrentSchemaVersion)
            {
                return new ReadOutcome(ReadKind.OldSchema, null, data.SchemaVersion, "");
            }

            if (data.SchemaVersion > LibraryData.CurrentSchemaVersion)
            {
                return new ReadOutcome(ReadKind.NewSchema, null, data.SchemaVersion, "");
            }

            // Липсващи списъци във файла (null) не трябва да счупят програмата.
            data.Books ??= new();
            data.Copies ??= new();
            data.Readers ??= new();
            data.Loans ??= new();
            data.Settings ??= new();
            return new ReadOutcome(ReadKind.Ok, data, data.SchemaVersion, "");
        }
        catch (JsonException ex)
        {
            return new ReadOutcome(ReadKind.Corrupt, null, 0, ex.Message);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new ReadOutcome(ReadKind.Unreadable, null, 0, ex.Message);
        }
    }

    private void WarnAboutInconsistencies(LibraryData data)
    {
        var problems = DataIntegrity.Check(data);
        if (problems.Count > 0)
        {
            LastError = $"Данните са заредени, но има {problems.Count} несъответствия: " +
                        string.Join(" ", problems.Take(3)) + (problems.Count > 3 ? " …" : "");
        }
    }

    private static bool TryCopy(string from, string to)
    {
        try
        {
            File.Copy(from, to, overwrite: true);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    public void Save(LibraryData data)
    {
        try
        {
            data.SchemaVersion = LibraryData.CurrentSchemaVersion;
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);

            File.WriteAllText(TempPath, JsonSerializer.Serialize(data, Options));
            if (File.Exists(_path))
            {
                File.Replace(TempPath, _path, BackupPath, ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(TempPath, _path);
            }

            LastError = null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            LastError = "Данните не са записани: " + ex.Message + " Промените са само в паметта и ще се загубят при затваряне.";
            try
            {
                File.Delete(TempPath);
            }
            catch (Exception cleanup) when (cleanup is IOException or UnauthorizedAccessException)
            {
                // Остатъкът във временния файл не е проблем – ще се презапише при следващия запис.
            }
        }
    }
}
