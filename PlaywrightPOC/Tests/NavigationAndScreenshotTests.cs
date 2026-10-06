using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using static Microsoft.Playwright.Assertions;

namespace PlaywrightPOC.Tests
{
    /// <summary>
    /// Smoke tests against playwright.dev itself, plus a screenshot test
    /// showing how to capture visual evidence from a run. Kept separate from
    /// TodoPageTests.cs to demonstrate testing more than one site/app in the
    /// same project.
    /// </summary>
    [TestFixture]
    public class NavigationAndScreenshotTests : PageTest
    {
        [Test]
        public async Task HomepageHasPlaywrightTitleAndGetStartedLink()
        {
            await Page.GotoAsync("https://playwright.dev");

            await Expect(Page).ToHaveTitleAsync(new Regex("Playwright"));

            var getStarted = Page.GetByRole(AriaRole.Link, new() { Name = "Get started" });
            await Expect(getStarted).ToBeVisibleAsync();
        }

        [Test]
        public async Task GetStartedLinkNavigatesToInstallationDocs()
        {
            await Page.GotoAsync("https://playwright.dev");
            await Page.GetByRole(AriaRole.Link, new() { Name = "Get started" }).ClickAsync();

            await Expect(Page).ToHaveURLAsync(new Regex(".*intro"));
            await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Installation" })).ToBeVisibleAsync();
        }

        [Test]
        public async Task CapturesFullPageScreenshotAsEvidence()
        {
            await Page.GotoAsync("https://playwright.dev");

            var screenshotDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, "screenshots");
            Directory.CreateDirectory(screenshotDir);
            var screenshotPath = Path.Combine(screenshotDir, "playwright-dev-home.png");

            await Page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = screenshotPath,
                FullPage = true
            });

            Assert.That(File.Exists(screenshotPath), Is.True, "Expected a screenshot file to be written to disk.");
        }
    }
}
