using System;
using Microsoft.SPOT;

namespace uk.andyjohnson0.Netduino.Drivers.Gps
{
    /// <summary>
    /// Represents an NMEA Recommended minimum specific GPS transit data message.
    /// </summary>
    public class NmeaRecommendedMinimumMessage : NmeaMessage
    {
        internal NmeaRecommendedMinimumMessage(DateTime dateTime)
        {
            this.DateTime = dateTime;
        }

        /// <summary>
        /// Date and time of the message in the UTC timezone.
        /// </summary>
        public DateTime DateTime { get; private set; }
    }
}
