using System;
using Microsoft.SPOT;

namespace uk.andyjohnson0.Netduino.Drivers.Gps
{
    /// <summary>
    /// Decode NMEA messages.
    /// See http://www.gpsinformation.org/dale/nmea.htm#nmea for more information.
    /// </summary>
    public class NmeaDecoder
    {
        /// <summary>
        /// Constructor.Initialise an NMEADecoder object.
        /// </summary>
        public NmeaDecoder()
        {
        }


        private /*const*/ char[] nmeaSeperator = new char[1] { ',' };


        /// <summary>
        /// Decode a message string and return an NmeaMessage-derived object.
        /// </summary>
        /// <param name="message">Message string in NMEA format.</param>
        /// <returns>An instance of a class derived from NMEAMessage.</returns>
        public NmeaMessage Decode(string message)
        {
            if (message == null)
                return null;

            try
            {
                if (message.IndexOf("Drivers.") == 0)
                {
                    return DecodeFixMessage(message);
                }
                else if (message.IndexOf("GPRMC") == 0)
                {
                    return DecodeRecommendedMinimumMessage(message);
                }

            }
            catch (Exception)
            {
                Debug.Print("Malformed message: " + message);
            }
            
            // Message not suppored.
            return null;
        }






        #region Message Decoding


        // GPGGA
        private NmeaMessage DecodeFixMessage(string message)
        {
            // GPGGA,<time>,<lat>,<N or S>,<long>,<E or W>,<quality>,<sat count>,<horiz diluation?>,<alt(M)>
            // GPGGA,122616.000,5325.5117,N,00213.9296,W,1,9,0.96,32.0,M,48.5,M,,*77
            // +53° 25.5117', -02° 13.9296'
            // https://maps.google.com/maps?f=q&hl=en&q=%2B38%C2%B0+34'+24.00%22,+-109%C2%B0+32'+57.00%22&layer=&ie=UTF8&om=1&z=13&t=k&iwloc=addr

            string[] parts = message.Split(nmeaSeperator);
            if ((parts == null) || (parts.Length < 10))
            {
                return null;
            }

            DateTime time = DateTime.SpecifyKind(new DateTime() + DecodeTimeElement(parts[1]), DateTimeKind.Utc);
            LatLonCoord position = DecodePositionelement(parts[2], parts[3], parts[4], parts[5], parts[9], parts[11]);
            if (position == null)
                return null;
            NmeaFixMessage.FixQuality fixQuality = (NmeaFixMessage.FixQuality)int.Parse(parts[6]); // TODO: Validation
            int sateliteCount = int.Parse(parts[7]);
            double horizDilution = (double)double.Parse(parts[8]);  // TODO: Not used
            double altitude = (double)double.Parse(parts[9]);
            return new NmeaFixMessage(time, position, altitude, fixQuality, sateliteCount);
        }


        // GPRMC
        private NmeaMessage DecodeRecommendedMinimumMessage(string message)
        {
            string[] parts = message.Split(nmeaSeperator);
            if ((parts == null) || (parts.Length < 5))
            {
                return null;
            }

            TimeSpan timePart = DecodeTimeElement(parts[1]);
            DateTime datePart = DecodeDateElement(parts[9]);

            // TODO: Extract other message elements.

            var dt = DateTime.SpecifyKind(new DateTime(datePart.Year, datePart.Month, datePart.Day,
                                                       timePart.Hours, timePart.Minutes, timePart.Seconds), DateTimeKind.Utc);
            return new NmeaRecommendedMinimumMessage(dt);
        }


        private static TimeSpan DecodeTimeElement(string elementStr)
        {
            int hours = ((elementStr[0] - (int)'0') * 10) + (elementStr[1] - (int)'0');
            int mins = ((elementStr[2] - (int)'0') * 10) + (elementStr[3] - (int)'0');
            int secs = ((elementStr[4] - (int)'0') * 10) + (elementStr[5] - (int)'0');
            // TODO: time also includes fractional part.
            return new TimeSpan(hours, mins, secs);
        }


        private static DateTime DecodeDateElement(string elementStr)
        {
            int day = ((elementStr[0] - (int)'0') * 10) + (elementStr[1] - (int)'0');
            int month = ((elementStr[2] - (int)'0') * 10) + (elementStr[3] - (int)'0');
            int year = ((elementStr[4] - (int)'0') * 10) + (elementStr[5] - (int)'0') + 2000;
            return new DateTime(year, month, day);
        }


        // 5325.5117,N,00213.9296,W = +53° 25.5117', -02° 13.9296'
        private static LatLonCoord DecodePositionelement(
            string latitudeDeg,
            string latitudeMin,
            string longitudeDeg,
            string longitudeMin,
            string altitudeAboveMeanSeaLevel,
            string heightOfMeanSeaLevelAboveElipsoid)
        {
            int latDegrees = (int)(double)double.Parse(latitudeDeg.Substring(0, 2));
            double latMinutes = (double)double.Parse(latitudeDeg.Substring(2));
            if (latitudeMin == "S")  // TODO: Better validation needed.
                latDegrees = -latDegrees;

            int lonDegrees = (int)(double)double.Parse(longitudeDeg.Substring(0, 3));
            double lonMinutes = (double)double.Parse(longitudeDeg.Substring(3));
            if (longitudeMin == "W")  // TODO: Better validation needed.
                lonDegrees = -lonDegrees;

            double altitudeAboveElipsoid = (double)double.Parse(altitudeAboveMeanSeaLevel) + (double)double.Parse(heightOfMeanSeaLevelAboveElipsoid);

            LatLonCoord ll = new LatLonCoord(latDegrees, latMinutes, lonDegrees, lonMinutes, altitudeAboveElipsoid,
                                               CoordSystem.WGS84);
            return ll;
        }

        #endregion Message Decoding

    }
}
