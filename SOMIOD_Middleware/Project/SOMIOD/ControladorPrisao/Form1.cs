using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Net;
using System.Windows.Forms;

namespace ControladorPrisao
{
    public partial class FormControlador : Form
    {
        string baseUri = "http://localhost:59161/";

        // Definições
        string nomeAppEu = "controlador";
        string nomeAppAlvo = "prisao";    

        public FormControlador()
        {
            InitializeComponent();

            // Criação da App controlador, se não existir
            garantirAExistenciaDeControlador();

            this.Text = $"Controlador ({nomeAppEu}) -> A comandar: {nomeAppAlvo}";
        }

        private void garantirAExistenciaDeControlador()
        {
            var client = new RestClient(baseUri);

            // 1. Verificar a existencia do "controlador" 
            var requestGet = new RestRequest($"api/somiod/{nomeAppEu}", Method.Get);
            var responseGet = client.Execute(requestGet);

            if (responseGet.StatusCode == HttpStatusCode.NotFound)
            {
                // 2. Se não existe, cria
                try
                {
                    var requestPost = new RestRequest("api/somiod", Method.Post);
                    requestPost.AddHeader("Content-Type", "application/json");
                    requestPost.AddParameter("application/json", $"{{ \"res-type\": \"application\", \"resource-name\": \"{nomeAppEu}\" }}", ParameterType.RequestBody);

                    var responsePost = client.Execute(requestPost);

                    if (!responsePost.IsSuccessful)
                        MessageBox.Show($"Erro ao registar o Controlador: {responsePost.StatusCode}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erro de conexão (Controlador): " + ex.Message);
                }
            }
        }


        private void EnviarComandoAPI(string containerDestino, string conteudoComando)
        {
            try
            {
                var client = new RestClient(baseUri);
                // O destino é fixo: api/somiod/prisao/cela1
                string urlDestino = $"api/somiod/{nomeAppAlvo}/{containerDestino}";

                var request = new RestRequest(urlDestino, Method.Post);
                request.AddHeader("Content-Type", "application/json");

                var jsonBody = new JObject();
                jsonBody.Add("res-type", "content-instance");
                jsonBody.Add("content-type", "application/json");
                jsonBody.Add("content", conteudoComando);
                jsonBody.Add("resource-name", "cmd_" + DateTime.Now.Ticks);

                request.AddParameter("application/json", jsonBody.ToString(), ParameterType.RequestBody);

                var response = client.Execute(request);

                // ERRO 404 = aplicação "prisao" ainda nao existe
                if (!response.IsSuccessful)
                {
                    if (response.StatusCode == HttpStatusCode.NotFound)
                        MessageBox.Show($"Erro: A aplicação '{nomeAppAlvo}' ou o contentor '{containerDestino}' não existem. O Monitor está ligado?");
                    else
                        MessageBox.Show($"Erro API: {response.StatusCode} - {response.Content}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao enviar comando: " + ex.Message);
            }
        }

        private void buttonAbrirCela1_Click(object sender, EventArgs e) => EnviarComandoAPI("cela1", "ABRIR");
        private void buttonFecharCela1_Click(object sender, EventArgs e) => EnviarComandoAPI("cela1", "FECHAR");
        private void buttonAbrirCela2_Click_1(object sender, EventArgs e) => EnviarComandoAPI("cela2", "ABRIR");
        private void buttonFecharCela2_Click_1(object sender, EventArgs e) => EnviarComandoAPI("cela2", "FECHAR");
        private void buttonAbrirPortaEntrada_Click(object sender, EventArgs e) => EnviarComandoAPI("porta", "ABRIR");
        private void buttonFecharPortaEntrada_Click(object sender, EventArgs e) => EnviarComandoAPI("porta", "FECHAR");

        private void buttonAbrirTudo_Click(object sender, EventArgs e)
        {
            EnviarComandoAPI("cela1", "ABRIR"); EnviarComandoAPI("cela2", "ABRIR"); EnviarComandoAPI("porta", "ABRIR");
        }
        private void buttonFecharTudo_Click(object sender, EventArgs e)
        {
            EnviarComandoAPI("cela1", "FECHAR"); EnviarComandoAPI("cela2", "FECHAR"); EnviarComandoAPI("porta", "FECHAR");
        }

        private void FormControlador_Load_1(object sender, EventArgs e) { }
    }
}