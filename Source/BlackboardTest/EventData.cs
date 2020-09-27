using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackboardTest
{
    public class EventData
    {
        public EventData(string eventName, long eventCount, DateTime eventDate)
        {
            EventName = eventName;
            EventCount = eventCount;
            EventDate = eventDate;
        }

        public string EventName { get; private set; }

        public long EventCount { get; private set; }

        public DateTime EventDate { get; private set; }

    }
}
