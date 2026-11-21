using SchoolLibrary.Models;
using SchoolLibrary.Services.Validation;

namespace SchoolLibrary.Services;

/// <summary>Една намерена книга с наличността ѝ в момента на търсенето.</summary>
public sealed record BookSearchResult(Book Book, int Available, int Total, int Score)
{
    public bool IsAvailable => Available > 0;

    public string AvailabilityText =>
        Available > 0 ? $"Налични: {Available} от {Total}" : Total > 0 ? "Всички екземпляри са заети" : "Няма екземпляри";
}

public partial class LibraryService
{
    private const int ExactWord = 3;
    private const int PrefixWord = 2;
    private const int PartWord = 1;
    private const int TitleWeight = 3;
    private const int AuthorWeight = 2;
    private const int IsbnScore = 1000;

    /// <summary>
    /// Търсене по заглавие и автор (и по ISBN). Всяка въведена дума трябва да се среща в заглавието или в автора:
    /// цяла дума, начало на дума (докато човек още пише), част от дума или с една правописна грешка (думи от 4+ букви).
    /// Главни и малки букви и препинателни знаци не влияят. Реда определя съвпадението: заглавие преди автор, точно преди частично.
    /// Празна заявка връща целия каталог по азбучен ред. Деактивираните книги не се показват.
    /// </summary>
    public IReadOnlyList<BookSearchResult> FindBooks(string? query)
    {
        var active = _data.Books.Where(b => b.IsActive).ToList();
        var terms = Tokenize(query ?? "");
        var isbn = Isbn.Normalize(query);

        if (terms.Count == 0)
        {
            // Празна заявка = целият каталог; заявка само от препинателни знаци = нищо за търсене.
            if (!string.IsNullOrWhiteSpace(query))
            {
                return new List<BookSearchResult>();
            }

            return active.OrderBy(b => b.Title, StringComparer.CurrentCultureIgnoreCase)
                .Select(b => ToResult(b, 0)).ToList();
        }

        var phrase = string.Join(" ", terms);
        var results = new List<BookSearchResult>();
        foreach (var book in active)
        {
            var score = ScoreBook(book, terms, phrase, isbn);
            if (score > 0)
            {
                results.Add(ToResult(book, score));
            }
        }

        return results
            .OrderByDescending(r => r.Score)
            .ThenBy(r => r.Book.Title, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private BookSearchResult ToResult(Book book, int score) => new(book, Available(book), TotalCopies(book), score);

    private static int ScoreBook(Book book, List<string> terms, string phrase, string isbn)
    {
        if ((isbn.Length is 10 or 13) && Isbn.Normalize(book.Isbn) == isbn)
        {
            return IsbnScore;
        }

        var titleWords = Tokenize(book.Title);
        var authorWords = Tokenize(book.Author);

        var total = 0;
        foreach (var term in terms)
        {
            var fromTitle = BestMatch(titleWords, term) * TitleWeight;
            var fromAuthor = BestMatch(authorWords, term) * AuthorWeight;
            var best = Math.Max(fromTitle, fromAuthor);
            if (best == 0)
            {
                return 0; // всяка дума трябва да е намерена някъде
            }

            total += best;
        }

        var titlePhrase = string.Join(" ", titleWords);
        if (titlePhrase == phrase)
        {
            total += 20;
        }
        else if (titlePhrase.StartsWith(phrase, StringComparison.Ordinal))
        {
            total += 10;
        }

        return total;
    }

    private static int BestMatch(List<string> words, string term)
    {
        var best = 0;
        foreach (var word in words)
        {
            int score;
            if (word == term)
            {
                score = ExactWord;
            }
            else if (word.StartsWith(term, StringComparison.Ordinal))
            {
                score = PrefixWord;
            }
            else if ((term.Length >= 3 && word.Contains(term, StringComparison.Ordinal)) ||
                     (term.Length >= 4 && Levenshtein(word, term) <= 1))
            {
                score = PartWord;
            }
            else
            {
                score = 0;
            }

            best = Math.Max(best, score);
        }

        return best;
    }
}
