using System;
using System.Collections.Generic;

namespace Printer.Models
{
    public class ReceiptData
    {
        public string OrderId { get; set; }
        public DateTime? OrderDate { get; set; }
        public CustomerInfo Customer { get; set; }
        public List<ReceiptItem> Items { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Tax { get; set; }
        public decimal Discount { get; set; }
        public decimal Total { get; set; }
        public PaymentInfo Payment { get; set; }
        public string Notes { get; set; }
        public bool OpenCashDrawer { get; set; }

        public ReceiptData()
        {
            OrderId = "";
            Items = new List<ReceiptItem>();
            Notes = "";
            OpenCashDrawer = true;
        }
    }

    public class CustomerInfo
    {
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }

        public CustomerInfo()
        {
            Name = "";
            Phone = "";
            Address = "";
        }
    }

    public class ReceiptItem
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Total { get; set; }

        public ReceiptItem()
        {
            Name = "";
            Description = "";
            Quantity = 1;
        }
    }

    public class PaymentInfo
    {
        public string Method { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal Change { get; set; }

        public PaymentInfo()
        {
            Method = "Cash";
        }
    }
}
