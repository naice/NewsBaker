using System;

namespace ncoded.NetStandard
{
    public class DefaultDateTimeProvider : IDateTimeNowProvider
    {
        public DateTime Now
        {
            get
            {
                return DateTime.Now;
            }
        }
    }
}
