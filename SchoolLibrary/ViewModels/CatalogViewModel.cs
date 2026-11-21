using System.Windows.Input;
using SchoolLibrary.Models;
using SchoolLibrary.Services;
using SchoolLibrary.Services.Validation;

namespace SchoolLibrary.ViewModels;

/// <summary>Екран „Каталог“: преглед, търсене, добавяне и редактиране на книги и на техните екземпляри.</summary>
public class CatalogViewModel : PageViewModel
{
    private readonly LibraryService _s;

    private string _searchText = "";
    private IReadOnlyList<BookRow> _books = Array.Empty<BookRow>();
    private BookRow? _selectedBook;
    private IReadOnlyList<CopyRow> _copies = Array.Empty<CopyRow>();
    private CopyRow? _selectedCopy;

    // Форма за книга
    private bool _isFormOpen;
    private int? _editingId;
    private string _bookTitle = "";
    private string _bookAuthor = "";
    private string _bookGenre = "";
    private string _bookIsbn = "";
    private string _bookShelf = "";
    private string _bookDescription = "";
    private string _bookCopies = "1";
    private string _bookMinGrade = "1";
    private string _bookMaxGrade = "12";
    private bool _bookReferenceOnly;

    // Екземпляри
    private string _newInventoryNumber = "";
    private string _editInventoryNumber = "";
    private CopyStatusOption? _selectedStatus;

    public CatalogViewModel(LibraryService s)
    {
        _s = s;

        StatusOptions = new[]
        {
            new CopyStatusOption(StatusLabels.For(CopyStatus.Available), CopyStatus.Available),
            new CopyStatusOption(StatusLabels.For(CopyStatus.Damaged), CopyStatus.Damaged),
            new CopyStatusOption(StatusLabels.For(CopyStatus.Lost), CopyStatus.Lost),
            new CopyStatusOption(StatusLabels.For(CopyStatus.Withdrawn), CopyStatus.Withdrawn),
        };

        AddCommand = new RelayCommand(OpenAdd);
        EditCommand = new RelayCommand(OpenEdit, () => SelectedBook != null);
        SaveCommand = new RelayCommand(SaveBook);
        CancelCommand = new RelayCommand(() => { IsFormOpen = false; ClearMessage(); });
        DeactivateCommand = new RelayCommand(Deactivate, () => SelectedBook != null);
        AddCopyCommand = new RelayCommand(AddCopy, () => SelectedBook != null);
        SaveCopyCommand = new RelayCommand(SaveCopy, () => SelectedCopy != null && !SelectedCopy.IsOnLoan);

        Refresh();
    }

    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand DeactivateCommand { get; }
    public ICommand AddCopyCommand { get; }
    public ICommand SaveCopyCommand { get; }

    public IReadOnlyList<CopyStatusOption> StatusOptions { get; }

