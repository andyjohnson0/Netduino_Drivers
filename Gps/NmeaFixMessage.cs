using System;
using Microsoft.SPOT;

namespace uk.andyjohnson0.Netduino.Drivers.Gps
{
    /// <summary>
    /// Represents an NMEA fix (Drivers.) message giving information about the receive's geographical location.
    /// </summary>
    public class NmeaFixMessage : NmeaMessage
    {
        internal NmeaFixMessage(
            DateTime time,
            LatLonCoord coords,
            double altitude,  // Above sea level
            FixQuality quality,
            int sateliteCount)
        {
            this.Time = time;
            this.Coords = coords;
            this.Altitude = altitude;
            this.Quality = quality;
            this.SateliteCount = sateliteCount;
        }


        /// <summary>
        /// Date and time of the fix in the UTC timezone.
        /// </summary>
        public DateTime Time { get; private set; }

        /// <summary>
        /// Fix location.
        /// </summary>
        public LatLonCoord Coords { get; private set; }

        /// <summary>
        /// Fix altitue in metres.
        /// </summary>
        public double Altitude { get; private set; }

        /// <summary>
        /// Fix quality. This is a proxy for fix accuracy.
        /// </summary>
        public FixQuality Quality { get; private set; }

        /// <summary>
        /// Number of satelites that provided the fix.
        /// </summary>
        public int SateliteCount { get; private set; }


        /// <summary>
        /// Defines the quality of a GPS fix.
        /// </summary>
        public enum FixQuality
        {
            /// <summary>No fix</summary>
            Invalid = 0,
            /// <summary>GPS or standard positioning service (SPS) fix</summary>
            Gps = 1,
            /// <summary>Differential GPS fix</summary>
            Dgps = 2,
            /// <summary>Precise positioning service (PPS) fix</summary>
            Pps = 3,
            /// <summary>Real Time Kinematic (RTK) fixed solution</summary>
            RealTimeKinematic = 4,
            /// <summary>Real Time Kinematic (RTK) float solution</summary>
            DoubleRealTimeKinematic = 5,
            /// <summary>Estimated dead reckoning fix</summary>
            EstimateddeadReckoning = 6,
            /// <summary>Manual input mode</summary>
            ManualInputmode = 7,
            /// <summary>Simulation mode</summary>
            SimulationMode = 8,
            /// <summary>Other unknown fix technique</summary>
            Unknown = 999
        }


        #region INmeaMessage

        #endregion INmeaMessage
    }
}
