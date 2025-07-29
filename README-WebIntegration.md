# POS Receipt Printer - Web Integration Guide

A Windows-based POS receipt printer system that can be controlled via HTTP requests from web applications (Next.js, React, etc.). Perfect for restaurants, retail stores, and other businesses that need to print receipts from web-based point-of-sale systems.

## ✅ **CURRENT STATUS: WORKING & TESTED**

The system is now fully functional with:
- ✅ Successful compilation (fixed .NET Framework 4.6.1 compatibility issues)
- ✅ HTTP server running on localhost:8080
- ✅ Receipt printing working with test data
- ✅ Cash drawer control implemented
- ✅ Web integration examples provided
- ✅ React/Next.js integration code ready

## Features

- 🖨️ **Thermal POS Printer Support** - Works with 80mm thermal printers (tested with RONGTA series)
- 💰 **Cash Drawer Control** - Automatic cash drawer opening via RJ11/12 connection
- 🌐 **HTTP API** - RESTful API for web application integration
- ⚙️ **Configurable Store Settings** - Customizable store name, address, phone, currency
- 📋 **Dynamic Receipt Content** - Support for items, customer info, payments, notes
- 🔧 **Simple Setup** - Single executable with text-based configuration
- ⚡ **Real-time Printing** - Instant receipt printing from web requests

## Quick Start

### 1. Run the Printer Application

```bash
# Change to the executable directory
cd "c:\projects\PrintingFromWebToPOSPrinter\Printer\bin\Debug"

# Interactive mode
./Printer.exe

# HTTP Server mode (for web integration) - RECOMMENDED
./Printer.exe server

# Test printing
./Printer.exe test
```

### 2. Test Web Integration

1. Start the HTTP server: `./Printer.exe server`
2. Open `web-test.html` in your browser
3. Click "Check Status" - should show ✅ Server is running
4. Click "Print Receipt" to test printing

### 3. Integrate with Your Web App

```javascript
// Simple integration example
const printReceipt = async (orderData) => {
  const response = await fetch('http://localhost:8080/print', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      orderId: orderData.id,
      total: orderData.total,
      items: orderData.items,
      openCashDrawer: true
    })
  });
  
  if (response.ok) {
    console.log('Receipt printed successfully!');
  }
};
```

## HTTP API Endpoints

### GET /status
Check if the printer server is running.

**Response:**
```json
{
  "status": "running",
  "storeName": "My Store",
  "version": "1.0"
}
```

### POST /print
Print a receipt with order details.

**Request Body:**
```json
{
  "orderId": "ORD-001",
  "customer": {
    "name": "John Doe",
    "phone": "(555) 987-6543"
  },
  "items": [
    {
      "name": "Pizza Margherita",
      "quantity": 1,
      "price": 18.99,
      "total": 18.99
    }
  ],
  "subtotal": 18.99,
  "tax": 1.52,
  "total": 20.51,
  "payment": {
    "method": "Cash",
    "amountPaid": 25.00,
    "change": 4.49
  },
  "openCashDrawer": true
}
```

### GET /config
Get current store configuration.

### POST /config
Update store configuration with new settings.

## React/Next.js Integration

### Complete React Hook Example

```javascript
// usePrinter.js
import { useState, useEffect } from 'react';

export function usePrinter() {
  const [isConnected, setIsConnected] = useState(false);
  
  const checkConnection = async () => {
    try {
      const response = await fetch('http://localhost:8080/status');
      setIsConnected(response.ok);
    } catch {
      setIsConnected(false);
    }
  };

  const printOrder = async (orderData) => {
    const response = await fetch('http://localhost:8080/print', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(orderData)
    });
    
    if (!response.ok) throw new Error('Print failed');
    return response.json();
  };

  useEffect(() => {
    checkConnection();
  }, []);

  return { isConnected, printOrder, checkConnection };
}
```

### Component Usage

```jsx
// OrderSummary.jsx
import { usePrinter } from './usePrinter';

export default function OrderSummary({ order }) {
  const { isConnected, printOrder } = usePrinter();

  const handlePrint = async () => {
    try {
      await printOrder({
        orderId: order.id,
        items: order.items,
        total: order.total,
        openCashDrawer: true
      });
      alert('Receipt printed!');
    } catch (error) {
      alert('Print failed: ' + error.message);
    }
  };

  return (
    <div>
      <h2>Order #{order.id}</h2>
      <button 
        onClick={handlePrint}
        disabled={!isConnected}
      >
        {isConnected ? '🖨️ Print Receipt' : '❌ Printer Offline'}
      </button>
    </div>
  );
}
```

## Files & Structure

```
PrintingFromWebToPOSPrinter/
├── Printer/
│   ├── bin/Debug/
│   │   └── Printer.exe          # ← Main executable (WORKING)
│   ├── Models/
│   │   ├── ReceiptData.cs       # Receipt data structures
│   │   └── StoreConfig.cs       # Store configuration
│   ├── Services/
│   │   └── ConfigManager.cs     # Configuration management
│   ├── HttpPrintServer.cs       # HTTP API server
│   ├── SimpleReceiptPrint.cs    # Receipt printing logic
│   └── Program.cs               # Main application entry
├── web-test.html               # Web testing interface
├── react-integration-example.js # React/Next.js examples
└── README-WebIntegration.md    # This file
```

## Hardware Setup

### Supported Printers
- RONGTA 80mm Series (recommended)
- Most ESC/POS compatible thermal printers
- Any Windows-compatible receipt printer

### Cash Drawer Setup
1. Connect cash drawer to printer via RJ11/12 cable
2. Cash drawer opens automatically with cash payments
3. Test with `openCashDrawer: true` in your requests

### Windows Requirements
- Windows 10/11
- .NET Framework 4.6.1 (built-in on modern Windows)
- Printer drivers installed and working

## Testing & Troubleshooting

### ✅ Verify Setup
```bash
# 1. Test basic printing
./Printer.exe test

# 2. Start HTTP server
./Printer.exe server

# 3. Open web-test.html and click "Check Status"
```

### Common Issues

**"Printer not found"**
- Install printer drivers in Windows
- Set printer as default or check printer name
- Test print from Windows first

**"HTTP server won't start"**
- Run as Administrator
- Check if port 8080 is in use
- Disable Windows Firewall temporarily

**"Web requests fail"**
- Ensure server is running (`./Printer.exe server`)
- Check browser console for CORS errors
- Verify JSON format matches examples

## Next.js API Route Example

```javascript
// pages/api/print-receipt.js
export default async function handler(req, res) {
  if (req.method !== 'POST') {
    return res.status(405).json({ error: 'Method not allowed' });
  }

  try {
    const response = await fetch('http://localhost:8080/print', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(req.body)
    });

    const result = await response.json();
    res.status(200).json(result);
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
}
```

## Configuration

The app creates `store-config.txt` automatically:

```
StoreName=My Store
Address=123 Main St
Phone=(555) 123-4567
Currency=$
EnableCashDrawer=True
Port=8080
```

## Version History

- **v1.0** - Original print:// protocol implementation
- **v2.0** - **CURRENT VERSION** - HTTP API server, React integration, dynamic receipts

## Support

1. **Test basic functionality first**: Use `./Printer.exe test`
2. **Verify HTTP server**: Use `web-test.html` 
3. **Check hardware**: Ensure printer works from Windows
4. **Integration**: Follow React/Next.js examples in `react-integration-example.js`

---

**Ready to integrate with your Next.js/React app! The HTTP server is working and tested.** 🚀
