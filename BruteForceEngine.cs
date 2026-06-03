using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BruteForceApp
{
    /// <summary>
    /// Coordinates the multi-threaded brute-force attack.
    /// Splits work across (CPU cores - 1) threads, each validating its own slice.
    /// </summary>
    public class BruteForceEngine
    {
        private readonly int _threadCount;
        private CancellationTokenSource? _cts;

        public event Action<long, long>? ProgressUpdated;   // (checked, total)
        public event Action<string, TimeSpan>? PasswordFound;
        public event Action? AttackStopped;

        // Tracks how many candidates have been checked across all threads
        private long _checkedCount;
        private long _totalCount;

        public int ThreadCount => _threadCount;

        public BruteForceEngine()
        {
            // Use at most (CPU cores - 1), minimum of 1
            _threadCount = Math.Max(1, Environment.ProcessorCount - 1);
        }

        /// <summary>
        /// Starts a multi-threaded brute-force attack.
        /// </summary>
        public Task StartAsync(string targetHash)
        {
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            _checkedCount = 0;

            var generator = new BruteForceGenerator(maxLength: 6);
            _totalCount = generator.TotalCombinations();

            return Task.Run(() => RunAttack(targetHash, token), token);
        }

        /// <summary>
        /// Signals all threads to stop immediately.
        /// </summary>
        public void Stop()
        {
            _cts?.Cancel();
        }

        private void RunAttack(string targetHash, CancellationToken token)
        {
            var sw = Stopwatch.StartNew();
            var validator = new PasswordValidator(targetHash);

            // Process each password length with its own parallel batch
            for (int length = 1; length <= 6; length++)
            {
                if (token.IsCancellationRequested) break;

                var lengthGen = new BruteForceGenerator(maxLength: length);
                // Materialise this length's candidates so we can partition them
                var candidates = lengthGen.GenerateOfLength(length).ToList();

                // Partition candidates into (threadCount) slices
                var slices = Partition(candidates, _threadCount);

                var tasks = slices.Select(slice => Task.Run(() =>
                {
                    foreach (var candidate in slice)
                    {
                        if (token.IsCancellationRequested) return;

                        if (validator.IsMatch(candidate))
                        {
                            sw.Stop();
                            _cts!.Cancel(); // stop all other threads
                            PasswordFound?.Invoke(candidate, sw.Elapsed);
                            return;
                        }

                        long done = Interlocked.Increment(ref _checkedCount);
                        // Report progress every 5000 checks to avoid UI flooding
                        if (done % 5000 == 0)
                            ProgressUpdated?.Invoke(done, _totalCount);
                    }
                }, token)).ToArray();

                Task.WaitAll(tasks, token);
            }

            if (!token.IsCancellationRequested)
                AttackStopped?.Invoke();
        }

        private static List<List<T>> Partition<T>(List<T> source, int parts)
        {
            var result = new List<List<T>>(parts);
            int size = (int)Math.Ceiling(source.Count / (double)parts);
            for (int i = 0; i < parts; i++)
            {
                int start = i * size;
                if (start >= source.Count) break;
                result.Add(source.GetRange(start, Math.Min(size, source.Count - start)));
            }
            return result;
        }
    }
}
