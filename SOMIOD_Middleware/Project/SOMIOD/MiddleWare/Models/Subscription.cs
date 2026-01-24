using System;
using System.Xml.Serialization;
using Newtonsoft.Json; // Importante para o mapeamento JSON

namespace MiddleWare.Models
{
    [XmlRoot(ElementName = "subscription")]
    public class Subscription
    {
        [XmlIgnore]
        [JsonIgnore] // Oculta o ID interno no JSON
        public int Id { get; set; }

        /// <summary>Unique name for the subscription.</summary>
        [XmlElement(ElementName = "resource-name")]
        [JsonProperty("resource-name")]
        public string Name { get; set; }

        [XmlElement(ElementName = "creation-datetime")]
        [JsonProperty("creation-datetime")]
        public string CreationDate { get; set; }

        [XmlElement(ElementName = "res-type")]
        [JsonProperty("res-type")]
        public string ResType { get; set; } = "subscription";

        /// <summary>Target endpoint (MQTT broker address or HTTP URL).</summary>
        [XmlElement(ElementName = "endpoint")]
        [JsonProperty("endpoint")]
        public string Endpoint { get; set; }

        /// <summary>Event type: 1 for Creation, 2 for Deletion.</summary>
        [XmlElement(ElementName = "evt")]
        [JsonProperty("evt")]
        public int Event { get; set; }

        [XmlIgnore]
        [JsonIgnore] // Oculta a ligação ao Container pai no JSON
        public int ParentId { get; set; }

        public Subscription()
        {
            CreationDate = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
        }

        public Subscription(int id, string name, string endpoint, int evt, int parentId)
        {
            Id = id;
            Name = name;
            Endpoint = endpoint;
            Event = evt;
            ParentId = parentId;
            CreationDate = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
        }
    }
}