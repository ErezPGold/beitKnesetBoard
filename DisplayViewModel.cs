using BeitKnesetBoard.Models;
using BeitKnesset.Services;
using BeitKnessetDisplay.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;


namespace BeitKnessetDisplay
{
    public record ReminderPage(string Title, string Body);

    public class DisplayViewModel : INotifyPropertyChanged
    {
        private readonly PrayerService _prayerService = new();
        private readonly ParashaService _parashaService = new();
        private readonly DailyLearningService _learning = new();
        private readonly JewishService _jewish = new();
        private readonly ZmanimService _zmanim = new(lat: 32.08, lng: 34.78, elevation: 0);
        private readonly SefariaService _sefaria = new SefariaService();
        private readonly ParashaSummaryService _parashaSummary = new ParashaSummaryService();

        private int _learningPage = 0;          // 0,1,2
        private bool _isLearningPage1 = true;
        private bool _isLearningPage2 = false;
        private bool _isLearningPage3 = false;

        public bool IsLearningPage1 { get => _isLearningPage1; set => Set(ref _isLearningPage1, value); }
        public bool IsLearningPage2 { get => _isLearningPage2; set => Set(ref _isLearningPage2, value); }
        public bool IsLearningPage3 { get => _isLearningPage3; set => Set(ref _isLearningPage3, value); }

        public void AdvanceLearningPage()
        {
            _learningPage = (_learningPage + 1) % 3;
            IsLearningPage1 = _learningPage == 0;
            IsLearningPage2 = _learningPage == 1;
            IsLearningPage3 = _learningPage == 2;
        }

        // ===== עמודים מתחלפים =====
        // עמוד 0 = דשבורד. אח"כ: תזכורות, זמני תפילות, לזכות, לעילוי נשמה.

        public static readonly IReadOnlyList<ReminderPage> Reminders = new List<ReminderPage>
        {
            new("הכבוד לבית הכנסת",
                "אסור לדבר בשעת התפילה וקריאת התורה"),
            new("נטילת ידיים לסעודה",
                "יש ליטול את הידיים שלוש פעמים לסירוגין על כל יד, ולברך 'על נטילת ידיים' לפני הניגוב"),
            //new("ברכת המזון",
            //    "ברכת המזון בכוונה — סגולה לפרנסה ולשמירה. נכון לברך מתוך הסידור"),
            //new("שמירת הזמן",
            //    "מנהג חב\"ד: לומר את כל התהילים לפני התפילה בשבת מברכים, ולא לאחר זמן התפילה"),
            //new("צדקה לפני התפילה",
            //    "מנהג ישראל לתת צדקה לפני התפילה כמו שנאמר— \"ואני בצדק אחזה פניך\""),
            //new("קידוש שבת מברכים",
            //    "אנו שמחים להודיע כי קידוש בכל שבת מברכים ייתרם על ידי משפחת אמיתי משה"),
            //new("שמירת הניקיון",
            //    "בבקשה לשמור על הניקיון של בית הכנסת"),
            //new("מצוות ריצה לבית הכנסת",
            //    "ישנו עניין הלכתי לרוץ או ללכת במהירות בדרך אל בית הכנסת, כדי להראות חביבות ורצון לקיים את המצווה. לעומת זאת, כאשר יוצאים מבית הכנסת, אסור לרוץ, מכיוון שריצה החוצה משדרת שממהרים לברוח מהמקום ושהשהות בו הייתה עול.")
        };

        // שמות לזכות רפואה שלמה / הצלחה
        public static readonly IReadOnlyList<string> RefuahNames = new List<string>
        {
            "ארז בן פנחס",
            "גאולה גילה בת שושנה מזל",
            "אליהו אברהם בן רבי יעקב",
        };

        // שמות לעילוי נשמה
        public static readonly IReadOnlyList<string> NeshamaNames = new List<string>
        {
            "יעקב בן שלמה","לאה בת אסתר","סבא רחמים","סבתא סוליקה","דוד פיניאן",
        };

        // משך הצגה של כל עמוד (שניות)
        public const int DashboardDurationSeconds = 24;
        public const int OtherPageDurationSeconds = 10;

        public ObservableCollection<Tzaddik> Yahrzeits { get; } = new();
        public string YahrzeitHeader { get; set; } = "🕯 יום הילולא";

        public bool IsYahrzeitVisible => _pageIndex == 6; // קבע לפי המיקום בסבב

        public int CurrentPageDurationSeconds =>
            IsDashboardVisible ? DashboardDurationSeconds : OtherPageDurationSeconds;