    // ===== Списък =====

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (Set(ref _searchText, value))
            {
                Refresh();
            }
        }
    }

    public IReadOnlyList<BookRow> Books
    {
        get => _books;
        private set => Set(ref _books, value);
    }

    public BookRow? SelectedBook
    {
        get => _selectedBook;
        set
        {
            if (Set(ref _selectedBook, value))
            {
                OnPropertyChanged(nameof(HasSelection));
                LoadCopies();
            }
        }
    }

    public bool HasSelection => _selectedBook != null;

    // ===== Екземпляри =====

    public IReadOnlyList<CopyRow> Copies
    {
        get => _copies;
        private set => Set(ref _copies, value);
    }

    public CopyRow? SelectedCopy
    {
        get => _selectedCopy;
        set
        {
            if (Set(ref _selectedCopy, value))
            {
                EditInventoryNumber = value?.InventoryNumber ?? "";
                SelectedStatus = StatusOptions.FirstOrDefault(o => value != null && o.Value == value.Status);
                OnPropertyChanged(nameof(HasSelectedCopy));
                OnPropertyChanged(nameof(SelectedCopyEditable));
                OnPropertyChanged(nameof(SelectedCopyLocked));
            }
        }
    }

    public bool HasSelectedCopy => _selectedCopy != null;
    public bool SelectedCopyEditable => _selectedCopy != null && !_selectedCopy.IsOnLoan;
    public bool SelectedCopyLocked => _selectedCopy != null && _selectedCopy.IsOnLoan;

    public string NewInventoryNumber { get => _newInventoryNumber; set => Set(ref _newInventoryNumber, value); }
    public string EditInventoryNumber { get => _editInventoryNumber; set => Set(ref _editInventoryNumber, value); }
    public CopyStatusOption? SelectedStatus { get => _selectedStatus; set => Set(ref _selectedStatus, value); }

    // ===== Форма за книга =====

    public bool IsFormOpen
    {
        get => _isFormOpen;
        private set => Set(ref _isFormOpen, value);
    }

    public bool IsAddMode => _editingId == null;
    public string FormHeading => _editingId == null ? "Нова книга" : "Редактиране на книга";

    public string BookTitle { get => _bookTitle; set => Set(ref _bookTitle, value); }
    public string BookAuthor { get => _bookAuthor; set => Set(ref _bookAuthor, value); }
    public string BookGenre { get => _bookGenre; set => Set(ref _bookGenre, value); }
    public string BookIsbn { get => _bookIsbn; set => Set(ref _bookIsbn, value); }
    public string BookShelf { get => _bookShelf; set => Set(ref _bookShelf, value); }
    public string BookDescription { get => _bookDescription; set => Set(ref _bookDescription, value); }
    public string BookCopies { get => _bookCopies; set => Set(ref _bookCopies, value); }
    public string BookMinGrade { get => _bookMinGrade; set => Set(ref _bookMinGrade, value); }
    public string BookMaxGrade { get => _bookMaxGrade; set => Set(ref _bookMaxGrade, value); }
    public bool BookReferenceOnly { get => _bookReferenceOnly; set => Set(ref _bookReferenceOnly, value); }

    // ===== Действия =====

    private void Refresh(int? selectId = null)
    {
        var keep = selectId ?? _selectedBook?.Id;
        Books = _s.FindBooks(_searchText).Select(r => RowFactory.ToBookRow(_s, r.Book)).ToList();
        SelectedBook = Books.FirstOrDefault(b => b.Id == keep);

        // Записите са едни и същи обекти, затова обновяваме ръчно показаното за избраната книга.
        OnPropertyChanged(nameof(SelectedBook));
        OnPropertyChanged(nameof(HasSelection));
        LoadCopies();
    }

    private void LoadCopies()
    {
        Copies = _selectedBook == null
            ? Array.Empty<CopyRow>()
            : _s.CopiesOf(_selectedBook.Book).Select(c => new CopyRow(c)).ToList();
        SelectedCopy = null;
    }

    private void OpenAdd()
    {
        ClearMessage();
        _editingId = null;
        BookTitle = "";
        BookAuthor = "";
        BookGenre = "";
        BookIsbn = "";
        BookShelf = "";
        BookDescription = "";
        BookCopies = "1";
        BookMinGrade = "1";
        BookMaxGrade = "12";
        BookReferenceOnly = false;
        NotifyForm();
        IsFormOpen = true;
    }

    private void OpenEdit()
    {
        var row = SelectedBook;
        if (row == null)
        {
            return;
        }

        ClearMessage();
        var b = row.Book;
        _editingId = b.Id;
        BookTitle = b.Title;
        BookAuthor = b.Author;
        BookGenre = b.Genre;
        BookIsbn = b.Isbn;
        BookShelf = b.Shelf;
        BookDescription = b.Description;
        BookMinGrade = b.MinGrade.ToString();
        BookMaxGrade = b.MaxGrade.ToString();
        BookReferenceOnly = b.ReferenceOnly;
        NotifyForm();
        IsFormOpen = true;
    }

    private void NotifyForm()
    {
        OnPropertyChanged(nameof(IsAddMode));
        OnPropertyChanged(nameof(FormHeading));
    }

    private void SaveBook()
    {
        // Нечислов текст става 0, а услугата връща разбираемо съобщение за полето.
        var draft = new Book
        {
            Id = _editingId ?? 0,
            Title = BookTitle,
            Author = BookAuthor,
            Genre = BookGenre,
            Isbn = BookIsbn,
            Shelf = BookShelf,
            Description = BookDescription,
            MinGrade = ParseInt(BookMinGrade),
            MaxGrade = ParseInt(BookMaxGrade),
            ReferenceOnly = BookReferenceOnly,
        };

        OperationResult result;
        int? selectId;
        string success;

        if (_editingId == null)
        {
            var copies = ParseInt(BookCopies);
            var added = _s.AddBook(draft, copies);
            result = added;
            selectId = added.Value?.Id;
            success = $"Книгата „{draft.Title.Trim()}“ е добавена с {copies} екземпляра.";
        }
        else
        {
            var updated = _s.UpdateBook(draft);
            result = updated;
            selectId = _editingId;
            success = $"Промените по „{draft.Title.Trim()}“ са запазени.";
        }

        if (!result.Success)
        {
            Show(MessageKind.Error, result.Summary);
            return;
        }

        IsFormOpen = false;
        Refresh(selectId);
        Show(MessageKind.Success, success);
    }

    private void Deactivate()
    {
        var row = SelectedBook;
        if (row == null)
        {
            return;
        }

        var result = _s.DeactivateBook(row.Book);
        if (!result.Success)
        {
            Show(MessageKind.Error, result.Summary);
            return;
        }

        Refresh();
        Show(MessageKind.Success, $"Книгата „{row.Title}“ е премахната от каталога.");
    }

    private void AddCopy()
    {
        var row = SelectedBook;
        if (row == null)
        {
            return;
        }

        var result = _s.AddCopy(row.Book, NewInventoryNumber);
        if (!result.Success)
        {
            Show(MessageKind.Error, result.Summary);
            return;
        }

        NewInventoryNumber = "";
        Refresh(row.Id);
        Show(MessageKind.Success, $"Добавен е екземпляр {result.Value!.InventoryNumber}.");
    }

    private void SaveCopy()
    {
        var row = SelectedCopy;
        var bookId = SelectedBook?.Id;
        if (row == null || SelectedStatus == null)
        {
            Show(MessageKind.Error, "Изберете състояние на екземпляра.");
            return;
        }

        var result = _s.UpdateCopy(row.Id, EditInventoryNumber, SelectedStatus.Value);
        if (!result.Success)
        {
            Show(MessageKind.Error, result.Summary);
            return;
        }

        Refresh(bookId);
        Show(MessageKind.Success, $"Екземпляр {result.Value!.InventoryNumber} е запазен.");
    }

    private static int ParseInt(string? text) => int.TryParse(text?.Trim(), out var value) ? value : 0;
}
