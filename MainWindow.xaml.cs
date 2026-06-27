using BeitKnesetBoard.Services;
using BeitKnesetDisplay.Services;
using BeitKnessetDisplay.Services;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;


namespace BeitKnessetDisplay
{
    public partial class MainWindow : Window
    {
        private readonly DisplayViewModel _vm = new();
        private readonly DispatcherTimer _clockTimer = new();
        private readonly DispatcherTimer _refreshTimer = new();
        private readonly DispatcherTimer _pageTimer = new();
        private async Task LoadYahrzeitAsync()
        {
            try
            {
                var tzaddikim = await YahrzeitService.GetTodayAsync();
                _vm.SetYahrzeit(tzaddikim);
                await _vm.LoadShabbatTimesAsync();

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[Yahrzeit] " + ex.Message);
            }
        }

        private async Task RefreshAllSafeAsync()
        {
            try
            {
                await _vm.RefreshAll();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[RefreshAll] " + ex.Message);
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = _vm;

            // טעינה ראשונית
            _ = RefreshAllSafeAsync();

            // שעון — רק שעה, כל שנייה
            _clockTimer.Interval = TimeSpan.FromSeconds(1);
            _clockTimer.Tick += (_, _) => _vm.RefreshClock();
            _clockTimer.Start();

            // רענון תוכן — לא כל שנייה
            _refreshTimer.Interval = TimeSpan.FromMinutes(30);
            _refreshTimer.Tick += async (_, _) => await RefreshAllSafeAsync();
            _refreshTimer.Start();


            // החלפת עמודים בלולאה
            // החלפת עמודים בלולאה - משך משתנה לפי סוג הדף
            _pageTimer.Interval = TimeSpan.FromSeconds(_vm.CurrentPageDurationSeconds);
            _pageTimer.Tick += (_, _) =>
            {
                _vm.AdvancePage();
                _pageTimer.Interval = TimeSpan.FromSeconds(_vm.CurrentPageDurationSeconds);
            };
            _pageTimer.Start();

            // טעינת ימי הזיכרון פעם ביום
            _ = LoadYahrzeitAsync();


        }
    }
}
