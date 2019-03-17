using System;
using System.Collections.Generic;
using System.Text;

namespace NewsBaker
{
    interface INewsSender
    {
        void Send(MetaInformation meta);
    }
}
