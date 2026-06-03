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
            _totalCount = 0; // grows as deeper lengths are searched (see RunAttack)

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
            var generator = new BruteForceGenerator(maxLength: 6);

            // Search length by length, starting at 1 (the engine does not know the
            // real password length in advance).
            for (int length = 1; length <= 6; length++)
            {
                if (token.IsCancellationRequested) break;

                // Total candidates of this length = alphabet^length.
                long total = generator.CombinationsForLength(length);

                // The engine doesn't know the real length, so the "total" used for the
                // progress bar grows to include each length as we reach it. This keeps
                // the bar meaningful (e.g. ~80% of the length-4 space) instead of being
                // dwarfed by the full length-6 keyspace from the start.
                _totalCount += total;

                // Split [0, total) into one contiguous index range per thread. Every
                // thread runs simultaneously over its own slice, generating each
                // candidate on the fly — no shared list, no materialisation.
                long chunk = (total + _threadCount - 1) / _threadCount;

                var tasks = new Task[_threadCount];
                for (int t = 0; t < _threadCount; t++)
                {
                    long start = (long)t * chunk;
                    long end = Math.Min(start + chunk, total);
                    tasks[t] = Task.Run(() => SearchRange(generator, validator, length,
                                                          start, end, sw, token), token);
                }

                // Wait WITHOUT passing the token: when the password is found we cancel
                // the token ourselves, and Task.WaitAll(tasks, token) would then throw
                // OperationCanceledException. The threads already exit promptly on
                // cancellation, so a plain WaitAll is what we want.
                try { Task.WaitAll(tasks); }
                catch (AggregateException) { /* tasks honour cancellation by returning */ }
            }

            if (!token.IsCancellationRequested)
                AttackStopped?.Invoke();
        }

        /// <summary>
        /// One thread's work: validate every candidate whose index falls in [start, end).
        /// </summary>
        private void SearchRange(BruteForceGenerator generator, PasswordValidator validator,
                                 int length, long start, long end, Stopwatch sw, CancellationToken token)
        {
            for (long idx = start; idx < end; idx++)
            {
                if (token.IsCancellationRequested) return;

                string candidate = generator.IndexToCombination(idx, length);
                if (validator.IsMatch(candidate))
                {
                    sw.Stop();
                    _cts!.Cancel(); // stop every other thread immediately
                    PasswordFound?.Invoke(candidate, sw.Elapsed);
                    return;
                }

                long done = Interlocked.Increment(ref _checkedCount);
                // Throttle UI updates to avoid flooding the dispatcher.
                if (done % 20000 == 0)
                    ProgressUpdated?.Invoke(done, _totalCount);
            }
        }
    }
}
