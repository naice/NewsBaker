using System;
using System.Collections.Generic;
using System.Text;

namespace ncoded.NetStandard.Log
{
    public interface ILogger
    {
        void Info(string msg);
        void Warn(string msg);
        void Error(string msg);
        void Exception(Exception ex);
    }
}
