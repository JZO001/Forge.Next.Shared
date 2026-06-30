namespace Forge.Next.Shared;

/// <summary>
/// Fluent extension methods for <see cref="bool"/> that branch on the value of a condition,
/// allowing <see langword="if"/>/<see langword="else"/> logic to be expressed as a single expression.
/// </summary>
public static class BoolExtensions
{

    /// <summary>
    /// Invokes <paramref name="onTrue"/> when <paramref name="condition"/> is <see langword="true"/>;
    /// otherwise invokes <paramref name="onFalse"/> when supplied, or returns the default value of
    /// <typeparamref name="TNextValue"/>.
    /// </summary>
    /// <typeparam name="TNextValue">The type of the value produced by the selected callback.</typeparam>
    /// <param name="condition">The condition to evaluate.</param>
    /// <param name="onTrue">The function invoked when <paramref name="condition"/> is <see langword="true"/>.</param>
    /// <param name="onFalse">The optional function invoked when <paramref name="condition"/> is <see langword="false"/>. When <see langword="null"/>, the default value of <typeparamref name="TNextValue"/> is returned instead.</param>
    /// <returns>The value returned by the invoked callback, or <see langword="default"/> when <paramref name="condition"/> is <see langword="false"/> and no <paramref name="onFalse"/> callback is supplied.</returns>
    public static TNextValue IfTrue<TNextValue>(
        this bool condition,
        Func<TNextValue> onTrue,
        Func<TNextValue>? onFalse = null)
    {
        if (condition)
        {
            return onTrue();
        }
        else if (onFalse is null)
        {
            return default!;
        }
        else
        {
            return onFalse();
        }
    }

    /// <summary>
    /// Invokes <paramref name="onTrue"/> with <paramref name="value"/> when <paramref name="condition"/> is
    /// <see langword="true"/>; otherwise invokes <paramref name="onFalse"/> with <paramref name="value"/> when
    /// supplied, or returns the default value of <typeparamref name="TNextValue"/>.
    /// </summary>
    /// <typeparam name="TValue">The type of the value passed to the selected callback.</typeparam>
    /// <typeparam name="TNextValue">The type of the value produced by the selected callback.</typeparam>
    /// <param name="condition">The condition to evaluate.</param>
    /// <param name="value">The value passed to the invoked callback.</param>
    /// <param name="onTrue">The function invoked with <paramref name="value"/> when <paramref name="condition"/> is <see langword="true"/>.</param>
    /// <param name="onFalse">The optional function invoked with <paramref name="value"/> when <paramref name="condition"/> is <see langword="false"/>. When <see langword="null"/>, the default value of <typeparamref name="TNextValue"/> is returned instead.</param>
    /// <returns>The value returned by the invoked callback, or <see langword="default"/> when <paramref name="condition"/> is <see langword="false"/> and no <paramref name="onFalse"/> callback is supplied.</returns>
    public static TNextValue IfTrue<TValue, TNextValue>(
        this bool condition,
        TValue value,
        Func<TValue, TNextValue> onTrue,
        Func<TValue, TNextValue>? onFalse = null)
    {
        if (condition)
        {
            return onTrue(value);
        }
        else if (onFalse is null)
        {
            return default!;
        }
        else
        {
            return onFalse(value);
        }
    }

