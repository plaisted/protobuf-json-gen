using System;

namespace Plaisted.ProtobufJsonGen
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            GenerateTypescript.FromDll(
                @"C:\Users\mplaisted\Source\Repos\BI.CLT.EBPP.AuthServer\src\EBPP.Authentication.Interop\bin\Debug\netcoreapp2.0\EBPP.Authentication.Interop.dll");
        }
    }
}
