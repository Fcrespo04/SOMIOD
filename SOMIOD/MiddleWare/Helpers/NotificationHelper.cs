using MiddleWare.Models; // Para os modelos de recursos (ContentInstance, etc.)
using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Net;

namespace MiddleWare.Helpers
{
    // DTO para o Payload da Notificação (JSON exigido pelo projeto)
    public class NotificationMessage
    {
        [JsonProperty("evt")]
        public int Event { get; set; }

        [JsonProperty("resource")]
        public object Resource { get; set; }

        [JsonProperty("resource-type")]
        public string ResourceType { get; set; } // Propriedade ResType do modelo

        [JsonProperty("creation-datetime")]
        public string CreationDate { get; set; } // Propriedade CreationDate do modelo
    }

    public static class NotificationHelper
    {
        // Publicação com M2Mqtt
        public static async Task PublishMqttMessage(string endpointUrl, string topic, NotificationMessage message)
        {
            try
            {
                var uri = new Uri(endpointUrl);
                var broker = uri.Host;
                var port = uri.IsDefaultPort ? 1883 : uri.Port;

                // 1. Serializa o payload JSON
                var jsonPayload = JsonConvert.SerializeObject(message);
                var payload = Encoding.UTF8.GetBytes(jsonPayload);

                // 2. Conexão e Publicação (executada numa thread Task)
                await Task.Run(() =>
                {
                    IPAddress brokerIp = IPAddress.Parse(broker);

                    // Inicializa com o IP, Porta e SSL=false (para a porta padrão)
                    var mClient = new MqttClient(brokerIp.ToString(), port, false, null, null, MqttSslProtocols.None);

                    string clientId = Guid.NewGuid().ToString();

                    mClient.Connect(clientId);

                    if (mClient.IsConnected)
                    {
                        mClient.Publish(
                            topic,
                            payload,
                            MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE,
                            false
                        );
                        mClient.Disconnect();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"MQTT Error: Failed to connect to Broker at {endpointUrl}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MQTT Publishing Error: {ex.Message}");
            }
        }
    }
}