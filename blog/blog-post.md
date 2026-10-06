# Getting Started with Playwright in .NET: An End-to-End Testing POC

Every few years a new browser automation tool promises to fix what came before it — flaky waits, brittle selectors, slow feedback loops. Playwright is the one that's actually delivered on that promise for a lot of teams, and its .NET bindings mean C# developers don't have to reach for a separate JavaScript toolchain just to get browser tests into their pipeline. This post walks through a small proof of concept built with Playwright for .NET and NUnit: what it tests, why it's structured the way it is, and what to look out for if you're setting this up for the first time.

## Why Playwright

A few things make Playwright worth the switch if you're coming from Selenium or an older WebDriver-based setup:

- **Auto-waiting.** Playwright waits for elements to be actionable — visible, stable, enabled — before interacting with them, instead of you sprinkling `Thread.Sleep` or explicit waits through your test code.
- **One API, three engines.** The same test can run against Chromium, Firefox, and WebKit without rewriting anything, which catches the "works in Chrome, breaks in Safari" class of bug before it ships.
- **Built-in tracing and screenshots.** When a test fails, you get a trace, a screenshot, and (optionally) a video — not just a stack trace pointing at a timeout.
- **Fast, isolated contexts.** Each test can spin up its own isolated browser context in milliseconds, so tests don't leak cookies or local storage into each other.

## Why .NET

Playwright's .NET package (`Microsoft.Playwright`) is a first-party binding maintained alongside the JS, Python, and Java versions — not a community wrapper that lags behind. For a team already writing C#, that means E2E tests can live in the same solution, use the same CI pipeline, and follow the same code review process as the rest of the codebase, instead of bolting on a separate Node.js project just for browser tests.

## The POC

The goal of this POC was narrow on purpose: prove out a real, runnable Playwright + NUnit project structure — package setup, test patterns, cross-browser configuration, and CI — against two targets that are stable and don't require any auth or test data setup:

1. The [Playwright TodoMVC demo app](https://demo.playwright.dev/todomvc/) — the same demo app Playwright's own documentation uses to teach E2E testing.
2. [playwright.dev](https://playwright.dev) itself, for a couple of navigation smoke tests and a screenshot example.

### Project setup

```bash
dotnet new nunit -n PlaywrightPOC
cd PlaywrightPOC
dotnet add package Microsoft.Playwright.NUnit
```

`Microsoft.Playwright.NUnit` pulls in `Microsoft.Playwright` and wires up an NUnit base class (`PageTest`) that gives every test its own isolated `Page`, `BrowserContext`, and `Browser` — no manual setup/teardown needed. After the first `dotnet build`, a `playwright.ps1` (or `.sh` on Linux/macOS) script appears in the build output; running it once installs the actual browser binaries:

```bash
pwsh bin/Debug/net8.0/playwright.ps1 install --with-deps
```

That install step is easy to forget and the resulting error message — a generic "executable doesn't exist" — doesn't make the fix obvious if you haven't hit it before.

### Writing the tests

Inheriting from `PageTest` is what makes the tests read almost like plain English:

```csharp
[TestFixture]
public class TodoPageTests : PageTest
{
    [SetUp]
    public async Task SetUp() => await Page.GotoAsync("https://demo.playwright.dev/todomvc/#/");

    [Test]
    public async Task ShouldAddNewTodoItems()
    {
        var newTodo = Page.GetByPlaceholder("What needs to be done?");
        await newTodo.FillAsync("buy some milk");
        await newTodo.PressAsync("Enter");

        await Expect(Page.Locator(".todo-count")).ToHaveTextAsync("1 item left");
    }
}
```

A few things worth calling out for anyone new to Playwright's API:

- **`GetByPlaceholder`, `GetByRole`, etc.** are the recommended way to locate elements — they mirror how a user or a screen reader identifies things on the page, rather than relying on CSS classes or XPath that break the moment a stylesheet changes.
- **`Expect(...)` assertions are web-first.** `await Expect(locator).ToHaveTextAsync(...)` doesn't just check the current DOM state once — it polls with the same auto-waiting behavior as actions, so a test doesn't fail just because a re-render hasn't happened yet.
- **No manual browser lifecycle.** There's no `driver.Quit()` anywhere. `PageTest` handles creating and disposing the browser context around each test.

The full suite in this POC covers eight scenarios across two fixtures: adding todo items, marking one complete, filtering by Active/Completed/All, confirming todos survive a page reload (local storage), clearing completed items, a homepage title check, a link-click navigation test, and a full-page screenshot capture used as an example of collecting visual evidence from a run.

### Running against multiple browsers

Instead of hardcoding a browser in test code, the POC uses a `playwright.runsettings` file:

```xml
<RunSettings>
  <Playwright>
    <BrowserName>chromium</BrowserName>
    <LaunchOptions>
      <Headless>true</Headless>
    </LaunchOptions>
  </Playwright>
</RunSettings>
```

The same suite then runs against a different engine with one flag, no code changes:

```bash
dotnet test --settings playwright.runsettings -- Playwright.BrowserName=firefox
dotnet test --settings playwright.runsettings -- Playwright.BrowserName=webkit
```

That's the payoff of "one API, three engines" in practice — cross-browser coverage becomes a CI matrix parameter instead of three separate test suites to maintain.

### Wiring it into CI

The POC includes a GitHub Actions workflow that restores packages, builds, installs the Playwright browser binaries, runs the suite, and uploads test results and screenshots as build artifacts — so a failing run in CI comes with evidence attached instead of just a red X:

```yaml
- name: Install Playwright browsers
  run: pwsh PlaywrightPOC/bin/Debug/net8.0/playwright.ps1 install --with-deps

- name: Run Playwright tests
  run: dotnet test --no-build --settings PlaywrightPOC/playwright.runsettings --logger "trx;LogFileName=test-results.trx"
```

## What this POC does and doesn't prove

It proves the project structure, the API patterns, and the CI wiring are sound — that's the point of a POC. It doesn't replace a real test suite: production tests would need better test data isolation (this POC deliberately picks apps with no auth or persistent state to worry about), retries for genuinely flaky infrastructure, and probably `Trace.StartAsync`/`Trace.StopAsync` around each test so failures come with a full Playwright trace, not just a screenshot.

## Takeaways

If you're a .NET team evaluating browser test frameworks, the honest pitch for Playwright is that it removes a category of problems — flaky waits, selector churn, cross-browser drift — rather than just moving them around. The setup cost is genuinely small: two NuGet packages, one install command for the browser binaries, and a test base class that handles the plumbing. Everything after that is ordinary C# and NUnit, which is exactly what makes it easy to fold into an existing .NET codebase instead of standing up a parallel toolchain just for E2E tests.
