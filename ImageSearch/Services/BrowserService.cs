using ImageSearchRobot.Models;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using System.Drawing;

namespace ImageSearchRobot.Services;

public class BrowserService : IDisposable
{
    private IWebDriver? _driver;
    private readonly string _tempDir;
    private bool _disposed;

    public BrowserService()
    {
        //временная папка для изображений
        _tempDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp");
        Directory.CreateDirectory(_tempDir);
    }

    public async Task<SearchResult> SearchAsync(string query)
    {
        try
        {
            OpenBrowser();
            OpenGoogleImages();
            PerformSearch(query);

            string screenshotPath = TakeScreenshot();
            string imagePath = await DownloadFirstImageAsync();

            return new SearchResult
            {
                SearchText = query,
                ScreenshotPath = screenshotPath,
                ImagePath = imagePath
            };
        }
        finally
        {
            CloseBrowser();
        }
    }

    private void OpenBrowser()
    {
        //yfcnhjqrf Chrome
        ChromeOptions options = new();
        options.AddArgument("--start-maximized");
        options.AddArgument("--disable-notifications");
        options.AddArgument("--disable-blink-features=AutomationControlled");
        options.AddExcludedArgument("enable-automation");

        ChromeDriverService service = ChromeDriverService.CreateDefaultService();
        service.SuppressInitialDiagnosticInformation = true;
        service.HideCommandPromptWindow = true;

        _driver = new ChromeDriver(service, options);
        _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(2);
    }

    private void OpenGoogleImages()
    {
        _driver!.Navigate().GoToUrl(AppSettings.GoogleImagesUrl);
        WaitForPageLoad();
    }

    private void PerformSearch(string query)
    {
        WebDriverWait wait = CreateWait();

        //находим строку поиска
        IWebElement searchBox = wait.Until(d =>
        {
            try
            {
                IWebElement el = d.FindElement(By.Name("q"));
                return el.Displayed ? el : null!;
            }
            catch { return null!; }
        });

        searchBox.Click();
        searchBox.SendKeys(query);
        searchBox.SendKeys(Keys.Enter);

        //ожидание результата поиска
        wait.Until(d =>
        {
            int count = d.FindElements(By.CssSelector("div[data-id] img, g-img img, h3 + div img")).Count;
            return count > 0;
        });

        Thread.Sleep(TimeSpan.FromSeconds(AppSettings.SearchResultPause));
    }

    private string TakeScreenshot()
    {
        string path = Path.Combine(_tempDir, AppSettings.ScreenshotFileName);

        //снимок окна браузера
        ((ITakesScreenshot)_driver!).GetScreenshot().SaveAsFile(path);

        return path;
    }

    private async Task<string> DownloadFirstImageAsync()
    {
        WebDriverWait wait = CreateWait();


        //берем первую подходящую миниатюру
        IWebElement thumbnail = wait.Until(d =>
        {
            IReadOnlyList<IWebElement> elements = d.FindElements(
                By.CssSelector("a[href*='/imgres'] img, a[jsname='hSRGPd'] img"));

            return elements.FirstOrDefault(x => x.Displayed && x.Size.Width > 100)!;
        });

        ((IJavaScriptExecutor)_driver!)
            .ExecuteScript("arguments[0].scrollIntoView({block:'center'});", thumbnail);

        Thread.Sleep(500);

        //открытие изображения кликом
        try
        {
            thumbnail.Click();
        }
        //при неудаче используем JS
        catch
        {
            ((IJavaScriptExecutor)_driver)
                .ExecuteScript("arguments[0].click();", thumbnail);
        }

        Thread.Sleep(2500);

        string? imageUrl = TryGetLargeImageUrl() ?? thumbnail.GetAttribute("src");

        if (string.IsNullOrWhiteSpace(imageUrl))
            throw new InvalidOperationException("Не удалось получить ссылку на изображение.");

        return await SaveImageAsync(imageUrl);
    }

    private string? TryGetLargeImageUrl()
    {
        //перебираем разные варианты селекторов для боковой панели
        string[] selectors =
        {
            "img.sFlh5c",
            "img.iPVvYb",
            "img.r48jcc",
            "img.n3VNCb",
            "div[jsname] img[src^='http']"
        };

        foreach (string selector in selectors)
        {
            IWebElement? el = _driver!
                .FindElements(By.CssSelector(selector))
                .FirstOrDefault(x =>
                {
                    try
                    {
                        string? src = x.GetAttribute("src");
                        return !string.IsNullOrEmpty(src) && src.StartsWith("http");
                    }
                    catch { return false; }
                });

            if (el != null)
                return el.GetAttribute("src");
        }

        return null;
    }

    private async Task<string> SaveImageAsync(string url)
    {
        string path = Path.Combine(_tempDir, AppSettings.FoundImageFileName);

        //если фото в base64
        if (url.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
        {
            int commaIndex = url.IndexOf(',');
            if (commaIndex > 0)
            {
                byte[] data = Convert.FromBase64String(url[(commaIndex + 1)..]);
                await File.WriteAllBytesAsync(path, data);
                return path;
            }
        }

        //http-ссылка
        using HttpClient http = new();
        http.DefaultRequestHeaders.Add(
            "User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/123.0 Safari/537.36");

        byte[] bytes = await http.GetByteArrayAsync(url);
        await File.WriteAllBytesAsync(path, bytes);

        return path;
    }

    private void CloseBrowser()
    {
        try
        {
            _driver?.Quit();
            _driver?.Dispose();
        }

        catch { }

        finally
        {
            _driver = null;
        }
    }

    private WebDriverWait CreateWait()
    {
        WebDriverWait wait = new(_driver!, TimeSpan.FromSeconds(AppSettings.BrowserWaitSeconds));
        wait.IgnoreExceptionTypes(typeof(NoSuchElementException), typeof(StaleElementReferenceException));
        return wait;
    }

    private void WaitForPageLoad()
    {
        CreateWait().Until(d =>
            ((IJavaScriptExecutor)d)
            .ExecuteScript("return document.readyState")?
            .ToString() == "complete");
    }

    public void Dispose()
    {
        if (_disposed) return;
        CloseBrowser();
        _disposed = true;
    }
}