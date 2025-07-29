// React/Next.js POS Printer Integration
// This example shows how to integrate the POS printer with your React/Next.js application

// 1. Create a service to handle printer API calls
class PrinterService {
  constructor(baseUrl = 'http://localhost:8080') {
    this.baseUrl = baseUrl;
  }

  async makeRequest(endpoint, method = 'GET', data = null) {
    try {
      const options = {
        method,
        headers: {
          'Content-Type': 'application/json',
        },
      };

      if (data) {
        options.body = JSON.stringify(data);
      }

      const response = await fetch(`${this.baseUrl}${endpoint}`, options);
      
      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }
      
      return await response.json();
    } catch (error) {
      console.error('Printer API Error:', error);
      throw error;
    }
  }

  // Check if printer server is running
  async checkStatus() {
    return await this.makeRequest('/status');
  }

  // Get current store configuration
  async getConfig() {
    return await this.makeRequest('/config');
  }

  // Update store configuration
  async updateConfig(config) {
    return await this.makeRequest('/config', 'POST', config);
  }

  // Print a receipt
  async printReceipt(receiptData) {
    return await this.makeRequest('/print', 'POST', receiptData);
  }
}

// 2. React Hook for printer functionality
import { useState, useEffect } from 'react';

export function usePrinter() {
  const [printer] = useState(() => new PrinterService());
  const [isConnected, setIsConnected] = useState(false);
  const [storeConfig, setStoreConfig] = useState(null);

  useEffect(() => {
    checkConnection();
  }, []);

  const checkConnection = async () => {
    try {
      await printer.checkStatus();
      setIsConnected(true);
      
      // Load store config
      const config = await printer.getConfig();
      setStoreConfig(config);
    } catch (error) {
      setIsConnected(false);
      console.warn('Printer not connected:', error.message);
    }
  };

  const printOrder = async (orderData) => {
    if (!isConnected) {
      throw new Error('Printer not connected');
    }

    // Transform your order data to receipt format
    const receiptData = {
      orderId: orderData.id,
      orderDate: new Date().toISOString(),
      customer: {
        name: orderData.customerName,
        phone: orderData.customerPhone,
      },
      items: orderData.items.map(item => ({
        name: item.name,
        description: item.description || '',
        quantity: item.quantity,
        price: item.price,
        total: item.quantity * item.price,
      })),
      subtotal: orderData.subtotal,
      tax: orderData.tax,
      discount: orderData.discount || 0,
      total: orderData.total,
      payment: {
        method: orderData.paymentMethod,
        amountPaid: orderData.amountPaid,
        change: orderData.change || 0,
      },
      notes: orderData.notes || '',
      openCashDrawer: orderData.paymentMethod === 'Cash',
    };

    return await printer.printReceipt(receiptData);
  };

  return {
    isConnected,
    storeConfig,
    checkConnection,
    printOrder,
    updateConfig: printer.updateConfig.bind(printer),
  };
}

// 3. React Component Example
export default function CheckoutForm() {
  const { isConnected, printOrder } = usePrinter();
  const [order, setOrder] = useState({
    id: 'ORD-001',
    customerName: 'John Doe',
    customerPhone: '(555) 123-4567',
    items: [
      { name: 'Pizza Margherita', quantity: 1, price: 18.99 },
      { name: 'Coke', quantity: 2, price: 2.50 },
    ],
    subtotal: 23.99,
    tax: 1.92,
    total: 25.91,
    paymentMethod: 'Cash',
    amountPaid: 30.00,
    change: 4.09,
  });

  const handlePrintReceipt = async () => {
    try {
      await printOrder(order);
      alert('Receipt printed successfully!');
    } catch (error) {
      alert(`Failed to print receipt: ${error.message}`);
    }
  };

  return (
    <div className="checkout-form">
      <h2>Order Summary</h2>
      
      {/* Display order details */}
      <div className="order-details">
        <p>Order ID: {order.id}</p>
        <p>Customer: {order.customerName}</p>
        
        <div className="items">
          {order.items.map((item, index) => (
            <div key={index}>
              {item.quantity}x {item.name} - ${item.price}
            </div>
          ))}
        </div>
        
        <div className="totals">
          <p>Subtotal: ${order.subtotal}</p>
          <p>Tax: ${order.tax}</p>
          <p><strong>Total: ${order.total}</strong></p>
        </div>
      </div>

      {/* Print button */}
      <button
        onClick={handlePrintReceipt}
        disabled={!isConnected}
        className={`print-btn ${isConnected ? 'connected' : 'disconnected'}`}
      >
        {isConnected ? '🖨️ Print Receipt' : '❌ Printer Not Connected'}
      </button>

      {!isConnected && (
        <p className="warning">
          Make sure the printer server is running: run "Printer.exe server"
        </p>
      )}
    </div>
  );
}

// 4. Next.js API Route Example (/pages/api/print-receipt.js)
export default async function handler(req, res) {
  if (req.method !== 'POST') {
    return res.status(405).json({ error: 'Method not allowed' });
  }

  try {
    const printer = new PrinterService();
    const result = await printer.printReceipt(req.body);
    res.status(200).json(result);
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
}

// 5. Usage with any state management (Redux, Zustand, etc.)
export const printReceiptAction = async (orderData) => {
  const printer = new PrinterService();
  
  try {
    await printer.printReceipt(orderData);
    // Dispatch success action
    return { success: true };
  } catch (error) {
    // Dispatch error action
    return { success: false, error: error.message };
  }
};

// 6. TypeScript interfaces (if using TypeScript)
interface OrderItem {
  name: string;
  description?: string;
  quantity: number;
  price: number;
}

interface OrderData {
  id: string;
  customerName?: string;
  customerPhone?: string;
  items: OrderItem[];
  subtotal: number;
  tax: number;
  discount?: number;
  total: number;
  paymentMethod: string;
  amountPaid: number;
  change?: number;
  notes?: string;
}

interface ReceiptData {
  orderId: string;
  orderDate?: string;
  customer?: {
    name?: string;
    phone?: string;
  };
  items?: Array<{
    name: string;
    description?: string;
    quantity: number;
    price: number;
    total: number;
  }>;
  subtotal: number;
  tax: number;
  discount?: number;
  total: number;
  payment?: {
    method: string;
    amountPaid: number;
    change?: number;
  };
  notes?: string;
  openCashDrawer?: boolean;
}
