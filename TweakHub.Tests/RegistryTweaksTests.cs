using System;
using System.Threading;
using NUnit.Framework;
using TweakHub.Views;
using TweakHub.Models;
using System.Windows.Controls;

namespace TweakHub.Tests
{
    [CancelAfter(30000)]
    public class RegistryTweaksTests
    {
        [Test]
        public void CreateCustomTweakCard_CreatesExpectedStructure()
        {
            Exception? ex = null;
            Border? card = null;
            var thr = new Thread(() =>
            {
                try
                {
                    if (System.Windows.Application.Current == null) new System.Windows.Application();
                    var page = new RegistryTweaksPage();
                    var tweak = new CustomRegistryTweak
                    {
                        Name = "MyTestTweak",
                        RegistryPath = "HKCU\\Software\\Test",
                        RegistryKey = "TestValue",
                        ValueType = "REG_SZ",
                        Data = "Hello"
                    };
                    card = InvokeCreateCard(page, tweak);
                }
                catch (Exception e)
                {
                    ex = e;
                }
            });
            thr.SetApartmentState(ApartmentState.STA);
            thr.Start();
            thr.Join();
            if (ex != null) throw ex;
            Assert.That(card, Is.Not.Null);
            Assert.That(card!.Child, Is.TypeOf<StackPanel>());
        }

        private Border InvokeCreateCard(RegistryTweaksPage page, CustomRegistryTweak tweak)
        {
            var mi = typeof(RegistryTweaksPage).GetMethod("CreateCustomTweakCard", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.That(mi, Is.Not.Null);
            var result = mi!.Invoke(page, new object[] { tweak });
            Assert.That(result, Is.Not.Null);
            return (Border)result!;
        }
    }
}
