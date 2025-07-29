using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Printer.Models;
using Printer.Services;

namespace Printer
{
    public class HttpPrintServer
    {
        private HttpListener listener;
        private bool isRunning;
        private Thread listenerThread;
        private ConfigManager configManager;

        public HttpPrintServer()
        {
            configManager = new ConfigManager();
        }

        public void Start(int port = 8080)
        {
            if (isRunning) return;

            listener = new HttpListener();
            listener.Prefixes.Add(string.Format("http://localhost:{0}/", port));
            listener.Prefixes.Add(string.Format("http://127.0.0.1:{0}/", port));
            
            try
            {
                listener.Start();
                isRunning = true;
                
                Console.WriteLine("HTTP Print Server started on port {0}", port);
                Console.WriteLine("Endpoints:");
                Console.WriteLine("  POST http://localhost:{0}/print - Print receipt", port);
                Console.WriteLine("  GET  http://localhost:{0}/status - Server status", port);
                Console.WriteLine("  GET  http://localhost:{0}/config - Get configuration", port);
                Console.WriteLine("  POST http://localhost:{0}/config - Update configuration", port);
                Console.WriteLine();
                Console.WriteLine("Press 'q' to quit the server...");

                listenerThread = new Thread(HandleRequests);
                listenerThread.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to start HTTP server: " + ex.Message);
                Console.WriteLine("Make sure you run as Administrator or the port is not in use.");
            }
        }

        public void Stop()
        {
            if (!isRunning) return;

            isRunning = false;
            listener.Stop();
            listenerThread.Join(5000);
            Console.WriteLine("HTTP Print Server stopped.");
        }

        private void HandleRequests()
        {
            while (isRunning)
            {
                try
                {
                    HttpListenerContext context = listener.GetContext();
                    ThreadPool.QueueUserWorkItem(ProcessRequest, context);
                }
                catch (Exception ex)
                {
                    if (isRunning)
                    {
                        Console.WriteLine("Error handling request: " + ex.Message);
                    }
                }
            }
        }

        private void ProcessRequest(object state)
        {
            HttpListenerContext context = (HttpListenerContext)state;
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            try
            {
                string responseText = "";
                int statusCode = 200;

                // Add CORS headers
                response.Headers.Add("Access-Control-Allow-Origin", "*");
                response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

                if (request.HttpMethod == "OPTIONS")
                {
                    // Handle preflight CORS request
                    statusCode = 200;
                    responseText = "";
                }
                else if (request.HttpMethod == "POST" && request.Url.AbsolutePath == "/print")
                {
                    responseText = HandlePrintRequest(request);
                }
                else if (request.HttpMethod == "GET" && request.Url.AbsolutePath == "/status")
                {
                    responseText = HandleStatusRequest();
                }
                else if (request.HttpMethod == "GET" && request.Url.AbsolutePath == "/config")
                {
                    responseText = HandleGetConfig();
                }
                else if (request.HttpMethod == "POST" && request.Url.AbsolutePath == "/config")
                {
                    responseText = HandleUpdateConfig(request);
                }
                else
                {
                    statusCode = 404;
                    responseText = "{\"error\":\"Endpoint not found\"}";
                }

                response.StatusCode = statusCode;
                response.ContentType = "application/json";
                byte[] buffer = Encoding.UTF8.GetBytes(responseText);
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.OutputStream.Close();

                Console.WriteLine("{0} {1} {2} - {3}", 
                    DateTime.Now.ToString("HH:mm:ss"), 
                    request.HttpMethod, 
                    request.Url.AbsolutePath, 
                    statusCode);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error processing request: " + ex.Message);
                try
                {
                    response.StatusCode = 500;
                    byte[] errorBuffer = Encoding.UTF8.GetBytes("{\"error\":\"Internal server error\"}");
                    response.OutputStream.Write(errorBuffer, 0, errorBuffer.Length);
                    response.OutputStream.Close();
                }
                catch { }
            }
        }

        private string HandlePrintRequest(HttpListenerRequest request)
        {
            try
            {
                string body = "";
                using (Stream receiveStream = request.InputStream)
                using (StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8))
                {
                    body = readStream.ReadToEnd();
                }

                if (string.IsNullOrEmpty(body))
                {
                    return "{\"error\":\"Request body is empty\"}";
                }

                // Parse the JSON manually (simple parsing for our needs)
                ReceiptData receiptData = ParseReceiptJson(body);
                
                if (receiptData == null)
                {
                    return "{\"error\":\"Invalid JSON format\"}";
                }

                // Print the receipt
                SimpleReceiptPrint printer = new SimpleReceiptPrint();
                printer.Print(receiptData);

                return "{\"success\":true,\"message\":\"Receipt printed successfully\"}";
            }
            catch (Exception ex)
            {
                return "{\"error\":\"" + ex.Message.Replace("\"", "\\\"") + "\"}";
            }
        }

