using NUnit.Framework;
using System.Windows.Media;
using TweakHub.Services;

namespace TweakHub.Tests;

public class ThemeServiceTests
{
    [Test]
    public void AccentForegroundUsesReadableContrast()
    {
        Assert.That(ThemeService.GetContrastingForeground(Colors.Black), Is.EqualTo(Colors.White));
        Assert.That(ThemeService.GetContrastingForeground(Colors.White), Is.EqualTo(Colors.Black));
        Assert.That(ThemeService.GetContrastingForeground(Color.FromRgb(0, 120, 212)), Is.EqualTo(Colors.White));
        Assert.That(ThemeService.GetContrastingForeground(Color.FromRgb(252, 225, 0)), Is.EqualTo(Colors.Black));
    }
}
