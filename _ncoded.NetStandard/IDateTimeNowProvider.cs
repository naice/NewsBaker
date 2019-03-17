using System;

namespace ncoded.NetStandard
{
    public interface IDateTimeNowProvider
    {
        DateTime Now { get; }
    }
}
