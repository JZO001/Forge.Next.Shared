using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Forge.Next.Shared.Logging;

/// <summary>
/// Diagnostic logging helpers that dump current process, <see cref="AppDomain"/> and loaded-assembly
/// information through an <see cref="ILogger"/>, and that can optionally subscribe to AppDomain events
/// (assembly loads and unhandled exceptions).
/// <para>
/// All informational output is written at <c>LogLevel.Information</c> (the unhandled-exception trace
/// uses <c>LogLevel.Critical</c>), and every method is a no-op when that level is not enabled. Call
/// <see cref="ConfigureLogging(ILoggerFactory)"/> first; until then a no-op logger is used.
/// </para>
/// </summary>
public static class LogUtils
{

    private static ILogger _logger = NullLogger.Instance;

    private static bool _isSubscribedForAppDomainUnhandledException = false;

    private static bool _isDynamicAvailable = true;

    private static bool _isFullyTrustedAvailable = true;

    /// <summary>
    /// Gets a value indicating whether this helper is currently subscribed to the
    /// <see cref="AppDomain.AssemblyLoad"/> event. Controlled by <see cref="TraceAssemblyLoads(bool)"/>.
    /// </summary>
    /// <value><see langword="true"/> while assembly-load tracing is active; otherwise <see langword="false"/>.</value>
    public static bool IsSubscribedForAssemblyLoad { get; private set; }

    /// <summary>
    /// Gets or sets a value indicating whether this helper is subscribed to the
    /// <see cref="AppDomain.UnhandledException"/> event. Setting it to <see langword="true"/> subscribes a
    /// handler that logs unhandled exceptions at <c>LogLevel.Critical</c>; setting it to
    /// <see langword="false"/> unsubscribes. The operation is idempotent and thread-safe.
    /// </summary>
    /// <value><see langword="true"/> while the unhandled-exception handler is subscribed; otherwise <see langword="false"/>.</value>
    public static bool IsSubscribedForAppDomainUnhandledException
    {
        get
        {
            return _isSubscribedForAppDomainUnhandledException;
        }
        [MethodImpl(MethodImplOptions.Synchronized)]
        set
        {
            if (!_isSubscribedForAppDomainUnhandledException && value)
            {
                _isSubscribedForAppDomainUnhandledException = true;
                AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            }
            else if (!value && _isSubscribedForAppDomainUnhandledException)
            {
                _isSubscribedForAppDomainUnhandledException = false;
                AppDomain.CurrentDomain.UnhandledException -= new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            }
        }
    }

    /// <summary>
    /// Sets the <see cref="ILogger"/> used by every logging method. The logger is created from
    /// <paramref name="loggerFactory"/> with the category name <c>"LogUtils"</c>. When
    /// <paramref name="loggerFactory"/> is <see langword="null"/>, logging falls back to a no-op logger.
    /// </summary>
    /// <param name="loggerFactory">The factory used to create the logger, or <see langword="null"/> to disable logging.</param>
    public static void ConfigureLogging(ILoggerFactory loggerFactory)
    {
        if (loggerFactory is null)
        {
            _logger = NullLogger.Instance;
            return;
        }

        _logger = loggerFactory.CreateLogger(nameof(LogUtils));
    }

    /// <summary>
    /// Starts or stops tracing assembly loads by subscribing to / unsubscribing from the
    /// <see cref="AppDomain.AssemblyLoad"/> event. While active, information about each newly loaded assembly
    /// is logged. Updates <see cref="IsSubscribedForAssemblyLoad"/> accordingly; thread-safe.
    /// </summary>
    /// <param name="state"><see langword="true"/> to start tracing assembly loads; <see langword="false"/> to stop.</param>
    [MethodImpl(MethodImplOptions.Synchronized)]
    public static void TraceAssemblyLoads(bool state)
    {
        if (state && !IsSubscribedForAssemblyLoad)
        {
            AppDomain.CurrentDomain.AssemblyLoad += new AssemblyLoadEventHandler(CurrentDomain_AssemblyLoad);
            IsSubscribedForAssemblyLoad = true;
        }
        else if (IsSubscribedForAssemblyLoad)
        {
            AppDomain.CurrentDomain.AssemblyLoad -= new AssemblyLoadEventHandler(CurrentDomain_AssemblyLoad);
            IsSubscribedForAssemblyLoad = false;
        }
    }

