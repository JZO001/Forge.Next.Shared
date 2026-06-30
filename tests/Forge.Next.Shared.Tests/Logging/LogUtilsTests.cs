using Forge.Next.Shared.Logging;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Forge.Next.Shared.Tests.Logging;

/// <summary>
/// Unit tests for <see cref="LogUtils"/>.
///
/// <para>
/// <see cref="LogUtils"/> is a <b>static</b> class with global, process-wide side effects (it subscribes to
/// <see cref="AppDomain"/> events and reads <see cref="System.Diagnostics.Process"/> / <see cref="AppDomain"/>
/// state). The tests therefore:
/// </para>
/// <list type="bullet">
/// <item>configure logging through an NSubstitute <see cref="ILoggerFactory"/> that returns a
/// <see cref="RecordingLogger"/>, so the actual log messages can be asserted (NSubstitute cannot easily verify
/// the generic <c>ILogger.Log&lt;TState&gt;</c> call, because <c>TState</c> is an internal framework type);</item>
/// <item>reset the static state in <see cref="Dispose"/> after every test (xunit creates a fresh instance per
/// test and runs the methods of one class sequentially), and</item>
/// <item>neutralise the non-removable global subscriptions by leaving a disabled logger configured, so any
/// dangling handler can never produce output later in the run.</item>
/// </list>
/// </summary>
public sealed class LogUtilsTests : IDisposable
{
    /// <summary>
    /// A minimal <see cref="ILogger"/> that captures the formatted messages it receives, so the tests can make
    /// concrete assertions about what was logged. <see cref="IsEnabled"/> is driven by a configurable minimum
    /// level (use <see cref="LogLevel.None"/> to model a fully disabled logger).
    /// </summary>
    private sealed class RecordingLogger : ILogger
    {
        // Messages below this level are reported as disabled (and never recorded). LogLevel.None disables all.
        private readonly LogLevel _minLevel;

        public RecordingLogger(LogLevel minLevel = LogLevel.Trace) => _minLevel = minLevel;

        /// <summary>The formatted messages captured so far, in order.</summary>
        public List<string> Entries { get; } = new();

        // Scopes are irrelevant to these tests; return a no-op disposable.
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NoopDisposable.Instance;

        public bool IsEnabled(LogLevel logLevel) => _minLevel != LogLevel.None && logLevel >= _minLevel;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            // Mirror a real logger: respect the enabled-level gate, then record the formatted text.
            if (!IsEnabled(logLevel)) return;
            Entries.Add(formatter(state, exception));
        }

        private sealed class NoopDisposable : IDisposable
        {
            public static readonly NoopDisposable Instance = new();

