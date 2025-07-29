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

        public HttpPrintServer()
        {
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
                bool success = printer.Print("", receiptData);

                if (success)
                {
                    return "{\"success\":true,\"message\":\"Receipt printed successfully\"}";
                }
                else
                {
                    return "{\"error\":\"Failed to print receipt\"}";
                }
            }
            catch (Exception ex)
            {
                return "{\"error\":\"" + ex.Message.Replace("\"", "\\\"") + "\"}";
            }
        }

        private string HandleStatusRequest()
        {
            StoreConfig config = ConfigManager.GetConfig();
            return string.Format("{{\"status\":\"running\",\"storeName\":\"{0}\",\"version\":\"1.0\"}}", 
                config.StoreName.Replace("\"", "\\\""));
        }

        private string HandleGetConfig()
        {
            try
            {
                StoreConfig config = ConfigManager.GetConfig();
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

                Console.WriteLine("Received config update: " + body);

                // Create a config object with only the fields to update
                StoreConfig updatedFields = new StoreConfig();
                
                string storeName = ExtractJsonString(body, "storeName");
                if (storeName != null) {
                    updatedFields.StoreName = storeName;
                    Console.WriteLine("HTTP: Setting StoreName = " + storeName);
                }
                
                string address = ExtractJsonString(body, "address");
                if (address != null) {
                    updatedFields.Address = address;
                    Console.WriteLine("HTTP: Setting Address = " + address);
                }
                
                string phone = ExtractJsonString(body, "phone");
                if (phone != null) {
                    updatedFields.Phone = phone;
                    Console.WriteLine("HTTP: Setting Phone = " + phone);
                }
                
                string email = ExtractJsonString(body, "email");
                if (email != null) {
                    updatedFields.Email = email;
                    Console.WriteLine("HTTP: Setting Email = " + email);
                }
                
                string logoPath = ExtractJsonString(body, "logoPath");
                if (logoPath != null) {
                    updatedFields.LogoPath = logoPath;
                    Console.WriteLine("HTTP: Setting LogoPath = " + logoPath);
                }
                
                string printerName = ExtractJsonString(body, "printerName");
                if (printerName != null) {
                    updatedFields.PrinterName = printerName;
                    Console.WriteLine("HTTP: Setting PrinterName = " + printerName);
                }
                
                string currency = ExtractJsonString(body, "currency");
                if (currency != null) {
                    updatedFields.Currency = currency;
                    Console.WriteLine("HTTP: Setting Currency = " + currency);
                }
                
                string enableCashDrawer = ExtractJsonString(body, "enableCashDrawer");
                if (enableCashDrawer != null) {
                    updatedFields.EnableCashDrawer = enableCashDrawer.ToLower() == "true";
                    Console.WriteLine("HTTP: Setting EnableCashDrawer = " + enableCashDrawer);
                }
                
                // Update only the provided fields
                ConfigManager.UpdateFields(updatedFields);
                
                Console.WriteLine("HTTP Server: Config update completed");
                
                return "{\"success\":true,\"message\":\"Configuration updated successfully\"}";
            }
            catch (Exception ex)
            {
                Console.WriteLine("Config update error: " + ex.Message);
                return "{\"error\":\"" + ex.Message.Replace("\"", "\\\"") + "\"}";
            }
        }

        private ReceiptData ParseReceiptJson(string json)
        {
            // Simple JSON parsing for .NET Framework 4.6.1 compatibility
            try
            {
                Console.WriteLine("ParseReceiptJson: Starting to parse JSON: " + json.Substring(0, Math.Min(200, json.Length)) + "...");
                
                ReceiptData receipt = new ReceiptData();
                
                // Extract basic fields
                receipt.OrderId = ExtractJsonString(json, "orderId");
                receipt.Notes = ExtractJsonString(json, "notes");
                
                Console.WriteLine("ParseReceiptJson: OrderId = " + receipt.OrderId + ", Notes = " + receipt.Notes);
                
                // Extract customer info
                string customerName = ExtractJsonString(json, "customer.name");
                if (customerName == null)
                {
                    // Try nested customer object
                    int customerStart = json.IndexOf("\"customer\"");
                    if (customerStart > -1)
                    {
                        customerName = ExtractJsonString(json.Substring(customerStart), "name");
                    }
                }
                
                if (!string.IsNullOrEmpty(customerName))
                {
                    receipt.Customer = new CustomerInfo { Name = customerName };
                    
                    string customerPhone = ExtractJsonString(json, "customer.phone");
                    if (customerPhone == null)
                    {
                        int customerStart = json.IndexOf("\"customer\"");
                        if (customerStart > -1)
                        {
                            customerPhone = ExtractJsonString(json.Substring(customerStart), "phone");
                        }
                    }
                    if (!string.IsNullOrEmpty(customerPhone))
                    {
                        receipt.Customer.Phone = customerPhone;
                    }
                    
                    Console.WriteLine("ParseReceiptJson: Customer = " + customerName + ", Phone = " + customerPhone);
                }
                
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
                
                string discountStr = ExtractJsonString(json, "discount");
                if (!string.IsNullOrEmpty(discountStr))
                {
                    decimal discount;
                    if (decimal.TryParse(discountStr, out discount))
                        receipt.Discount = discount;
                }
                
                // Extract payment info
                int paymentStart = json.IndexOf("\"payment\"");
                if (paymentStart > -1)
                {
                    string paymentSection = json.Substring(paymentStart);
                    string paymentMethod = ExtractJsonString(paymentSection, "method");
                    if (!string.IsNullOrEmpty(paymentMethod))
                    {
                        receipt.Payment = new PaymentInfo { Method = paymentMethod };
                        
                        string amountPaidStr = ExtractJsonString(paymentSection, "amountPaid");
                        if (!string.IsNullOrEmpty(amountPaidStr))
                        {
                            decimal amountPaid;
                            if (decimal.TryParse(amountPaidStr, out amountPaid))
                                receipt.Payment.AmountPaid = amountPaid;
                        }
                        
                        string changeStr = ExtractJsonString(paymentSection, "change");
                        if (!string.IsNullOrEmpty(changeStr))
                        {
                            decimal change;
                            if (decimal.TryParse(changeStr, out change))
                                receipt.Payment.Change = change;
                        }
                    }
                }
                
                // Extract items array (basic parsing)
                int itemsStart = json.IndexOf("\"items\"");
                if (itemsStart > -1)
                {
                    string itemsSection = json.Substring(itemsStart);
                    int arrayStart = itemsSection.IndexOf("[");
                    int arrayEnd = itemsSection.IndexOf("]");
                    
                    if (arrayStart > -1 && arrayEnd > arrayStart)
                    {
                        string itemsArray = itemsSection.Substring(arrayStart + 1, arrayEnd - arrayStart - 1);
                        // Simple item parsing - split by objects
                        string[] itemObjects = itemsArray.Split(new string[] { "},{" }, StringSplitOptions.RemoveEmptyEntries);
                        
                        foreach (string itemObj in itemObjects)
                        {
                            string cleanItemObj = itemObj.Replace("{", "").Replace("}", "");
                            
                            string itemName = ExtractJsonString("{" + cleanItemObj + "}", "name");
                            string qtyStr = ExtractJsonString("{" + cleanItemObj + "}", "quantity");
                            string priceStr = ExtractJsonString("{" + cleanItemObj + "}", "price");
                            string totalStr2 = ExtractJsonString("{" + cleanItemObj + "}", "total");
                            
                            if (!string.IsNullOrEmpty(itemName))
                            {
                                ReceiptItem item = new ReceiptItem { Name = itemName };
                                
                                if (!string.IsNullOrEmpty(qtyStr))
                                {
                                    int qty;
                                    if (int.TryParse(qtyStr, out qty))
                                        item.Quantity = qty;
                                }
                                
                                if (!string.IsNullOrEmpty(priceStr))
                                {
                                    decimal price;
                                    if (decimal.TryParse(priceStr, out price))
                                        item.Price = price;
                                }
                                
                                if (!string.IsNullOrEmpty(totalStr2))
                                {
                                    decimal total2;
                                    if (decimal.TryParse(totalStr2, out total2))
                                        item.Total = total2;
                                }
                                else
                                {
                                    item.Total = item.Quantity * item.Price;
                                }
                                
                                receipt.Items.Add(item);
                            }
                        }
                    }
                }
                
                // Extract openCashDrawer
                string openCashDrawerStr = ExtractJsonString(json, "openCashDrawer");
                if (!string.IsNullOrEmpty(openCashDrawerStr))
                {
                    bool openCashDrawer;
                    if (bool.TryParse(openCashDrawerStr, out openCashDrawer))
                        receipt.OpenCashDrawer = openCashDrawer;
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
            try
            {
                string pattern = "\"" + key + "\"";
                int keyIndex = json.IndexOf(pattern);
                if (keyIndex == -1) 
                {
                    Console.WriteLine("ExtractJsonString: Key '" + key + "' not found in JSON");
                    return null;
                }

                int colonIndex = json.IndexOf(":", keyIndex);
                if (colonIndex == -1) 
                {
                    Console.WriteLine("ExtractJsonString: Colon not found after key '" + key + "'");
                    return null;
                }

                int startQuoteIndex = json.IndexOf("\"", colonIndex);
                if (startQuoteIndex == -1) 
                {
                    Console.WriteLine("ExtractJsonString: Start quote not found for key '" + key + "'");
                    return null;
                }

                int endQuoteIndex = json.IndexOf("\"", startQuoteIndex + 1);
                if (endQuoteIndex == -1) 
                {
                    Console.WriteLine("ExtractJsonString: End quote not found for key '" + key + "'");
                    return null;
                }

                string value = json.Substring(startQuoteIndex + 1, endQuoteIndex - startQuoteIndex - 1);
                Console.WriteLine("ExtractJsonString: " + key + " = '" + value + "'");
                return value;
            }
            catch (Exception ex)
            {
                Console.WriteLine("ExtractJsonString error for key '" + key + "': " + ex.Message);
                return null;
            }
        }
    }
}
