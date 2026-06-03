# BruteForce Password Cracker

A WPF (.NET 8) application that demonstrates multi-threaded brute-force password recovery.

## Features

| Feature | Detail |
|---|---|
| Password hashing | SHA256 + static salt (`BruteForce_Static_Salt_2024`) |
| Password length | Randomly generated in \[4, 6) characters |
| Character set | a-z, A-Z, 0-9 (62 chars) |
| Search space | All combinations length 1 → 6 |
| Threading | Up to `CPU cores - 1` threads (Thread-based) |
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
