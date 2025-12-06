namespace Loupedeck.TimerPlugin.Services
{
    using System;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Loupedeck.TimerPlugin.Models;

    public class WebConfigurationService : IDisposable
    {
        private static WebConfigurationService _instance;
        private HttpListener _listener;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _serverTask;
        private readonly int _port;
        private bool _disposed;

        private WebConfigurationService()
        {
            _port = FindAvailablePort(34521);
        }

        public static WebConfigurationService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new WebConfigurationService();
                }
                return _instance;
            }
        }

        public void Start()
        {
            if (_listener != null)
            {
                return;
            }

            try
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add($"http://localhost:{_port}/");
                _listener.Start();

                _cancellationTokenSource = new CancellationTokenSource();
                _serverTask = Task.Run(() => HandleRequests(_cancellationTokenSource.Token));

                PluginLog.Info($"Web configuration server started on http://localhost:{_port}");
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Failed to start web configuration server");
            }
        }

        public void Stop()
        {
            try
            {
                _cancellationTokenSource?.Cancel();
                _listener?.Stop();
                _serverTask?.Wait(TimeSpan.FromSeconds(5));
                _listener?.Close();
                _listener = null;
                PluginLog.Info("Web configuration server stopped");
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Error stopping web configuration server");
            }
        }

        private async Task HandleRequests(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && _listener.IsListening)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    _ = Task.Run(() => ProcessRequest(context), cancellationToken);
                }
                catch (HttpListenerException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    PluginLog.Error(ex, "Error handling HTTP request");
                }
            }
        }

        private void ProcessRequest(HttpListenerContext context)
        {
            try
            {
                var request = context.Request;
                var response = context.Response;

                PluginLog.Info($"HTTP {request.HttpMethod} {request.Url.PathAndQuery}");

                // Enable CORS
                response.Headers.Add("Access-Control-Allow-Origin", "*");
                response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE");
                response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

                if (request.HttpMethod == "OPTIONS")
                {
                    response.StatusCode = 200;
                    response.Close();
                    return;
                }

                var path = request.Url.AbsolutePath;

                if (path == "/api/config" && request.HttpMethod == "GET")
                {
                    HandleGetConfig(response);
                }
                else if (path == "/api/config" && request.HttpMethod == "POST")
                {
                    HandleSaveConfig(request, response);
                }
                else if (path.StartsWith("/"))
                {
                    HandleStaticFile(path, response);
                }
                else
                {
                    response.StatusCode = 404;
                    response.Close();
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Error processing request");
                try
                {
                    context.Response.StatusCode = 500;
                    context.Response.Close();
                }
                catch { }
            }
        }

        private void HandleGetConfig(HttpListenerResponse response)
        {
            try
            {
                var config = TimerConfigurationService.Instance.GetConfiguration();
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = null, // Use exact property names
                    WriteIndented = true
                };
                var json = JsonSerializer.Serialize(config, options);

                PluginLog.Info($"Returning config: {json}");

                var buffer = Encoding.UTF8.GetBytes(json);
                response.ContentType = "application/json";
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.StatusCode = 200;
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Error in HandleGetConfig");
                response.StatusCode = 500;
            }
            finally
            {
                response.Close();
            }
        }

        private void HandleSaveConfig(HttpListenerRequest request, HttpListenerResponse response)
        {
            try
            {
                using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                {
                    var json = reader.ReadToEnd();
                    PluginLog.Info($"Received config to save: {json}");

                    var options = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = null // Use exact property names
                    };
                    var config = JsonSerializer.Deserialize<TimerConfiguration>(json, options);
                    TimerConfigurationService.Instance.UpdateConfiguration(config);

                    response.StatusCode = 200;
                    PluginLog.Info("Configuration saved successfully");
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Error in HandleSaveConfig");
                response.StatusCode = 500;
            }
            finally
            {
                response.Close();
            }
        }

        private void HandleStaticFile(string path, HttpListenerResponse response)
        {
            // Map URL path to resource
            var resourceName = path.TrimStart('/');
            if (string.IsNullOrEmpty(resourceName) || resourceName == "/")
            {
                resourceName = "settings-ui.html";
            }

            try
            {
                var assembly = typeof(TimerPlugin).Assembly;

                // List all resources for debugging
                var resourceNames = assembly.GetManifestResourceNames();
                PluginLog.Info($"Looking for resource: {resourceName}");
                PluginLog.Info($"Available resources: {string.Join(", ", resourceNames)}");

                // Try to find the resource with various naming patterns
                Stream stream = null;

                // Try direct name
                stream = assembly.GetManifestResourceStream(resourceName);

                // Try with namespace prefix
                if (stream == null)
                {
                    var withNamespace = $"Loupedeck.TimerPlugin.{resourceName}";
                    stream = assembly.GetManifestResourceStream(withNamespace);
                }

                // Try finding a resource that ends with the file name
                if (stream == null)
                {
                    foreach (var name in resourceNames)
                    {
                        if (name.EndsWith(resourceName, StringComparison.OrdinalIgnoreCase))
                        {
                            stream = assembly.GetManifestResourceStream(name);
                            PluginLog.Info($"Found resource with name: {name}");
                            break;
                        }
                    }
                }

                if (stream == null)
                {
                    PluginLog.Warning($"Resource not found: {resourceName}");
                    response.StatusCode = 404;
                    response.Close();
                    return;
                }

                using (stream)
                {
                    // Set content type
                    var extension = Path.GetExtension(resourceName).ToLower();
                    response.ContentType = extension switch
                    {
                        ".html" => "text/html",
                        ".css" => "text/css",
                        ".js" => "application/javascript",
                        ".svg" => "image/svg+xml",
                        ".png" => "image/png",
                        ".jpg" or ".jpeg" => "image/jpeg",
                        _ => "application/octet-stream"
                    };

                    response.ContentLength64 = stream.Length;
                    stream.CopyTo(response.OutputStream);
                }

                response.StatusCode = 200;
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Error serving static file: {resourceName}");
                response.StatusCode = 500;
            }
            finally
            {
                response.Close();
            }
        }

        private int FindAvailablePort(int startPort)
        {
            for (int port = startPort; port < startPort + 100; port++)
            {
                try
                {
                    var listener = new HttpListener();
                    listener.Prefixes.Add($"http://localhost:{port}/");
                    listener.Start();
                    listener.Stop();
                    return port;
                }
                catch
                {
                    continue;
                }
            }
            return startPort;
        }

        public void Dispose()
        {
            if (_disposed) return;

            Stop();
            _cancellationTokenSource?.Dispose();
            _disposed = true;
        }
    }
}
