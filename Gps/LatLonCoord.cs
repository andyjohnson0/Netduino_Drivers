using System;
using Microsoft.SPOT;

using Math = uk.andyjohnson0.Netduino.Utils.Math;



namespace uk.andyjohnson0.Netduino.Drivers.Gps
{
    public enum CoordSystem
    {
        /// <summary>
        /// World Geodetic System. GRS80 datum.
        /// </summary>
        WGS84,

        /// <summary>
        /// UK national grid. Airy 1830 ellipsoid.
        /// </summary>
        OSGB36
    }


    /// <summary>
    /// Represents a latitude/longitude coordinate on a given elipsoid.
    /// Latitudes south of the equator and longitudes west of the meridian are negative.
    /// </summary>
    public class LatLonCoord
    {
        public LatLonCoord(
            double latitude,
            double longitude,
            double altitude,  // Above Elipsoid
            CoordSystem elipsoid)
        {
            Latitude = latitude;
            Longitude = longitude;
            Altitude = altitude;
            Elipsoid = elipsoid;
        }


        public LatLonCoord(
            int latitudeDegrees,
            double latitudeMinutes,
            int longitudeDegrees,
            double longitudeMinutes,
            double altitude,  // Above Elipsoid
            CoordSystem elipsoid)
        {
            // TODO: Calculate seconds from double minutes; See http://notinthemanual.blogspot.co.uk/2008/07/convert-nmea-latitude-longitude-to.html
            Latitude = (double)latitudeDegrees;
            if (latitudeDegrees >= 0)
                Latitude += (latitudeMinutes / 60D);
            else
                Latitude -= (latitudeMinutes / 60D);
            Longitude = (double)longitudeDegrees;
            if (longitudeDegrees >= 0)
                Longitude += (longitudeMinutes / 60D);
            else
                Longitude -= (longitudeMinutes / 60D);
            Altitude = altitude;
            Elipsoid = elipsoid;
        }


        public double Latitude { get; private set; }
        public double Longitude { get; private set; }
        public double Altitude { get; private set; }
        public CoordSystem Elipsoid { get; private set; }


        public bool IsEqualTo(LatLonCoord coord)
        {
            return (coord != null) &&
                   (this.Latitude == coord.Latitude) &&
                   (this.Longitude == coord.Longitude) &&
                   (this.Altitude == coord.Altitude) &&
                   (this.Elipsoid == coord.Elipsoid);
        }


        public int LatitudeDegrees
        {
            get { return (int)Latitude; }
        }

        public double LatitudeMinutes
        {
            get { return Math.Abs((Latitude - (double)LatitudeDegrees) * 60D); }
        }

        public int LongitudeDegrees
        {
            get { return (int)Longitude; }
        }

        public double LongitudeMinutes
        {
            get { return Math.Abs((Longitude - (double)LongitudeDegrees) * 60D); }
        }



        public override string ToString()
        {
            return ((LatitudeDegrees >= 0) ? "+" : "") + LatitudeDegrees.ToString() + " " + LatitudeMinutes.ToString("N3") + "' " +
                   ((LongitudeDegrees >= 0) ? "+" : "") + LongitudeDegrees.ToString() + " " + LongitudeMinutes.ToString("N3") + "'";
        }


        public static LatLonCoord Convert(
            LatLonCoord sourceCoord,
            CoordSystem targetElipsoid)
        {
            if ((sourceCoord.Elipsoid == CoordSystem.WGS84) && (targetElipsoid == CoordSystem.OSGB36))
            {
                return WGS84ToOSGB36(sourceCoord);
            }

            throw new NotImplementedException("Conversion not supported");;
        }


