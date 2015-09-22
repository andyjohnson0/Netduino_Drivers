//
// Contents of this file are based on work by Foger Muggleton,
// and available at http://www.dorcus.co.uk/carabus/ll_ngr.html
// 

using System;
using Microsoft.SPOT;

using Math = uk.andyjohnson0.Netduino.Utils.Math;



namespace uk.andyjohnson0.Netduino.Drivers.Gps
{
    /// <summary>
    /// Represents an Ordnance Survey of Great Britain grid reference.
    /// Based on work by Foger Muggleton at http://www.dorcus.co.uk/carabus/ll_ngr.html.
    /// </summary>
    public class OSGBGridRef
    {
        public OSGBGridRef(LatLonCoord coord)
        {
            // Debug.Assert(coord.Elipsoid == CoordSystem.OSGB36);
            Initialise(coord);
        }


        /// <summary>
        /// Eastings part of the grid reference.
        /// </summary>
        public double Eastings { get; private set; }

        /// <summary>
        /// Northings part of the grid reference.
        /// </summary>
        public double Northings { get; private set; }


        /// <summary>
        /// Returns a grid reference string consisting of two letters indicating the grid square and a six figure northing and easting.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            //convert 12 (6e & 6n) figure numeric to letter and number grid system
            double eX = Eastings / 500000.0D;
            double nX = Northings / 500000.0D;
            double tmp = Math.Floor(eX) - 5.0 * Math.Floor(nX) + 17.0;  //Math.Floor Returns the largest integer less than or equal to the specified number.
            nX = 5 * (nX - Math.Floor(nX));
            eX = 20 - 5.0 * Math.Floor(nX) + Math.Floor(5.0 * (eX - Math.Floor(eX)));
            if (eX > 7.5) eX = eX + 1;
            if (tmp > 7.5) tmp = tmp + 1;
            string eing = Eastings.ToString();
            string ning = Northings.ToString();
            int lnth = eing.Length;
            eing = eing.Substring(lnth - 5);
            lnth = ning.Length;
            ning = ning.Substring(lnth - 5);

            char c1 = (char)(65 + (int)tmp);
            char c2 = (char)(65 + (int)eX);
            string ngr = c1.ToString() + c2.ToString() + " " + eing + " " + ning;

            return ngr;
        }


        // converts lat and lon (OSGB36)  to OS 6 figure northing and easting
        private void Initialise(LatLonCoord coord)
        {
            // Debug.Assert(coord.Elipsoid == CoordSystem.OSGB36);

            double lat = coord.Latitude;
            double lon = coord.Longitude;

            double phi = Math.DegToRad(lat);          // convert latitude to radians
            double lam = Math.DegToRad(lon);          // convert longitude to radians
            double a = 6377563.396;              // OSGB semi-major axis
            double b = 6356256.91;               // OSGB semi-minor axis
            double e0 = 400000;                  // easting of false origin
            double n0 = -100000;                 // northing of false origin
            double f0 = 0.9996012717;            // OSGB scale factor on central meridian
            double e2 = 0.0066705397616;         // OSGB eccentricity squared
            double lam0 = -0.034906585039886591; // OSGB false east
            double phi0 = 0.85521133347722145;   // OSGB false north
            double af0 = a * f0;
            double bf0 = b * f0;

            // easting
            double slat2 = Math.Sin(phi) * Math.Sin(phi);
            double nu = af0 / (Math.Sqrt(1 - (e2 * (slat2))));
            double rho = (nu * (1 - e2)) / (1 - (e2 * slat2));
            double eta2 = (nu / rho) - 1;
            double p = lam - lam0;
            double IV = nu * Math.Cos(phi);
            double clat3 = Math.Pow(Math.Cos(phi), 3);
            double tlat2 = Math.Tan(phi) * Math.Tan(phi);
            double V = (nu / 6) * clat3 * ((nu / rho) - tlat2);
            double clat5 = Math.Pow(Math.Cos(phi), 5);
            double tlat4 = Math.Pow(Math.Tan(phi), 4);
            double VI = (nu / 120) * clat5 * ((5 - (18 * tlat2)) + tlat4 + (14 * eta2) - (58 * tlat2 * eta2));
            double east = e0 + (p * IV) + (Math.Pow(p, 3) * V) + (Math.Pow(p, 5) * VI);

            // northing
            double n = (af0 - bf0) / (af0 + bf0);
            double M = Marc(bf0, n, phi0, phi);
            double I = M + (n0);
            double II = (nu / 2) * Math.Sin(phi) * Math.Cos(phi);
            double III = ((nu / 24) * Math.Sin(phi) * Math.Pow(Math.Cos(phi), 3)) * (5 - Math.Pow(Math.Tan(phi), 2) + (9 * eta2));
            double IIIA = ((nu / 720) * Math.Sin(phi) * clat5) * (61 - (58 * tlat2) + tlat4);
            double north = I + ((p * p) * II) + (Math.Pow(p, 4) * III) + (Math.Pow(p, 6) * IIIA);

            // make whole number values
            east = Math.Round(east);   // round to whole number
            north = Math.Round(north); // round to whole number

            // convert to nat grid ref
            Eastings = east;
            Northings = north;
        }


        // a function used in LLtoNE  - that's all I know about it
        private double Marc(double bf0, double n, double phi0, double phi)
        {
            return bf0 * (((1 + n + ((5 / 4) * (n * n)) + ((5 / 4) * (n * n * n))) * (phi - phi0))
               - (((3 * n) + (3 * (n * n)) + ((21 / 8) * (n * n * n))) * (Math.Sin(phi - phi0)) * (Math.Cos(phi + phi0)))
               + ((((15 / 8) * (n * n)) + ((15 / 8) * (n * n * n))) * (Math.Sin(2 * (phi - phi0))) * (Math.Cos(2 * (phi + phi0))))
               - (((35 / 24) * (n * n * n)) * (Math.Sin(3 * (phi - phi0))) * (Math.Cos(3 * (phi + phi0)))));
        }
    }
}
