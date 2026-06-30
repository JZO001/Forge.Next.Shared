using System.Reflection;

namespace Forge.Next.Shared.UI;

/// <summary>
/// Helper class for UI reflection operations
/// </summary>
public static class UIExtensions
{

    /// <summary>Determines whether is object win forms control</summary>
    /// <param name="obj">The object.</param>
    /// <returns>
    ///   <c>true</c> if is object win forms control; otherwise, <c>false</c>.</returns>
    public static bool IsWinFormsControl(this object? obj)
    {
        if (obj is null) return false;

        bool result = false;
        Type? type = obj.GetType()!;

        while (!result && type is not null)
        {
            result = Consts.WINDOWS_FORMS_CONTROL.Equals(type.FullName);
            type = type.BaseType;
        }

        return result;
    }

    /// <summary>Invokes the on win forms control.</summary>
    /// <param name="control">The control.</param>
    /// <param name="delegate">The delegate.</param>
    /// <param name="parameters">The parameters.</param>
    /// <returns>
    ///   <br />
    /// </returns>
    public static object? InvokeOnWinFormsControl(this object? control, Delegate @delegate, IEnumerable<object?>? parameters = null)
    {
        if (control is null) return null;

        MethodInfo miInvoke = control.GetType().GetMethod("Invoke", new Type[] { typeof(Delegate), typeof(object[]) })!;
        return miInvoke.Invoke(control, [@delegate, parameters?.ToArray()]);
    }

    /// <summary>Determines whether object WPF dependency</summary>
    /// <param name="obj">The object.</param>
    /// <returns>
    ///   <c>true</c> if object WPF dependency; otherwise, <c>false</c>.</returns>
    public static bool IsWPFDependency(this object? obj)
    {
        if (obj is null) return false;

        bool result = false;

        Type? type = obj.GetType();

        while (!result && type is not null)
        {
            result = Consts.WINDOWS_DEPENDENCY_OBJECT.Equals(type.FullName);
            type = type.BaseType;
        }

        return result;
    }

    /// <summary>Invokes on WPF dependency.</summary>
    /// <param name="control">The control.</param>
    /// <param name="delegate">The delegate</param>
    /// <param name="parameters">The parameters.</param>
    /// <returns>object</returns>
    public static object? InvokeOnWPFDependency(this object? control, Delegate @delegate, IEnumerable<object?>? parameters = null)
    {
        if (control is null) return null;

        MethodInfo miDispatcher = control.GetType().GetProperty("Dispatcher")!.GetGetMethod()!;
        object dispatcherObj = miDispatcher.Invoke(control, null)!;
        MethodInfo miInvoke = dispatcherObj.GetType().GetMethod("Invoke", new Type[] { typeof(Delegate), typeof(object[]) })!;
        return miInvoke.Invoke(dispatcherObj, [@delegate, parameters?.ToArray()]);
    }

}
