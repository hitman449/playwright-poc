# Getting Started with Playwright in .NET

## I built a small end-to-end testing POC with NUnit. Here's how it's put together, and what I'd watch out for.

Browser tests have a reputation for being the flaky, slow part of a pipeline. Playwright is the first tool I've looked at that seriously tries to fix the causes of that, not just the symptoms. And because it has a first-party .NET package, a C# team doesn't need a separate Node.js project just to get browser tests into CI.

So I built a small proof of concept to see what a real Playwright + NUnit setup looks like, from project creation to a CI workflow. This post walks through it.

[ADD: link to the GitHub repo]

## Why Playwright?

A few things stood out compared with older WebDriver-style tools:

- **Auto-waiting.** Playwright waits for an element to be visible, stable and enabled before it acts on it. No `Thread.Sleep`, no hand-rolled wait helpers.
- **One API, three engines.** The same test runs on Chromium, Firefox and WebKit.
- **Isolated contexts.** Every test gets a fresh browser context, so cookies and local storage don't leak between tests.
- **Tracing and screenshots built in.** When something fails you can have more than a stack trace to look at.

## The setup

The goal was deliberately narrow: a real, runnable project structure against targets that are stable and need no login or test data. I picked two:

- The official [Playwright TodoMVC demo](https://demo.playwright.dev/todomvc/), which Playwright's own docs use for teaching.
- playwright.dev itself, for a few navigation smoke tests and a screenshot.

Project creation is three commands:

```
dotnet new nunit -n PlaywrightPOC
cd PlaywrightPOC
dotnet add package Microsoft.Playwright.NUnit
```

`Microsoft.Playwright.NUnit` brings in a `PageTest` base class that gives every test its own isolated `Page`. After the first build, a `playwright.ps1` script appears in the output folder. You run it once to download the browser binaries:

```
pwsh bin/Debug/net8.0/playwright.ps1 install --with-deps
```

It's easy to forget this step, and the error you get if you do ("executable doesn't exist") doesn't make the fix obvious.

## Writing the tests

Inheriting from `PageTest` is what makes the tests read almost like plain English:

```
[TestFixture]
public class TodoPageTests : PageTest
{
    [SetUp]
    public async Task SetUp() =>
        await Page.GotoAsync("https://demo.playwright.dev/todomvc/#/");

    [Test]
    public async Task ShouldAddNewTodoItems()
    {
        var newTodo = Page.GetByPlaceholder("What needs to be done?");
        await newTodo.FillAsync("buy some milk");
        await newTodo.PressAsync("Enter");

        await Expect(Page.Locator(".todo-count"))
            .ToHaveTextAsync("1 item left");
    }
}
```

Three things worth noticing if you're new to the API:

- **Locators like `GetByPlaceholder` and `GetByRole`** describe elements the way a user or screen reader would, instead of leaning on CSS classes that change with the next redesign.
- **`Expect(...)` assertions retry.** They poll until the condition is true or a timeout hits, so a test doesn't fail just because a re-render is a few milliseconds late.
- **There's no browser lifecycle code.** No `driver.Quit()` anywhere. `PageTest` creates and disposes the context around each test.

The full suite is eight tests in two fixtures: adding items, completing one, filtering by Active / Completed / All, checking that todos survive a page reload, clearing completed items, a homepage title check, a link-click navigation check, and a full-page screenshot.

## Running on other browsers

I kept the browser choice out of the test code and put it in a `playwright.runsettings` file:

```
<RunSettings>
  <Playwright>
    <BrowserName>chromium</BrowserName>
    <LaunchOptions>
      <Headless>true</Headless>
    </LaunchOptions>
  </Playwright>
</RunSettings>
```

Switching engines is then a command-line flag:

```
dotnet test --settings playwright.runsettings -- Playwright.BrowserName=firefox
dotnet test --settings playwright.runsettings -- Playwright.BrowserName=webkit
```

That turns cross-browser coverage into a CI matrix setting instead of three suites to maintain.

[ADD: screenshot of your own `dotnet test` output here]

## Wiring it into CI

The repo includes a GitHub Actions workflow that restores, builds, installs the browsers, runs the suite, and uploads test results and screenshots as artifacts, so a red build comes with evidence attached:

```
- name: Install Playwright browsers
  run: pwsh PlaywrightPOC/bin/Debug/net8.0/playwright.ps1 install --with-deps

- name: Run Playwright tests
  run: dotnet test --no-build --settings PlaywrightPOC/playwright.runsettings
```

## What this POC does and doesn't show

It shows the project structure, the API patterns and the CI wiring. It doesn't replace a real suite. Both targets are public demo apps with no login and no persistent state, which is exactly why I chose them, but production tests need proper test-data isolation, a plan for retries, and Playwright traces captured on failure.

## Takeaways

The setup cost is small: two NuGet packages, one browser-install command, and a base class that handles the plumbing. Everything after that is ordinary C# and NUnit, which is what makes it easy to fold into an existing .NET codebase.

If you try it, I'd start with the TodoMVC tests, switch the browser to Firefox and WebKit to see the cross-browser setup work, and only then point it at your own app.
