using System;
using System.Xml.Serialization;
using Newtonsoft.Json; // Importante para o [JsonIgnore] e [JsonProperty]

namespace MiddleWare.Models
{
    [XmlRoot(ElementName = "application")]
    public class Application
    {
        // Oculta tanto no JSON como no XML
        [XmlIgnore]
        [JsonIgnore]
        public int Id { get; set; }

        /// <summary>Unique name of the application.</summary>
        // Mapeia o nome para resource-name em ambos os formatos
        [XmlElement(ElementName = "resource-name")]
        [JsonProperty("resource-name")]
        public string Name { get; set; }

        /// <summary>Timestamp of resource creation (ISO format).</summary>
        // Mapeia o nome para creation-datetime
        [XmlElement(ElementName = "creation-datetime")]
        [JsonProperty("creation-datetime")]
        public string CreationDate { get; set; }

        // Mapeia o nome para res-type
        [XmlElement(ElementName = "res-type")]
        [JsonProperty("res-type")]
        public string ResType { get; set; } = "application";

        public Application()
        {
            CreationDate = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
        }

        public Application(int id, string name)
        {
            Id = id;
            Name = name;
            CreationDate = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
        }
    }
}