    /// <summary>
    /// Logs information about the current process — id, base priority, name, machine, session, start time and
    /// (when available) start-info details — at <c>LogLevel.Information</c>. Does nothing when
    /// information logging is disabled.
    /// </summary>
    public static void LogProcessInfo()
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            Process p = Process.GetCurrentProcess();

            _logger.LogInformation("LOGUTILS, Current process information:");
            _logger.LogInformation("LOGUTILS, ProcessId: {ProcessId}", p.Id);
            _logger.LogInformation("LOGUTILS, Base priority: {BasePriority}", p.BasePriority);
            _logger.LogInformation("LOGUTILS, Enable raising events: {EnableRaisingEvents}", p.EnableRaisingEvents);
            _logger.LogInformation("LOGUTILS, Machine name: {MachineName}", p.MachineName);
            _logger.LogInformation("LOGUTILS, Process name: {ProcessName}", p.ProcessName);
            _logger.LogInformation("LOGUTILS, SessionId: {SessionId}", p.SessionId);
            _logger.LogInformation("LOGUTILS, Start time: {StartTime}", p.StartTime);

            try
            {
                if (p.StartInfo != null)
                {
                    _logger.LogInformation("LOGUTILS, StartInfo, arguments: {Arguments}", p.StartInfo.Arguments);
                    _logger.LogInformation("LOGUTILS, StartInfo, create no window: {CreateNoWindow}", p.StartInfo.CreateNoWindow);
                    _logger.LogInformation("LOGUTILS, StartInfo, file name: {FileName}", p.StartInfo.FileName);
#if IS_WINDOWS
                    _logger.LogInformation("LOGUTILS, StartInfo, domain: {Domain}", p.StartInfo.Domain);
                    _logger.LogInformation("LOGUTILS, StartInfo, load user profile: {LoadUserProfile}", p.StartInfo.LoadUserProfile);
#endif
                    _logger.LogInformation("LOGUTILS, StartInfo, redirect standard error: {RedirectStandardError}", p.StartInfo.RedirectStandardError);
                    _logger.LogInformation("LOGUTILS, StartInfo, redirect standard input: {RedirectStandardInput}", p.StartInfo.RedirectStandardInput);
                    _logger.LogInformation("LOGUTILS, StartInfo, user name: {UserName}", p.StartInfo.UserName);
                    _logger.LogInformation("LOGUTILS, StartInfo, use shell execute: {UseShellExecute}", p.StartInfo.UseShellExecute);
                    _logger.LogInformation("LOGUTILS, StartInfo, verb: {Verb}", p.StartInfo.Verb);
                    _logger.LogInformation("LOGUTILS, StartInfo, working directory: {WorkingDirectory}", p.StartInfo.WorkingDirectory);
                }
            }
            catch (Exception)
            {
                _logger.LogInformation("LOGUTILS, StartInfo is not available.");
            }

