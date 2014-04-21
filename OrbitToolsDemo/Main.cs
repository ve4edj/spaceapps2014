using System;
using System.Collections.Generic;
using OrbitTools;
using SatelliteTools;
using System.Timers;
using System.Xml;
using System.IO;

namespace OrbitToolsDemo
{
    class MainDemo
    {
        // //////////////////////////////////////////////////////////////////////////
        //
        // Routine to output position and velocity information of satellite
        // in orbit described by TLE information.
        //
        static void PrintPosVel(TLE tle)
        {
            const int Step = 360;

            Orbit orbit = new Orbit(tle);
            List<Eci> coords = new List<Eci>();

            // Calculate position, velocity
            // mpe = "minutes past epoch"
            for (int mpe = 0; mpe <= (Step * 4); mpe += Step)
            {
                // Get the position of the satellite at time "mpe".
                // The coordinates are placed into the variable "eci".
                Eci eci = orbit.GetPosition(mpe);

                // Add the coordinate object to the list
                coords.Add(eci);
            }

            // Print TLE data
            Console.Write("{0}\n", tle.Name);
            Console.Write("{0}\n", tle.Line1);
            Console.Write("{0}\n", tle.Line2);

            // Header
            Console.Write("\n  TSINCE            X                Y                Z\n\n");

            // Iterate over each of the ECI position objects pushed onto the
            // coordinate list, above, printing the ECI position information
            // as we go.
            for (int i = 0; i < coords.Count; i++)
            {
                Eci e = coords[i] as Eci;

                Console.Write("{0,8}.00 {1,16:f8} {2,16:f8} {3,16:f8}\n",
                              i * Step,
                              e.Position.X,
                              e.Position.Y,
                              e.Position.Z);
            }

            Console.Write("\n                  XVEL             YVEL             ZVEL\n\n");

            // Iterate over each of the ECI position objects in the coordinate
            // list again, but this time print the velocity information.
            for (int i = 0; i < coords.Count; i++)
            {
                Eci e = coords[i] as Eci;

                Console.Write("{0,24:f8} {1,16:f8} {2,16:f8}\n",
                              e.Velocity.X,
                              e.Velocity.Y,
                              e.Velocity.Z);
            }
        }

        // /////////////////////////////////////////////////////////////////////
        private static TLEdata dataObject;
        private static string[] satelliteList;
        private static int numTLEs = 1;
        private static string filename;

        static void writeConfigSatelliteFile()
        {
            StreamWriter xml = new StreamWriter(filename);
            System.Xml.Serialization.XmlSerializer xSerializer = new System.Xml.Serialization.XmlSerializer(dataObject.GetType());
            xSerializer.Serialize(xml, dataObject);
            xml.Close();
        }

        static void readConfigSatelliteFile()
        {
            try
            {
                StreamReader xml = new StreamReader(filename);
                dataObject = new TLEdata();
                System.Xml.Serialization.XmlSerializer xSerializer = new System.Xml.Serialization.XmlSerializer(dataObject.GetType());
                dataObject = (TLEdata)(xSerializer.Deserialize(xml));
                xml.Close();
            }
            catch (FileNotFoundException)
            {
                dataObject = new TLEdata("username", "password", new TimeSpan(4, 0, 0), 10); // update once every 4 hours, keep last 10 TLEs
            }
        }

