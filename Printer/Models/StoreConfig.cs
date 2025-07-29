using System;

namespace Printer.Models
{
    public class StoreConfig
    {
        public string StoreName { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string LogoPath { get; set; }
        public string PrinterName { get; set; }
        public bool EnableCashDrawer { get; set; }
        public string Currency { get; set; }
        public int Port { get; set; }
        public bool AutoStart { get; set; }

        public StoreConfig()
        {
            StoreName = "";
            Address = "";
            Phone = "";
            Email = "";
            LogoPath = "";
            PrinterName = "";
            EnableCashDrawer = true;
            Currency = "$";
            Port = 8080;
            AutoStart = true;
        }
    }
}
