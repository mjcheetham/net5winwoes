using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Desktop;

namespace testnet5
{
    public static class Program
    {
        private static readonly Uri RedirectUri = new Uri("https://localhost"); // Redirect URI for System WebView flows
        private const string ClientId = "872cd9fa-d31f-45e0-9eab-6e460a02d1f1"; // Visual Studio
        private const string UserReadScope = "user.read"; // MS Graph scope
        private const string WebView2RuntimePath = @"C:\Users\mattche\webview2"; // Path to WebView2 runtime directory
        private const bool UseWam = false;

        public static async Task Main(string[] args)
        {
            string[] scopes = {UserReadScope};

            AuthenticationResult result;

            if (UseWam)
            {
                result = await CreateAppBuilder()
                    .Build()
                    .AcquireTokenInteractive(scopes)
                    .ExecuteAsync();
            }
            else if (HasWebView2(out string runtimePath))
            {
                var webView2Options = new WebView2Options
                {
                    Title = "Hello, World!",
                    BrowserExecutableFolder = runtimePath
                };

                result = await CreateAppBuilder()
                    .Build()
                    .AcquireTokenInteractive(scopes)
                    .WithUseEmbeddedWebView(true)
                    .WithWebView2Options(webView2Options)
                    .ExecuteAsync();
            }
            else if (HasLegacyWebView())
            {
                result = await CreateAppBuilder()
                    .Build()
                    .AcquireTokenInteractive(scopes)
                    .WithUseEmbeddedWebView(true)
                    .ExecuteAsync();
            }
            
            else if (RedirectUri.IsLoopback) // System webview requires loopback redirect
            {
                var systemWebViewOptions = new SystemWebViewOptions
                {
                    HtmlMessageSuccess = "It worked! :)",
                    HtmlMessageError = "It failed! :(",
                };

                result = await CreateAppBuilder()
                    .WithRedirectUri(RedirectUri.ToString())
                    .Build()
                    .AcquireTokenInteractive(scopes)
                    .WithUseEmbeddedWebView(false)
                    .WithSystemWebViewOptions(systemWebViewOptions)
                    .ExecuteAsync();
            }
            else // Fall back to device code flow
            {
                result = await CreateAppBuilder()
                    .Build()
                    .AcquireTokenWithDeviceCode(scopes, WriteDeviceCode)
                    .ExecuteAsync();
            }

            Console.WriteLine(result.AccessToken);
        }

        private static PublicClientApplicationBuilder CreateAppBuilder()
        {
            var builder = PublicClientApplicationBuilder.Create(ClientId);

            if (IsWindows())
            {
                builder = builder.WithDesktopFeatures();

                if (IsWindows10() && UseWam)
                {
                    builder = builder.WithExperimentalFeatures()
                        .WithWindowsBroker();
                    //.WithBroker();
                }
            }

            return builder;
        }

        private static bool HasLegacyWebView()
        {
#if NETFRAMEWORK
            return IsWindows();
#else
            return false;
#endif
        }
        
        private static bool HasWebView2(out string runtimePath)
        {
            runtimePath = WebView2RuntimePath;
            return IsWindows() && Directory.Exists(runtimePath);
        }

        private static bool IsWindows()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        }
        
        private static bool IsWindows10()
        {
            // Must ensure we build with the app.manifest or else the version numbers here lie!
            return IsWindows() && Environment.OSVersion.Version.Major == 10;
        }

        private static Task WriteDeviceCode(DeviceCodeResult dcr)
        {
            Console.Out.WriteLineAsync(dcr.Message);
            return Task.CompletedTask;
        }
    }
}