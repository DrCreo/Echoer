using System;

namespace Echoer
{
    class Program
    {
        static void Main(string[] args) => new Bot().StartAsync().GetAwaiter().GetResult();
    }
}