            _logger.LogInformation("--------------------------------------------------------");
        }
    }

    /// <summary>
    /// Logs information about the current <see cref="AppDomain"/> — id, directories, friendly name, trust flags
    /// and (on .NET Framework targets) setup information — at <c>LogLevel.Information</c>. Does nothing
    /// when information logging is disabled.
    /// </summary>
    public static void LogDomainInfo()
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            AppDomain domain = AppDomain.CurrentDomain;

            _logger.LogInformation("LOGUTILS, Domain, Id: {DomainId}", domain.Id);
            _logger.LogInformation("LOGUTILS, Domain, base directory: {BaseDirectory}", domain.BaseDirectory);
            _logger.LogInformation("LOGUTILS, Domain, dynamic directory: {DynamicDirectory}", domain.DynamicDirectory);
            _logger.LogInformation("LOGUTILS, Domain, friendly name: {FriendlyName}", domain.FriendlyName);

            try
            {
                _logger.LogInformation("LOGUTILS, Domain, is fully trusted: {IsFullyTrusted}", domain.IsFullyTrusted);
            }
            catch
            {
                // Do nothing
            }

            try
            {
                _logger.LogInformation("LOGUTILS, Domain, is homogenous: {IsHomogenous}", domain.IsHomogenous);
            }
            catch
            {
                // Do nothing
            }

            _logger.LogInformation("LOGUTILS, Domain, relative search path: {RelativeSearchPath}", domain.RelativeSearchPath);
            _logger.LogInformation("LOGUTILS, Domain, shadow copy files: {ShadowCopyFiles}", domain.ShadowCopyFiles);

            if (domain.SetupInformation != null)
            {
#if NETCOREAPP3_1_OR_GREATER
#else
                try
                {
                    _logger.LogInformation("LOGUTILS, Domain, Setup Information, AppDomainManagerAssembly: {AppDomainManagerAssembly}", domain.SetupInformation.AppDomainManagerAssembly);
                }
                catch
                {
                    // Do nothing
                }
                try
                {
                    _logger.LogInformation("LOGUTILS, Domain, Setup Information, AppDomainManagerType: {AppDomainManagerType}", domain.SetupInformation.AppDomainManagerType);
                }
                catch
                {
                    // Do nothing
                }

                _logger.LogInformation("LOGUTILS, Domain, Setup Information, ApplicationBase: {ApplicationBase}", domain.SetupInformation.ApplicationBase);

                _logger.LogInformation("LOGUTILS, Domain, Setup Information, ApplicationName: {ApplicationName}", domain.SetupInformation.ApplicationName);
                _logger.LogInformation("LOGUTILS, Domain, Setup Information, CachePath: {CachePath}", domain.SetupInformation.CachePath);
                _logger.LogInformation("LOGUTILS, Domain, Setup Information, ConfigurationFile: {ConfigurationFile}", domain.SetupInformation.ConfigurationFile);
                _logger.LogInformation("LOGUTILS, Domain, Setup Information, DisallowApplicationBaseProbing: {DisallowApplicationBaseProbing}", domain.SetupInformation.DisallowApplicationBaseProbing);
                _logger.LogInformation("LOGUTILS, Domain, Setup Information, DisallowBindingRedirects: {DisallowBindingRedirects}", domain.SetupInformation.DisallowBindingRedirects);
                _logger.LogInformation("LOGUTILS, Domain, Setup Information, DisallowCodeDownload: {DisallowCodeDownload}", domain.SetupInformation.DisallowCodeDownload);
                _logger.LogInformation("LOGUTILS, Domain, Setup Information, DisallowPublisherPolicy: {DisallowPublisherPolicy}", domain.SetupInformation.DisallowPublisherPolicy);
                _logger.LogInformation("LOGUTILS, Domain, Setup Information, DynamicBase: {DynamicBase}", domain.SetupInformation.DynamicBase);
                _logger.LogInformation("LOGUTILS, Domain, Setup Information, LicenseFile: {LicenseFile}", domain.SetupInformation.LicenseFile);
                _logger.LogInformation("LOGUTILS, Domain, Setup Information, LoaderOptimization: {LoaderOptimization}", domain.SetupInformation.LoaderOptimization);
                _logger.LogInformation("LOGUTILS, Domain, Setup Information, PrivateBinPath: {PrivateBinPath}", domain.SetupInformation.PrivateBinPath);
                _logger.LogInformation("LOGUTILS, Domain, Setup Information, PrivateBinPathProbe: {PrivateBinPathProbe}", domain.SetupInformation.PrivateBinPathProbe);
                _logger.LogInformation("LOGUTILS, Domain, Setup Information, ShadowCopyDirectories: {ShadowCopyDirectories}", domain.SetupInformation.ShadowCopyDirectories);
                _logger.LogInformation("LOGUTILS, Domain, Setup Information, ShadowCopyFiles: {SetupShadowCopyFiles}", domain.SetupInformation.ShadowCopyFiles);
#endif
            }

            _logger.LogInformation("--------------------------------------------------------");
        }
    }

    /// <summary>
    /// Logs every assembly currently loaded into the <see cref="AppDomain"/> together with its properties
    /// (full name, location, runtime version, ...) at <c>LogLevel.Information</c>. Does nothing when
    /// information logging is disabled.
    /// </summary>
    public static void LogLoadedAssemblies()
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            bool logMark = false;

            _logger.LogInformation("LOGUTILS, Loaded assemblies:");

            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (logMark)
                {
                    _logger.LogInformation("***********");
                }
                else
                {
                    logMark = true;
                }

                _logger.LogInformation("LOGUTILS, Assembly, full name: {FullName}", a.FullName);

#if NETCOREAPP
                try
                {
                    _logger.LogInformation("LOGUTILS, Assembly, code base: {CodeBase}", a.Location);
                }
                catch (Exception)
                {
                    // Do nothing
                }
#else
                try
                {
                    _logger.LogInformation("LOGUTILS, Assembly, code base: {CodeBase}", a.CodeBase);
                }
                catch (Exception)
                {
                    // Do nothing
                }
#endif

#if NETCOREAPP
#else
                _logger.LogInformation("LOGUTILS, Assembly, global assembly cache: {GlobalAssemblyCache}", a.GlobalAssemblyCache);
