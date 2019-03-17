using System;
using System.IO;

namespace NewsBaker
{
    class RunTimeInformation
    {
        public string CurrentDir { get; set; }
        public string StartUp { get; set; }
        public string StartUpPath { get; set; }

        public RunTimeInformation()
        {
            CurrentDir = Environment.CurrentDirectory;
            var exeAssm = System.Reflection.Assembly.GetExecutingAssembly();
            StartUp = exeAssm.Location;
            StartUpPath = Path.GetDirectoryName(StartUp);
        }
    }
}
