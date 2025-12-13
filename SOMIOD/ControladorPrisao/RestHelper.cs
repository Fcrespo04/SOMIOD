using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms; // Necessário para MessageBox

namespace ControladorPrisao
{
    public static class RestHelper
    {
        // Certifica-te que esta porta é a mesma que vês no browser (MiddleWare)
        private static readonly string BaseUrl = "http://localhost:59161/api/somiod/";
        private static readonly HttpClient client = new HttpClient();

        // 1. Criar Aplicação
        public static async Task CreateApplication(string appName)
        {
            string json = "{ \"res-type\": \"application\", \"resource-name\": \"" + appName + "\" }";
            await EnviarPedido(BaseUrl, json);
        }

        // 2. Criar Container
        public static async Task CreateContainer(string appName, string containerName)
        {
            string url = BaseUrl + appName;
            string json = "{ \"res-type\": \"container\", \"resource-name\": \"" + containerName + "\" }";
            await EnviarPedido(url, json);
        }

        // 3. Criar Subscrição
        public static async Task CreateSubscription(string appName, string containerName, string subName, string endpoint, string evt)
        {
            string url = BaseUrl + appName + "/" + containerName;
            string json = string.Format("{{ \"res-type\": \"subscription\", \"resource-name\": \"{0}\", \"endpoint\": \"{1}\", \"evt\": \"{2}\" }}",
                                        subName, endpoint, evt);
            await EnviarPedido(url, json);
        }

        // 4. Enviar Comando (Content-Instance)
        public static async Task SendCommand(string appName, string containerName, string command)
        {
            string url = BaseUrl + appName + "/" + containerName;
            string nomeUnico = "cmd_" + DateTime.Now.Ticks;
            string xmlContent = "<cmd>" + command + "</cmd>";

            string json = string.Format("{{ \"res-type\": \"content-instance\", \"resource-name\": \"{0}\", \"content\": \"{1}\", \"content-type\": \"application/xml\" }}",
                                        nomeUnico, xmlContent);

            await EnviarPedido(url, json);
        }

        // Função Genérica de Envio
        private static async Task EnviarPedido(string url, string jsonContent)
        {
            try
            {
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(url, content);

                // Só mostra janela se houver ERRO. Se for sucesso, é silencioso.
                if (!response.IsSuccessStatusCode)
                {
                    string msgErro = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Erro da API ({response.StatusCode}):\n{msgErro}", "Erro API");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Não foi possível contactar a API:\n{ex.Message}", "Erro de Conexão");
            }
        }
    }
}