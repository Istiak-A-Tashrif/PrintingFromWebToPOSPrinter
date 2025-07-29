// Copyright © 2018 Dmitry Sikorsky. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Printer.Services;
using Printer.Models;

namespace Printer
{
  class Program
  {
    static void Main(string[] args)
    {
      Console.WriteLine("=== POS Receipt Printer ===");
      
      // Load config
      var config = ConfigManager.GetConfig();
      Console.WriteLine("Configuration loaded. Press 'c' to configure, 't' to test, 'q' to quit, or provide order data:");
      
      // If called with arguments (from custom protocol), handle receipt printing
      if (args != null && args.Length > 0)
      {
        HandlePrintRequest(args[0], config);
        return;
      }
      
      // Interactive mode
      while (true)
      {
        var input = Console.ReadLine();
        
        if (string.IsNullOrEmpty(input))
          continue;
          
        if (input.ToLower() == "q")
        {
          Console.WriteLine("Goodbye!");
          break;
        }
        else if (input.ToLower() == "c")
        {
          ShowConfigMenu(config);
        }
        else if (input.ToLower() == "t")
        {
          TestPrint(config);
        }
        else if (input.StartsWith("print://") || input.StartsWith("{"))
        {
          HandlePrintRequest(input, config);
        }
        else
        {
          Console.WriteLine("Commands: 'c' (configure), 't' (test), 'q' (quit)");
          Console.WriteLine("Or provide JSON receipt data or print:// URL");
        }
      }
    }
    
    private static void HandlePrintRequest(string input, StoreConfig config)
    {
      try
      {
        if (input.StartsWith("print://"))
        {
          // Legacy mode - just print order ID
          var orderId = input.Replace("print://", "").Replace("/", "");
          Console.WriteLine("Processing print request: " + input);
          new ReceiptPrint().Print(config.PrinterName, orderId);
        }
        else if (input.StartsWith("{"))
        {
          // JSON mode - parse and print dynamic receipt
          Console.WriteLine("Processing JSON receipt data...");
          var receiptData = ParseSimpleJson(input);
          if (receiptData != null)
          {
            var printer = new SimpleReceiptPrint();
            printer.Print(config.PrinterName, receiptData);
          }
          else
          {
            Console.WriteLine("Error: Could not parse JSON data");
          }
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine("Error processing print request: " + ex.Message);
      }
    }
    
    private static void ShowConfigMenu(StoreConfig config)
    {
      Console.WriteLine("\n" + new string('=', 50));
      Console.WriteLine("CONFIGURATION");
      Console.WriteLine(new string('=', 50));
      
      Console.WriteLine("Store Name: " + config.StoreName);
      Console.WriteLine("Address: " + config.Address);
      Console.WriteLine("Phone: " + config.Phone);
      Console.WriteLine("Email: " + config.Email);
      Console.WriteLine("Logo Path: " + config.LogoPath);
      Console.WriteLine("Printer: " + config.PrinterName);
      Console.WriteLine("Cash Drawer: " + (config.EnableCashDrawer ? "Enabled" : "Disabled"));
      Console.WriteLine("Currency: " + config.Currency);
      
      Console.WriteLine("\nTo modify settings, edit the 'store-config.txt' file");
      Console.WriteLine(new string('=', 50) + "\n");
    }
    
    private static void TestPrint(StoreConfig config)
    {
      Console.WriteLine("Printing test receipt...");
      
      try
      {
        var testData = new ReceiptData();
        testData.OrderId = "TEST-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
        testData.OrderDate = DateTime.Now;
        
        testData.Customer = new CustomerInfo();
        testData.Customer.Name = "Test Customer";
        testData.Customer.Phone = "(555) 123-4567";
        
        testData.Items.Add(new ReceiptItem 
        { 
          Name = "Test Item 1", 
          Description = "Sample product",
          Quantity = 2, 
          Price = 15.50m, 
          Total = 31.00m 
        });
        
        testData.Items.Add(new ReceiptItem 
        { 
          Name = "Test Item 2", 
          Quantity = 1, 
          Price = 8.99m, 
          Total = 8.99m 
        });
        
        testData.Subtotal = 39.99m;
        testData.Tax = 3.20m;
        testData.Discount = 2.00m;
        testData.Total = 41.19m;
        
        testData.Payment = new PaymentInfo();
        testData.Payment.Method = "Cash";
        testData.Payment.AmountPaid = 50.00m;
        testData.Payment.Change = 8.81m;
        
        testData.Notes = "Thank you for testing our POS system!";
        testData.OpenCashDrawer = true;

        var printer = new SimpleReceiptPrint();
        var success = printer.Print(config.PrinterName, testData);
        
        if (success)
        {
          Console.WriteLine("✓ Test receipt printed successfully!");
        }
        else
        {
          Console.WriteLine("✗ Failed to print test receipt");
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine("✗ Error printing test receipt: " + ex.Message);
      }
    }
    
    // Simple JSON parser for basic receipt data (no external dependencies)
    private static ReceiptData ParseSimpleJson(string json)
    {
      try
      {
        var data = new ReceiptData();
        
        // Very basic JSON parsing - for production use a proper JSON library
        if (json.Contains("\"orderId\""))
        {
          var start = json.IndexOf("\"orderId\":\"") + 11;
          var end = json.IndexOf("\"", start);
          if (end > start) data.OrderId = json.Substring(start, end - start);
        }
        
        if (json.Contains("\"total\""))
        {
          var start = json.IndexOf("\"total\":") + 8;
          var end = json.IndexOfAny(new char[] { ',', '}' }, start);
          if (end > start) 
          {
            var totalStr = json.Substring(start, end - start).Trim();
            decimal total;
            if (decimal.TryParse(totalStr, out total))
            {
              data.Total = total;
            }
          }
        }
        
        // Add basic customer info if present
        if (json.Contains("\"customer\""))
        {
          data.Customer = new CustomerInfo();
          if (json.Contains("\"name\""))
          {
            var start = json.IndexOf("\"name\":\"") + 8;
            var end = json.IndexOf("\"", start);
            if (end > start) data.Customer.Name = json.Substring(start, end - start);
          }
        }
        
        return data;
      }
      catch
      {
        return null;
      }
    }
  }
}