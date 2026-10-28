using System.Text;
using NUnit.Framework;
using TweakHub.Services;

namespace TweakHub.Tests;

public class UsbSpeedServiceTests
{
    [Test]
    public void UsbStringDescriptorsPreserveUnicodeAndRejectMalformedResponses()
    {
        var text = Encoding.Unicode.GetBytes("ASMedia · 控制器");
        var response = new byte[14 + text.Length];
        response[12] = (byte)(2 + text.Length);
        response[13] = 3;
        text.CopyTo(response, 14);
        Assert.Multiple(() =>
        {
            Assert.That(UsbSpeedService.DescriptorData(response, response.Length), Is.EqualTo(text));
            Assert.That(UsbSpeedService.DescriptorData(response, response.Length - 1), Is.Empty, "Truncated descriptor");
            Assert.That(UsbSpeedService.DescriptorData(response, response.Length + 1), Is.Empty);
            Assert.That(UsbSpeedService.DescriptorData([], 0), Is.Empty);
        });
        response[12]--;
        Assert.That(UsbSpeedService.DescriptorData(response, response.Length), Is.Empty, "Odd UTF-16 byte count");
        response[12]++;
        response[13] = 1;
        Assert.That(UsbSpeedService.DescriptorData(response, response.Length), Is.Empty, "Not a string descriptor");
    }
}
