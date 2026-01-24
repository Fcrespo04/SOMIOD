using System;
using System.Xml.Serialization;
using Newtonsoft.Json; // Essencial para o mapeamento JSON

namespace MiddleWare.Models
{
    [XmlRoot(ElementName = "content-instance")]
    public class ContentInstance
    {
        [XmlIgnore]
        [JsonIgnore] // Oculta o ID da base de dados no JSON
        public int Id { get; set; }

        [XmlElement(ElementName = "resource-name")]
        [JsonProperty("resource-name")]
        /// <summary>Unique name for this data record.</summary>
        public string Name { get; set; }

        [XmlElement(ElementName = "creation-datetime")]
        [JsonProperty("creation-datetime")]
        public string CreationDate { get; set; }

        [XmlElement(ElementName = "res-type")]
        [JsonProperty("res-type")]
        public string ResType { get; set; } = "content-instance";

        [XmlElement(ElementName = "content")]
        [JsonProperty("content")]
        /// <summary>The actual data payload stored.</summary>
        public string Content { get; set; }

        [XmlElement(ElementName = "content-type")]
        [JsonProperty("content-type")]
        /// <summary>Format of the content (e.g., application/json, application/xml, text/plain).</summary>
        public string ContentType { get; set; }

        [XmlIgnore]
        [JsonIgnore] // Oculta a ligação ao Container pai no JSON
        public int ParentId { get; set; }

        public ContentInstance()
        {
            CreationDate = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
        }

        public ContentInstance(int id, string name, string content, string contentType, int parentId)
        {
            Id = id;
            Name = name;
            Content = content;
            ContentType = contentType;
            ParentId = parentId;
            CreationDate = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
        }
    }
}