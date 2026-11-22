using SchoolLibrary.Models;

namespace SchoolLibrary.Services;

/// <summary>Примерни данни за първо стартиране. Датите са спрямо подадения ден.</summary>
public static class SeedData
{
    public static LibraryData Create(DateTime today)
    {
        var data = new LibraryData { SchemaVersion = LibraryData.CurrentSchemaVersion };

        data.Books.AddRange(new[]
        {
            new Book { Id = 1, Title = "Под игото", Author = "Иван Вазов", Genre = "Роман", MinGrade = 8, MaxGrade = 12, Shelf = "А-12", Description = "Роман за Априлското въстание." },
            new Book { Id = 2, Title = "Хари Потър и философския камък", Author = "Дж. К. Роулинг", Genre = "Фентъзи", MinGrade = 5, MaxGrade = 8, Shelf = "Б-03", Description = "Момче открива, че е магьосник." },
            new Book { Id = 3, Title = "Островът на съкровищата", Author = "Р. Л. Стивънсън", Genre = "Приключения", MinGrade = 5, MaxGrade = 7, Shelf = "Б-07", Description = "Пирати, карта и съкровище." },
            new Book { Id = 4, Title = "Тримата мускетари", Author = "Александър Дюма", Genre = "Приключения", MinGrade = 6, MaxGrade = 9, Shelf = "Б-09", Description = "Приключения във Франция през 17. век." },
            new Book { Id = 5, Title = "Малкият принц", Author = "Антоан дьо Сент-Екзюпери", Genre = "Приказка", MinGrade = 3, MaxGrade = 7, Shelf = "В-01", Description = "Философска приказка за дружбата." },
            new Book { Id = 6, Title = "Български тълковен речник", Author = "Колектив", Genre = "Справочник", MinGrade = 1, MaxGrade = 12, Shelf = "Р-01", Description = "Само за четене в библиотеката.", ReferenceOnly = true },
            new Book { Id = 7, Title = "Хобитът", Author = "Дж. Р. Р. Толкин", Genre = "Фентъзи", MinGrade = 6, MaxGrade = 9, Shelf = "Б-04", Description = "Билбо тръгва на неочаквано пътешествие." },
            new Book { Id = 8, Title = "Пипи Дългото чорапче", Author = "Астрид Линдгрен", Genre = "Приказка", MinGrade = 2, MaxGrade = 5, Shelf = "В-02", Description = "Най-силното момиче на света." },
            new Book { Id = 9, Title = "Бай Ганьо", Author = "Алеко Константинов", Genre = "Роман", MinGrade = 9, MaxGrade = 12, Shelf = "А-14", Description = "Сатира за българина и Европа." },

            // Българска литература и още заглавия за разширения каталог
            new Book { Id = 10, Title = "Българи от старо време", Author = "Иван Вазов", Genre = "Разкази", MinGrade = 6, MaxGrade = 9, Shelf = "А-01", Description = "Разкази за живота на българите преди Освобождението." },
            new Book { Id = 11, Title = "Немили-недраги", Author = "Иван Вазов", Genre = "Повест", MinGrade = 7, MaxGrade = 9, Shelf = "А-02", Description = "Съдбата на българските емигранти в Румъния." },
            new Book { Id = 12, Title = "Нова земя", Author = "Иван Вазов", Genre = "Роман", MinGrade = 9, MaxGrade = 12, Shelf = "А-03", Description = "Българското общество малко след Освобождението." },
            new Book { Id = 13, Title = "Чичовци", Author = "Иван Вазов", Genre = "Повест", MinGrade = 7, MaxGrade = 10, Shelf = "А-04", Description = "Сатирична картина на градския живот в Македония." },
            new Book { Id = 14, Title = "Епопея на забравените", Author = "Иван Вазов", Genre = "Поезия", MinGrade = 8, MaxGrade = 12, Shelf = "П-01", Description = "Цикъл оди за героите на Възраждането." },
            new Book { Id = 15, Title = "Опълченците на Шипка", Author = "Иван Вазов", Genre = "Поезия", MinGrade = 5, MaxGrade = 8, Shelf = "П-02", Description = "Стихотворение за подвига на опълченците." },
            new Book { Id = 16, Title = "До Чикаго и назад", Author = "Алеко Константинов", Genre = "Пътепис", MinGrade = 9, MaxGrade = 12, Shelf = "А-15", Description = "Пътепис за посещение на Световното изложение в Чикаго." },
            new Book { Id = 17, Title = "Старопланински легенди", Author = "Йордан Йовков", Genre = "Разкази", MinGrade = 6, MaxGrade = 9, Shelf = "А-05", Description = "Легенди за хората и природата на Стара планина." },
            new Book { Id = 18, Title = "Чифликът край границата", Author = "Йордан Йовков", Genre = "Роман", MinGrade = 9, MaxGrade = 12, Shelf = "А-06", Description = "Драмата на семейство край границата." },
            new Book { Id = 19, Title = "Вечери в Антимовския хан", Author = "Йордан Йовков", Genre = "Разкази", MinGrade = 7, MaxGrade = 10, Shelf = "А-07", Description = "Разкази, разказани в един крайпътен хан." },
            new Book { Id = 20, Title = "Албена", Author = "Йордан Йовков", Genre = "Драма", MinGrade = 8, MaxGrade = 11, Shelf = "Д-01", Description = "Драма за любовта и греха в една селска общност." },
            new Book { Id = 21, Title = "Женско сърце", Author = "Йордан Йовков", Genre = "Разкази", MinGrade = 8, MaxGrade = 11, Shelf = "А-08", Description = "Разкази за силата и нежността на женската душа." },
            new Book { Id = 22, Title = "Железният светилник", Author = "Димитър Талев", Genre = "Роман", MinGrade = 10, MaxGrade = 12, Shelf = "А-09", Description = "Първата книга от тетралогията за Преспа." },
            new Book { Id = 23, Title = "Преспанските камбани", Author = "Димитър Талев", Genre = "Роман", MinGrade = 10, MaxGrade = 12, Shelf = "А-10", Description = "Втората книга от тетралогията за Преспа." },
            new Book { Id = 24, Title = "Тютюн", Author = "Димитър Димов", Genre = "Роман", MinGrade = 11, MaxGrade = 12, Shelf = "А-11", Description = "Голям български роман за тютюневата индустрия." },
            new Book { Id = 25, Title = "Осъдени души", Author = "Димитър Димов", Genre = "Роман", MinGrade = 11, MaxGrade = 12, Shelf = "А-13", Description = "Роман за любовта и избора на лекаря Алберт." },
            new Book { Id = 26, Title = "Гераците", Author = "Елин Пелин", Genre = "Роман", MinGrade = 8, MaxGrade = 11, Shelf = "А-16", Description = "Семейна драма в едно българско село." },
            new Book { Id = 27, Title = "Под манастирската лоза", Author = "Елин Пелин", Genre = "Разкази", MinGrade = 6, MaxGrade = 9, Shelf = "А-17", Description = "Разкази за селския живот и хората от него." },
            new Book { Id = 28, Title = "Ян Бибиян", Author = "Елин Пелин", Genre = "Приказка", MinGrade = 3, MaxGrade = 6, Shelf = "В-03", Description = "Приключенията на момче, което върви към Луната." },
            new Book { Id = 29, Title = "Ян Бибиян на луната", Author = "Елин Пелин", Genre = "Приказка", MinGrade = 3, MaxGrade = 6, Shelf = "В-04", Description = "Продължението на приключенията на Ян Бибиян." },
            new Book { Id = 30, Title = "Пижо и Пенда", Author = "Елин Пелин", Genre = "Приказка", MinGrade = 3, MaxGrade = 6, Shelf = "В-05", Description = "Весела история за двама приятели." },
            new Book { Id = 31, Title = "Земя", Author = "Елин Пелин", Genre = "Разкази", MinGrade = 7, MaxGrade = 10, Shelf = "А-18", Description = "Разказ за привързаността към земята." },
            new Book { Id = 32, Title = "Стихотворения", Author = "Христо Ботев", Genre = "Поезия", MinGrade = 7, MaxGrade = 12, Shelf = "П-03", Description = "Хаджи Димитър, На прощаване, Обесването на Васил Левски." },
            new Book { Id = 33, Title = "Подир сенките на облаците", Author = "Пейо Яворов", Genre = "Поезия", MinGrade = 9, MaxGrade = 12, Shelf = "П-04", Description = "Стихосбирка на големия български символист." },
            new Book { Id = 34, Title = "Стихотворения", Author = "Димчо Дебелянов", Genre = "Поезия", MinGrade = 8, MaxGrade = 12, Shelf = "П-05", Description = "Тиха стихосбирка за скръбта и спомена." },
            new Book { Id = 35, Title = "Да бъде ден!", Author = "Христо Смирненски", Genre = "Поезия", MinGrade = 8, MaxGrade = 12, Shelf = "П-06", Description = "Поезия за надеждата и социалната справедливост." },
            new Book { Id = 36, Title = "Моторни песни", Author = "Никола Вапцаров", Genre = "Поезия", MinGrade = 9, MaxGrade = 12, Shelf = "П-07", Description = "Единствената прижизнена стихосбирка на поета." },
            new Book { Id = 37, Title = "Вечната и святата", Author = "Елисавета Багряна", Genre = "Поезия", MinGrade = 8, MaxGrade = 12, Shelf = "П-08", Description = "Първата стихосбирка на Елисавета Багряна." },
            new Book { Id = 38, Title = "Стихотворения", Author = "Атанас Далчев", Genre = "Поезия", MinGrade = 10, MaxGrade = 12, Shelf = "П-09", Description = "Философска лирика на един от големите български поети." },
            new Book { Id = 39, Title = "Антихрист", Author = "Емилиян Станев", Genre = "Роман", MinGrade = 11, MaxGrade = 12, Shelf = "А-19", Description = "Исторически роман за Търновското царство." },
            new Book { Id = 40, Title = "Крадецът на праскови", Author = "Емилиян Станев", Genre = "Роман", MinGrade = 9, MaxGrade = 12, Shelf = "А-20", Description = "Любовна история по време на война." },
            new Book { Id = 41, Title = "Иван Кондарев", Author = "Емилиян Станев", Genre = "Роман", MinGrade = 11, MaxGrade = 12, Shelf = "А-21", Description = "Роман за България между двете войни." },
            new Book { Id = 42, Title = "Диви разкази", Author = "Николай Хайтов", Genre = "Разкази", MinGrade = 8, MaxGrade = 11, Shelf = "А-22", Description = "Разкази за родопските хора и нрави." },
            new Book { Id = 43, Title = "Ние, врабчетата", Author = "Йордан Радичков", Genre = "Разкази", MinGrade = 8, MaxGrade = 12, Shelf = "А-23", Description = "Сборник с разкази с особен хумор." },
            new Book { Id = 44, Title = "Време разделно", Author = "Антон Дончев", Genre = "Роман", MinGrade = 10, MaxGrade = 12, Shelf = "А-24", Description = "Роман за Родопите през 17. век." },
            new Book { Id = 45, Title = "Хоро", Author = "Антон Страшимиров", Genre = "Роман", MinGrade = 10, MaxGrade = 12, Shelf = "А-25", Description = "Роман за селото преди Балканската война." },
            new Book { Id = 46, Title = "Майстори", Author = "Рачо Стоянов", Genre = "Драма", MinGrade = 8, MaxGrade = 11, Shelf = "Д-02", Description = "Драма за майсторите строители и дадената дума." },
            new Book { Id = 47, Title = "Службогонци", Author = "Стефан Л. Костов", Genre = "Комедия", MinGrade = 9, MaxGrade = 12, Shelf = "Д-03", Description = "Сатирична комедия за кариеризма." },
            new Book { Id = 48, Title = "Записки по българските въстания", Author = "Захарий Стоянов", Genre = "Мемоари", MinGrade = 9, MaxGrade = 12, Shelf = "А-26", Description = "Спомени за Априлското въстание." },
            new Book { Id = 49, Title = "Патиланско царство", Author = "Ран Босилек", Genre = "Приказка", MinGrade = 2, MaxGrade = 5, Shelf = "В-06", Description = "Весели стихотворни приказки за деца." },
            new Book { Id = 50, Title = "Български народни приказки", Author = "Колектив", Genre = "Приказка", MinGrade = 1, MaxGrade = 5, Shelf = "В-07", Description = "Сборник с най-известните български приказки." },
            new Book { Id = 51, Title = "Приказки", Author = "Ангел Каралийчев", Genre = "Приказка", MinGrade = 3, MaxGrade = 6, Shelf = "В-08", Description = "Приказки за деца от български автор." },
            new Book { Id = 52, Title = "Избрани разкази", Author = "Чудомир", Genre = "Хумор", MinGrade = 7, MaxGrade = 10, Shelf = "А-27", Description = "Хумористични разкази за българските нрави." },
            new Book { Id = 53, Title = "Физика на тъгата", Author = "Георги Господинов", Genre = "Роман", MinGrade = 11, MaxGrade = 12, Shelf = "С-01", Description = "Съвременен български роман за детството и паметта." },
            new Book { Id = 54, Title = "Градинарят и смъртта", Author = "Георги Господинов", Genre = "Роман", MinGrade = 11, MaxGrade = 12, Shelf = "С-02", Description = "Кратък съвременен роман за загубата." },
            new Book { Id = 55, Title = "Времеубежище", Author = "Георги Господинов", Genre = "Роман", MinGrade = 11, MaxGrade = 12, Shelf = "С-03", Description = "Роман за миналото като убежище." },
            new Book { Id = 56, Title = "Приключенията на Том Сойер", Author = "Марк Твен", Genre = "Приключения", MinGrade = 4, MaxGrade = 7, Shelf = "Б-10", Description = "Весели приключения на едно момче край реката." },
            new Book { Id = 57, Title = "Граф Монте Кристо", Author = "Александър Дюма", Genre = "Приключения", MinGrade = 8, MaxGrade = 11, Shelf = "Б-11", Description = "История за несправедливост и отмъщение." },
            new Book { Id = 58, Title = "Престъпление и наказание", Author = "Фьодор Достоевски", Genre = "Роман", MinGrade = 11, MaxGrade = 12, Shelf = "Ч-01", Description = "Психологически роман за вина и съвест." },
            new Book { Id = 59, Title = "1984", Author = "Джордж Оруел", Genre = "Роман", MinGrade = 10, MaxGrade = 12, Shelf = "Ч-02", Description = "Антиутопия за тоталитарно общество." },
            new Book { Id = 60, Title = "Правописен речник на българския език", Author = "Колектив", Genre = "Справочник", MinGrade = 1, MaxGrade = 12, Shelf = "Р-02", Description = "Само за четене в библиотеката.", ReferenceOnly = true },
        });

        // Колко екземпляра има всяко заглавие. Номерата са BK-00001, BK-00002 …
        var copiesPerBook = new Dictionary<int, int>
        {
            [1] = 3, [2] = 2, [3] = 2, [4] = 1, [5] = 2, [6] = 1, [7] = 2, [8] = 2, [9] = 2,
            [10] = 3, [11] = 2, [12] = 2, [13] = 2, [14] = 2, [15] = 2, [16] = 1, [17] = 3, [18] = 2, [19] = 2, [20] = 2, [21] = 1, [22] = 3, [23] = 1, [24] = 3, [25] = 1, [26] = 2, [27] = 2, [28] = 3, [29] = 2, [30] = 2, [31] = 1, [32] = 4, [33] = 2, [34] = 2, [35] = 2, [36] = 3, [37] = 2, [38] = 1, [39] = 2, [40] = 2, [41] = 1, [42] = 2, [43] = 2, [44] = 2, [45] = 1, [46] = 2, [47] = 1, [48] = 2, [49] = 3, [50] = 4, [51] = 2, [52] = 2, [53] = 2, [54] = 1, [55] = 3, [56] = 3, [57] = 2, [58] = 2, [59] = 2, [60] = 1,
        };
        var seq = 0;
        foreach (var book in data.Books)
        {
            for (var n = 0; n < copiesPerBook[book.Id]; n++)
            {
                seq++;
                data.Copies.Add(new BookCopy { Id = seq, BookId = book.Id, InventoryNumber = $"BK-{seq:D5}" });
            }
        }

        data.Readers.AddRange(new[]
        {
            new Reader { Id = 101, CardNumber = "RD-0101", Name = "Иван Петров", ClassName = "7Б", Role = ReaderRole.Student },
            new Reader { Id = 102, CardNumber = "RD-0102", Name = "Мария Георгиева", ClassName = "6А", Role = ReaderRole.Student },
            new Reader { Id = 103, CardNumber = "RD-0103", Name = "Георги Димитров", ClassName = "8В", Role = ReaderRole.Student },
            new Reader { Id = 104, CardNumber = "RD-0104", Name = "Елена Стоянова", ClassName = "Учител", Role = ReaderRole.Teacher },

            // По-голямо училище: ученици от 5. до 12. клас, учители и двама отписани читатели
            new Reader { Id = 105, CardNumber = "RD-0105", Name = "Александра Николова", ClassName = "5А", Role = ReaderRole.Student },
            new Reader { Id = 106, CardNumber = "RD-0106", Name = "Мартин Стефанов", ClassName = "5А", Role = ReaderRole.Student },
            new Reader { Id = 107, CardNumber = "RD-0107", Name = "Виктория Йорданова", ClassName = "5Б", Role = ReaderRole.Student },
            new Reader { Id = 108, CardNumber = "RD-0108", Name = "Борис Ангелов", ClassName = "5Б", Role = ReaderRole.Student },
            new Reader { Id = 109, CardNumber = "RD-0109", Name = "Дара Тодорова", ClassName = "6А", Role = ReaderRole.Student },
            new Reader { Id = 110, CardNumber = "RD-0110", Name = "Никола Атанасов", ClassName = "6Б", Role = ReaderRole.Student },
            new Reader { Id = 111, CardNumber = "RD-0111", Name = "Калоян Маринов", ClassName = "6В", Role = ReaderRole.Student },
            new Reader { Id = 112, CardNumber = "RD-0112", Name = "Ивана Колева", ClassName = "6В", Role = ReaderRole.Student },
            new Reader { Id = 113, CardNumber = "RD-0113", Name = "Радослав Георгиев", ClassName = "7А", Role = ReaderRole.Student },
            new Reader { Id = 114, CardNumber = "RD-0114", Name = "Симона Петкова", ClassName = "7А", Role = ReaderRole.Student },
            new Reader { Id = 115, CardNumber = "RD-0115", Name = "Теодор Христов", ClassName = "7В", Role = ReaderRole.Student },
            new Reader { Id = 116, CardNumber = "RD-0116", Name = "Габриела Минчева", ClassName = "7Г", Role = ReaderRole.Student },
            new Reader { Id = 117, CardNumber = "RD-0117", Name = "Стилиян Иванов", ClassName = "8А", Role = ReaderRole.Student },
            new Reader { Id = 118, CardNumber = "RD-0118", Name = "Йоана Василева", ClassName = "8Б", Role = ReaderRole.Student },
            new Reader { Id = 119, CardNumber = "RD-0119", Name = "Кристиян Донев", ClassName = "8Б", Role = ReaderRole.Student },
            new Reader { Id = 120, CardNumber = "RD-0120", Name = "Деница Събева", ClassName = "8Г", Role = ReaderRole.Student },
            new Reader { Id = 121, CardNumber = "RD-0121", Name = "Виктор Лазаров", ClassName = "9А", Role = ReaderRole.Student },
            new Reader { Id = 122, CardNumber = "RD-0122", Name = "Моника Тенева", ClassName = "9А", Role = ReaderRole.Student },
            new Reader { Id = 123, CardNumber = "RD-0123", Name = "Даниел Костов", ClassName = "9Б", Role = ReaderRole.Student },
            new Reader { Id = 124, CardNumber = "RD-0124", Name = "Ралица Златева", ClassName = "9В", Role = ReaderRole.Student },
            new Reader { Id = 125, CardNumber = "RD-0125", Name = "Преслав Недялков", ClassName = "9Г", Role = ReaderRole.Student },
            new Reader { Id = 126, CardNumber = "RD-0126", Name = "Анна Маврова", ClassName = "10А", Role = ReaderRole.Student },
            new Reader { Id = 127, CardNumber = "RD-0127", Name = "Любомир Русев", ClassName = "10Б", Role = ReaderRole.Student },
            new Reader { Id = 128, CardNumber = "RD-0128", Name = "Елица Вутова", ClassName = "10Б", Role = ReaderRole.Student },
            new Reader { Id = 129, CardNumber = "RD-0129", Name = "Станислав Пенев", ClassName = "10В", Role = ReaderRole.Student },
            new Reader { Id = 130, CardNumber = "RD-0130", Name = "Невена Цветанова", ClassName = "11А", Role = ReaderRole.Student },
            new Reader { Id = 131, CardNumber = "RD-0131", Name = "Владимир Андонов", ClassName = "11Б", Role = ReaderRole.Student },
            new Reader { Id = 132, CardNumber = "RD-0132", Name = "Силвия Гаврилова", ClassName = "11В", Role = ReaderRole.Student },
            new Reader { Id = 133, CardNumber = "RD-0133", Name = "Огнян Радев", ClassName = "11Г", Role = ReaderRole.Student },
            new Reader { Id = 134, CardNumber = "RD-0134", Name = "Тереза Бонева", ClassName = "12А", Role = ReaderRole.Student },
            new Reader { Id = 135, CardNumber = "RD-0135", Name = "Явор Стойков", ClassName = "12Б", Role = ReaderRole.Student },
            new Reader { Id = 136, CardNumber = "RD-0136", Name = "Христина Дамянова", ClassName = "12В", Role = ReaderRole.Student },
            new Reader { Id = 137, CardNumber = "RD-0137", Name = "Атанас Белев", ClassName = "12Г", Role = ReaderRole.Student },
            new Reader { Id = 138, CardNumber = "RD-0138", Name = "Лилия Спасова", ClassName = "5В", Role = ReaderRole.Student },
            new Reader { Id = 139, CardNumber = "RD-0139", Name = "Евгени Митев", ClassName = "5Г", Role = ReaderRole.Student },
            new Reader { Id = 140, CardNumber = "RD-0140", Name = "Анастасия Попова", ClassName = "6Б", Role = ReaderRole.Student },
            new Reader { Id = 141, CardNumber = "RD-0141", Name = "Добромир Кирилов", ClassName = "7Б", Role = ReaderRole.Student },
            new Reader { Id = 142, CardNumber = "RD-0142", Name = "Пламена Борисова", ClassName = "7В", Role = ReaderRole.Student },
            new Reader { Id = 143, CardNumber = "RD-0143", Name = "Цветан Шопов", ClassName = "8В", Role = ReaderRole.Student },
            new Reader { Id = 144, CardNumber = "RD-0144", Name = "Радина Ненова", ClassName = "8А", Role = ReaderRole.Student },
            new Reader { Id = 145, CardNumber = "RD-0145", Name = "Йордан Куманов", ClassName = "9Б", Role = ReaderRole.Student },
            new Reader { Id = 146, CardNumber = "RD-0146", Name = "Магдалена Янева", ClassName = "10А", Role = ReaderRole.Student },
            new Reader { Id = 147, CardNumber = "RD-0147", Name = "Константин Вълчев", ClassName = "11А", Role = ReaderRole.Student },
            new Reader { Id = 148, CardNumber = "RD-0148", Name = "Зорница Генова", ClassName = "12Б", Role = ReaderRole.Student },
            new Reader { Id = 149, CardNumber = "RD-0149", Name = "Румяна Коларова", ClassName = "Учител", Role = ReaderRole.Teacher, Contact = "r.kolarova@school.example" },
            new Reader { Id = 150, CardNumber = "RD-0150", Name = "Петя Атанасова", ClassName = "Учител", Role = ReaderRole.Teacher, Contact = "p.atanasova@school.example" },
            new Reader { Id = 151, CardNumber = "RD-0151", Name = "Стоян Ковачев", ClassName = "Учител", Role = ReaderRole.Teacher, Contact = "s.kovachev@school.example" },
            new Reader { Id = 152, CardNumber = "RD-0152", Name = "Милена Христова", ClassName = "Учител", Role = ReaderRole.Teacher, Contact = "m.hristova@school.example" },
            new Reader { Id = 153, CardNumber = "RD-0153", Name = "Райна Желева", ClassName = "Учител", Role = ReaderRole.Teacher, Contact = "r.zheleva@school.example" },
            new Reader { Id = 154, CardNumber = "RD-0154", Name = "Васил Панайотов", ClassName = "Учител", Role = ReaderRole.Teacher, Contact = "v.panayotov@school.example" },
            new Reader { Id = 155, CardNumber = "RD-0155", Name = "Борислав Ценов", ClassName = "12А", Role = ReaderRole.Student, IsActive = false },
            new Reader { Id = 156, CardNumber = "RD-0156", Name = "Мирослава Данчева", ClassName = "12Г", Role = ReaderRole.Student, IsActive = false },
        });

        // Активно заемане: dueOffset е колко дни от днес е срокът (отрицателно = просрочено).
        Loan Active(int id, int bookId, int readerId, int dueOffset)
        {
            var copy = data.Copies.First(c => c.BookId == bookId && c.Status == CopyStatus.Available);
            copy.Status = CopyStatus.OnLoan;
            return new Loan
            {
                Id = id, CopyId = copy.Id, BookId = bookId, ReaderId = readerId,
                LoanedOn = today.AddDays(dueOffset - 14),
                DueDate = today.AddDays(dueOffset),
            };
        }

        // Като Active, но пропуска заемането, ако няма свободен екземпляр (за допълнителните примерни данни).
        Loan? TryActive(int id, int bookId, int readerId, int dueOffset) =>
            data.Copies.Any(c => c.BookId == bookId && c.Status == CopyStatus.Available)
                ? Active(id, bookId, readerId, dueOffset)
                : null;

        // Приключено заемане – за статистиката и за препоръките.
        Loan Done(int id, int bookId, int readerId, int loanedDaysAgo, int keptDays)
        {
            var copy = data.Copies.First(c => c.BookId == bookId);
            return new Loan
            {
                Id = id, CopyId = copy.Id, BookId = bookId, ReaderId = readerId,
                LoanedOn = today.AddDays(-loanedDaysAgo),
                DueDate = today.AddDays(-loanedDaysAgo + 14),
                ReturnedOn = today.AddDays(-loanedDaysAgo + keptDays),
                Condition = BookCondition.Good,
            };
        }

        data.Loans.AddRange(new[]
        {
            Active(1, 1, 101, 8),
            Active(2, 2, 101, -7),
            Active(3, 3, 102, 5),
            Active(4, 5, 102, 3),
            Active(5, 4, 102, 9),
            Active(6, 2, 103, -20),

            Done(11, 1, 103, 75, 12),
            Done(12, 3, 101, 52, 10),
            Done(13, 5, 104, 38, 7),
            Done(14, 1, 102, 95, 13),
            Done(15, 4, 103, 33, 14),
            Done(16, 7, 101, 20, 9),
            Done(17, 2, 102, 48, 13),
            Done(18, 3, 103, 22, 5),
        });

        // Допълнителни текущи заемания: повечето са в срок, няколко изтичат скоро, четирима читатели закъсняват.
        data.Loans.AddRange(new Loan?[]
        {
            TryActive(21, 10, 105, 6),
            TryActive(22, 50, 106, 9),
            TryActive(23, 28, 107, 4),
            TryActive(24, 49, 108, 11),
            TryActive(25, 15, 109, 2),
            TryActive(26, 52, 110, -3),
            TryActive(27, 10, 111, 7),
            TryActive(28, 17, 112, 12),
            TryActive(29, 27, 113, 1),
            TryActive(30, 11, 114, 8),
            TryActive(31, 17, 115, 3),
            TryActive(32, 42, 116, 10),
            TryActive(33, 20, 117, -35),
            TryActive(34, 14, 118, 5),
            TryActive(35, 40, 119, 13),
            TryActive(36, 26, 120, 0),
            TryActive(37, 36, 121, -1),
            TryActive(38, 32, 122, 6),
            TryActive(39, 22, 123, 9),
            TryActive(40, 40, 124, 4),
            TryActive(41, 18, 125, -12),
            TryActive(42, 24, 126, 7),
            TryActive(43, 36, 127, 2),
            TryActive(44, 44, 128, 11),
            TryActive(45, 43, 129, 5),
            TryActive(46, 55, 130, 8),
            TryActive(47, 53, 131, 12),
            TryActive(48, 39, 132, 3),
            TryActive(49, 25, 133, 10),
            TryActive(50, 48, 134, 6),
            TryActive(51, 55, 135, 1),
            TryActive(52, 58, 136, 9),
            TryActive(53, 56, 138, 7),
            TryActive(54, 51, 139, 5),
            TryActive(55, 22, 149, 9),
            TryActive(56, 24, 149, 9),
            TryActive(57, 23, 149, 9),
            TryActive(58, 45, 152, 4),
            TryActive(59, 46, 152, 4),
            TryActive(60, 59, 150, 12),
        }.OfType<Loan>());

        // История за статистиката и препоръките: заемания през последните осем месеца.
        data.Loans.AddRange(new[]
        {
            Done(101, 2, 125, 142, 20),
            Done(102, 2, 119, 243, 17),
            Done(103, 49, 153, 211, 18),
            Done(104, 24, 105, 171, 6),
            Done(105, 2, 123, 223, 7),
            Done(106, 5, 105, 222, 19),
            Done(107, 28, 118, 115, 12),
            Done(108, 26, 127, 220, 16),
            Done(109, 9, 151, 101, 6),
            Done(110, 17, 123, 131, 8),
            Done(111, 10, 106, 231, 15),
            Done(112, 32, 134, 122, 6),
            Done(113, 40, 142, 155, 19),
            Done(114, 2, 132, 143, 19),
            Done(115, 40, 138, 80, 17),
            Done(116, 56, 137, 146, 4),
            Done(117, 24, 154, 47, 5),
            Done(118, 2, 150, 173, 17),
            Done(119, 56, 114, 137, 11),
            Done(120, 2, 145, 193, 18),
            Done(121, 56, 150, 153, 12),
            Done(122, 40, 147, 92, 20),
            Done(123, 56, 156, 240, 16),
            Done(124, 53, 117, 89, 12),
            Done(125, 1, 107, 52, 18),
            Done(126, 49, 128, 249, 10),
            Done(127, 36, 149, 138, 18),
            Done(128, 49, 156, 125, 5),
            Done(129, 17, 125, 131, 7),
            Done(130, 5, 149, 197, 20),
            Done(131, 36, 133, 250, 9),
            Done(132, 53, 151, 136, 6),
            Done(133, 36, 136, 108, 7),
            Done(134, 22, 152, 233, 17),
            Done(135, 7, 133, 248, 4),
            Done(136, 9, 105, 129, 6),
            Done(137, 22, 120, 44, 5),
            Done(138, 36, 147, 207, 15),
            Done(139, 7, 105, 94, 19),
            Done(140, 7, 139, 71, 19),
            Done(141, 7, 143, 238, 20),
            Done(142, 17, 110, 120, 9),
            Done(143, 7, 155, 214, 13),
            Done(144, 55, 129, 220, 8),
            Done(145, 22, 129, 17, 7),
            Done(146, 42, 109, 27, 15),
            Done(147, 26, 154, 20, 4),
            Done(148, 56, 125, 188, 13),
            Done(149, 5, 125, 247, 7),
            Done(150, 49, 134, 160, 9),
            Done(151, 7, 142, 147, 10),
            Done(152, 53, 144, 100, 18),
            Done(153, 24, 148, 195, 6),
            Done(154, 40, 107, 64, 9),
            Done(155, 1, 146, 153, 5),
            Done(156, 5, 153, 178, 10),
            Done(157, 56, 150, 187, 4),
            Done(158, 7, 146, 149, 10),
            Done(159, 7, 115, 183, 9),
            Done(160, 42, 122, 122, 17),
            Done(161, 9, 129, 92, 9),
            Done(162, 26, 116, 81, 18),
            Done(163, 49, 154, 222, 17),
            Done(164, 17, 134, 59, 14),
            Done(165, 56, 133, 44, 20),
            Done(166, 36, 111, 231, 10),
            Done(167, 24, 105, 37, 18),
            Done(168, 20, 131, 153, 9),
            Done(169, 1, 127, 230, 9),
            Done(170, 26, 140, 189, 10),
            Done(171, 2, 144, 197, 12),
            Done(172, 22, 141, 89, 12),
            Done(173, 7, 111, 214, 12),
            Done(174, 2, 132, 57, 7),
            Done(175, 9, 153, 105, 9),
            Done(176, 2, 137, 94, 12),
            Done(177, 9, 123, 94, 9),
            Done(178, 36, 121, 30, 4),
            Done(179, 55, 154, 79, 17),
            Done(180, 1, 108, 38, 11),
        });

        // По един повреден и един отписан екземпляр – както в истинска библиотека.
        MarkLast(data, 32, CopyStatus.Damaged);
        MarkLast(data, 50, CopyStatus.Withdrawn);

        return data;
    }

    /// <summary>Отбелязва последния свободен екземпляр на книгата със зададеното състояние.</summary>
    private static void MarkLast(LibraryData data, int bookId, CopyStatus status)
    {
        var copy = data.Copies.LastOrDefault(c => c.BookId == bookId && c.Status == CopyStatus.Available);
        if (copy != null)
        {
            copy.Status = status;
        }
    }
}
