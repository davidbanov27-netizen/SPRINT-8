# Училищна библиотека

Десктоп приложение за Windows (**WPF, .NET 9, Visual Studio**), което замества ръчното водене на
училищната библиотека: каталог, читатели, заемане и връщане, просрочени книги, статистика и
търсене и препоръки с изкуствен интелект.

Проектът е за **интегриране на ИИ в училищата на нишово ниво** – тясна ниша с ясни данни и повтарящи се процеси.

## Бърз старт

1. Нужен е **Visual Studio 2022 17.12 или по-нова** (или 2026) с компонента **„.NET desktop development“**.
2. Отворете `SchoolLibrary.sln`.
3. Натиснете **F5**. Няма външни NuGet пакети.

При първо стартиране се зареждат примерни данни. Те се пазят в `%LocalAppData%\SchoolLibrary\library.json`.
За да ги върнете в началното състояние, затворете приложението и изтрийте този файл.

## Документи

| Файл | Съдържание |
|------|------------|
| [docs/01_problem_and_goal.md](docs/01_problem_and_goal.md) | Проблем, потребители, цел, критерии за успех |
| [docs/02_requirements_and_backlog.md](docs/02_requirements_and_backlog.md) | MVP, изисквания, бизнес правила |
| [docs/03_user_scenarios_and_prototype.md](docs/03_user_scenarios_and_prototype.md) | User stories, use cases, прототип, тест за използваемост |
| [docs/04_architecture.md](docs/04_architecture.md) | **Архитектура**, компоненти, диаграми, екипна работа |
| [docs/05_books_and_readers.md](docs/05_books_and_readers.md) | **Книги, екземпляри, читатели**: правила за валидация, тестове, демонстрация |
| [docs/06_loans_and_returns.md](docs/06_loans_and_returns.md) | **Заемане и връщане**: правила, защита от двойно заемане, тестове, демонстрация |
| [docs/backlog.md](docs/backlog.md) | **Актуален backlog** със статуси |
| [CONTRIBUTING.md](CONTRIBUTING.md) | Клонове, commit-и, pull request, Definition of Done |

## Архитектура накратко

```
Views (XAML)  →  ViewModels  →  LibraryService  →  ILibraryRepository  →  library.json
 показва          свързва        РЕШАВА (правила)      пази
```

Бизнес правилата са **само** в `LibraryService`. Подробности и схеми: [docs/04_architecture.md](docs/04_architecture.md).

## Структура

```
SchoolLibrary.sln
SchoolLibrary/
├── App.xaml(.cs)         старт, свързване на слоевете
├── MainWindow.xaml       меню + текущ екран
├── Themes/Styles.xaml    цветове и стилове
├── Models/               Book, BookCopy, Reader, Loan, настройки
├── Services/             LibraryService, ILibraryRepository, DataStore,
│   │                     IAiSearchService, SeedData
│   └── Validation/       валидатори и OperationResult
├── ViewModels/           по един ViewModel на екран
└── Views/                по един XAML екран
SchoolLibrary.Tests/      xUnit тестове
docs/                     документи, диаграми, backlog, прототип
```

## Бизнес правила (настройки в `LibrarySettings`)

Срок 14 дни · лимит 3 книги за ученик и 5 за учител · блокиране при просрочена книга ·
без второ копие на същото заглавие · справочните издания не се изнасят ·
повредена или загубена книга излиза от наличността · при просрочие над 30 дни редът се маркира „за разглеждане“.

## Състояние

Работи: вход по роля, каталог с добавяне/редактиране на книги и екземпляри (уникален инвентарен номер), читатели с уникален читателски номер, заемане на конкретен екземпляр, връщане (и по инвентарен номер), просрочени, статистика, търсене, препоръки, „Моите книги“.

Още не е направено (виж [docs/backlog.md](docs/backlog.md)): парола, истински ИИ модел, удължаване и резервации.

## Тестове

Visual Studio: **Test → Run All Tests** (или Test Explorer). Команден ред: `dotnet test`.
Какво се проверява: [docs/05_books_and_readers.md](docs/05_books_and_readers.md).

> Ако преди това сте стартирали по-стара версия, файлът `%LocalAppData%\SchoolLibrary\library.json`
> се копира в `library.v0.bak` и се зарежда ново примерно съдържание (схема v2).

## Работа в екип

Накратко: всяка задача е в свой клон (`feature/...`), commit-ите са във формат `тип: описание`,
а в `main` се влиза само с pull request. Всички правила: [CONTRIBUTING.md](CONTRIBUTING.md).
