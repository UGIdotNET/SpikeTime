// See https://aka.ms/new-console-template for more information
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

using var playwright = await Playwright.CreateAsync();
await using var browser = await playwright.Chromium.LaunchAsync(new()
{
    Headless = false,
    SlowMo = 150
});
var context = await browser.NewContextAsync(/*playwright.Devices["iPhone 13"]*/);

var page = await context.NewPageAsync();
await page.GotoAsync("http://ugidotnet-spiketime-playwrightapp.dev.localhost:5194/");
//await page.GetByRole(AriaRole.Checkbox, new() { Name = "Navigation menu" }).CheckAsync();

var counterLink = page.GetByRole(AriaRole.Link, new() { Name = "Counter" });
await counterLink.ClickAsync();
await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Counter" })).ToBeVisibleAsync();
await page.GetByRole(AriaRole.Button, new() { Name = "Click me" }).ClickAsync();
await Expect(page.GetByRole(AriaRole.Status)).ToContainTextAsync("Current count: 1");

await page.ScreenshotAsync(new()
{
    Path = "counter-screenshot.png",
    Mask = [counterLink],
    MaskColor = "#000000",
    Type = ScreenshotType.Png
});
