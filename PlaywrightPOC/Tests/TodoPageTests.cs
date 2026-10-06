using System.Threading.Tasks;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using static Microsoft.Playwright.Assertions;

namespace PlaywrightPOC.Tests
{
    /// <summary>
    /// End-to-end tests against the official Playwright TodoMVC demo app
    /// (https://demo.playwright.dev/todomvc/). This is the same demo app
    /// Playwright's own documentation uses to teach E2E testing, which makes
    /// it a good target for a POC: predictable DOM, no auth, no external
    /// dependencies.
    /// </summary>
    [Parallelizable(ParallelScope.Self)]
    [TestFixture]
    public class TodoPageTests : PageTest
    {
        private const string TodoAppUrl = "https://demo.playwright.dev/todomvc/#/";

        private static readonly string[] TodoItems =
        {
            "buy some milk",
            "walk the dog",
            "write a blog post about Playwright"
        };

        [SetUp]
        public async Task SetUp()
        {
            await Page.GotoAsync(TodoAppUrl);
        }

        private async Task AddDefaultTodosAsync()
        {
            var newTodo = Page.GetByPlaceholder("What needs to be done?");
            foreach (var item in TodoItems)
            {
                await newTodo.FillAsync(item);
                await newTodo.PressAsync("Enter");
            }
        }

        [Test]
        public async Task ShouldAddNewTodoItems()
        {
            await AddDefaultTodosAsync();

            var todoTitles = Page.Locator(".todo-list li .view label");
            await Expect(todoTitles).ToHaveTextAsync(TodoItems);
            await Expect(Page.Locator(".todo-count")).ToHaveTextAsync("3 items left");
        }

        [Test]
        public async Task ShouldMarkItemAsComplete()
        {
            await AddDefaultTodosAsync();

            var firstTodo = Page.Locator(".todo-list li").First;
            await firstTodo.Locator(".toggle").CheckAsync();

            await Expect(firstTodo).ToHaveClassAsync("completed");
            await Expect(Page.Locator(".todo-count")).ToHaveTextAsync("2 items left");
        }

        [Test]
        public async Task ShouldFilterActiveAndCompletedItems()
        {
            await AddDefaultTodosAsync();

            await Page.Locator(".todo-list li").Nth(1).Locator(".toggle").CheckAsync();

            await Page.GetByRole(AriaRole.Link, new() { Name = "Active" }).ClickAsync();
            await Expect(Page.Locator(".todo-list li")).ToHaveCountAsync(2);

            await Page.GetByRole(AriaRole.Link, new() { Name = "Completed" }).ClickAsync();
            await Expect(Page.Locator(".todo-list li")).ToHaveCountAsync(1);

            await Page.GetByRole(AriaRole.Link, new() { Name = "All" }).ClickAsync();
            await Expect(Page.Locator(".todo-list li")).ToHaveCountAsync(3);
        }

        [Test]
        public async Task ShouldPersistTodosAfterReload()
        {
            var newTodo = Page.GetByPlaceholder("What needs to be done?");
            await newTodo.FillAsync(TodoItems[0]);
            await newTodo.PressAsync("Enter");

            await Page.ReloadAsync();

            await Expect(Page.Locator(".todo-list li")).ToHaveCountAsync(1);
            await Expect(Page.Locator(".todo-list li .view label")).ToHaveTextAsync(TodoItems[0]);
        }

        [Test]
        public async Task ShouldClearCompletedItems()
        {
            await AddDefaultTodosAsync();

            await Page.Locator(".todo-list li").First.Locator(".toggle").CheckAsync();
            await Page.GetByRole(AriaRole.Button, new() { Name = "Clear completed" }).ClickAsync();

            await Expect(Page.Locator(".todo-list li")).ToHaveCountAsync(2);
        }
    }
}
