using System;
using System.Net;
using System.Threading;
using Loupedeck.TimerPlugin.Services;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Testing TimerPlugin Web Server...");

        // Initialize the web configuration service
        var webService = WebConfigurationService.Instance;
        webService.Start();

        // Print available resources for debugging
        var assembly = typeof(Loupedeck.TimerPlugin.TimerPlugin).Assembly;
        var resources = assembly.GetManifestResourceNames();
        Console.WriteLine("Available embedded resources:");
        foreach (var resource in resources)
        {
            Console.WriteLine($"  {resource}");
        }

        Console.WriteLine("\nTesting endpoints...");

        // Test the config endpoint
        using (var client = new WebClient())
        {
            try
            {
                string configUrl = "http://localhost:34521/api/config";
                Console.WriteLine($"Testing {configUrl}");

                // Try to get current config (should return empty or default)
                string response = client.DownloadString(configUrl);
                Console.WriteLine("Config endpoint response:");
                Console.WriteLine(response);

                // Test favicon endpoint
                string faviconUrl = "http://localhost:34521/Icon256x256.png";
                Console.WriteLine($"Testing {faviconUrl}");

                byte[] faviconData = client.DownloadData(faviconUrl);
                Console.WriteLine($"Favicon downloaded: {faviconData.Length} bytes");

                // Test HTML page
                string htmlUrl = "http://localhost:34521/settings-ui.html";
                Console.WriteLine($"Testing {htmlUrl}");

                string htmlContent = client.DownloadString(htmlUrl);
                Console.WriteLine("HTML page loaded successfully");
                Console.WriteLine($"Contains favicon link: {htmlContent.Contains("Icon256x256.png")}");

                Console.WriteLine("✅ All tests passed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test failed: {ex.Message}");
            }
        }

        webService.Stop();
        Console.WriteLine("Web server stopped.");
    }
}
