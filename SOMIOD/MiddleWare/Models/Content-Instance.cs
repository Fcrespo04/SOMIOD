using System;
using System.Xml.Serialization;

namespace MiddleWare.Models
{
    [XmlRoot(ElementName = "content-instance")]
    public class ContentInstance
    {
        [XmlIgnore] // Só é usado na BD
        public int Id { get; set; }

        
        [XmlElement(ElementName = "resource-name")]
        public string Name { get; set; }

        
        [XmlElement(ElementName = "creation-datetime")]
        public string CreationDate { get; set; }

        
        [XmlElement(ElementName = "res-type")]
        public string ResType { get; set; } = "content-instance";

        
        [XmlElement(ElementName = "content")]
        public string Content { get; set; }

        
        [XmlElement(ElementName = "content-type")]
        public string ContentType { get; set; }

        
        [XmlIgnore] // Foreign Key para ligar ao Container Pai
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