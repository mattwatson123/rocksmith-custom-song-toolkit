using System;
using System.Xml.Serialization;

namespace RocksmithToolkitLib.Xml
{
    [XmlRoot("vocals", Namespace = "", IsNullable = false)]
    public class Vocals
    {
        [XmlAttribute("count")]
        public Int32 Count { get; set; }

        [XmlElement("vocal")]
        public Vocal[] Vocal { get; set; }
    }

    [XmlType("vocal")]
    public class Vocal
    {
        [XmlAttribute("time")]
        public float Time { get; set; }

        [XmlAttribute("note")]
        public Int32 Note { get; set; }

        [XmlAttribute("length")]
        public float Length { get; set; }

        [XmlAttribute("lyric")] // len 32
        public string Lyric { get; set; }
    }
}