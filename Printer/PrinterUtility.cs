using System;
using System.Drawing.Printing;
using System.Linq;

namespace Printer
{
    public static class PrinterUtility
    {
        public static void ListAvailablePrinters()
        {
            Console.WriteLine("=== Available Printers ===");
            
            PrinterSettings.StringCollection printers = PrinterSettings.InstalledPrinters;
            
            if (printers.Count == 0)
            {
                Console.WriteLine("No printers installed on this system.");
                return;
            }
            
            for (int i = 0; i < printers.Count; i++)
            {
                string printerName = printers[i];
                bool isDefault = IsDefaultPrinter(printerName);
                
                Console.WriteLine("{0}. {1}{2}", 
                    i + 1, 
                    printerName, 
                    isDefault ? " (Default)" : "");
            }
            
            Console.WriteLine();
            Console.WriteLine("To use a specific printer, update the PrinterName in store-config.txt");
            Console.WriteLine("Leave PrinterName empty to use the default printer");
        }
        
        public static bool IsDefaultPrinter(string printerName)
        {
            try
            {
                PrinterSettings settings = new PrinterSettings();
                settings.PrinterName = printerName;
                return settings.IsDefaultPrinter;
            }
            catch
            {
                return false;
            }
        }
        
        public static string GetDefaultPrinter()
        {
            try
            {
                PrinterSettings settings = new PrinterSettings();
                return settings.PrinterName;
            }
            catch
            {
                return "";
            }
        }
        
        public static bool IsPrinterValid(string printerName)
        {
            if (string.IsNullOrEmpty(printerName))
                return true; // Empty means use default
                
            PrinterSettings.StringCollection printers = PrinterSettings.InstalledPrinters;
            return printers.Cast<string>().Any(p => p.Equals(printerName, StringComparison.OrdinalIgnoreCase));
        }
        
        public static void CreateTestPrinterConfig()
        {
            Console.WriteLine("=== Setting up Test Configuration ===");
            
            string defaultPrinter = GetDefaultPrinter();
            
            if (string.IsNullOrEmpty(defaultPrinter))
            {
                Console.WriteLine("Warning: No default printer found.");
                Console.WriteLine("You can still test the system, but receipts won't print to a physical device.");
                Console.WriteLine();
                Console.WriteLine("Options:");
                Console.WriteLine("1. Install Microsoft Print to PDF (virtual printer)");
                Console.WriteLine("2. Install any printer driver for testing");
                Console.WriteLine("3. Use the system with empty PrinterName (may show print dialog)");
            }
            else
            {
                Console.WriteLine("Default printer found: " + defaultPrinter);
                Console.WriteLine("The system will use this printer for receipt printing.");
                
                // Test if the default printer is accessible
                if (TestPrinterAccess(defaultPrinter))
                {
                    Console.WriteLine("✓ Printer is accessible and ready");
                }
                else
                {
                    Console.WriteLine("⚠ Printer may be offline or have issues");
                }
            }
            
            // Update config to use the default printer name (not empty)
            var configManager = new Services.ConfigManager();
            var config = Services.ConfigManager.GetConfig();
            config.PrinterName = defaultPrinter ?? ""; // Use actual printer name or empty
            Services.ConfigManager.SetConfig(config);
            Services.ConfigManager.SaveConfig();
            
            Console.WriteLine();
            Console.WriteLine("Configuration updated to use: " + (string.IsNullOrEmpty(defaultPrinter) ? "system default" : defaultPrinter));
            Console.WriteLine("You can now test the system!");
        }
        
        public static bool TestPrinterAccess(string printerName)
        {
            try
            {
                PrinterSettings settings = new PrinterSettings();
                if (!string.IsNullOrEmpty(printerName))
                {
                    settings.PrinterName = printerName;
                }
                
                // Try to access basic printer properties
                bool isValid = settings.IsValid;
                bool isDefault = settings.IsDefaultPrinter;
                
                Console.WriteLine("Printer Status:");
                Console.WriteLine("  Name: " + settings.PrinterName);
                Console.WriteLine("  Valid: " + isValid);
                Console.WriteLine("  Default: " + isDefault);
                Console.WriteLine("  Can Duplex: " + settings.CanDuplex);
                
                return isValid;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error accessing printer: " + ex.Message);
                return false;
            }
        }
    }
}
