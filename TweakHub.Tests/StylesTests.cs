using NUnit.Framework;
using System.Windows;

namespace TweakHub.Tests
{
    [SetUpFixture]
    public class TestAppSetup
    {
        [OneTimeSetUp]
        public void EnsureApplication()
        {
            if (Application.Current == null)
            {
                _ = new Application();
                var uri = new System.Uri("pack://application:,,,/TweakHub;component/Styles/CustomTheme.xaml", System.UriKind.Absolute);
                var dict = new ResourceDictionary { Source = uri };
                var app = Application.Current;
                Assert.That(app, Is.Not.Null);
                app!.Resources.MergedDictionaries.Add(dict);
            }
        }
    }

    public class StylesTests
    {
        [CancelAfter(30000)]
        [Test]
        public void GlobalStyles_AreAvailable()
        {
            var app = Application.Current;
            Assert.That(app, Is.Not.Null);

            Assert.That(app!.TryFindResource("ModernButtonStyle") as Style, Is.Not.Null);
            Assert.That(app.TryFindResource("ExecuteButtonStyle") as Style, Is.Not.Null);
            Assert.That(app.TryFindResource("DangerButtonStyle") as Style, Is.Not.Null);
        }
    }
}