#endif
                _logger.LogInformation("LOGUTILS, Assembly, host context: {HostContext}", a.HostContext);
                _logger.LogInformation("LOGUTILS, Assembly, ImageRuntimeVersion: {ImageRuntimeVersion}", a.ImageRuntimeVersion);

                LogAssemblyNewProperties(a);

                try
                {
                    _logger.LogInformation("LOGUTILS, Assembly, Location: {Location}", a.Location);
                }
                catch (Exception)
                {
                    // Do nothing
                }

                _logger.LogInformation("LOGUTILS, Assembly, ReflectionOnly: {ReflectionOnly}", a.ReflectionOnly);
            }

            _logger.LogInformation("--------------------------------------------------------");
        }
    }

    /// <summary>
    /// One-call diagnostic dump: logs process, AppDomain and loaded-assembly information, then starts tracing
    /// assembly loads (<see cref="TraceAssemblyLoads(bool)"/>) and subscribes to the unhandled-exception event
    /// (<see cref="IsSubscribedForAppDomainUnhandledException"/>).
    /// </summary>
    public static void LogAll()
    {
        LogProcessInfo();
        LogDomainInfo();
        LogLoadedAssemblies();
        TraceAssemblyLoads(true);
        IsSubscribedForAppDomainUnhandledException = true;
    }

    private static void CurrentDomain_AssemblyLoad(object? sender, AssemblyLoadEventArgs args)
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            Assembly a = args.LoadedAssembly;

            _logger.LogInformation("LOGUTILS, new assembly loaded.");
            _logger.LogInformation("LOGUTILS, Assembly, full name: {FullName}", a.FullName);

            try
            {
#if NETCOREAPP
                _logger.LogInformation("LOGUTILS, Assembly, code base: {CodeBase}", a.Location);
#else
                _logger.LogInformation("LOGUTILS, Assembly, code base: {CodeBase}", a.CodeBase);
#endif
            }
            catch (Exception)
            {
                // Do nothing
            }

#if NETCOREAPP
#else
            _logger.LogInformation("LOGUTILS, Assembly, global assembly cache: {GlobalAssemblyCache}", a.GlobalAssemblyCache);
#endif

            _logger.LogInformation("LOGUTILS, Assembly, host context: {HostContext}", a.HostContext);
            _logger.LogInformation("LOGUTILS, Assembly, ImageRuntimeVersion: {ImageRuntimeVersion}", a.ImageRuntimeVersion);
            LogAssemblyNewProperties(a);
            try
            {
                _logger.LogInformation("LOGUTILS, Assembly, Location: {Location}", a.Location);
            }
            catch (Exception)
            {
                // Do nothing
            }
            _logger.LogInformation("LOGUTILS, Assembly, ReflectionOnly: {ReflectionOnly}", a.ReflectionOnly);
            _logger.LogInformation("--------------------------------------------------------");
        }
    }

    private static void CurrentDomain_UnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        if (_logger.IsEnabled(LogLevel.Critical))
            _logger.LogCritical(
                e.ExceptionObject as Exception,
                "LOGUTILS, unhandled exception detected in the application domain which will {TerminatingPrefix}terminate the current process.",
                e.IsTerminating ? string.Empty : "NOT ");
    }

    private static void LogAssemblyNewProperties(Assembly a)
    {
        // Log assembly properties which are available in newer version of Framework.NET
        if (_isDynamicAvailable)
        {
            try
            {
                if (_logger.IsEnabled(LogLevel.Information)) _logger.LogInformation("LOGUTILS, Assembly, IsDynamic: {IsDynamic}", a.IsDynamic);
            }
            catch (MissingFieldException)
            {
                _isDynamicAvailable = false;

            }
            catch (MissingMemberException)
            {
                _isDynamicAvailable = false;
            }
            catch (Exception)
            {
                // Do nothing
            }
        }
        if (_isFullyTrustedAvailable)
        {
            try
            {
                if (_logger.IsEnabled(LogLevel.Information)) _logger.LogInformation("LOGUTILS, Assembly, IsFullyTrusted: {IsFullyTrusted}", a.IsFullyTrusted);
            }
            catch (MissingFieldException)
            {
                _isFullyTrustedAvailable = false;
            }
            catch (MissingMemberException)
            {
                _isFullyTrustedAvailable = false;
            }
            catch (Exception)
            {
                // Do nothing
            }
        }
    }

}
