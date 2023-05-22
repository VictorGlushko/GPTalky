using PuppeteerSharp;
using PuppeteerSharp.Media;
using System.Text;

namespace CodeImageGenerator
{
    public class Generator
    {
        private IBrowser _browser;

        private readonly string _highlightDir = "highlight";

        private readonly string[] _minimaArgs = {
                "--disable-features=IsolateOrigins",
                "--disable-site-isolation-trials",
                "--autoplay-policy=user-gesture-required",
                "--disable-background-networking",
                "--disable-background-timer-throttling",
                "--disable-backgrounding-occluded-windows",
                "--disable-breakpad",
                "--disable-client-side-phishing-detection",
                "--disable-component-update",
                "--disable-default-apps",
                "--disable-dev-shm-usage",
                "--disable-domain-reliability",
                "--disable-extensions",
                "--disable-features=AudioServiceOutOfProcess",
                "--disable-hang-monitor",
                "--disable-ipc-flooding-protection",
                "--disable-notifications",
                "--disable-offer-store-unmasked-wallet-cards",
                "--disable-popup-blocking",
                "--disable-print-preview",
                "--disable-prompt-on-repost",
                "--disable-renderer-backgrounding",
                "--disable-setuid-sandbox",
                "--disable-speech-api",
                "--disable-sync",
                "--hide-scrollbars",
                "--ignore-gpu-blacklist",
                "--metrics-recording-only",
                "--mute-audio",
                "--no-default-browser-check",
                "--no-first-run",
                "--no-pings",
                "--no-sandbox",
                "--no-zygote",
                "--password-store=basic",
                "--use-gl=swiftshader",
                "--use-mock-keychain"
        };

        private const string _startHtml =
                "<link rel=\"stylesheet\" href=\".\\railscasts.min.css\">\r\n<script src=\".\\highlight.min.js\"></script>\r\n<script>hljs.highlightAll();</script>\r\n\r\n<div style=\"display:inline-block;\">\r\n   <pre>\r\n   <code >";
        private const string _endHtml = "    </code>\r\n\t</pre>\r\n</div>\r\n\r\n";


        public Generator()
        {
            Task.Run(() => InitBrowserAsunc()).Wait();
        }

        async Task InitBrowserAsunc()
        {
            await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultChromiumRevision);
            _browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Args = _minimaArgs,
                Headless = true,
                UserDataDir = "/tmp"
            });
        }

        public async Task<(Stream Stream, bool IsSuccess, string Error)> GetCodeImageAsync(string code)
        {

            code = HTMLEncode(code);
            Stream outputStream = new MemoryStream();
            string pathTmpHtml;
            try
            {
                var tmpFileName = Path.GetRandomFileName();
                pathTmpHtml = await CreateTmpPageAsync(tmpFileName, code);
                await using (var page = await _browser.NewPageAsync())
                {
                    await page.GoToAsync(pathTmpHtml, WaitUntilNavigation.DOMContentLoaded);
                    var element = await page.WaitForSelectorAsync("pre code");
                    var box = await element.BoundingBoxAsync();

                    outputStream = await page.ScreenshotStreamAsync(new ScreenshotOptions()
                    {
                        Clip = new Clip()
                        {
                            X = box.X,
                            Y = box.Y,
                            Width = box.Width,
                            Height = box.Height,
                            Scale = 3
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                return (outputStream, false, ex.Message);
            }

            if (File.Exists(pathTmpHtml))
                File.Delete(pathTmpHtml);

            return (outputStream, true, string.Empty);
        }


        public static string HTMLEncode(string input)
        {
            if (input == null)
            {
                return null;
            }

            StringBuilder sb = new StringBuilder();
            char[] chars = input.ToCharArray();

            foreach (char c in chars)
            {
                switch (c)
                {
                    case '<':
                        sb.Append("&lt;");
                        break;

                    case '>':
                        sb.Append("&gt;");
                        break;

                    case '&':
                        sb.Append("&amp;");
                        break;

                    case '\"':
                        sb.Append("&quot;");
                        break;

                    case '\'':
                        sb.Append("&#39;");
                        break;

                    default:
                        sb.Append(c);
                        break;
                }
            }

            return sb.ToString();
        }

        private async Task<string> CreateTmpPageAsync(string tmpFileName, string code)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(_startHtml);
            sb.Append(code);
            sb.Append(_endHtml);

            var filePath = Path.Combine(Environment.CurrentDirectory, _highlightDir, $"{tmpFileName}.html");

            using (var sw = File.CreateText(filePath))
            {
                await sw.WriteAsync(sb.ToString());
            }

            return filePath;
        }
    }
}
