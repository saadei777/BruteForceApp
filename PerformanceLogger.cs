using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace BruteForceApp
{
    /// <summary>
    /// Benchmarks and logs the difference in performance between
    /// single-threaded and multi-threaded brute-force execution.
    /// </summary>
    public class PerformanceLogger
    {
        public record BenchmarkResult(string Mode, TimeSpan Elapsed, long CandidatesChecked, string? FoundPassword);

        private readonly List<BenchmarkResult> _results = new();

        /// <summary>
        /// Runs a single-thread brute force against the given hash and records the time.
        /// </summary>
        public BenchmarkResult RunSingleThread(string targetHash)
        {
            var validator = new PasswordValidator(targetHash);
            var generator = new BruteForceGenerator(maxLength: 6);
            long count = 0;
            string? found = null;

            var sw = Stopwatch.StartNew();
            foreach (var candidate in generator.GenerateAll())
            {
                count++;
                if (validator.IsMatch(candidate))
                {
                    found = candidate;
                    break;
                }
            }
            sw.Stop();

            var result = new BenchmarkResult("Single-Thread", sw.Elapsed, count, found);
            _results.Add(result);
            return result;
        }

        /// <summary>
        /// Runs a multi-thread brute force and records the time.
        /// </summary>
        public BenchmarkResult RunMultiThread(string targetHash, int threadCount)
        {
            var validator = new PasswordValidator(targetHash);
            long count = 0;
            string? found = null;
            using var cts = new CancellationTokenSource();

            var gen = new BruteForceGenerator(maxLength: 6);
            var sw = Stopwatch.StartNew();

            for (int length = 1; length <= 6 && !cts.IsCancellationRequested; length++)
            {
                long total = gen.CombinationsForLength(length);
                long chunk = (total + threadCount - 1) / threadCount;

                var threads = new Thread[threadCount];
                for (int t = 0; t < threadCount; t++)
                {
                    long start = (long)t * chunk;
                    long end = Math.Min(start + chunk, total);
                    int len = length;
                    threads[t] = new Thread(() =>
                    {
                        // Each thread scans its own contiguous slice of the index space.
                        for (long idx = start; idx < end; idx++)
                        {
                            if (cts.Token.IsCancellationRequested) return;
                            Interlocked.Increment(ref count);
                            string cand = gen.IndexToCombination(idx, len);
                            if (validator.IsMatch(cand))
                            {
                                found = cand;
                                cts.Cancel();
                                return;
                            }
                        }
                    }) { IsBackground = true };
                    threads[t].Start();
                }

                foreach (var thread in threads)
                    thread.Join();
            }

            sw.Stop();
            var result = new BenchmarkResult($"Multi-Thread ({threadCount} threads)", sw.Elapsed, count, found);
            _results.Add(result);
            return result;
        }

        /// <summary>
        /// Produces a formatted comparison report.
        /// </summary>
        public string BuildReport()
        {
            if (_results.Count < 2)
                return "Not enough benchmark data.";

            var sb = new StringBuilder();
            sb.AppendLine("=== PERFORMANCE COMPARISON ===");
            sb.AppendLine();
            foreach (var r in _results)
            {
                sb.AppendLine($"Mode            : {r.Mode}");
                sb.AppendLine($"Time Elapsed    : {r.Elapsed.TotalSeconds:F3} s");
                sb.AppendLine($"Candidates      : {r.CandidatesChecked:N0}");
                sb.AppendLine($"Password Found  : {r.FoundPassword ?? "not found"}");
                sb.AppendLine();
            }

            if (_results.Count >= 2)
            {
                double single = _results[0].Elapsed.TotalSeconds;
                double multi  = _results[1].Elapsed.TotalSeconds;
                double speedup = single > 0 && multi > 0 ? single / multi : 0;
                sb.AppendLine($"Speedup (single / multi): {speedup:F2}x");
            }

            return sb.ToString();
        }

        public void Clear() => _results.Clear();
    }
}
