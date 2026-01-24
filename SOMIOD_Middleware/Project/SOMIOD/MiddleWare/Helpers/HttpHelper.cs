using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MiddleWare.Helpers
{
    public static class HttpHelper
    {
        private static readonly HttpClient client = new HttpClient();

        public static async Task SendPostRequest(string endpointUrl, string xmlPayload)
        {
            try
            {
                var content = new StringContent(xmlPayload, Encoding.UTF8, "application/xml");

                // Envio assíncrono (Fire-and-forget para não bloquear a API)
                // O middleware não fica à espera que o cliente responda.
                await client.PostAsync(endpointUrl, content);

                System.Diagnostics.Debug.WriteLine($"[HTTP SUCCESS] Enviado para {endpointUrl}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HTTP ERROR] Falha ao enviar para {endpointUrl}: {ex.Message}");
            }
        }
    }
}