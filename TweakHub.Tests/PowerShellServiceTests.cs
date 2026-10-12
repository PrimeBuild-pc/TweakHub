using NUnit.Framework;
using TweakHub.Services;

namespace TweakHub.Tests;

public class PowerShellServiceTests
{
    [Test]
    public async Task ExecuteScriptAsync_IgnoresPowerShell7ModulePath()
    {
        var original = Environment.GetEnvironmentVariable("PSModulePath");
        try
        {
            Environment.SetEnvironmentVariable("PSModulePath", @"C:\Program Files\PowerShell\7\Modules;" + original);
            var result = await PowerShellService.Instance.ExecuteScriptAsync(
                @"(Get-AuthenticodeSignature -LiteralPath (Join-Path $env:windir 'System32\notepad.exe')).Status");
            Assert.That(result.Output.Trim(), Is.EqualTo("Valid"), result.Error);
        }
        finally
        {
            Environment.SetEnvironmentVariable("PSModulePath", original);
        }
    }

    [Test]
    public async Task ExecuteScriptAsync_PreservesContentAndExitCode()
    {
        var result = await PowerShellService.Instance.ExecuteScriptAsync("Write-Output 'hello \"world\"'\nexit 7");

        Assert.That(result.Output.Trim(), Is.EqualTo("hello \"world\""));
        Assert.That(result.ExitCode, Is.EqualTo(7));
        Assert.That(result.Success, Is.False);
    }

    [Test]
    public async Task ExecuteScriptAsync_StopsAtTimeout()
    {
        var result = await PowerShellService.Instance.ExecuteScriptAsync(
            "Start-Sleep -Seconds 10",
            timeout: TimeSpan.FromMilliseconds(500));

        Assert.That(result.Success, Is.False);
        Assert.That(result.TimedOut, Is.True);
        Assert.That(result.Duration, Is.LessThan(TimeSpan.FromSeconds(5)));
    }
}
