// Copyright © 2018 Dmitry Sikorsky. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Printer
{
  class Program
  {
    static void Main(string[] args)
    {
      if (args == null || args.Length == 0)
      {
        Console.WriteLine("Order ID is not specified.");
        return;
      }

      new ReceiptPrint().Print("RONGTA 80mm Series Printer", args[0].Replace("print://", string.Empty).Replace("/", string.Empty));
    }
  }
}