        private string HandleStatusRequest()
        {
            StoreConfig config = configManager.LoadConfig();
            return string.Format("{{\"status\":\"running\",\"storeName\":\"{0}\",\"version\":\"1.0\"}}", 
                config.StoreName.Replace("\"", "\\\""));
        }

        private string HandleGetConfig()
        {
            try
            {
                StoreConfig config = configManager.LoadConfig();
                return string.Format("{{\"storeName\":\"{0}\",\"address\":\"{1}\",\"phone\":\"{2}\",\"currency\":\"{3}\"}}",
                    config.StoreName.Replace("\"", "\\\""),
                    config.Address.Replace("\"", "\\\""),
                    config.Phone.Replace("\"", "\\\""),
                    config.Currency.Replace("\"", "\\\""));
            }
            catch (Exception ex)
            {
                return "{\"error\":\"" + ex.Message.Replace("\"", "\\\"") + "\"}";
            }
        }

        private string HandleUpdateConfig(HttpListenerRequest request)
        {
            try
            {
                string body = "";
                using (Stream receiveStream = request.InputStream)
                using (StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8))
                {
                    body = readStream.ReadToEnd();
                }

                StoreConfig config = ParseConfigJson(body);
                if (config == null)
                {
                    return "{\"error\":\"Invalid JSON format\"}";
                }

                configManager.SaveConfig(config);
                return "{\"success\":true,\"message\":\"Configuration updated successfully\"}";
            }
            catch (Exception ex)
            {
                return "{\"error\":\"" + ex.Message.Replace("\"", "\\\"") + "\"}";
            }
        }

        private ReceiptData ParseReceiptJson(string json)
        {
            // Simple JSON parsing for .NET Framework 4.6.1 compatibility
            // This is a basic implementation - in production you'd use a proper JSON library
            try
            {
                ReceiptData receipt = new ReceiptData();
                
                // Extract basic fields
                receipt.OrderNumber = ExtractJsonString(json, "orderNumber");
                receipt.StoreName = ExtractJsonString(json, "storeName");
                receipt.Notes = ExtractJsonString(json, "notes");
                
                // Extract decimal values
                string subtotalStr = ExtractJsonString(json, "subtotal");
                if (!string.IsNullOrEmpty(subtotalStr))
                {
                    decimal subtotal;
                    if (decimal.TryParse(subtotalStr, out subtotal))
                        receipt.Subtotal = subtotal;
                }

                string taxStr = ExtractJsonString(json, "tax");
                if (!string.IsNullOrEmpty(taxStr))
                {
                    decimal tax;
                    if (decimal.TryParse(taxStr, out tax))
                        receipt.Tax = tax;
                }

                string totalStr = ExtractJsonString(json, "total");
                if (!string.IsNullOrEmpty(totalStr))
                {
                    decimal total;
                    if (decimal.TryParse(totalStr, out total))
                        receipt.Total = total;
                }

                return receipt;
            }
            catch
            {
                return null;
            }
        }

        private StoreConfig ParseConfigJson(string json)
        {
            try
            {
                return new StoreConfig
                {
                    StoreName = ExtractJsonString(json, "storeName") ?? "My Store",
                    Address = ExtractJsonString(json, "address") ?? "",
                    Phone = ExtractJsonString(json, "phone") ?? "",
                    Currency = ExtractJsonString(json, "currency") ?? "$"
                };
            }
            catch
            {
                return null;
            }
        }

        private string ExtractJsonString(string json, string key)
        {
            string pattern = "\"" + key + "\"\\s*:\\s*\"";
            int startIndex = json.IndexOf(pattern);
            if (startIndex == -1) return null;

            startIndex += pattern.Length;
            int endIndex = json.IndexOf("\"", startIndex);
            
            while (endIndex > startIndex && json[endIndex - 1] == '\\')
            {
                endIndex = json.IndexOf("\"", endIndex + 1);
            }

            if (endIndex == -1) return null;

            return json.Substring(startIndex, endIndex - startIndex);
        }
    }
}
