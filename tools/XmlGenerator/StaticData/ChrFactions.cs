using System.Xml.Serialization;
using EVEMon.XmlGenerator.Interfaces;

namespace EVEMon.XmlGenerator.StaticData
{
    public sealed class ChrFactions : IHasID
    {
        [XmlElement("factionID")]
        public int ID { get; set; }

        [XmlElement("factionName")]
        public string FactionName { get; set; }

        [XmlElement("description")]
        public string Description { get; set; }

        [XmlElement("raceID")]
        public int RaceID { get; set; }

        [XmlElement("solarSystemID")]
        public int SolarSystemID { get; set; }

        [XmlElement("corporationID")]
        public int CorporationID { get; set; }

        [XmlElement("sizeFactor")]
        public double SizeFactor { get; set; }

        [XmlElement("stationCount")]
        public short StationCount { get; set; }

        [XmlElement("stationSystemCount")]
        public short StationSystemCount { get; set; }

        [XmlElement("militiaCorporationID")]
        public int? MilitiaCorporationID { get; set; }

        [XmlElement("iconID")]
        public int IconID { get; set; }
    }
}