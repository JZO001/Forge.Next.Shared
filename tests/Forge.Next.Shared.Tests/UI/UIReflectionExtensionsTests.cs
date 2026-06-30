using Forge.Next.Shared.UI;
using Shouldly;
using System.Windows;
using System.Windows.Forms;
using Xunit;

// -----------------------------------------------------------------------------------------------------
// Fake UI types.
//
// The fake types MUST live in the System.Windows.Forms / System.Windows namespaces (regardless of the
// physical folder), because UIExtensions recognises WinForms / WPF objects purely by comparing
// Type.FullName against string
// constants, and it invokes members purely through reflection. That means we do NOT need to reference the
// real WinForms/WPF assemblies (which are unavailable on this net10.0 test target). Instead we declare
// stand-in types whose *full names* and *member shapes* match what the production code looks for.
// -----------------------------------------------------------------------------------------------------

namespace System.Windows.Forms
{
    /// <summary>
    /// Fake stand-in whose full name is exactly "System.Windows.Forms.Control", matching
    /// <see cref="Forge.Next.Shared.Consts.WINDOWS_FORMS_CONTROL"/> so that
    /// <see cref="Forge.Next.Shared.UI.UIExtensions.IsWinFormsControl(object)"/> treats it as a control.
    /// This MUST stay in the System.Windows.Forms namespace (the FullName is what the detection matches on);
    /// do not let an IDE "sync namespace with folder" refactor move it.
    /// </summary>
    public class Control
    {
        /// <summary>
        /// Records each delegate marshalled through this control's <see cref="Invoke"/>, so tests (notably the
        /// Event tests) can assert that an invocation really was routed onto the (fake) UI thread.
        /// </summary>
        public List<Delegate> InvokedDelegates { get; } = new();

        /// <summary>
        /// Mirrors <c>Control.Invoke(Delegate, object[])</c>. <c>InvokeOnWinFormsControl</c> discovers this
        /// method via reflection and calls it; here we record the call and forward to
        /// <see cref="Delegate.DynamicInvoke"/>. The <paramref name="args"/> array may be <see langword="null"/>
        /// (the helper passes <c>null</c> when no parameters are supplied), in which case the delegate is
        /// invoked with no arguments.
        /// </summary>
        /// <param name="method">The delegate to invoke.</param>
        /// <param name="args">The arguments forwarded to the delegate; may be <see langword="null"/>.</param>
        /// <returns>Whatever the delegate returns.</returns>
        public object Invoke(Delegate method, object[] args)
        {
            InvokedDelegates.Add(method);
            return method.DynamicInvoke(args)!;
        }
    }
}

namespace System.Windows
{
    /// <summary>
    /// Fake stand-in whose full name is exactly "System.Windows.DependencyObject", matching
    /// <see cref="Forge.Next.Shared.Consts.WINDOWS_DEPENDENCY_OBJECT"/> so that
    /// <see cref="UIExtensions.IsWPFDependency(object)"/> treats it as a dependency object.
    /// </summary>
    public class DependencyObject
    {
    }
}

namespace Forge.Next.Shared.Tests
{
    /// <summary>
    /// A subclass of the fake WinForms control, used to verify that the base-type walk inside
    /// <see cref="UIExtensions.IsWinFormsControl(object)"/> climbs the hierarchy to find Control.
    /// </summary>
    public sealed class DerivedWinFormsControl : Control
    {
    }

    /// <summary>
    /// Fake WPF dispatcher exposing an <c>Invoke(Delegate, object[])</c> method, located via reflection by
    /// <see cref="UIExtensions.InvokeOnWPFDependency"/>.
    /// </summary>
    public sealed class FakeDispatcher
    {
        /// <summary>
        /// Records each delegate marshalled through this dispatcher, so tests can assert the WPF dispatch path
        /// was used.
        /// </summary>
        public List<Delegate> InvokedDelegates { get; } = new();

        /// <summary>Forwards the delegate invocation, mirroring WPF's <c>Dispatcher.Invoke</c>.</summary>
        public object Invoke(Delegate method, object[] args)
        {
            InvokedDelegates.Add(method);
            return method.DynamicInvoke(args)!;
        }
    }

    /// <summary>
    /// Fake WPF control exposing a <c>Dispatcher</c> property; <c>InvokeOnWPFDependency</c> reads that
    /// property by name through reflection and then invokes the dispatcher.
    /// </summary>
    public sealed class FakeWpfControl
    {
        /// <summary>The dispatcher reflected on by the production code via the property name "Dispatcher".</summary>
        public FakeDispatcher Dispatcher { get; } = new FakeDispatcher();
    }

