# BruteForce Password Cracker

A WPF (.NET 8) application that demonstrates multi-threaded brute-force password recovery.

## Features

| Feature | Detail |
|---|---|
| Password hashing | SHA256 + static salt (`BruteForce_Static_Salt_2024`) |
| Password length | Randomly generated in \[4, 6) characters |
| Character set | a-z (26 chars) |
| Search space | All combinations length 1 → 6 |
| Threading | Up to `CPU cores - 1` threads (Task / Thread based) |
| GUI | Progress bar, elapsed time, found-password display, start/stop |
| Benchmark | Compares single-thread vs multi-thread execution time |

## Class Structure

| File | Class | Responsibility |
|---|---|---|
| `PasswordManager.cs` | `PasswordManager` | Creates password, hashes with SHA256 + salt |
| `PasswordValidator.cs` | `PasswordValidator` | Validates a candidate against a target hash |
| `BruteForceGenerator.cs` | `BruteForceGenerator` | Generates all combinations (length 1–6) |
| `BruteForceEngine.cs` | `BruteForceEngine` | Coordinates multi-threaded attack |
| `PerformanceLogger.cs` | `PerformanceLogger` | Benchmarks single vs multi-thread |
| `MainWindow.xaml/.cs` | `MainWindow` | WPF GUI, wires all classes together |

## How to Run

```
cd BruteForceApp
dotnet run
```

Or open in Visual Studio and press F5.

## Version History

### v1.0 — Initial implementation
- Project scaffold (WPF .NET 8)
- `PasswordManager`: SHA256 + static salt, random length [4,6)
- `PasswordValidator`: independent hash comparison
- `BruteForceGenerator`: enumerable combinations, length 1 → 6
- `BruteForceEngine`: multi-threaded attack, cancellation token, partitioned work
- `PerformanceLogger`: single-thread and multi-thread benchmarks with speedup ratio
- `MainWindow`: full GUI with progress bar, elapsed timer, start/stop, benchmark button

### v1.1 — Task 1: UML class diagram
- `UML_ClassDiagram.puml` (PlantUML source) and `UML_ClassDiagram.png` (image used in the report)

### v1.2 — Bug fixes & performance
- **Fixed crash on password-found:** `Task.WaitAll(tasks, token)` threw
  `OperationCanceledException` when the engine cancelled its own token; switched to a
  plain `Task.WaitAll(tasks)` and guarded the awaited call.
- **Memory/perf:** replaced per-length candidate materialisation (`.ToList()` of up to
  millions of strings) with **index-range partitioning** — each thread maps a contiguous
  slice of the index space to candidates on the fly via `IndexToCombination`. No large
  allocations, genuinely parallel.
- Alphabet set to lowercase a–z (26) so a length 4–5 password is crackable in seconds.
- Progress bar denominator now grows per length searched, and shows 100% on found.
- Added `screenshots/` of the running application.

### v1.3 — Test report
- `TestReport.docx`: cover page, UML diagram, requirement-by-requirement functionality,
  single vs multi-thread performance, real screenshots, and the challenges faced.

### v1.4 — Final cleanup
- Repository trimmed to the application source and its deliverables (report, UML diagram,
  screenshots). IDE cache, build output, and temporary files are excluded via `.gitignore`.
