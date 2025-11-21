using System;
using System.Xml.Serialization;

namespace MiddleWare.Models
{
    [XmlRoot(ElementName = "application")]
    public class Application
    {

        [XmlIgnore] // Só é usado na BD 
        public int Id { get; set; }

        
        [XmlElement(ElementName = "resource-name")]
        public string Name { get; set; }

        
        [XmlElement(ElementName = "creation-datetime")]
        public string CreationDate { get; set; }

        
        [XmlElement(ElementName = "res-type")]
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