using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace BruteForceApp
{
    public partial class MainWindow : Window
    {
        private readonly PasswordManager _passwordManager = new();
        private readonly PerformanceLogger _logger = new();
        private BruteForceEngine? _engine;
        private readonly DispatcherTimer _elapsedTimer = new();
        private readonly Stopwatch _attackStopwatch = new();

        public MainWindow()
        {
            InitializeComponent();
            _elapsedTimer.Interval = TimeSpan.FromMilliseconds(100);
            _elapsedTimer.Tick += (_, _) =>
                TxtElapsed.Text = $"{_attackStopwatch.Elapsed.TotalSeconds:F3} s";

            // Show thread count on load
            int threads = Math.Max(1, Environment.ProcessorCount - 1);
            TxtThreadCount.Text = threads.ToString();
        }

        private void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            _passwordManager.CreatePassword();
            TxtPlainPassword.Text = _passwordManager.GeneratedPassword;
            TxtHash.Text = _passwordManager.HashedPassword;
            TxtFoundPassword.Text = "—";
            TxtFoundPassword.Foreground = System.Windows.Media.Brushes.LightGray;
            ResetProgress();
            _logger.Clear();
            TxtLog.Clear();

            BtnStart.IsEnabled = true;
            BtnBenchmark.IsEnabled = true;
            Log($"Password generated (length {_passwordManager.GeneratedPassword.Length}). Hash stored. Ready to attack.");
        }

        private async void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            SetAttackingState(true);
            ResetProgress();
            TxtFoundPassword.Text = "Searching…";
            TxtFoundPassword.Foreground = System.Windows.Media.Brushes.Yellow;

            _engine = new BruteForceEngine();
            TxtThreadCount.Text = _engine.ThreadCount.ToString();

            _engine.ProgressUpdated += OnProgressUpdated;
            _engine.PasswordFound += OnPasswordFound;
            _engine.AttackStopped += OnAttackStopped;

            Log($"Multi-thread attack started with {_engine.ThreadCount} thread(s).");
            _attackStopwatch.Restart();
            _elapsedTimer.Start();

            await _engine.StartAsync(_passwordManager.HashedPassword);
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            _engine?.Stop();
            _elapsedTimer.Stop();
            _attackStopwatch.Stop();
            SetAttackingState(false);
            TxtFoundPassword.Text = "Stopped";
            TxtFoundPassword.Foreground = System.Windows.Media.Brushes.OrangeRed;
            Log("Attack stopped by user.");
        }

        private async void BtnBenchmark_Click(object sender, RoutedEventArgs e)
        {
            BtnBenchmark.IsEnabled = false;
            BtnStart.IsEnabled = false;
            BtnGenerate.IsEnabled = false;
            TxtFoundPassword.Text = "Benchmarking…";
            TxtFoundPassword.Foreground = System.Windows.Media.Brushes.Yellow;
            _logger.Clear();
            Log("Running single-thread benchmark…");

            string hash = _passwordManager.HashedPassword;
            int threadCount = Math.Max(1, Environment.ProcessorCount - 1);

            PerformanceLogger.BenchmarkResult single = default!;
            PerformanceLogger.BenchmarkResult multi = default!;

            await Task.Run(() =>
            {
                single = _logger.RunSingleThread(hash);
                Dispatcher.Invoke(() => Log($"Single-thread done in {single.Elapsed.TotalSeconds:F3} s"));
                multi = _logger.RunMultiThread(hash, threadCount);
                Dispatcher.Invoke(() => Log($"Multi-thread done in {multi.Elapsed.TotalSeconds:F3} s"));
            });

            TxtFoundPassword.Text = multi.FoundPassword ?? single.FoundPassword ?? "—";
            TxtFoundPassword.Foreground = System.Windows.Media.Brushes.LightGreen;

            Log(string.Empty);
            Log(_logger.BuildReport());

            BtnBenchmark.IsEnabled = true;
            BtnStart.IsEnabled = true;
            BtnGenerate.IsEnabled = true;
        }

        private void OnProgressUpdated(long checked_, long total)
        {
            Dispatcher.Invoke(() =>
            {
                double pct = total > 0 ? checked_ * 100.0 / total : 0;
                ProgressBar.Value = Math.Min(pct, 100);
                TxtProgress.Text = $"{pct:F1}%";
                TxtChecked.Text = $"{checked_:N0}";
            });
        }

        private void OnPasswordFound(string password, TimeSpan elapsed)
        {
            Dispatcher.Invoke(() =>
            {
                _elapsedTimer.Stop();
                _attackStopwatch.Stop();
                TxtElapsed.Text = $"{elapsed.TotalSeconds:F3} s";
                TxtFoundPassword.Text = password;
                TxtFoundPassword.Foreground = System.Windows.Media.Brushes.LightGreen;
                ProgressBar.Value = 100;
                SetAttackingState(false);
                Log($"PASSWORD FOUND: \"{password}\" in {elapsed.TotalSeconds:F3} s");
            });
        }

        private void OnAttackStopped()
        {
            Dispatcher.Invoke(() =>
            {
                _elapsedTimer.Stop();
                _attackStopwatch.Stop();
                SetAttackingState(false);
                Log("Exhausted all combinations — password not found.");
            });
        }

        private void SetAttackingState(bool attacking)
        {
            BtnStart.IsEnabled = !attacking;
            BtnStop.IsEnabled = attacking;
            BtnGenerate.IsEnabled = !attacking;
            BtnBenchmark.IsEnabled = !attacking;
        }

        private void ResetProgress()
        {
            ProgressBar.Value = 0;
            TxtProgress.Text = "0%";
            TxtChecked.Text = "0";
            TxtElapsed.Text = "0.000 s";
        }

        private void Log(string message)
        {
            TxtLog.AppendText(message + Environment.NewLine);
            LogScroller.ScrollToBottom();
        }
    }
}
