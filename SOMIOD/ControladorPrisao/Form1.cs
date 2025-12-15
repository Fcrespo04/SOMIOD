using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Linq;
using System.Net;
using System.Windows.Forms;

namespace ControladorPrisao
{
    public partial class FormControlador : Form
    {
        string baseUri = "http://localhost:59161/";
        string nomeAplicacaoMonotorizada = null;

        public FormControlador()
        {
            InitializeComponent();

            // Pequeno delay para dar tempo ao Monitor de criar a App e os Containers
            System.Threading.Thread.Sleep(3000);

            DescobrirNomeAplicacao();
            CriarAplicacaoControlador();
        }

        // ==========================================
        // BOTÕES CELA 1
        // ==========================================
        private void buttonAbrirCela1_Click(object sender, EventArgs e)
        {
            EnviarComandoAPI("cela1", "ABRIR");
        }

        private void buttonFecharCela1_Click(object sender, EventArgs e)
        {
            EnviarComandoAPI("cela1", "FECHAR");
        }

        private void buttonAbrirCela2_Click_1(object sender, EventArgs e)
        {
            EnviarComandoAPI("cela2", "ABRIR");

        }

        private void buttonFecharCela2_Click_1(object sender, EventArgs e)
        {
            EnviarComandoAPI("cela2", "FECHAR");

        }

        private void buttonAbrirPortaEntrada_Click(object sender, EventArgs e)
        {
            EnviarComandoAPI("porta", "ABRIR");
        }

        private void buttonFecharPortaEntrada_Click(object sender, EventArgs e)
        {
            EnviarComandoAPI("porta", "FECHAR");
        }


        private void buttonAbrirTudo_Click(object sender, EventArgs e)
        {
            EnviarComandoAPI("cela1", "ABRIR");
            EnviarComandoAPI("cela2", "ABRIR");
            EnviarComandoAPI("porta", "ABRIR");
        }


        private void buttonFecharTudo_Click(object sender, EventArgs e)
        {
            EnviarComandoAPI("cela1", "FECHAR");
            EnviarComandoAPI("cela2", "FECHAR");
            EnviarComandoAPI("porta", "FECHAR");
        }

        // ==========================================
        // LÓGICA DE ENVIO (API)
        // ==========================================
        private void EnviarComandoAPI(string containerDestino, string conteudoComando)
        {
            if (string.IsNullOrEmpty(nomeAplicacaoMonotorizada))
            {
                MessageBox.Show("Erro: Ainda não descobri o nome da aplicação Prisão!");
                return;
            }

            try
            {
                var client = new RestClient(baseUri);
                // Ex: api/somiod/prisao_50/cela2
                string urlDestino = $"api/somiod/{nomeAplicacaoMonotorizada}/{containerDestino}";

                var request = new RestRequest(urlDestino, Method.Post);
                request.AddHeader("Content-Type", "application/json");

                var jsonBody = new JObject();
                jsonBody.Add("res-type", "content-instance");
                jsonBody.Add("content-type", "application/json"); // O teu servidor valida isto
                jsonBody.Add("content", conteudoComando);
                jsonBody.Add("resource-name", "cmd_" + DateTime.Now.Ticks); // Nome único

                request.AddParameter("application/json", jsonBody.ToString(), ParameterType.RequestBody);

                var response = client.Execute(request);

                if (!response.IsSuccessful)
                {
                    // DICA DEBUG: Se a cela 2 não funciona, este erro vai aparecer. Lê o que diz!
                    MessageBox.Show($"Erro API ({containerDestino}): {response.StatusCode} - {response.Content}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao enviar comando: " + ex.Message);
            }
        }

        // ==========================================
        // DESCOBERTA DA APP (DISCOVERY)
        // ==========================================
        private void DescobrirNomeAplicacao()
        {
            try
            {
                var client = new RestClient(baseUri);
                var request = new RestRequest("api/somiod", Method.Get);
                request.AddHeader("somiod-discovery", "application");

                var response = client.Execute(request);

                if (response.IsSuccessful)
                {
                    var listaRecursos = JArray.Parse(response.Content);

                    // Procura a aplicação 'prisao' com o número mais alto (a mais recente)
                    var nomeEncontrado = listaRecursos
                        .Select(t => t.ToString())
                        .Select(path => path.Split('/').Last())
                        .Where(nome => nome.ToLower().Contains("prisao"))
                        .OrderByDescending(nome => {
                            var partes = nome.Split('_');
                            if (partes.Length > 1 && int.TryParse(partes[1], out int id)) return id;
                            return 0;
                        })
                        .FirstOrDefault();

                    if (!string.IsNullOrEmpty(nomeEncontrado))
                    {
                        nomeAplicacaoMonotorizada = nomeEncontrado;
                        this.Text = $"Controlador - A controlar: {nomeEncontrado}"; // Muda o titulo da janela
                    }
                    else
                    {
                        MessageBox.Show("Não encontrei nenhuma aplicação 'prisao'. O Monitor está ligado?");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro no Discovery: " + ex.Message);
            }
        }

        private void CriarAplicacaoControlador()
        {
            // Esta função apenas regista o controlador na API, não afeta o envio de comandos
            try
            {
                var client = new RestClient(baseUri);
                var requestApp = new RestRequest("api/somiod", Method.Post);
                requestApp.AddHeader("Content-Type", "application/json");
                requestApp.AddParameter("application/json", "{ \"res-type\": \"application\", \"resource-name\": \"controlador\" }", ParameterType.RequestBody);
                client.Execute(requestApp);
            }
            catch { }
        }

        private void FormControlador_Load_1(object sender, EventArgs e) { }

    }
}