        static void Main(string[] args)
        {
            //TODO: implement iterator for TLEs in satellite class, satellites in datastore class
            //TODO: get cookie that is valid for longer, store in XML

            // Sample code to test the SGP4 and SDP4 implementation. The test
            // TLEs come from the NORAD document "Space Track Report No. 3".

            filename = @"satellites.xml";
            satelliteList = new string[] { "25544", "37820", "39412", "39414", "39506", "36086", "39567", "39570", "39622", "39648", "39571", };
            readConfigSatelliteFile();
            dataObject.authenticate();
            if (dataObject.getLatestN(satelliteList, numTLEs)) { writeConfigSatelliteFile(); }


            // Test SGP4
            //string str1 = "SGP4 Test";
            //string str2 = "1 88888U          80275.98708465  .00073094  13844-3  66816-4 0     8";
            //string str3 = "2 88888  72.8435 115.9689 0086731  52.6988 110.5714 16.05824518   105";
            //Satellite otherthing;
            //dataObject.satelliteList.TryGetValue(satelliteList[1], out otherthing);
            //TLE tle1 = new TLE(otherthing.TLEs[0].name, otherthing.TLEs[0].line1, otherthing.TLEs[0].line2);
            //PrintPosVel(tle1);
            //Console.WriteLine();
            // Test SDP4
            //str1 = "SDP4 Test";
            //str2 = "1 11801U          80230.29629788  .01431103  00000-0  14311-1       8";
            //str3 = "2 11801  46.7916 230.4354 7318036  47.4722  10.4117  2.28537848     6";

            // Example: Define a location on the earth, then determine the look-angle
            // to the SDP4 satellite defined above.
            // First create a site object. Site objects represent a location on the 
            // surface of the earth. Here we arbitrarily select a point on the
            // equator.
            //Site siteEquator = new Site(0.0, -100.0, 0); // 0.00 N, 100.00 W, 0 km altitude
            //Site siteWinnipeg = new Site(49.8994, -97.1392, 0.232);
            Site centerOfEarth = new Site(0, 0, -6378.1);

            Timer timer1 = new Timer(250);
            timer1.Elapsed += delegate { calculateSatellitePosition(satelliteList[0], centerOfEarth); };
            timer1.Start();
            Console.ReadLine();
        }

        public static void calculateSatellitePosition(string catalogID, Site site) {
            // update TLE values
            if (dataObject.getLatestN(satelliteList, numTLEs)) { writeConfigSatelliteFile(); }

            Satellite theSat;
            dataObject.satelliteList.TryGetValue(catalogID, out theSat);
            TLE tle = new TLE(theSat.TLEs[0].name, theSat.TLEs[0].line1, theSat.TLEs[0].line2);

            // Create an orbit object using the TLE object.
            Orbit orbit = new Orbit(tle);

            // Get the location of the satellite from the Orbit object. The 
            // earth-centered inertial information is placed into eciSDP4.
            // Here we ask for the location of the satellite 90 minutes after
            // the TLE epoch.
            //EciTime eciSDP4 = orbitSDP4.GetPosition(90.0);
            // get the position, as of NOW!!!
            EciTime eciSDP4 = orbit.GetPosition((DateTime.UtcNow - orbit.EpochTime).TotalMinutes);

            Console.Write("\n      TSINCE                     X                   Y                   Z\n");
            Console.Write("{0,12} {1,19:f8} {2,19:f8} {3,19:f8}\n",
                              DateTime.UtcNow.ToString(),
                              eciSDP4.Position.X,
                              eciSDP4.Position.Y,
                              eciSDP4.Position.Z);
            Console.Write("\n                              XVEL                 YVEL                 ZVEL\n");
            Console.Write("{0,38:f8} {1,20:f8} {2,20:f8}\n",
                              eciSDP4.Velocity.X,
                              eciSDP4.Velocity.Y,
                              eciSDP4.Velocity.Z);

            // Now get the "look angle" from the site to the satellite. 
            // Note that the ECI object "eciSDP4" has a time associated
            // with the coordinates it contains; this is the time at which
            // the look angle is valid.
            Topo topoLook = site.GetLookAngle(eciSDP4);

            // Print out the results. Note that the Azimuth and Elevation are
            // stored in the CoordTopo object as radians. Here we convert
            // to degrees using Rad2Deg()
            Console.Write("\n{0,15} - AZ: {1:f3}  EL: {2:f3}\n", tle.Name, topoLook.AzimuthDeg, topoLook.ElevationDeg);
            Console.WriteLine("-------------------------------------------------------------------------");
        }
    }
}
