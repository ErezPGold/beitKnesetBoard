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
            _pageTimer.Interval = TimeSpan.FromSeconds(DisplayViewModel.PageDurationSeconds);
            _pageTimer.Tick += (_, _) => _vm.AdvancePage();
            _pageTimer.Start();

            var _learningTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
            _learningTimer.Tick += (_, _) => _vm.AdvanceLearningPage();
            _learningTimer.Start();

        }
    }
}
