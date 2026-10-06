# Playwright POC (.NET / NUnit)

A small end-to-end (E2E) testing proof of concept using [Playwright for .NET](https://playwright.dev/dotnet/) with NUnit. It exists to answer one question concretely: what does a real Playwright test suite look like in a .NET project, from project setup to CI, and what does it actually catch?

> **Note on how this was built:** this project was authored in a sandboxed environment whose network policy blocks `nuget.org`, so `dotnet restore` / `dotnet test` could not be executed there to confirm the suite passes. The code follows the standard Microsoft.Playwright.NUnit APIs and patterns (verified against the official docs), but **you should run it yourself** (see below) before relying on it or writing about it as "verified." If anything doesn't compile as-is, it's most likely a package version pin in `PlaywrightPOC.csproj` that's drifted since this was written — bump it to the latest `Microsoft.Playwright.NUnit` release and it should resolve.

## What's in here

```
playwright-poc/
├── PlaywrightPOC/
│   ├── PlaywrightPOC.csproj        # NUnit + Microsoft.Playwright.NUnit project
│   ├── playwright.runsettings      # browser / headless / worker config
│   └── Tests/
│       ├── TodoPageTests.cs             # 5 tests against the Playwright TodoMVC demo app
│       └── NavigationAndScreenshotTests.cs  # 3 tests against playwright.dev + a screenshot capture
├── blog/                            # blog post drafts (generic + Medium)
├── .github/workflows/playwright.yml  # CI: restore, build, install browsers, run, upload artifacts
└── .gitignore
```

Two test fixtures, eight tests total:

- **`TodoPageTests`** — drives the official [Playwright TodoMVC demo](https://demo.playwright.dev/todomvc/) (the same app Playwright's own docs use to teach testing). Covers adding items, marking complete, filtering (Active/Completed/All), persistence across a page reload, and clearing completed items.
- **`NavigationAndScreenshotTests`** — smoke-tests playwright.dev itself (title, a link click that navigates to the docs) and captures a full-page screenshot to disk as an example of collecting visual evidence from a run.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- Network access to `nuget.org` (for package restore) and to the public internet (the tests navigate to real sites)

## Setup

```bash
cd PlaywrightPOC

# 1. Restore NuGet packages
dotnet restore

# 2. Build once (this also drops a playwright.ps1 / playwright.sh helper script in bin/)
dotnet build

# 3. Install the Playwright browser binaries (Chromium, Firefox, WebKit)
pwsh bin/Debug/net8.0/playwright.ps1 install --with-deps
# no PowerShell? on macOS/Linux you can also run:
#   bin/Debug/net8.0/playwright.sh install --with-deps
```

## Running the tests

```bash
# Run everything (headless Chromium, per playwright.runsettings)
dotnet test --settings playwright.runsettings

# Run against a different browser without editing any file
dotnet test --settings playwright.runsettings -- Playwright.BrowserName=firefox
dotnet test --settings playwright.runsettings -- Playwright.BrowserName=webkit

# Watch it run headed (see the actual browser window)
dotnet test --settings playwright.runsettings -- Playwright.LaunchOptions.Headless=false

# Run a single test
dotnet test --filter "Name=ShouldAddNewTodoItems"
```

Screenshots land in `bin/Debug/net8.0/screenshots/`. Test result files (`.trx`) can be produced with `--logger "trx;LogFileName=test-results.trx"`.

## CI

`.github/workflows/playwright.yml` runs the same suite on every push/PR to `main`: restore → build → install browsers → `dotnet test` → upload test results and screenshots as build artifacts, so a failure comes with evidence attached.

## Why these choices

- **NUnit over MSTest/xUnit** — `Microsoft.Playwright.NUnit`'s `PageTest` base class gives each test its own isolated `Page` (and `Context`/`Browser`) for free, and `[Parallelizable]` lets independent tests run concurrently.
- **TodoMVC demo app** — deliberately boring: no auth, no flaky third-party state, stable selectors. Good for a POC because failures point at the test or Playwright, not at an unrelated moving target.
- **`playwright.runsettings`** — keeps browser/headless/worker config out of test code, so the same suite can target Chromium, Firefox, or WebKit from the command line.
