using Forge.Next.Shared.UI;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Forge.Next.Shared;

/// <summary>
/// Helpers for raising multicast delegates (events) in a fault-tolerant way.
/// <para>
/// Each subscriber in the delegate's invocation list is invoked independently: a handler that throws does
/// not stop the remaining handlers, and its exception is captured into the result rather than propagated.
/// Invocations can optionally be marshalled onto a WinForms/WPF UI thread (see
/// <see cref="UIExtensions"/>), and an optional <see cref="ILogger"/> traces every invocation.
/// </para>
/// </summary>
public static class Event
{

    /// <summary>
    /// Invokes every handler of a multicast delegate one by one, collecting each handler's return value — or
    /// the exception it threw — into the returned collection. A failing handler never prevents the remaining
    /// handlers from running.
    /// </summary>
    /// <param name="event">
    /// The (multicast) delegate to invoke. When <see langword="null"/>, an empty collection is returned.
    /// </param>
    /// <param name="parameters">The arguments passed to each handler.</param>
    /// <param name="controlInvoke">
    /// When <see langword="true"/>, a handler whose target is a WinForms control or WPF dependency object is
    /// marshalled onto that object's UI thread; otherwise every handler is invoked directly on the calling thread.
    /// </param>
    /// <param name="logger">
    /// An optional logger. When supplied, a debug entry is written before and after each invocation and an
    /// error entry is written when a handler throws.
    /// </param>
    /// <returns>
    /// A read-only collection holding, in handler order, each handler's return value (which may be
    /// <see langword="null"/>) or, when a handler threw, the <see cref="Exception"/> that was caught.
    /// </returns>
    [DebuggerStepThrough]
    public static IReadOnlyCollection<object?> Fire(
        Delegate @event,
        IEnumerable<object?> parameters,
        bool controlInvoke = false,
        ILogger? logger = null)
    {
        if (@event is null) return [];

        List<object?> result = new List<object?>();

        // Each entry in the invocation list is invoked in isolation so one failure cannot abort the rest.
        foreach (Delegate del in @event.GetInvocationList())
        {
            try
            {
                LogBeforeInvoke(logger, del, controlInvoke);
                result.Add(InvokeHandler(del, parameters, controlInvoke));
            }
            catch (Exception e)
            {
                // Capture the failure as a result element instead of throwing it to the caller.
                LogInvokeError(logger, del, controlInvoke, e);
                result.Add(e);
            }
            finally
            {
                LogAfterInvoke(logger, del, controlInvoke);
            }
        }

        return result.AsReadOnly();
    }

    /// <summary>
    /// Invokes a single handler, optionally marshalling the call onto the target's UI thread.
    /// </summary>
    /// <param name="del">The handler to invoke.</param>
    /// <param name="parameters">The arguments passed to the handler.</param>
    /// <param name="controlInvoke">Whether UI targets should be invoked on their UI thread.</param>
    /// <returns>The handler's return value, or <see langword="null"/> for a void handler.</returns>
    private static object? InvokeHandler(Delegate del, IEnumerable<object?> parameters, bool controlInvoke)
    {
        // WinForms control target -> marshal onto its UI thread.
        if (controlInvoke && UIExtensions.IsWinFormsControl(del.Target))
            return UIExtensions.InvokeOnWinFormsControl(del.Target, del, parameters);

        // WPF dependency-object target -> marshal onto its dispatcher thread.
        if (controlInvoke && UIExtensions.IsWPFDependency(del.Target))
            return UIExtensions.InvokeOnWPFDependency(del.Target, del, parameters);

        // Plain (non-UI) handler -> invoke directly via reflection.
        return del.Method.Invoke(del.Target, parameters?.ToArray());
    }

    /// <summary>
    /// Resolves a human-readable name for a handler's target: the instance type's full name when the handler
    /// is an instance method, or the declaring type's full name when it is static.
    /// </summary>
    /// <param name="del">The handler whose target name is requested.</param>
    /// <returns>The resolved full type name, or <see cref="string.Empty"/> when it cannot be determined.</returns>
    private static string GetTargetName(Delegate del)
        => del.Target is null
            ? del.Method.DeclaringType?.FullName ?? string.Empty
            : del.Target.GetType().FullName ?? string.Empty;

    /// <summary>Writes the "before invoke" debug entry when debug logging is enabled.</summary>
    /// <param name="logger">The optional logger.</param>
    /// <param name="del">The handler about to be invoked.</param>
    /// <param name="controlInvoke">Whether the invocation marshals onto a UI thread.</param>
    private static void LogBeforeInvoke(ILogger? logger, Delegate del, bool controlInvoke)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Debug)) return;

        logger.LogDebug(
            "Event, before invoke on '{Target}' with method '{MethodName}'. Invoke on control: {ControlInvoke}",
            GetTargetName(del),
            del.Method.Name,
            controlInvoke.ToString());
    }

    /// <summary>Writes the "after invoke" debug entry when debug logging is enabled.</summary>
    /// <param name="logger">The optional logger.</param>
    /// <param name="del">The handler that was invoked.</param>
    /// <param name="controlInvoke">Whether the invocation marshalled onto a UI thread.</param>
    private static void LogAfterInvoke(ILogger? logger, Delegate del, bool controlInvoke)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Debug)) return;

        logger.LogDebug(
            "Event, after invoke on '{Target}' with method '{MethodName}'. Invoke on control: {ControlInvoke}",
            GetTargetName(del),
            del.Method.Name,
            controlInvoke.ToString());
    }

    /// <summary>Writes the error entry for a failed handler when error logging is enabled.</summary>
    /// <param name="logger">The optional logger.</param>
    /// <param name="del">The handler that threw.</param>
    /// <param name="controlInvoke">Whether the invocation marshalled onto a UI thread.</param>
    /// <param name="e">The exception thrown by the handler.</param>
    private static void LogInvokeError(ILogger? logger, Delegate del, bool controlInvoke, Exception e)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Error)) return;

        logger.LogError(
            e,
            "Event, failed to invoke on '{Target}' with method '{MethodName}'. Invoke on control: {ControlInvoke}, Reason: {Reason}",
            GetTargetName(del),
            del.Method.Name,
            controlInvoke.ToString(),
            e.Message);
    }

}