        public void SetYahrzeit(YahrzeitDay day)
        {
            Yahrzeits.Clear();
            foreach (var t in day.Tzaddikim) Yahrzeits.Add(t);
            YahrzeitHeader = $"🕯 יום הילולא — {day.HebDate}";
            OnPropertyChanged(nameof(YahrzeitHeader));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (Equals(field, value)) return;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private string _clock = "", _hebrewDate = "", _gregorianDate = "";
        private string _parasha = "", _haftarah = "", _prayers = "";
        private string _tehillim = "", _chumash = "";
        private string _dafYomi = "", _rambam = "", _tanya = "", _hayomYom = "";
        private string _moladText = "", _shabbatMevarchimText = "";
        private string _geshemText = "", _talText = "";
        private bool _isShabbatMevarchim;
        private IReadOnlyList<ZmanItem> _zmanimList = Array.Empty<ZmanItem>();
        private string _rambam1Perek = "", _mishna = "", _yerushalmi = "", _halakha = "";
        private string _tanakhYomi = "", _yom929 = "", _chokLeYisrael = "", _arukhHaShulchan = "";
        private string _parshaName = "", _parshaSummary2 = "";
        public string ParshaName { get => _parshaName; set => Set(ref _parshaName, value); }
        public string ParshaSummary { get => _parshaSummary2; set => Set(ref _parshaSummary2, value); }


        private int _pageIndex = 0;
        private string _reminderTitle = "", _reminderBody = "";
        private bool _isDashboardVisible = true;
        private bool _isReminderVisible = false;
        private bool _isRefuahVisible = false;
        private bool _isNeshamaVisible = false;
        private bool _isPrayerTimesVisible = false;
        private IReadOnlyList<string> _refuahList = Array.Empty<string>();
        private IReadOnlyList<string> _neshamaList = Array.Empty<string>();

        public string Clock { get => _clock; set => Set(ref _clock, value); }
        public string HebrewDate { get => _hebrewDate; set => Set(ref _hebrewDate, value); }
        public string GregorianDate { get => _gregorianDate; set => Set(ref _gregorianDate, value); }
        public string Parasha { get => _parasha; set => Set(ref _parasha, value); }
        public string Haftarah { get => _haftarah; set => Set(ref _haftarah, value); }
        public string Prayers { get => _prayers; set => Set(ref _prayers, value); }
        public string Tehillim { get => _tehillim; set => Set(ref _tehillim, value); }
        public string Chumash { get => _chumash; set => Set(ref _chumash, value); }
        public string DafYomi { get => _dafYomi; set => Set(ref _dafYomi, value); }
        public string Rambam { get => _rambam; set => Set(ref _rambam, value); }
        public string Tanya { get => _tanya; set => Set(ref _tanya, value); }
        public string HayomYom { get => _hayomYom; set => Set(ref _hayomYom, value); }
        public string MoladText { get => _moladText; set => Set(ref _moladText, value); }
        public string ShabbatMevarchimText { get => _shabbatMevarchimText; set => Set(ref _shabbatMevarchimText, value); }
        public string GeshemText { get => _geshemText; set => Set(ref _geshemText, value); }
        public string TalText { get => _talText; set => Set(ref _talText, value); }
        public bool IsShabbatMevarchim { get => _isShabbatMevarchim; set => Set(ref _isShabbatMevarchim, value); }
        public IReadOnlyList<ZmanItem> Zmanim { get => _zmanimList; set => Set(ref _zmanimList, value); }

        public string ReminderTitle { get => _reminderTitle; set => Set(ref _reminderTitle, value); }
        public string ReminderBody { get => _reminderBody; set => Set(ref _reminderBody, value); }
        public bool IsDashboardVisible { get => _isDashboardVisible; set => Set(ref _isDashboardVisible, value); }
        public bool IsReminderVisible { get => _isReminderVisible; set => Set(ref _isReminderVisible, value); }
        public bool IsRefuahVisible { get => _isRefuahVisible; set => Set(ref _isRefuahVisible, value); }
        public bool IsNeshamaVisible { get => _isNeshamaVisible; set => Set(ref _isNeshamaVisible, value); }
        public bool IsPrayerTimesVisible { get => _isPrayerTimesVisible; set => Set(ref _isPrayerTimesVisible, value); }
        public IReadOnlyList<string> RefuahList { get => _refuahList; set => Set(ref _refuahList, value); }
        public IReadOnlyList<string> NeshamaList { get => _neshamaList; set => Set(ref _neshamaList, value); }

        public void RefreshClock() => Clock = DateTime.Now.ToString("HH:mm:ss");
        
        public string Mishna { get => _mishna; set => Set(ref _mishna, value); }
        public string Yerushalmi { get => _yerushalmi; set => Set(ref _yerushalmi, value); }
        public string Halakha { get => _halakha; set => Set(ref _halakha, value); }
        public string TanakhYomi { get => _tanakhYomi; set => Set(ref _tanakhYomi, value); }
        public string Yom929 { get => _yom929; set => Set(ref _yom929, value); }
        public string ChokLeYisrael { get => _chokLeYisrael; set => Set(ref _chokLeYisrael, value); }
        public string Rambam1Perek { get => _rambam1Perek; set => Set(ref _rambam1Perek, value); }
        public string ArukhHaShulchan { get => _arukhHaShulchan; set => Set(ref _arukhHaShulchan, value); }

        private string _parshaRashi = "טוען...";
        public string ParshaRashi
        {
            get => _parshaRashi;
            set { _parshaRashi = value; OnPropertyChanged(nameof(ParshaRashi)); }
        }

        /// <summary>
        /// מחזוריות: דשבורד → תזכורות → זמני תפילות → לזכות → לעילוי נשמה → חוזר.
        /// </summary>
        public void AdvancePage()
        {
            int total = 1 + Reminders.Count + 1 + 1;
            _pageIndex = (_pageIndex + 1) % total;

            IsDashboardVisible = false;
            IsReminderVisible = false;
            IsRefuahVisible = false;
            IsNeshamaVisible = false;
            IsPrayerTimesVisible = false;

            if (_pageIndex == 0)
            {
                IsDashboardVisible = true;
                AdvanceLearningPage();
            }
            else if (_pageIndex <= Reminders.Count)
            {
                var r = Reminders[_pageIndex - 1];
                ReminderTitle = r.Title;
                ReminderBody = r.Body;
                IsReminderVisible = true;
            }
            else if (_pageIndex == Reminders.Count + 1)
            {
                IsPrayerTimesVisible = true;
            }
            else if (_pageIndex == Reminders.Count + 2)
            {
                RefuahList = RefuahNames;
                IsRefuahVisible = true;
            }
            else
            {
                NeshamaList = NeshamaNames;
                IsNeshamaVisible = true;
            }
        }

        // 2. השדות הפרטיים של מזג האוויר
        private readonly WeatherService _weatherService = new WeatherService();
        private string _currentTemperature = "--°C";
        private string _weatherCondition = "טוען...";

        // 3. המאפיינים הציבוריים (Properties) עם מנגנון העדכון של WPF
        public string CurrentTemperature
        {
            get => _currentTemperature;
            set
            {
                if (_currentTemperature != value)
                {
                    _currentTemperature = value;
                    OnPropertyChanged(); // קריאה לפונקציית העדכון (שם המשתנה נשלח אוטומטית)
                }
            }
        }

        public string WeatherCondition
        {
            get => _weatherCondition;
            set
            {
                if (_weatherCondition != value)
                {
                    _weatherCondition = value;
                    OnPropertyChanged(); // קריאה לפונקציית העדכון (שם המשתנה נשלח אוטומטית)
                }
            }
        }
        // 5. פונקציית העדכון שמביאה את הנתונים מהשירות
        public async Task UpdateWeatherAsync()
        {
            var (temp, condition) = await _weatherService.GetCurrentWeatherAsync();
            CurrentTemperature = $"{Math.Round(temp)}°C";
            WeatherCondition = condition;
        }

        // 🔥 6. הפונקציה שהייתה חסרה לך! היא זו שמונעת את שגיאה CS0103
        // המאפיין [CallerMemberName] דואג לשלוח אוטומטית את שם ה-Property ממנו קראו לה
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        public async Task LoadParshaAsync()
        {
            var p = await _parashaSummary.GetWeeklyParshaAsync();
            ParshaName = p.Name;
            ParshaSummary = p.Summary;
        }
        private readonly TehillimService _tehillimSvc = new();
        private string _tehillimText = "", _tehillimChapterTitle = "";
        public string TehillimText { get => _tehillimText; set => Set(ref _tehillimText, value); }
        public string TehillimChapterTitle { get => _tehillimChapterTitle; set => Set(ref _tehillimChapterTitle, value); }


        public async Task RefreshAll()
        {
            // טעינת לוח הלימודים מ-Sefaria (cache יומי)
            await _sefaria.LoadAsync();

            var hc = new HebrewCalendar();
            var now = DateTime.Now;
            int hYear = hc.GetYear(now);
            int hMonth = hc.GetMonth(now);
            int hDay = hc.GetDayOfMonth(now);
            bool leap = hc.IsLeapYear(hYear);

            RefreshClock();

            HebrewDate = $"{HebrewDateFormatter.Day(hDay)} " +
                         $"{HebrewDateFormatter.Month(hMonth, leap)} " +
                         $"{HebrewDateFormatter.Year(hYear)}";
            GregorianDate = now.ToString("dddd, d MMMM yyyy", new CultureInfo("he-IL"));

            
            Parasha = _sefaria.Get("Parashat Hashavua", _parashaService.GetWeeklyParasha(now));
            Haftarah = _sefaria.Get("Haftarah", _jewish.GetHaftarah(Parasha));
            Prayers = _prayerService.GetFormatted();

            var t = _learning.GetTehillim(hDay);
            Tehillim = $"פרקים {HebrewNumber.Range(t.from, t.to)}";
            Chumash = _learning.GetChumashAliya(now);

            // 🆕 כל הלימודים מ-Sefaria
            DafYomi = _sefaria.Get("Daf Yomi", _learning.GetDafYomi(now));
            Rambam = _sefaria.Get("Daily Rambam (3 Chapters)", _jewish.GetRambam3(now));
            Rambam1Perek = _sefaria.Get("Daily Rambam", "");
            Tanya = _sefaria.Get("Tanya Yomi", _learning.GetTanya(now));
            Mishna = _sefaria.Get("Daily Mishnah", "");
            Yerushalmi = _sefaria.Get("Yerushalmi Yomi", "");
            Halakha = _sefaria.Get("Halakhah Yomit", "");
            TanakhYomi = _sefaria.Get("Tanakh Yomi", "");
            Yom929 = _sefaria.Get("929", "");
            ChokLeYisrael = _sefaria.Get("Chok LeYisrael", "");
            ArukhHaShulchan = _sefaria.Get("Arukh HaShulchan Yomi", "");


            HayomYom = _learning.GetHayomYom(now);
            if (_jewish.IsShabbatMevarchim(now, out _, out var monthName))
            {
                IsShabbatMevarchim = true;
                ShabbatMevarchimText = $"השבת מברכים חודש {monthName}";
                var molad = _jewish.GetMolad(now);
                MoladText = $"המולד יהיה ב{molad.Display}";
            }
            else
            {
                IsShabbatMevarchim = false;
                ShabbatMevarchimText = "";
                MoladText = "";
            }

            // משיב הרוח / מוריד הטל + ותן טל ומטר / ברכנו
            int nisanMonth = leap ? 8 : 7;
            bool isGeshem =
                (hMonth == 1 && hDay >= 22) ||
                (hMonth > 1 && hMonth < nisanMonth) ||
                (hMonth == nisanMonth && hDay <= 15);
            bool isTalUmatar =
                (hMonth == 2 && hDay >= 7) ||
                (hMonth > 2 && hMonth < nisanMonth) ||
                (hMonth == nisanMonth && hDay <= 15);

            GeshemText = isGeshem
                ? "אומרים: משיב הרוח ומוריד הגשם"
                : "אומרים: מוריד הטל";
            TalText = isTalUmatar
                ? "אומרים: ותן טל ומטר לברכה"
                : "אומרים: ברכנו / ותן ברכה";

            Zmanim = _zmanim.GetTodayZmanim();

            // הפעלת עדכון מזג האוויר מיד בעליית המסך
            // 1. הפעלת עדכון מזג האוויר מיד בעליית המסך
            _ = UpdateWeatherAsync();

            // 2. הגדרת טיימר של WPF לעדכון אוטומטי פעם בשעה
            System.Windows.Threading.DispatcherTimer weatherTimer = new System.Windows.Threading.DispatcherTimer();
            weatherTimer.Interval = TimeSpan.FromHours(1); // מגדיר הרצה פעם בשעה בדיוק
            weatherTimer.Tick += async (s, e) => await UpdateWeatherAsync();
            weatherTimer.Start();

            await LoadParshaAsync();

            // בתוך RefreshAll(), אחרי קביעת Tehillim:
            int chapterOfDay = ((DateTime.Now.DayOfYear - 1) % 150) + 1;
            TehillimChapterTitle = $"פרק תהילים יומי — פרק {HebrewNumber.Range(chapterOfDay, chapterOfDay)}";
            TehillimText = await _tehillimSvc.GetChapterTextAsync(chapterOfDay);

            try
            {
                var rashi = await _sefaria.GetRashiOnParshaAsync();
                ParshaRashi = rashi ?? "—";
            }
            catch { ParshaRashi = "—"; }

        }
    }
}
