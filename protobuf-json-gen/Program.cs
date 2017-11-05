using System;

namespace Plaisted.ProtobufJsonGen
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            GenerateTypescript.FromDll(
                @"C:\Users\mplaisted\Source\Repos\BI.EBPP.API\src\EBPP.Interop\bin\Debug\netcoreapp2.0\EBPP.Interop.dll");
        }
    }
}
