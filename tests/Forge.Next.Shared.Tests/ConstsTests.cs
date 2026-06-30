using Forge.Next.Shared.UI;
using Shouldly;
using Xunit;

namespace Forge.Next.Shared.Tests;

/// <summary>
/// Unit tests for <see cref="Consts"/>.
///
/// <see cref="Consts"/> exposes no methods, only constant strings whose exact values are part of the
/// contract: <see cref="UIExtensions"/> compares reflected type full names against them, so an
/// accidental edit would silently break UI-type detection. These tests pin the values down.
/// </summary>
public class ConstsTests
{
    /// <summary>
    /// Verifies <see cref="Consts.WINDOWS_FORMS_CONTROL"/> equals the full name of the WinForms control type.
    /// </summary>
    [Fact]
    public void WindowsFormsControlTest()
    {
        Consts.WINDOWS_FORMS_CONTROL.ShouldBe("System.Windows.Forms.Control");
    }

    /// <summary>
    /// Verifies <see cref="Consts.WINDOWS_DEPENDENCY_OBJECT"/> equals the full name of the WPF dependency type.
    /// </summary>
    [Fact]
    public void WindowsDependencyObjectTest()
    {
        Consts.WINDOWS_DEPENDENCY_OBJECT.ShouldBe("System.Windows.DependencyObject");
    }
}