        // Processes WGS84 lat and lon in decimal form with S and W as -ve
        private static LatLonCoord WGS84ToOSGB36(LatLonCoord coord)
        {
            // Debug.Assert(coord.Elipsoid == CoordSystem.WGS84);

            double WGlat = coord.Latitude;
            double WGlon = coord.Longitude;
            double height = coord.Altitude;

            //first off convert to radians
            double radWGlat = Math.DegToRad(WGlat);
            double radWGlon = Math.DegToRad(WGlon);

            /* these calculations were derived from the work of
             * Roger Muggleton (http://www.carabus.co.uk/) */

            /* quoting Roger Muggleton :-
             * There are many ways to convert data from one system to another, the most accurate 
             * being the most complex! For this example I shall use a 7 parameter Helmert 
             * transformation.
             * The process is in three parts: 
             * (1) Convert latitude and longitude to Cartesian coordinates (these also include height 
             * data, and have three parameters, X, Y and Z). 
             * (2) Transform to the new system by applying the 7 parameters and using a little maths.
             * (3) Convert back to latitude and longitude.
             * For the example we shall transform a GRS80 location to Airy, e.g. a GPS reading to 
             * the Airy spheroid.
             * The following code converts latitude and longitude to Cartesian coordinates. The 
             * input parameters are: WGS84 latitude and longitude, axis is the GRS80/WGS84 major 
             * axis in metres, ecc is the eccentricity, and height is the height above the 
             *  ellipsoid.
             *  v = axis / (Math.sqrt (1 - ecc * (Math.Pow (Math.sin(lat), 2))));
             *  x = (v + height) * Math.cos(lat) * Math.cos(lon);
             * y = (v + height) * Math.cos(lat) * Math.sin(lon);
             * z = ((1 - ecc) * v + height) * Math.sin(lat);
             * The transformation requires the 7 parameters: xp, yp and zp correct the coordinate 
             * origin, xr, yr and zr correct the orientation of the axes, and sf deals with the 
             * changing scale factors. */

            //these are the values for WGS86(GRS80) to OSGB36(Airy)
            double h = height;                     // height above datum  (from GPS GGA sentence)
            const double a = 6378137;              // WGS84_AXIS
            const double e = 0.00669438037928458;  // WGS84_ECCENTRIC
            const double a2 = 6377563.396;         //OSGB_AXIS
            const double e2 = 0.0066705397616;     // OSGB_ECCENTRIC 
            const double xp = -446.448;
            const double yp = 125.157;
            const double zp = -542.06;
            const double xr = -0.1502;
            const double yr = -0.247;
            const double zr = -0.8421;
            const double s = 20.4894;

            // convert to cartesian; lat, lon are radians
            double sf = s * 0.000001;
            double v = a / (Math.Sqrt(1 - (e * (Math.Sin(radWGlat) * Math.Sin(radWGlat)))));
            double x = (v + h) * Math.Cos(radWGlat) * Math.Cos(radWGlon);
            double y = (v + h) * Math.Cos(radWGlat) * Math.Sin(radWGlon);
            double z = ((1 - e) * v + h) * Math.Sin(radWGlat);

            // transform cartesian
            double xrot = Math.DegToRad(xr / 3600);
            double yrot = Math.DegToRad(yr / 3600);
            double zrot = Math.DegToRad(zr / 3600);
            double hx = x + (x * sf) - (y * zrot) + (z * yrot) + xp;
            double hy = (x * zrot) + y + (y * sf) - (z * xrot) + yp;
            double hz = (-1 * x * yrot) + (y * xrot) + z + (z * sf) + zp;

            // Convert back to lat, lon
            double newLon = Math.Atan(hy / hx);
            double p = Math.Sqrt((hx * hx) + (hy * hy));
            double newLat = Math.Atan(hz / (p * (1 - e2)));
            v = a2 / (Math.Sqrt(1 - e2 * (Math.Sin(newLat) * Math.Sin(newLat))));
            double errvalue = 1.0;
            double lat0 = 0;
            while (errvalue > 0.001)
            {
                lat0 = Math.Atan((hz + e2 * v * Math.Sin(newLat)) / p);
                errvalue = Math.Abs(lat0 - newLat);
                newLat = lat0;
            }

            //convert back to degrees
            newLat = Math.RadToDeg(newLat);
            newLon = Math.RadToDeg(newLon);

            return new LatLonCoord(newLat, newLon, height, CoordSystem.OSGB36);
        }
    }
}
