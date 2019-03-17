using System;
using System.Collections.Generic;
using System.Text;

namespace ncoded.NetStandard.Log
{
    public class ConsoleLogger : ILogger
    {
        public void Error(string msg) => Write("ERR", msg);
        public void Exception(Exception ex) => Write("EXC", $"Type: {ex.GetType().FullName} Exception: {ex.ToString()}");
        public void Info(string msg) =>Write("INF", msg);
        public void Warn(string msg) => Write("WAR", msg);

        private void Write(string type, string msg)
        {
            Console.WriteLine($"[{type}]: {msg}");
        }
    }
}
