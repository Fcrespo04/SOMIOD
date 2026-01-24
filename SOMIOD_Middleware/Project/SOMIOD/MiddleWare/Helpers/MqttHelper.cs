using System;
using System.Text;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace MiddleWare.Helpers
{
    public static class MqttHelper
    {
        public static void PublishToBroker(string endpoint, string topico, string mensagem)
        {
            try
            {
                // Verifica se o tópico é válido antes de tentar
                if (string.IsNullOrEmpty(topico))
                {
                    System.Diagnostics.Debug.WriteLine("[ERRO MQTT] Tópico vazio. Abortar envio.");
                    return;
                }

                string brokerIp = "127.0.0.1";
                MqttClient client = new MqttClient(brokerIp);
                string clientId = Guid.NewGuid().ToString();

                client.Connect(clientId);

                if (client.IsConnected)
                {
                    byte[] msgBytes = Encoding.UTF8.GetBytes(mensagem);

                    // Publica a mensagem
                    client.Publish(topico, msgBytes, MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, false);

                    // =========================================================
                    // A CORREÇÃO MÁGICA:
                    // Dá tempo à biblioteca para enviar os bytes antes de cortar o cabo.
                    // =========================================================
                    System.Threading.Thread.Sleep(500); // Pausa de meio segundo

                    if (client.IsConnected)
                    {
                        client.Disconnect();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERRO MQTT] Falha ao enviar: {ex.Message}");
            }
        }
    }
}