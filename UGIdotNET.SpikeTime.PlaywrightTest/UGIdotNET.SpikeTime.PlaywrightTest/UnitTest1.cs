using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;
using System.Reflection;
using System.Text.RegularExpressions;
using Xunit.Sdk;

namespace UGIdotNET.SpikeTime.PlaywrightTest;

[WithTestName]
public class UnitTest1 : PageTest
{
    public override async Task InitializeAsync()
    {
        await base.InitializeAsync().ConfigureAwait(false);

        await Context.Tracing.StartAsync(new()
        {
            Title = nameof(UnitTest1),
            Screenshots = true,
            Snapshots = true,
            Sources = true
        });
    }

    public override async Task DisposeAsync()
    {
        await Context.Tracing.StopAsync(new()
        {
            Path = Path.Combine(
                Environment.CurrentDirectory,
                "playwright-traces",
                $"{nameof(UnitTest1)}.zip")
        });

        await base.DisposeAsync().ConfigureAwait(false);
    }

    [Fact]
    public async Task HasTitle()
    {
        await Page.GotoAsync("https://playwright.dev");

        await Expect(Page).ToHaveTitleAsync(new Regex("Playwright"));
    }

    [Fact]
    public async Task GetStartedLink()
    {
        await Page.GotoAsync("https://playwright.dev");

        // Click the get started link.
        var getStartedLink = Page.GetByRole(AriaRole.Link, new() { Name = "Get started" });
        await getStartedLink.ClickAsync();

        // Expects page to have a heading with the name of Installation.
        var pageHeading = Page.GetByRole(AriaRole.Heading, new() { Name = "Installation" });
        await Expect(pageHeading).ToBeVisibleAsync();
    }

    [Fact]
    public async Task MyTest()
    {
        await Page.GotoAsync("http://ugidotnet-spiketime-playwrightapp.dev.localhost:5194/");
        await Page.GetByRole(AriaRole.Link, new() { Name = "Counter" }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Click me" }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Click me" }).ClickAsync();
        await Page.GetByRole(AriaRole.Link, new() { Name = "Weather" }).ClickAsync();
        await Page.GetByRole(AriaRole.Link, new() { Name = "Login" }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Log in", Exact = true }).ClickAsync();
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Email" }).ClickAsync();
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Email" }).FillAsync("alberto@morialberto.it");
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Email" }).PressAsync("Tab");
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Password" }).FillAsync("Password123!");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Log in", Exact = true }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Main)).ToMatchAriaSnapshotAsync("- main:\n  - link \"About\":\n    - /url: https://learn.microsoft.com/aspnet/core/\n  - article:\n    - heading \"Hello, world!\" [level=1]\n    - text: Welcome to your new app.");
    }
}

public class WithTestNameAttribute : BeforeAfterTestAttribute
{
    public static string CurrentTestName = string.Empty;
    public static string CurrentClassName = string.Empty;

    public override void Before(MethodInfo methodInfo)
    {
        CurrentTestName = methodInfo.Name;
        CurrentClassName = methodInfo.DeclaringType!.Name;
    }

    public override void After(MethodInfo methodInfo)
    {
    }
}