    /// <summary>
    /// Asynchronously invokes <paramref name="onTrue"/> when <paramref name="condition"/> is <see langword="true"/>;
    /// otherwise invokes <paramref name="onFalse"/> when supplied, or returns the default value of
    /// <typeparamref name="TNextValue"/>.
    /// </summary>
    /// <typeparam name="TNextValue">The type of the value produced by the selected callback.</typeparam>
    /// <param name="condition">The condition to evaluate.</param>
    /// <param name="onTrue">The asynchronous function invoked when <paramref name="condition"/> is <see langword="true"/>.</param>
    /// <param name="onFalse">The optional asynchronous function invoked when <paramref name="condition"/> is <see langword="false"/>. When <see langword="null"/>, the default value of <typeparamref name="TNextValue"/> is returned instead.</param>
    /// <returns>A task that produces the value returned by the invoked callback, or <see langword="default"/> when <paramref name="condition"/> is <see langword="false"/> and no <paramref name="onFalse"/> callback is supplied.</returns>
    public static async Task<TNextValue> IfTrueAsync<TNextValue>(
        this bool condition,
        Func<Task<TNextValue>> onTrue,
        Func<Task<TNextValue>>? onFalse = null)
    {
        if (condition)
        {
            return await onTrue().ConfigureAwait(false);
        }
        else if (onFalse is null)
        {
            return default!;
        }
        else
        {
            return await onFalse().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Asynchronously invokes <paramref name="onTrue"/> with <paramref name="value"/> when
    /// <paramref name="condition"/> is <see langword="true"/>; otherwise invokes <paramref name="onFalse"/> with
    /// <paramref name="value"/> when supplied, or returns the default value of <typeparamref name="TNextValue"/>.
    /// </summary>
    /// <typeparam name="TValue">The type of the value passed to the selected callback.</typeparam>
    /// <typeparam name="TNextValue">The type of the value produced by the selected callback.</typeparam>
    /// <param name="condition">The condition to evaluate.</param>
    /// <param name="value">The value passed to the invoked callback.</param>
    /// <param name="onTrue">The asynchronous function invoked with <paramref name="value"/> when <paramref name="condition"/> is <see langword="true"/>.</param>
    /// <param name="onFalse">The optional asynchronous function invoked with <paramref name="value"/> when <paramref name="condition"/> is <see langword="false"/>. When <see langword="null"/>, the default value of <typeparamref name="TNextValue"/> is returned instead.</param>
    /// <returns>A task that produces the value returned by the invoked callback, or <see langword="default"/> when <paramref name="condition"/> is <see langword="false"/> and no <paramref name="onFalse"/> callback is supplied.</returns>
    public static async Task<TNextValue> IfTrueAsync<TValue, TNextValue>(
        this bool condition,
        TValue value,
        Func<TValue, Task<TNextValue>> onTrue,
        Func<TValue, Task<TNextValue>>? onFalse = null)
    {
        if (condition)
        {
            return await onTrue(value).ConfigureAwait(false);
        }
        else if (onFalse is null)
        {
            return default!;
        }
        else
        {
            return await onFalse(value).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Executes <paramref name="onTrue"/> when <paramref name="condition"/> is <see langword="true"/>;
    /// otherwise executes <paramref name="onFalse"/> when supplied. No action is taken when
    /// <paramref name="condition"/> is <see langword="false"/> and <paramref name="onFalse"/> is <see langword="null"/>.
    /// </summary>
    /// <param name="condition">The condition to evaluate.</param>
    /// <param name="onTrue">The action executed when <paramref name="condition"/> is <see langword="true"/>.</param>
    /// <param name="onFalse">The optional action executed when <paramref name="condition"/> is <see langword="false"/>.</param>
    /// <returns>The original <paramref name="condition"/>, enabling further chaining.</returns>
    public static bool IfTrueDo(
        this bool condition,
        Action onTrue,
        Action? onFalse = null)
    {
        if (condition)
        {
            onTrue();
        }
        else
        {
            onFalse?.Invoke();
        }

        return condition;
    }

    /// <summary>
    /// Executes <paramref name="onTrue"/> with <paramref name="value"/> when <paramref name="condition"/> is
    /// <see langword="true"/>; otherwise executes <paramref name="onFalse"/> with <paramref name="value"/> when
    /// supplied. No action is taken when <paramref name="condition"/> is <see langword="false"/> and
    /// <paramref name="onFalse"/> is <see langword="null"/>.
    /// </summary>
    /// <typeparam name="TValue">The type of the value passed to the selected action.</typeparam>
    /// <param name="condition">The condition to evaluate.</param>
    /// <param name="value">The value passed to the executed action.</param>
    /// <param name="onTrue">The action executed with <paramref name="value"/> when <paramref name="condition"/> is <see langword="true"/>.</param>
    /// <param name="onFalse">The optional action executed with <paramref name="value"/> when <paramref name="condition"/> is <see langword="false"/>.</param>
    /// <returns>The original <paramref name="condition"/>, enabling further chaining.</returns>
    public static bool IfTrueDo<TValue>(
        this bool condition,
        TValue value,
        Action<TValue> onTrue,
        Action<TValue>? onFalse = null)
    {
        if (condition)
        {
            onTrue(value);
        }
        else
        {
            onFalse?.Invoke(value);
        }

        return condition;
    }

    /// <summary>
    /// Asynchronously executes <paramref name="onTrue"/> when <paramref name="condition"/> is <see langword="true"/>;
    /// otherwise executes <paramref name="onFalse"/> when supplied. No action is taken when
    /// <paramref name="condition"/> is <see langword="false"/> and <paramref name="onFalse"/> is <see langword="null"/>.
    /// </summary>
    /// <param name="condition">The condition to evaluate.</param>
    /// <param name="onTrue">The asynchronous action executed when <paramref name="condition"/> is <see langword="true"/>.</param>
    /// <param name="onFalse">The optional asynchronous action executed when <paramref name="condition"/> is <see langword="false"/>.</param>
    /// <returns>A task that produces the original <paramref name="condition"/>, enabling further chaining.</returns>
    public static async Task<bool> IfTrueDoAsync(
        this bool condition,
        Func<Task> onTrue,
        Func<Task>? onFalse = null)
    {
        if (condition)
        {
            await onTrue().ConfigureAwait(false);
        }
        else if (onFalse is not null)
        {
            await onFalse().ConfigureAwait(false);
        }

        return condition;
    }

    /// <summary>
    /// Asynchronously executes <paramref name="onTrue"/> with <paramref name="value"/> when
    /// <paramref name="condition"/> is <see langword="true"/>; otherwise executes <paramref name="onFalse"/> with
    /// <paramref name="value"/> when supplied. No action is taken when <paramref name="condition"/> is
    /// <see langword="false"/> and <paramref name="onFalse"/> is <see langword="null"/>.
    /// </summary>
    /// <typeparam name="TValue">The type of the value passed to the selected action.</typeparam>
    /// <param name="condition">The condition to evaluate.</param>
    /// <param name="value">The value passed to the executed action.</param>
    /// <param name="onTrue">The asynchronous action executed with <paramref name="value"/> when <paramref name="condition"/> is <see langword="true"/>.</param>
    /// <param name="onFalse">The optional asynchronous action executed with <paramref name="value"/> when <paramref name="condition"/> is <see langword="false"/>.</param>
    /// <returns>A task that produces the original <paramref name="condition"/>, enabling further chaining.</returns>
    public static async Task<bool> IfTrueDoAsync<TValue>(
        this bool condition,
        TValue value,
        Func<TValue, Task> onTrue,
        Func<TValue, Task>? onFalse = null)
    {
        if (condition)
        {
            await onTrue(value).ConfigureAwait(false);
        }
        else if (onFalse is not null)
        {
            await onFalse(value).ConfigureAwait(false);
        }

        return condition;
    }

    /// <summary>
    /// Invokes <paramref name="onFalse"/> when <paramref name="condition"/> is <see langword="false"/>;
    /// otherwise invokes <paramref name="onTrue"/> when supplied, or returns the default value of
    /// <typeparamref name="TNextValue"/>.
    /// </summary>
    /// <typeparam name="TNextValue">The type of the value produced by the selected callback.</typeparam>
    /// <param name="condition">The condition to evaluate.</param>
    /// <param name="onFalse">The function invoked when <paramref name="condition"/> is <see langword="false"/>.</param>
    /// <param name="onTrue">The optional function invoked when <paramref name="condition"/> is <see langword="true"/>. When <see langword="null"/>, the default value of <typeparamref name="TNextValue"/> is returned instead.</param>
    /// <returns>The value returned by the invoked callback, or <see langword="default"/> when <paramref name="condition"/> is <see langword="true"/> and no <paramref name="onTrue"/> callback is supplied.</returns>
    public static TNextValue IfFalse<TNextValue>(
        this bool condition,
        Func<TNextValue> onFalse,
        Func<TNextValue>? onTrue = null)
    {
        if (!condition)
        {
            return onFalse();
        }
        else if (onTrue is null)
        {
            return default!;
        }
        else
        {
            return onTrue();
        }
    }

    /// <summary>
    /// Invokes <paramref name="onFalse"/> with <paramref name="value"/> when <paramref name="condition"/> is
    /// <see langword="false"/>; otherwise invokes <paramref name="onTrue"/> with <paramref name="value"/> when
    /// supplied, or returns the default value of <typeparamref name="TNextValue"/>.
    /// </summary>
    /// <typeparam name="TValue">The type of the value passed to the selected callback.</typeparam>
    /// <typeparam name="TNextValue">The type of the value produced by the selected callback.</typeparam>
    /// <param name="condition">The condition to evaluate.</param>
    /// <param name="value">The value passed to the invoked callback.</param>
    /// <param name="onFalse">The function invoked with <paramref name="value"/> when <paramref name="condition"/> is <see langword="false"/>.</param>
    /// <param name="onTrue">The optional function invoked with <paramref name="value"/> when <paramref name="condition"/> is <see langword="true"/>. When <see langword="null"/>, the default value of <typeparamref name="TNextValue"/> is returned instead.</param>
    /// <returns>The value returned by the invoked callback, or <see langword="default"/> when <paramref name="condition"/> is <see langword="true"/> and no <paramref name="onTrue"/> callback is supplied.</returns>
    public static TNextValue IfFalse<TValue, TNextValue>(
        this bool condition,
        TValue value,
        Func<TValue, TNextValue> onFalse,
        Func<TValue, TNextValue>? onTrue = null)
    {
        if (!condition)
        {
            return onFalse(value);
        }
        else if (onTrue is null)
        {
            return default!;
        }
        else
        {
            return onTrue(value);
        }
    }

    /// <summary>
    /// Asynchronously invokes <paramref name="onFalse"/> when <paramref name="condition"/> is <see langword="false"/>;
    /// otherwise invokes <paramref name="onTrue"/> when supplied, or returns the default value of
    /// <typeparamref name="TNextValue"/>.
    /// </summary>
    /// <typeparam name="TNextValue">The type of the value produced by the selected callback.</typeparam>
    /// <param name="condition">The condition to evaluate.</param>
    /// <param name="onFalse">The asynchronous function invoked when <paramref name="condition"/> is <see langword="false"/>.</param>
    /// <param name="onTrue">The optional asynchronous function invoked when <paramref name="condition"/> is <see langword="true"/>. When <see langword="null"/>, the default value of <typeparamref name="TNextValue"/> is returned instead.</param>
    /// <returns>A task that produces the value returned by the invoked callback, or <see langword="default"/> when <paramref name="condition"/> is <see langword="true"/> and no <paramref name="onTrue"/> callback is supplied.</returns>
    public static async Task<TNextValue> IfFalseAsync<TNextValue>(
        this bool condition,
        Func<Task<TNextValue>> onFalse,
        Func<Task<TNextValue>>? onTrue = null)
    {
        if (!condition)
        {
            return await onFalse().ConfigureAwait(false);
        }
        else if (onTrue is null)
        {
            return default!;
        }
        else
        {
            return await onTrue().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Asynchronously invokes <paramref name="onFalse"/> with <paramref name="value"/> when
    /// <paramref name="condition"/> is <see langword="false"/>; otherwise invokes <paramref name="onTrue"/> with
    /// <paramref name="value"/> when supplied, or returns the default value of <typeparamref name="TNextValue"/>.
    /// </summary>
    /// <typeparam name="TValue">The type of the value passed to the selected callback.</typeparam>
    /// <typeparam name="TNextValue">The type of the value produced by the selected callback.</typeparam>
    /// <param name="condition">The condition to evaluate.</param>
    /// <param name="value">The value passed to the invoked callback.</param>
    /// <param name="onFalse">The asynchronous function invoked with <paramref name="value"/> when <paramref name="condition"/> is <see langword="false"/>.</param>
    /// <param name="onTrue">The optional asynchronous function invoked with <paramref name="value"/> when <paramref name="condition"/> is <see langword="true"/>. When <see langword="null"/>, the default value of <typeparamref name="TNextValue"/> is returned instead.</param>
    /// <returns>A task that produces the value returned by the invoked callback, or <see langword="default"/> when <paramref name="condition"/> is <see langword="true"/> and no <paramref name="onTrue"/> callback is supplied.</returns>
    public static async Task<TNextValue> IfFalseAsync<TValue, TNextValue>(
        this bool condition,
        TValue value,
        Func<TValue, Task<TNextValue>> onFalse,
        Func<TValue, Task<TNextValue>>? onTrue = null)
    {
        if (!condition)
        {
            return await onFalse(value).ConfigureAwait(false);
        }
        else if (onTrue is null)
        {
            return default!;
        }
        else
        {
            return await onTrue(value).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Executes <paramref name="onFalse"/> when <paramref name="condition"/> is <see langword="false"/>;
    /// otherwise executes <paramref name="onTrue"/> when supplied. No action is taken when
    /// <paramref name="condition"/> is <see langword="true"/> and <paramref name="onTrue"/> is <see langword="null"/>.
    /// </summary>
    /// <param name="condition">The condition to evaluate.</param>
    /// <param name="onFalse">The action executed when <paramref name="condition"/> is <see langword="false"/>.</param>
    /// <param name="onTrue">The optional action executed when <paramref name="condition"/> is <see langword="true"/>.</param>
    /// <returns>The original <paramref name="condition"/>, enabling further chaining.</returns>
    public static bool IfFalseDo(
        this bool condition,
        Action onFalse,
        Action? onTrue = null)
    {
        if (!condition)
        {
            onFalse();
        }
        else
        {
            onTrue?.Invoke();
        }

        return condition;
    }

    /// <summary>
    /// Executes <paramref name="onFalse"/> with <paramref name="value"/> when <paramref name="condition"/> is
    /// <see langword="false"/>; otherwise executes <paramref name="onTrue"/> with <paramref name="value"/> when
    /// supplied. No action is taken when <paramref name="condition"/> is <see langword="true"/> and
    /// <paramref name="onTrue"/> is <see langword="null"/>.
    /// </summary>
    /// <typeparam name="TValue">The type of the value passed to the selected action.</typeparam>
    /// <param name="condition">The condition to evaluate.</param>
    /// <param name="value">The value passed to the executed action.</param>
    /// <param name="onFalse">The action executed with <paramref name="value"/> when <paramref name="condition"/> is <see langword="false"/>.</param>
    /// <param name="onTrue">The optional action executed with <paramref name="value"/> when <paramref name="condition"/> is <see langword="true"/>.</param>
    /// <returns>The original <paramref name="condition"/>, enabling further chaining.</returns>
    public static bool IfFalseDo<TValue>(
        this bool condition,
        TValue value,
        Action<TValue> onFalse,
        Action<TValue>? onTrue = null)
    {
        if (!condition)
        {
            onFalse(value);
        }
        else
        {
            onTrue?.Invoke(value);
        }

        return condition;
    }

    /// <summary>
    /// Asynchronously executes <paramref name="onFalse"/> when <paramref name="condition"/> is <see langword="false"/>;
    /// otherwise executes <paramref name="onTrue"/> when supplied. No action is taken when
    /// <paramref name="condition"/> is <see langword="true"/> and <paramref name="onTrue"/> is <see langword="null"/>.
    /// </summary>
    /// <param name="condition">The condition to evaluate.</param>
    /// <param name="onFalse">The asynchronous action executed when <paramref name="condition"/> is <see langword="false"/>.</param>
    /// <param name="onTrue">The optional asynchronous action executed when <paramref name="condition"/> is <see langword="true"/>.</param>
    /// <returns>A task that produces the original <paramref name="condition"/>, enabling further chaining.</returns>
    public static async Task<bool> IfFalseDoAsync(
        this bool condition,
        Func<Task> onFalse,
        Func<Task>? onTrue = null)
    {
        if (!condition)
        {
            await onFalse().ConfigureAwait(false);
        }
        else if (onTrue is not null)
        {
            await onTrue().ConfigureAwait(false);
        }

        return condition;
    }

    /// <summary>
    /// Asynchronously executes <paramref name="onFalse"/> with <paramref name="value"/> when
    /// <paramref name="condition"/> is <see langword="false"/>; otherwise executes <paramref name="onTrue"/> with
    /// <paramref name="value"/> when supplied. No action is taken when <paramref name="condition"/> is
    /// <see langword="true"/> and <paramref name="onTrue"/> is <see langword="null"/>.
    /// </summary>
    /// <typeparam name="TValue">The type of the value passed to the selected action.</typeparam>
    /// <param name="condition">The condition to evaluate.</param>
    /// <param name="value">The value passed to the executed action.</param>
    /// <param name="onFalse">The asynchronous action executed with <paramref name="value"/> when <paramref name="condition"/> is <see langword="false"/>.</param>
    /// <param name="onTrue">The optional asynchronous action executed with <paramref name="value"/> when <paramref name="condition"/> is <see langword="true"/>.</param>
    /// <returns>A task that produces the original <paramref name="condition"/>, enabling further chaining.</returns>
    public static async Task<bool> IfFalseDoAsync<TValue>(
        this bool condition,
        TValue value,
        Func<TValue, Task> onFalse,
        Func<TValue, Task>? onTrue = null)
    {
        if (!condition)
        {
            await onFalse(value).ConfigureAwait(false);
        }
        else if (onTrue is not null)
        {
            await onTrue(value).ConfigureAwait(false);
        }

        return condition;
    }

}
