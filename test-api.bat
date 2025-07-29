@echo off
echo Starting HTTP Server Test...

echo.
echo Testing Config Update:
curl -X POST -H "Content-Type: application/json" -d "{\"storeName\":\"Test Pizza Shop\",\"address\":\"456 Test Street\",\"phone\":\"555-TEST\",\"currency\":\"USD\"}" http://localhost:8080/update-config

echo.
echo.
echo Testing Advanced Receipt:
curl -X POST -H "Content-Type: application/json" -d "{\"orderId\":\"TEST-001\",\"customer\":{\"name\":\"John Smith\",\"phone\":\"555-1234\"},\"items\":[{\"name\":\"Pepperoni Pizza\",\"quantity\":2,\"price\":15.99,\"total\":31.98},{\"name\":\"Soda\",\"quantity\":1,\"price\":2.50,\"total\":2.50}],\"subtotal\":34.48,\"tax\":2.76,\"total\":37.24,\"payment\":{\"method\":\"Credit Card\",\"amountPaid\":37.24,\"change\":0.00},\"notes\":\"Extra cheese\",\"openCashDrawer\":false}" http://localhost:8080/print

echo.
echo.
echo Test completed. Check server console for debug output.
pause
