using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using TaskHub.Abstractions;

namespace ProcessServicePlugin;

public class ProcessServicePlugin : IServicePlugin
{
    private readonly string[] _allowedExecutables;
    private readonly int _defaultTimeoutMilliseconds;
    private readonly int _maxTimeoutMilliseconds;

    public ProcessServicePlugin(IConfiguration? configuration = null)
    {
        _allowedExecutables = configuration?.GetSection("PluginSettings:Process:AllowedExecutables").Get<string[]>()
            ?? Array.Empty<string>();
        _defaultTimeoutMilliseconds = configuration?.GetValue("PluginSettings:Process:DefaultTimeoutMilliseconds", 30000) ?? 30000;
        _maxTimeoutMilliseconds = configuration?.GetValue("PluginSettings:Process:MaxTimeoutMilliseconds", 300000) ?? 300000;
    }

    public IServiceProvider Services { get; private set; } = default!;

    public string Name => "process";

    public void OnLoaded(IServiceProvider services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public object GetService() => new ProcessService(_allowedExecutables, _defaultTimeoutMilliseconds, _maxTimeoutMilliseconds);

    public class ProcessService
    {
        private readonly string[] _allowedExecutables;
        private readonly int _defaultTimeoutMilliseconds;
        private readonly int _maxTimeoutMilliseconds;

        public ProcessService(string[] allowedExecutables, int defaultTimeoutMilliseconds, int maxTimeoutMilliseconds)
        {
            _allowedExecutables = allowedExecutables;
            _defaultTimeoutMilliseconds = defaultTimeoutMilliseconds;
            _maxTimeoutMilliseconds = maxTimeoutMilliseconds;
        }

        public async Task<OperationResult> StartAsync(
            string fileName,
            string? arguments,
            string[]? argumentList,
            string? workingDirectory,
            Dictionary<string, string?>? environment,
            int? timeoutMilliseconds,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return new OperationResult(null, "fileName is required");
            }

            if (!IsExecutableAllowed(fileName))
            {
                return new OperationResult(null, $"Process '{fileName}' is not allowed");
            }

            if (!string.IsNullOrWhiteSpace(workingDirectory) && !Directory.Exists(workingDirectory))
            {
                return new OperationResult(null, $"Working directory '{workingDirectory}' does not exist");
            }

            var timeout = NormalizeTimeout(timeoutMilliseconds);
            var startedAt = DateTimeOffset.UtcNow;
            using var process = new Process
            {
                StartInfo = CreateStartInfo(fileName, arguments, argumentList, workingDirectory, environment)
            };

            try
            {
                if (!process.Start())
                {
                    return new OperationResult(null, $"Failed to start process '{fileName}'");
                }
            }
            catch (Exception ex)
            {
                return new OperationResult(null, $"Failed to start process '{fileName}': {ex.Message}");
            }

            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();
            var timedOut = false;

            using var timeoutCts = new CancellationTokenSource(timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            try
            {
                await process.WaitForExitAsync(linkedCts.Token);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                TryKill(process);
                throw;
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                timedOut = true;
                TryKill(process);
                await process.WaitForExitAsync(CancellationToken.None);
            }

            var stdout = await stdoutTask;
            var stderr = await stderrTask;
            var completedAt = DateTimeOffset.UtcNow;
            int? exitCode = timedOut ? null : process.ExitCode;

            var payload = JsonSerializer.SerializeToElement(new
            {
                fileName,
                arguments,
                argumentList,
                workingDirectory,
                exitCode,
                stdout,
                stderr,
                timedOut,
                startedAt,
                completedAt,
                durationMilliseconds = (long)(completedAt - startedAt).TotalMilliseconds
            });

            var result = timedOut
                ? $"Process timed out after {timeout.TotalMilliseconds:0} ms"
                : exitCode == 0 ? "success" : $"Process exited with code {exitCode}";

            return new OperationResult(payload, result);
        }

        private ProcessStartInfo CreateStartInfo(
            string fileName,
            string? arguments,
            string[]? argumentList,
            string? workingDirectory,
            Dictionary<string, string?>? environment)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = argumentList is null or { Length: 0 } ? arguments ?? string.Empty : string.Empty,
                WorkingDirectory = workingDirectory ?? string.Empty,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            if (argumentList is { Length: > 0 })
            {
                foreach (var argument in argumentList)
                {
                    startInfo.ArgumentList.Add(argument);
                }
            }

            if (environment is not null)
            {
                foreach (var (key, value) in environment)
                {
                    if (value is null)
                    {
                        startInfo.Environment.Remove(key);
                    }
                    else
                    {
                        startInfo.Environment[key] = value;
                    }
                }
            }

            return startInfo;
        }

        private bool IsExecutableAllowed(string fileName)
        {
            if (_allowedExecutables.Length == 0)
            {
                return true;
            }

            var requestedName = Path.GetFileName(fileName);
            var requestedFullPath = TryGetFullPath(fileName);

            return _allowedExecutables.Any(allowed =>
            {
                var allowedName = Path.GetFileName(allowed);
                var allowedFullPath = TryGetFullPath(allowed);
                return string.Equals(allowed, fileName, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(allowedName, requestedName, StringComparison.OrdinalIgnoreCase)
                    || (allowedFullPath is not null
                        && requestedFullPath is not null
                        && string.Equals(allowedFullPath, requestedFullPath, StringComparison.OrdinalIgnoreCase));
            });
        }

        private TimeSpan NormalizeTimeout(int? timeoutMilliseconds)
        {
            var value = timeoutMilliseconds.GetValueOrDefault(_defaultTimeoutMilliseconds);
            if (value <= 0)
            {
                value = _defaultTimeoutMilliseconds;
            }

            value = Math.Min(value, _maxTimeoutMilliseconds);
            return TimeSpan.FromMilliseconds(value);
        }

        private static string? TryGetFullPath(string path)
        {
            try
            {
                return Path.GetFullPath(path);
            }
            catch
            {
                return null;
            }
        }

        private static void TryKill(Process process)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch
            {
            }
        }
    }
}
