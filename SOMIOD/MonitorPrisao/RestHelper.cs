using System;
using System.Net.Http; // Biblioteca nativa do Windows
using System.Text;
using System.Threading.Tasks;

namespace MonitorPrisao
{
    public static class RestHelper
    {
        // Confirma se a porta 59161 é a que aparece no teu browser quando corres o Middleware
        private static readonly string BaseUrl = "http://localhost:59161/api/somiod/";
        private static readonly HttpClient client = new HttpClient();

        // 1. Criar Aplicação
        public static async Task CreateApplication(string appName)
        {
            // JSON feito "à mão" (string) para não precisares de instalar nada
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

            // Formatamos a string JSON manualmente
            string json = string.Format("{{ \"res-type\": \"subscription\", \"resource-name\": \"{0}\", \"endpoint\": \"{1}\", \"evt\": \"{2}\" }}",
                                        subName, endpoint, evt);

            await EnviarPedido(url, json);
        }

        // 4. Enviar Comando
        public static async Task SendCommand(string appName, string containerName, string command)
        {
            string url = BaseUrl + appName + "/" + containerName;
            string nomeUnico = "cmd_" + DateTime.Now.Ticks;
            string xmlContent = "<cmd>" + command + "</cmd>";

            string json = string.Format("{{ \"res-type\": \"content-instance\", \"resource-name\": \"{0}\", \"content\": \"{1}\", \"content-type\": \"application/xml\" }}",
                                        nomeUnico, xmlContent);

            await EnviarPedido(url, json);
        }

        // Função Genérica para enviar
        private static async Task EnviarPedido(string url, string jsonContent)
        {
            try
            {
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(url, content);

                // Se a API responder com erro (ex: 404 ou 500), mostra qual é
                if (!response.IsSuccessStatusCode)
                {
                    string erroApi = await response.Content.ReadAsStringAsync();
                    System.Windows.Forms.MessageBox.Show($"Erro da API ({response.StatusCode}):\n{erroApi}", "Falha no Envio");
                }
            }
            catch (Exception ex)
            {
                // Se nem conseguir ligar à API (ex: porta errada), mostra isto
                System.Windows.Forms.MessageBox.Show($"Não foi possível ligar à API:\n{ex.Message}", "Erro de Conexão");
            }
        }
    }
}