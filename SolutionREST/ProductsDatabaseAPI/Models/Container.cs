using System;
using System.Xml.Serialization;

namespace MiddleWare.Models
{
    [XmlRoot(ElementName = "container")]
    public class Container
    {
        [XmlIgnore] // Só é usado na BD
        public int Id { get; set; }

        
        [XmlElement(ElementName = "resource-name")]
        public string Name { get; set; }

        
        [XmlElement(ElementName = "creation-datetime")]
        public string CreationDate { get; set; }

        
        [XmlElement(ElementName = "res-type")]
        public string ResType { get; set; } = "container";

        
        [XmlIgnore] // Foreign Key para ligar à Application Pai (apenas BD)
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