using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Collections.Specialized;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace SatelliteTools
{
    public class TLEdata : IXmlSerializable
    {
        public Dictionary<string, Satellite> satelliteList { get; private set; }
        private CookieContainer _cookies = new CookieContainer();
        private string _username, _password;
        private TimeSpan _updateInterval;
        private int _TLEhistory;

        public TLEdata() { satelliteList = new Dictionary<string, Satellite>(); }

        public TLEdata(string username, string password, TimeSpan updateInterval, int TLEhistory)
        {
            satelliteList = new Dictionary<string, Satellite>();
            _username = username;
            _password = password;
            _updateInterval = updateInterval;
            _TLEhistory = TLEhistory;
        }

        public void authenticate()
        {
            using (var client = new WebClientEx(_cookies))
            {
                var data = new NameValueCollection
                {
                    { "identity", _username },
                    { "password", _password },
                };
                var response = client.UploadValues(uriBase + "/ajaxauth/login", data);
            }
        }

        private string uriBase = "https://www.space-track.org";

        public class WebClientEx : WebClient
        {
            public WebClientEx(CookieContainer container)
            {
                this.container = container;
            }

            private CookieContainer container;

            protected override WebRequest GetWebRequest(Uri address)
            {
                WebRequest r = base.GetWebRequest(address);
                var request = r as HttpWebRequest;
                if (request != null)
                {
                    request.CookieContainer = container;
                }
                return r;
            }

            protected override WebResponse GetWebResponse(WebRequest request, IAsyncResult result)
            {
                WebResponse response = base.GetWebResponse(request, result);
                ReadCookies(response);
                return response;
            }

            protected override WebResponse GetWebResponse(WebRequest request)
            {
                WebResponse response = base.GetWebResponse(request);
                ReadCookies(response);
                return response;
            }

            private void ReadCookies(WebResponse r)
            {
                var response = r as HttpWebResponse;
                if (response != null)
                {
                    CookieCollection cookies = response.Cookies;
                    container.Add(cookies);
                }
            }
        }   // END WebClient Class

        // Get the TLEs based of an array of NORAD CAT IDs, start date, and end date
        // NOTE - THIS IS UNTESTED AND PROBABLY WON'T WORK
        public string getBetweenDates(string[] norad, DateTime dtstart, DateTime dtend)
        {
            // OLD: // URL to retrieve all the latest tle's for the provided NORAD CAT 
            // OLD: // IDs for the provided Dates
            // OLD: //string predicateValues   = "/class/tle_latest/ORDINAL/1/NORAD_CAT_ID/" + string.Join(",", norad) + "/orderby/NORAD_CAT_ID%20ASC/format/tle";
            // URL to retrieve all the latest 3le's for the provided NORAD CAT 
            // IDs for the provided Dates
            string predicateValues = "/class/tle/EPOCH/" + dtstart.ToString("yyyy-MM-dd--") + dtend.ToString("yyyy-MM-dd") + "/NORAD_CAT_ID/" +string.Join(",", norad) + "/orderby/NORAD_CAT_ID%20ASC/format/3le";
            return getSpaceTrack(predicateValues);
        }

        public bool getLatestN(string[] norad, int elements)
        {
            bool changed = false;
            for (int idx = 0; idx < elements; idx++)
            {
                string updateList = "";
                foreach (string catalogID in norad)
                {
                    Satellite sat;
                    if (satelliteList.TryGetValue(catalogID, out sat))
                    {
                        if (sat.updateNeeded(_updateInterval)) { updateList += catalogID + ","; sat.updating(); }
                    }
                    else { updateList += catalogID + ","; }
                }
                updateList = updateList.Trim(new char[] { ',', });
                if (updateList != string.Empty)
                {
                    string predicateValues = "/class/tle_latest/ORDINAL/" + (idx + 1).ToString() + "/NORAD_CAT_ID/" + updateList + "/format/3le";
                    string[] lines = getSpaceTrack(predicateValues).Split(new string[] { "\r\n0 ", }, StringSplitOptions.RemoveEmptyEntries);
                    for (int stuff = 0; stuff < lines.Length; stuff++)
                    {
                        TLEset tle = new TLEset(lines[stuff].Split(new string[] { "\r\n", }, StringSplitOptions.RemoveEmptyEntries));
                        Satellite sat = null;
                        if (!satelliteList.TryGetValue(tle.getCatalogID(), out sat))
                        {
                            sat = new Satellite(_TLEhistory);
                            satelliteList.Add(tle.getCatalogID(), sat);
                        }
                        if (sat.addTLE(tle)) { changed = true; }
                    }
                }
            }
            return changed;
        }

        private string getSpaceTrack(string predicateValues)
        {
            string requestController = "/basicspacedata";
            string requestAction = "/query";
            string request = uriBase + requestController + requestAction + predicateValues;

            // Create new WebClient object to communicate with the service
            using (var client = new WebClientEx(_cookies))
            {
                // Generate the URL for the API Query and return the response
                var response = client.DownloadData(request);
                return (System.Text.Encoding.Default.GetString(response));
            }
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            reader.MoveToContent();
            _username = reader.GetAttribute("username");
            _password = reader.GetAttribute("password");
            _TLEhistory = int.Parse(reader.GetAttribute("currentTLEhistory"));
            _updateInterval = TimeSpan.Parse(reader.GetAttribute("currentUpdateInterval"));
            reader.ReadStartElement();
            if (!reader.IsEmptyElement)
            {
                reader.ReadStartElement();
                if (!reader.IsEmptyElement)
                {
                    while (reader.MoveToContent() != XmlNodeType.EndElement)
                    {
                        string key = reader.GetAttribute("ID");
                        reader.ReadStartElement();
                        if (!reader.IsEmptyElement)
                        {
                            object sat;
                            XmlSerializer satSerializer = new XmlSerializer(typeof(Satellite));
                            while ((sat = satSerializer.Deserialize(reader)) != null)
                            {
                                satelliteList.Add(key, (Satellite)sat);
                            }
                            reader.ReadEndElement();
                        }
                    }
                    reader.ReadEndElement();
                }
                reader.ReadEndElement();
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("username", _username);
            writer.WriteAttributeString("password", _password);
            writer.WriteAttributeString("currentTLEhistory", _TLEhistory.ToString());
            writer.WriteAttributeString("currentUpdateInterval", _updateInterval.ToString());
            writer.WriteStartElement("satellites");
            foreach (KeyValuePair<string, Satellite> sat in satelliteList)
            {
                writer.WriteStartElement("satellite");
                writer.WriteAttributeString("ID", sat.Key);
                XmlSerializer satSerializer = new XmlSerializer(sat.Value.GetType());
                satSerializer.Serialize(writer, sat.Value);
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }
    }

    public class Satellite : IXmlSerializable
    {
        public List<TLEset> TLEs { get; private set; }
        private DateTime _lastUpdate = DateTime.MinValue;
        private int _TLEhistory;

        public Satellite() { TLEs = new List<TLEset>(); }

        public Satellite(int TLEhistory)
        {
            TLEs = new List<TLEset>();
            _TLEhistory = TLEhistory;
        }

        public bool addTLE(TLEset theElement)
        {
            if (!TLEs.Contains(theElement))
            {
                _lastUpdate = DateTime.Now;
                TLEs.Add(theElement);
                if ((_TLEhistory > 0) && (TLEs.Count > _TLEhistory))
                {
                    TLEs.RemoveAt(0);
                }
                return true;
            }
            return false;
        }

        public bool updateNeeded(TimeSpan updateInterval)
        {
            if ((_lastUpdate + updateInterval) < DateTime.Now) return true;
            return false;
        }

        public void updating()
        {
            _lastUpdate = DateTime.Now;
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            reader.MoveToContent();
            _lastUpdate = DateTime.Parse(reader.GetAttribute("lastUpdate"));
            _TLEhistory = int.Parse(reader.GetAttribute("TLEhistory"));
            reader.ReadStartElement();
            if (!reader.IsEmptyElement)
            {
                object tle;
                XmlSerializer tleSerializer = new XmlSerializer(typeof(TLEset));
                while ((tle = tleSerializer.Deserialize(reader)) != null)
                {
                    TLEs.Add((TLEset)tle);
                }
                reader.ReadEndElement();
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("lastUpdate", _lastUpdate.ToString());
            writer.WriteAttributeString("TLEhistory", _TLEhistory.ToString());
            foreach (TLEset tle in TLEs)
            {
                XmlSerializer tleSerializer = new XmlSerializer(tle.GetType());
                tleSerializer.Serialize(writer, tle);
            }
        }
    }

    public class TLEset : IXmlSerializable
    {
        public string name { get; private set; }
        public string line1 { get; private set; }
        public string line2 { get; private set; }

        public TLEset() { }

        public TLEset(string[] lines)
        {
            name = lines[0].TrimStart(new char[] { '0', }).Trim();
            line1 = lines[1];
            line2 = lines[2];
        }

        public string getCatalogID()
        {
            return line2.Split(new char[] { ' ', })[1];
        }

        public override bool Equals(object other)
        {
            return (this.line1.CompareTo(((TLEset)other).line1) == 0) && (this.line2.CompareTo(((TLEset)other).line2) == 0);
        }

        public override int GetHashCode()
        {
            return new { name, line1, line2 }.GetHashCode();
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            reader.MoveToContent();
	        name = reader.GetAttribute("name");
	        reader.ReadStartElement();
            if (!reader.IsEmptyElement)
	        {
                line1 = reader.ReadElementString("line1");
                line2 = reader.ReadElementString("line2");
		        reader.ReadEndElement();
	        }
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("name", name);
            writer.WriteElementString("line1", line1);
            writer.WriteElementString("line2", line2);
        }
    }
}
