using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media;
using SchoolLibrary.Models;
using SchoolLibrary.Services;

namespace SchoolLibrary.ViewModels;

/// <summary>Елемент от менюто: иконка и цвят на раздела (същият цвят като темата на екрана му).</summary>
public sealed record NavItem(string Key, string Title, string Icon, string AccentHex)
{
    public Brush Accent => (Brush)new BrushConverter().ConvertFromString(AccentHex)!;
}

public class MainViewModel : ViewModelBase, INavigator
{
    private readonly LibraryService _service;
    private readonly IAiSearchService _ai;

    private ViewModelBase? _currentPage;
    private NavItem? _selectedNav;
    private Reader? _currentReader;
    private bool _isSignedIn;
    private string _userTitle = "";
    private string _userMode = "";
    private string _storageNotice = "";
    private bool _noticeIsSaveError;

    public MainViewModel(LibraryService service, IAiSearchService ai)
    {
        _service = service;
        _ai = ai;
        LogoutCommand = new RelayCommand(ShowLogin);
        DismissNoticeCommand = new RelayCommand(() => StorageNotice = "");

        // Предупреждение от зареждането (повреден файл, стара версия) и от всеки неуспешен запис.
        StorageNotice = service.LoadNotice ?? "";
        service.StorageStatusChanged += OnStorageStatus;
        ShowLogin();
    }

    public ObservableCollection<NavItem> NavItems { get; } = new();
    public ICommand LogoutCommand { get; }
    public ICommand DismissNoticeCommand { get; }

    /// <summary>Предупреждение за съхранението на данните (празно = всичко е наред).</summary>
    public string StorageNotice
    {
        get => _storageNotice;
        private set
        {
            if (Set(ref _storageNotice, value))
            {
                OnPropertyChanged(nameof(HasStorageNotice));
            }
        }
    }

    public bool HasStorageNotice => _storageNotice.Length > 0;

    private void OnStorageStatus(string? error)
    {
        if (!string.IsNullOrEmpty(error))
        {
            StorageNotice = error;
            _noticeIsSaveError = true;
        }
        else if (_noticeIsSaveError)
        {
            StorageNotice = "";
            _noticeIsSaveError = false;
        }
    }

    public ViewModelBase? CurrentPage
    {
        get => _currentPage;
        private set => Set(ref _currentPage, value);
    }

    public NavItem? SelectedNav
    {
        get => _selectedNav;
        set
        {
            if (Set(ref _selectedNav, value) && value != null)
            {
                CurrentPage = Create(value.Key);
            }
        }
    }

    public bool IsSignedIn
    {
        get => _isSignedIn;
        private set => Set(ref _isSignedIn, value);
    }

    public string UserTitle
    {
        get => _userTitle;
        private set => Set(ref _userTitle, value);
    }

    public string UserMode
    {
        get => _userMode;
        private set => Set(ref _userMode, value);
    }

    public void Navigate(string key)
    {
        var item = NavItems.FirstOrDefault(n => n.Key == key);
        if (item == null)
        {
            return;
        }

        if (ReferenceEquals(_selectedNav, item))
        {
            CurrentPage = Create(key); // същият екран – зареждаме го наново с чисти данни
        }
        else
        {
            SelectedNav = item;
        }
    }

    private void ShowLogin()
    {
        IsSignedIn = false;
        _currentReader = null;
        NavItems.Clear();
        _selectedNav = null;
        OnPropertyChanged(nameof(SelectedNav));
        CurrentPage = new LoginViewModel(_service, SignIn);
    }

    private void SignIn(Reader? reader)
    {
        _currentReader = reader;
        NavItems.Clear();

        if (reader == null)
        {
            UserTitle = "Библиотекар";
            UserMode = "Режим „Библиотекар“";
            NavItems.Add(new NavItem("dash", "Табло", "🏠", "#F59E0B"));
            NavItems.Add(new NavItem("catalog", "Каталог", "📚", "#EA580C"));
            NavItems.Add(new NavItem("readers", "Читатели", "👥", "#EC4899"));
            NavItems.Add(new NavItem("loan", "Заемане", "📖", "#84CC16"));
            NavItems.Add(new NavItem("return", "Връщане", "📥", "#22D3EE"));
            NavItems.Add(new NavItem("overdue", "Просрочени", "⏰", "#F87171"));
            NavItems.Add(new NavItem("stats", "Статистика", "📊", "#C084FC"));
        }
        else
        {
            UserTitle = $"Здравей, {reader.Name.Split(' ')[0]}! 👋";
            UserMode = "Режим „Читател“";
            NavItems.Add(new NavItem("search", "Търсене", "🔎", "#FB7C5C"));
            NavItems.Add(new NavItem("recs", "Препоръки", "✨", "#E879F9"));
            NavItems.Add(new NavItem("mybooks", "Моите книги", "🎒", "#EAB308"));
        }

        IsSignedIn = true;
        SelectedNav = NavItems[0];
    }

    private ViewModelBase Create(string key) => key switch
    {
        "dash" => new DashboardViewModel(_service, this),
        "catalog" => new CatalogViewModel(_service),
        "readers" => new ReadersViewModel(_service),
        "loan" => new LoanViewModel(_service, this),
        "return" => new ReturnViewModel(_service),
        "overdue" => new OverdueViewModel(_service),
        "stats" => new StatsViewModel(_service),
        "search" => new SearchViewModel(_service, _ai),
        "recs" => new RecsViewModel(_service, _currentReader!),
        "mybooks" => new MyBooksViewModel(_service, _currentReader!),
        _ => throw new ArgumentOutOfRangeException(nameof(key), key, "Непознат екран."),
    };
}

public class LoginViewModel : ViewModelBase
{
    private readonly Action<Reader?> _signIn;
    private int _roleIndex;
    private Reader? _selectedReader;

    public LoginViewModel(LibraryService service, Action<Reader?> signIn)
    {
        _signIn = signIn;
        Readers = service.Readers;
        _selectedReader = Readers.FirstOrDefault();
        LoginCommand = new RelayCommand(Login, () => !IsReaderMode || SelectedReader != null);
    }

    public IReadOnlyList<string> Roles { get; } = new[] { "Библиотекар", "Читател" };
    public IReadOnlyList<Reader> Readers { get; }
    public ICommand LoginCommand { get; }

    public int RoleIndex
    {
        get => _roleIndex;
        set
        {
            if (Set(ref _roleIndex, value))
            {
                OnPropertyChanged(nameof(IsReaderMode));
            }
        }
    }

    public bool IsReaderMode => _roleIndex == 1;

    public Reader? SelectedReader
    {
        get => _selectedReader;
        set => Set(ref _selectedReader, value);
    }

    private void Login() => _signIn(IsReaderMode ? SelectedReader : null);
}
