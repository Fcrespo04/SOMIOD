using System;
using System.Xml.Serialization;
using Newtonsoft.Json; // Adicionado para suportar atributos JSON

namespace MiddleWare.Models
{
    [XmlRoot(ElementName = "container")]
    public class Container
    {
        [XmlIgnore]
        [JsonIgnore] // Oculta o ID no JSON
        public int Id { get; set; }

        [XmlElement(ElementName = "resource-name")]
        [JsonProperty("resource-name")] // Garante o nome correto no JSON
        /// <summary>Unique name of the container within the application.</summary>
        public string Name { get; set; }

        [XmlElement(ElementName = "creation-datetime")]
        [JsonProperty("creation-datetime")] // Garante o nome correto no JSON
        /// <summary>Timestamp of resource creation (ISO format).</summary>
        public string CreationDate { get; set; }

        [XmlElement(ElementName = "res-type")]
        [JsonProperty("res-type")] // Garante o nome correto no JSON
        public string ResType { get; set; } = "container";

        [XmlIgnore]
        [JsonIgnore] // Oculta o ParentId no JSON
        public int ParentId { get; set; }

        public Container()
        {
            CreationDate = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
        }

        public Container(int id, string name, int parentId)
        {
            Id = id;
            Name = name;
            ParentId = parentId;
            CreationDate = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
        }
    }
}