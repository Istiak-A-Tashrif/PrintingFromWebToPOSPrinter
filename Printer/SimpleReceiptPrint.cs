using System;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using Printer.Models;
using Printer.Services;

namespace Printer
{
    public class SimpleReceiptPrint : PrintBase
    {
        private ReceiptData receiptData;
        private StoreConfig config;

        public bool Print(string printerName, ReceiptData data)
        {
            try
            {
                this.receiptData = data;
                this.config = ConfigManager.GetConfig();
                
                this.Print(printerName, this.PrintReceipt);
                
                if (data.OpenCashDrawer && config.EnableCashDrawer)
                {
                    Console.WriteLine("Opening cash drawer...");
                    // Cash drawer opening logic here
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error printing receipt: " + ex.Message);
                return false;
            }
        }

        private void PrintReceipt(Graphics g)
        {
            float y = 10;

            // Print logo if available
            if (!string.IsNullOrEmpty(config.LogoPath) && File.Exists(config.LogoPath))
            {
                try
                {
                    using (Image logoImage = Image.FromFile(config.LogoPath))
                    {
                        float logoWidth = Math.Min(60, logoImage.Width * 60 / logoImage.Height);
                        float logoHeight = logoImage.Height * logoWidth / logoImage.Width;
                        float logoX = (this.width - logoWidth) / 2;
                        
                        g.DrawImage(logoImage, logoX, y, logoWidth, logoHeight);
                        y += logoHeight + 10;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Could not load logo: " + ex.Message);
                }
            }

            // Store information
            if (!string.IsNullOrEmpty(config.StoreName))
            {
                y += this.DrawCenteredText(g, y, config.StoreName, 16f, FontStyle.Bold);
                y += 5;
            }

            if (!string.IsNullOrEmpty(config.Address))
            {
                y += this.DrawCenteredText(g, y, config.Address, 10f);
                y += 2;
            }

            if (!string.IsNullOrEmpty(config.Phone))
            {
                y += this.DrawCenteredText(g, y, config.Phone, 10f);
                y += 10;
            }

            // Order information
            if (!string.IsNullOrEmpty(receiptData.OrderId))
            {
                y += this.DrawTextColumns(
                    g, y,
                    new TextColumn("Order ID:", 0.6f, StringAlignment.Near, 12f),
                    new TextColumn(receiptData.OrderId, 0.4f, StringAlignment.Far, 12f)
                );
            }

            if (receiptData.OrderDate.HasValue)
            {
                y += this.DrawTextColumns(
                    g, y,
                    new TextColumn("Date:", 0.6f, StringAlignment.Near, 10f),
                    new TextColumn(receiptData.OrderDate.Value.ToString("MM/dd/yyyy HH:mm"), 0.4f, StringAlignment.Far, 10f)
                );
            }

            // Customer information
            if (receiptData.Customer != null && !string.IsNullOrEmpty(receiptData.Customer.Name))
            {
                y += 5;
                y += this.DrawTextColumns(
                    g, y,
                    new TextColumn("Customer:", 0.3f, StringAlignment.Near, 10f),
                    new TextColumn(receiptData.Customer.Name, 0.7f, StringAlignment.Near, 10f)
                );
            }

            y += 10;
            y += this.DrawLine(g, y);
            y += 5;

            // Items
            if (receiptData.Items != null && receiptData.Items.Count > 0)
            {
                // Items header
                y += this.DrawTextColumns(
                    g, y,
                    new TextColumn("Item", 0.5f, StringAlignment.Near, 10f),
                    new TextColumn("Qty", 0.15f, StringAlignment.Center, 10f),
                    new TextColumn("Price", 0.175f, StringAlignment.Far, 10f),
                    new TextColumn("Total", 0.175f, StringAlignment.Far, 10f)
                );
                
                y += this.DrawLine(g, y);
                y += 2;

                foreach (var item in receiptData.Items)
                {
                    string itemText = item.Name;
                    if (!string.IsNullOrEmpty(item.Description))
                    {
                        itemText += " - " + item.Description;
                    }

                    y += this.DrawTextColumns(
                        g, y,
                        new TextColumn(itemText, 0.5f, StringAlignment.Near, 10f),
                        new TextColumn(item.Quantity.ToString(), 0.15f, StringAlignment.Center, 10f),
                        new TextColumn(FormatCurrency(item.Price), 0.175f, StringAlignment.Far, 10f),
                        new TextColumn(FormatCurrency(item.Total), 0.175f, StringAlignment.Far, 10f)
                    );
                }

                y += 10;
                y += this.DrawLine(g, y);
                y += 5;
            }

            // Totals
            if (receiptData.Subtotal > 0)
            {
                y += this.DrawTextColumns(
                    g, y,
                    new TextColumn("Subtotal:", 0.7f, StringAlignment.Near, 11f),
                    new TextColumn(FormatCurrency(receiptData.Subtotal), 0.3f, StringAlignment.Far, 11f)
                );
            }

            if (receiptData.Tax > 0)
            {
                y += this.DrawTextColumns(
                    g, y,
                    new TextColumn("Tax:", 0.7f, StringAlignment.Near, 11f),
                    new TextColumn(FormatCurrency(receiptData.Tax), 0.3f, StringAlignment.Far, 11f)
                );
            }

            if (receiptData.Discount > 0)
            {
                y += this.DrawTextColumns(
                    g, y,
                    new TextColumn("Discount:", 0.7f, StringAlignment.Near, 11f),
                    new TextColumn("-" + FormatCurrency(receiptData.Discount), 0.3f, StringAlignment.Far, 11f)
                );
            }

            y += 5;
            y += this.DrawTextColumns(
                g, y,
                new TextColumn("TOTAL:", 0.7f, StringAlignment.Near, 14f),
                new TextColumn(FormatCurrency(receiptData.Total), 0.3f, StringAlignment.Far, 14f)
            );

            // Payment information
            if (receiptData.Payment != null)
            {
                y += 10;
                y += this.DrawLine(g, y);
                y += 5;

                if (!string.IsNullOrEmpty(receiptData.Payment.Method))
                {
                    y += this.DrawTextColumns(
                        g, y,
                        new TextColumn("Payment:", 0.6f, StringAlignment.Near, 10f),
                        new TextColumn(receiptData.Payment.Method, 0.4f, StringAlignment.Far, 10f)
                    );
                }

                if (receiptData.Payment.AmountPaid > 0)
                {
                    y += this.DrawTextColumns(
                        g, y,
                        new TextColumn("Paid:", 0.6f, StringAlignment.Near, 10f),
                        new TextColumn(FormatCurrency(receiptData.Payment.AmountPaid), 0.4f, StringAlignment.Far, 10f)
                    );
                }

                if (receiptData.Payment.Change > 0)
                {
                    y += this.DrawTextColumns(
                        g, y,
                        new TextColumn("Change:", 0.6f, StringAlignment.Near, 10f),
                        new TextColumn(FormatCurrency(receiptData.Payment.Change), 0.4f, StringAlignment.Far, 10f)
                    );
                }
            }

            // Notes
            if (!string.IsNullOrEmpty(receiptData.Notes))
            {
                y += 10;
                y += this.DrawLine(g, y);
                y += 5;
                y += this.DrawCenteredText(g, y, receiptData.Notes, 10f, FontStyle.Italic);
            }

            // Footer
            y += 15;
            y += this.DrawCenteredText(g, y, "Thank you for your business!", 12f, FontStyle.Bold);
            y += 5;
            y += this.DrawCenteredText(g, y, DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss"), 8f);
        }

        private string FormatCurrency(decimal amount)
        {
            return config.Currency + amount.ToString("0.00");
        }

        private float DrawCenteredText(Graphics g, float y, string text, float fontSize, FontStyle style = FontStyle.Regular)
        {
            using (Font font = new Font("Arial", fontSize, style))
            {
                SizeF textSize = g.MeasureString(text, font);
                float x = (this.width - textSize.Width) / 2;
                g.DrawString(text, font, Brushes.Black, x, y);
                return textSize.Height;
            }
        }

        private float DrawLine(Graphics g, float y)
        {
            using (Pen pen = new Pen(Color.Black, 0.5f))
            {
                g.DrawLine(pen, 5, y, this.width - 5, y);
                return 2f;
            }
        }
    }
}
