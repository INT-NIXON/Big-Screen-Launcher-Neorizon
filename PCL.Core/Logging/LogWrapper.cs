using System;
using System.Threading;
using System.Threading.Tasks;

namespace PCL.Core.Logging;

public delegate void LogHandler(LogLevel level, string msg, string? module = null, Exception? ex = null);

public static class LogWrapper
{
    private static Logger? _logger;
    private static bool _initialized;

    public static Logger CurrentLogger => _logger ?? throw new InvalidOperationException("LogWrapper 尚未初始化，请先调用 LogWrapper.Initialize()");

    /// <summary>
    /// 独立初始化日志系统（不依赖 PCL 生命周期系统）。
    /// 在应用程序入口处调用一次即可。
    /// </summary>
    /// <param name="logDirectory">日志文件存放目录</param>
    /// <param name="minLogLevel">最低日志级别，默认 Debug</param>
    public static void Initialize(string logDirectory, LogLevel minLogLevel = LogLevel.Debug)
    {
        if (_initialized) return;
        _initialized = true;

        var config = new LoggerConfiguration(logDirectory, MinLogLevel: minLogLevel);
        _logger = new Logger(config);

        OnLog += (level, msg, module, ex) =>
        {
            var thread = Thread.CurrentThread.Name ?? $"#{Environment.CurrentManagedThreadId}";
            if (module != null) module = $"[{module}] ";
            var result = $"[{DateTime.Now:HH:mm:ss.fff}] [{level.PrintName()}] [{thread}] {module}{msg}";
            _logger.Log((ex == null) ? result : $"{result}\n{ex}");
        };
    }

    /// <summary>
    /// 异步释放日志系统资源，确保所有缓冲日志写入磁盘。
    /// </summary>
    public static async ValueTask ShutdownAsync()
    {
        if (_logger != null)
        {
            await _logger.DisposeAsync();
            _logger = null;
        }
        _initialized = false;
    }

    public static event LogHandler? OnLog;
    
    // Fatal: can handle exceptions
    public static void Fatal(Exception? ex, string? module, string msg) => OnLog?.Invoke(LogLevel.Fatal, msg, module, ex);
    public static void Fatal(Exception? ex, string msg) => Fatal(ex, null, msg);
    public static void Fatal(string? module, string msg) => Fatal(null, module, msg);
    public static void Fatal(string msg) => Fatal((string?)null, msg);
    
    // Error: can handle exceptions
    public static void Error(Exception? ex, string? module, string msg) => OnLog?.Invoke(LogLevel.Error, msg, module, ex);
    public static void Error(Exception? ex, string msg) => Error(ex, null, msg);
    public static void Error(string? module, string msg) => Error(null, module, msg);
    public static void Error(string msg) => Error((string?)null, msg);
    
    // Warn: can handle exceptions
    public static void Warn(Exception? ex, string? module, string msg) => OnLog?.Invoke(LogLevel.Warning, msg, module, ex);
    public static void Warn(Exception? ex, string msg) => Warn(ex, null, msg);
    public static void Warn(string? module, string msg) => Warn(null, module, msg);
    public static void Warn(string msg) => Warn((string?)null, msg);
    
    // Info
    public static void Info(string? module, string msg) => OnLog?.Invoke(LogLevel.Info, msg, module);
    public static void Info(string msg) => Info(null, msg);

    // Debug
    public static void Debug(string? module, string msg) => OnLog?.Invoke(LogLevel.Debug, msg, module);
    public static void Debug(string msg) => Debug(null, msg);

    // Trace
    public static void Trace(string? module, string msg) => OnLog?.Invoke(LogLevel.Trace, msg, module);
    public static void Trace(string msg) => Trace(null, msg);

    private static readonly Lazy<LoggerFactoryAdapter> _LoggerFactory = new(static () =>
    {
        return new LoggerFactoryAdapter(CurrentLogger);
    });
    public static LoggerFactoryAdapter LoggerFactory => _LoggerFactory.Value;
}
