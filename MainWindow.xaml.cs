using BeitKnesetBoard.Services;
using System;
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
                var day = await YahrzeitService.GetTodayAsync();
                Dispatcher.Invoke(() => _vm.SetYahrzeit(day));
            }
            catch { /* fallback to empty */ }
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = _vm;

            _ = _vm.RefreshAll();

            // שעון — מתעדכן כל שנייה
            _clockTimer.Interval = TimeSpan.FromSeconds(1);
            _clockTimer.Tick += async (_, _) => await _vm.RefreshAll();
            _clockTimer.Start();

            // רענון תוכן יומי כל דקה (תאריך/זמנים מתעדכן בחצות)
            _refreshTimer.Interval = TimeSpan.FromMinutes(1);
            _refreshTimer.Tick += async (_, _) => await _vm.RefreshAll();
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