    /// <summary>
    /// Unit tests for <see cref="UIExtensions"/>. Because the helper only inspects type names and uses
    /// reflection, the fake types declared above are sufficient to drive every branch.
    ///
    /// Note: the helper is null-tolerant — the <c>IsObject*</c> methods return <see langword="false"/> for a
    /// <see langword="null"/> argument and the <c>InvokeOn*</c> methods return <see langword="null"/>; none of
    /// them throw. The tests assert exactly that.
    /// </summary>
    public class UIReflectionExtensionsTests
    {
        /// <summary>
        /// Tests <see cref="UIExtensions.IsWinFormsControl(object)"/>: non-controls return false,
        /// a control (or a subclass of one) returns true, and null returns false (no exception).
        /// </summary>
        [Fact]
        public void IsObjectWinFormsControlTest()
        {
            // A plain object is not a WinForms control.
            UIExtensions.IsWinFormsControl(new object()).ShouldBeFalse();

            // An instance of the fake System.Windows.Forms.Control matches by full name.
            UIExtensions.IsWinFormsControl(new System.Windows.Forms.Control()).ShouldBeTrue();

            // A subclass matches via the base-type walk.
            UIExtensions.IsWinFormsControl(new DerivedWinFormsControl()).ShouldBeTrue();

            // Null is tolerated -> false (the method no longer throws).
            UIExtensions.IsWinFormsControl(null).ShouldBeFalse();
        }

        /// <summary>
        /// Tests <see cref="UIExtensions.InvokeOnWinFormsControl"/>: it must locate the control's
        /// <c>Invoke(Delegate, object[])</c> method and forward the delegate plus the (optional) parameters,
        /// and it must return <see langword="null"/> for a null control.
        /// </summary>
        [Fact]
        public void InvokeOnWinFormsControlTest()
        {
            Control control = new();
            Func<int, int> increment = x => x + 1;

            // The helper invokes 'increment(41)' through the control's reflected Invoke method.
            object? result = UIExtensions.InvokeOnWinFormsControl(control, increment, new object?[] { 41 });
            result.ShouldBe(42);

            // With no parameters supplied, the delegate is invoked with no arguments (parameters defaults to null).
            Func<int> constant = () => 7;
            UIExtensions.InvokeOnWinFormsControl(control, constant).ShouldBe(7);

            // Null control is tolerated -> null (the method no longer throws).
            UIExtensions.InvokeOnWinFormsControl(null, increment, new object?[] { 1 }).ShouldBeNull();
        }

        /// <summary>
        /// Tests <see cref="UIExtensions.IsWPFDependency(object)"/>: non-dependency objects return
        /// false, a dependency object returns true, and null returns false (no exception).
        /// </summary>
        [Fact]
        public void IsObjectWPFDependencyTest()
        {
            // A plain object is not a WPF dependency object.
            UIExtensions.IsWPFDependency(new object()).ShouldBeFalse();

            // An instance of the fake System.Windows.DependencyObject matches by full name.
            UIExtensions.IsWPFDependency(new DependencyObject()).ShouldBeTrue();

            // Null is tolerated -> false (the method no longer throws).
            UIExtensions.IsWPFDependency(null).ShouldBeFalse();
        }

        /// <summary>
        /// Tests <see cref="UIExtensions.InvokeOnWPFDependency"/>: it must read the <c>Dispatcher</c>
        /// property, then invoke the dispatcher's <c>Invoke(Delegate, object[])</c>, and it must return
        /// <see langword="null"/> for a null control.
        /// </summary>
        [Fact]
        public void InvokeOnWPFDependencyTest()
        {
            FakeWpfControl control = new();
            Func<int, int> increment = x => x + 1;

            // The helper routes 'increment(41)' through control.Dispatcher.Invoke(...).
            object? result = UIExtensions.InvokeOnWPFDependency(control, increment, new object?[] { 41 });
            result.ShouldBe(42);

            // With no parameters supplied, the delegate is invoked with no arguments.
            Func<int> constant = () => 7;
            UIExtensions.InvokeOnWPFDependency(control, constant).ShouldBe(7);

            // Null control is tolerated -> null (the method no longer throws).
            UIExtensions.InvokeOnWPFDependency(null, increment, new object?[] { 1 }).ShouldBeNull();
        }
    }
}
