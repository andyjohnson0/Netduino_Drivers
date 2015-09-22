using System;

using Microsoft.SPOT;

namespace uk.andyjohnson0.Netduino.Drivers.Gps
{
    /// <summary>
    /// Monitors a stream of GPS NMEA messages and caches the latest fix location and time. 
    /// </summary>
    public class NmeaMonitor
    {
        /// <summary>
        /// Constructor. Initialise an NmeaMonitor object.
        /// </summary>
        public NmeaMonitor()
        {
            this.LastFixCoord = null;
        }


        /// <summary>
        /// Passes an NmeaMessage to the monitor.
        /// </summary>
        /// <param name="msg">An NMEA message</param>
        public void OnMessage(NmeaMessage msg)
        {
            if (msg == null)
                return;

            if (msg is NmeaFixMessage)
            {
                NmeaFixMessage fixMsg = msg as NmeaFixMessage;
                LastFixCoord = fixMsg.Coords;
                lastFixTime = DateTime.UtcNow;
            }
            else if (msg is NmeaRecommendedMinimumMessage)
            {
                NmeaRecommendedMinimumMessage timeMsg = msg as NmeaRecommendedMinimumMessage;
                lastGpsTimeValue = timeMsg.DateTime;
                lastGpsTimeTimestamp = DateTime.UtcNow;
            }
        }


        /// <summary>
        /// Latest fix coordinate.
        /// </summary>
        public LatLonCoord LastFixCoord
        {
            get; private set;
        }


        /// <summary>
        /// Time since the last fix was established.
        /// If no fix has been established then DateTime.MinValue is returned.
        /// </summary>
        public TimeSpan LastFixTimeOffset
        {
            get
            {
                if (lastFixTime != DateTime.MinValue)
                {
                    return DateTime.UtcNow - lastFixTime;
                }
                else
                {
                    return TimeSpan.MinValue;
                }
            }
        }
        private DateTime lastFixTime = DateTime.MinValue;


        /// <summary>
        /// Current GPS time in the UTC time zone, or DateTime.MinValue if the time is not yet known.
        /// The time is the sum of the last known GPS time and the time since it was received.
        /// </summary>
        public DateTime CurrentGpsTime
        {
            get
            {
                if (lastGpsTimeValue != DateTime.MinValue)
                    return lastGpsTimeValue + (DateTime.UtcNow - lastGpsTimeTimestamp);
                else
                    return DateTime.MinValue;
            }
        }
        private DateTime lastGpsTimeValue = DateTime.MinValue;  // Last time from the content of a gps message
        private DateTime lastGpsTimeTimestamp = DateTime.MinValue;  // Time that 'lastGpsTimeValue' was set.
    }
}