            public void Dispose()
            {
                // Nothing to dispose.
            }
        }
    }

    /// <summary>
    /// Configures <see cref="LogUtils"/> with a fresh <see cref="RecordingLogger"/> handed out by an
    /// NSubstitute <see cref="ILoggerFactory"/>, and returns that recording logger for assertions.
    /// </summary>
    /// <param name="minLevel">The minimum enabled level of the recording logger (<see cref="LogLevel.None"/> disables it).</param>
    /// <returns>The recording logger now in use by <see cref="LogUtils"/>.</returns>
    private static RecordingLogger ConfigureRecordingLogger(LogLevel minLevel = LogLevel.Trace)
    {
        RecordingLogger logger = new(minLevel);
        ILoggerFactory factory = Substitute.For<ILoggerFactory>();
        factory.CreateLogger(Arg.Any<string>()).Returns(logger);

        LogUtils.ConfigureLogging(factory);
        return logger;
    }

    #region ConfigureLogging

    /// <summary>
    /// Tests <see cref="LogUtils.ConfigureLogging(ILoggerFactory)"/>: a non-null factory is asked for a logger
    /// named "LogUtils" and that logger is then used; a null factory falls back to a no-op logger.
    /// </summary>
    [Fact]
    public void ConfigureLoggingTest()
    {
        // Non-null factory -> LogUtils requests a logger named after the class and uses it.
        RecordingLogger logger = new();
        ILoggerFactory factory = Substitute.For<ILoggerFactory>();
        factory.CreateLogger(Arg.Any<string>()).Returns(logger);

        LogUtils.ConfigureLogging(factory);

        factory.Received(1).CreateLogger("LogUtils");

        LogUtils.LogProcessInfo();
        logger.Entries.ShouldNotBeEmpty();     // the configured logger really is the active one

        // Null factory -> falls back to NullLogger; our previous logger is no longer used.
        LogUtils.ConfigureLogging(null!);
        logger.Entries.Clear();
        LogUtils.LogProcessInfo();
        logger.Entries.ShouldBeEmpty();
    }

    #endregion

    #region Logging methods

    /// <summary>
    /// Tests <see cref="LogUtils.LogProcessInfo"/>: it logs the process block when information logging is
    /// enabled, and logs nothing when it is disabled.
    /// </summary>
    [Fact]
    public void LogProcessInfoTest()
    {
        RecordingLogger enabled = ConfigureRecordingLogger(LogLevel.Information);
        LogUtils.LogProcessInfo();
        enabled.Entries.ShouldContain(e => e.Contains("Current process information"));

        RecordingLogger disabled = ConfigureRecordingLogger(LogLevel.None);
        LogUtils.LogProcessInfo();
        disabled.Entries.ShouldBeEmpty();
    }

    /// <summary>
    /// Tests <see cref="LogUtils.LogDomainInfo"/>: it logs the AppDomain block when enabled, nothing when disabled.
    /// </summary>
    [Fact]
    public void LogDomainInfoTest()
    {
        RecordingLogger enabled = ConfigureRecordingLogger(LogLevel.Information);
        LogUtils.LogDomainInfo();
        enabled.Entries.ShouldContain(e => e.Contains("Domain, Id"));

        RecordingLogger disabled = ConfigureRecordingLogger(LogLevel.None);
        LogUtils.LogDomainInfo();
        disabled.Entries.ShouldBeEmpty();
    }

    /// <summary>
    /// Tests <see cref="LogUtils.LogLoadedAssemblies"/>: it logs the loaded-assemblies block (including assembly
    /// names) when enabled, nothing when disabled.
    /// </summary>
    [Fact]
    public void LogLoadedAssembliesTest()
    {
        RecordingLogger enabled = ConfigureRecordingLogger(LogLevel.Information);
        LogUtils.LogLoadedAssemblies();
        enabled.Entries.ShouldContain(e => e.Contains("Loaded assemblies"));
        enabled.Entries.ShouldContain(e => e.Contains("Assembly, full name"));   // at least one assembly listed

        RecordingLogger disabled = ConfigureRecordingLogger(LogLevel.None);
        LogUtils.LogLoadedAssemblies();
        disabled.Entries.ShouldBeEmpty();
    }

    /// <summary>
    /// Tests <see cref="LogUtils.LogAll"/>: it aggregates the three info blocks and turns on the AppDomain
    /// unhandled-exception subscription.
    /// </summary>
    [Fact]
    public void LogAllTest()
    {
        RecordingLogger logger = ConfigureRecordingLogger(LogLevel.Information);
        try
        {
            LogUtils.LogAll();

            // All three info blocks are present in the aggregated output.
            logger.Entries.ShouldContain(e => e.Contains("Current process information"));
            logger.Entries.ShouldContain(e => e.Contains("Domain, Id"));
            logger.Entries.ShouldContain(e => e.Contains("Loaded assemblies"));

            // LogAll also subscribes to the unhandled-exception event.
            LogUtils.IsSubscribedForAppDomainUnhandledException.ShouldBeTrue();
        }
        finally
        {
            // Undo the global subscriptions LogAll created (unhandled-exception + assembly-load).
            LogUtils.IsSubscribedForAppDomainUnhandledException = false;
            LogUtils.TraceAssemblyLoads(false);
        }
    }

    #endregion

    #region AppDomain subscriptions

    /// <summary>
    /// Tests the <see cref="LogUtils.IsSubscribedForAppDomainUnhandledException"/> property: enabling and
    /// disabling is reflected by the getter and is idempotent (setting the same value twice is a no-op).
    /// </summary>
    [Fact]
    public void IsSubscribedForAppDomainUnhandledExceptionTest()
    {
        try
        {
            // Enable (and enabling again must remain true, not double-subscribe into an inconsistent state).
            LogUtils.IsSubscribedForAppDomainUnhandledException = true;
            LogUtils.IsSubscribedForAppDomainUnhandledException.ShouldBeTrue();

            LogUtils.IsSubscribedForAppDomainUnhandledException = true;
            LogUtils.IsSubscribedForAppDomainUnhandledException.ShouldBeTrue();

            // Disable (and disabling again must remain false).
            LogUtils.IsSubscribedForAppDomainUnhandledException = false;
            LogUtils.IsSubscribedForAppDomainUnhandledException.ShouldBeFalse();

            LogUtils.IsSubscribedForAppDomainUnhandledException = false;
            LogUtils.IsSubscribedForAppDomainUnhandledException.ShouldBeFalse();
        }
        finally
        {
            LogUtils.IsSubscribedForAppDomainUnhandledException = false;
        }
    }

    /// <summary>
    /// Tests <see cref="LogUtils.TraceAssemblyLoads(bool)"/>: enabling subscribes to the AppDomain
    /// <c>AssemblyLoad</c> event and flips <see cref="LogUtils.IsSubscribedForAssemblyLoad"/> to
    /// <see langword="true"/>; disabling unsubscribes and flips it back to <see langword="false"/>.
    /// </summary>
    [Fact]
    public void TraceAssemblyLoadsTest()
    {
        // Use a disabled logger so any AssemblyLoad callbacks during this test produce no output.
        ConfigureRecordingLogger(LogLevel.None);

        // Drive to a known unsubscribed state first (passing false always ends up unsubscribed).
        LogUtils.TraceAssemblyLoads(false);
        LogUtils.IsSubscribedForAssemblyLoad.ShouldBeFalse();

        // Enabling subscribes and sets the flag.
        LogUtils.TraceAssemblyLoads(true);
        LogUtils.IsSubscribedForAssemblyLoad.ShouldBeTrue();

        // Disabling unsubscribes (removing the global handler) and clears the flag.
        LogUtils.TraceAssemblyLoads(false);
        LogUtils.IsSubscribedForAssemblyLoad.ShouldBeFalse();
    }

    #endregion

    /// <summary>
    /// Resets <see cref="LogUtils"/>' static state after each test: removes the unhandled-exception and
    /// assembly-load subscriptions and reverts to the no-op logger.
    /// </summary>
    public void Dispose()
    {
        LogUtils.IsSubscribedForAppDomainUnhandledException = false;
        LogUtils.TraceAssemblyLoads(false);
        LogUtils.ConfigureLogging(null!);
    }
}
