using System;
using System.Xml.Serialization;

namespace MiddleWare.Models
{
    [XmlRoot(ElementName = "subscription")]
    public class Subscription
    {
        [XmlIgnore] // Só é usado na BD
        public int Id { get; set; }

        
        [XmlElement(ElementName = "resource-name")]
        public string Name { get; set; }

        
        [XmlElement(ElementName = "creation-datetime")]
        public string CreationDate { get; set; }

        
        [XmlElement(ElementName = "res-type")]
        public string ResType { get; set; } = "subscription";

        
        [XmlElement(ElementName = "endpoint")]
        public string Endpoint { get; set; } // URL para onde enviar a notificação (HTTP ou MQTT)


        [XmlElement(ElementName = "evt")]
        public int Event { get; set; } // 1 = Creation, 2 = Deletion
        // Se for ambos manda 2 pedidos, 1 com cada valor de event


        [XmlIgnore] // Foreign key para ligar ao Container Pai
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