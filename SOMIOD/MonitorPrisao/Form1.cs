using RestSharp;
using System;
using System.Drawing;
using System.Net;
using System.Text;
using System.Windows.Forms;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using Newtonsoft.Json.Linq;

namespace MonitorPrisao
{
    public partial class Monitor_Prisao : Form
    {
        MqttClient mClient = new MqttClient(IPAddress.Parse("127.0.0.1"));

        // Variáveis para guardar os tópicos
        string topicoCela1 = "";
        string topicoCela2 = "";
        string topicoPortaEntrada = "";

        public Monitor_Prisao()
        {
            InitializeComponent();
        }

        private void Monitor_Prisao_Load(object sender, EventArgs e)
        {
            CriarEstruturaNaAPI();
            try
            {
                if (string.IsNullOrEmpty(topicoCela1) || 
                    string.IsNullOrEmpty(topicoCela2) || 
                    string.IsNullOrEmpty(topicoPortaEntrada))
                {
                    MessageBox.Show("ERRO: Tópicos não definidos. Verifique a API.");
                    return;
                }

                string clientId = Guid.NewGuid().ToString();
                mClient.Connect(clientId);

                if (mClient.IsConnected)
                {
                    mClient.MqttMsgPublishReceived += client_MqttMsgPublishReceived;

                    // *** SUBSCREVER AOS 3 TÓPICOS ***
                    mClient.Subscribe(
                        new string[] { topicoCela1, topicoCela2, topicoPortaEntrada }, 
                        new byte[] { 
                            MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, 
                            MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE,
                            MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE 
                        }
                    );

                    this.Text = "Monitor Prisão - LIGADO";
                    
                    // Cores iniciais
                    portaCela1.BackColor = Color.Gray;
                    portaCela2.BackColor = Color.Gray;
                    portaEntrada.BackColor = Color.Gray; // <--- NOVO
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro CRÍTICO ao ligar ao MQTT: " + ex.Message);
            }
        }

        void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            string mensagem = Encoding.UTF8.GetString(e.Message);
            string topicoRecebido = e.Topic;

            this.Invoke((MethodInvoker)delegate {
                
                // Encaminha para a função visual correta
                if (topicoRecebido == topicoCela1)
                {
                    AtualizarVisualCela1(mensagem);
                }
                else if (topicoRecebido == topicoCela2)
                {
                    AtualizarVisualCela2(mensagem);
                }
                else if (topicoRecebido == topicoPortaEntrada) // <--- NOVO
                {
                    AtualizarVisualPortaEntrada(mensagem);
                }
            });
        }

        // ================== VISUAL CELA 1 ==================
        void AtualizarVisualCela1(string comando)
        {
            if (comando == "ABRIR") {
                portaCela1.BackColor = Color.Green;
                estadoCela1.Text = "ABERTO";
            } else if (comando == "FECHAR") {
                portaCela1.BackColor = Color.Red;
                estadoCela1.Text = "FECHADO";
            }
        }

        // ================== VISUAL CELA 2 ==================
        void AtualizarVisualCela2(string comando)
        {
            if (comando == "ABRIR") {
                portaCela2.BackColor = Color.Green;
                estadoCela2.Text = "ABERTO";
            } else if (comando == "FECHAR") {
                portaCela2.BackColor = Color.Red;
                estadoCela2.Text = "FECHADO";
            }
        }

        // ================== VISUAL PORTA ENTRADA (NOVO) ==================
        void AtualizarVisualPortaEntrada(string comando)
        {
            // Nota: Podes mudar as mensagens aqui se a porta da entrada tiver comandos diferentes
            if (comando == "ABRIR") 
            {
                portaEntrada.BackColor = Color.Green;
                estadoPortaEntrada.Text = "ABERTO";
            }
            else if (comando == "FECHAR") 
            {
                portaEntrada.BackColor = Color.Red;
                estadoPortaEntrada.Text = "FECHADO";
            }
        }

        private void Monitor_Prisao_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (mClient != null && mClient.IsConnected) mClient.Disconnect();
        }

        private void CriarEstruturaNaAPI()
        {
            string baseUri = "http://localhost:59161/"; 
            var client = new RestClient(baseUri);

            // 1. APP
            var requestApp = new RestRequest("api/somiod", Method.Post);
            requestApp.AddHeader("Content-Type", "application/json");
            requestApp.AddParameter("application/json", "{ \"res-type\": \"application\", \"resource-name\": \"prisao\" }", ParameterType.RequestBody);

            var responseApp = client.Execute(requestApp);

            if (responseApp.StatusCode == HttpStatusCode.OK || responseApp.StatusCode == HttpStatusCode.Created)
            {
                JObject jsonResposta = JObject.Parse(responseApp.Content);
                string nomeAPP = jsonResposta["Name"]?.ToString();

                // Define os tópicos
                topicoCela1 = $"{nomeAPP}/cela1";
                topicoCela2 = $"{nomeAPP}/cela2"; 
                topicoPortaEntrada = $"{nomeAPP}/porta"; // <--- NOVO (Resource Name: portaentrada)

                // 2. CRIA CONTAINERS E SUBSCRIÇÕES (Chama a função auxiliar 3 vezes)
                CriarContainerESubscricao(client, nomeAPP, "cela1");
                CriarContainerESubscricao(client, nomeAPP, "cela2");
                CriarContainerESubscricao(client, nomeAPP, "porta"); // <--- NOVO
            }
            else
            {
                 if (responseApp.StatusCode != HttpStatusCode.Conflict)
                    MessageBox.Show("Erro API: " + responseApp.Content);
            }
        }

        // Função Auxiliar (Mantém-se igual, mas agora é usada para a porta de entrada também)
        private void CriarContainerESubscricao(RestClient client, string nomeApp, string nomeRecurso)
        {
            var requestContainer = new RestRequest($"api/somiod/{nomeApp}", Method.Post);
            requestContainer.AddHeader("Content-Type", "application/json");
            requestContainer.AddParameter("application/json", $"{{ \"res-type\": \"container\", \"resource-name\": \"{nomeRecurso}\" }}", ParameterType.RequestBody);
            
            var response = client.Execute(requestContainer);

            if (response.IsSuccessful)
            {
                try
                {
                    var requestSub = new RestRequest($"api/somiod/{nomeApp}/{nomeRecurso}", Method.Post);
                    requestSub.AddHeader("Content-Type", "application/json");
                    var jsonSub = new JObject();
                    jsonSub.Add("res-type", "subscription");
                    jsonSub.Add("resource-name", "sub" + nomeRecurso);
                    jsonSub.Add("evt", "1"); 
                    jsonSub.Add("endpoint", "mqtt://127.0.0.1:1883");

                    requestSub.AddParameter("application/json", jsonSub.ToString(), ParameterType.RequestBody);
                    client.Execute(requestSub);
                }
                catch { }
            }
        }
    }
}