using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Schema;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.IO;
using System.Threading.Tasks;

namespace MonitorPrisao
{
    public partial class Monitor_Prisao : Form
    {
        MqttClient mClient = new MqttClient(IPAddress.Parse("127.0.0.1"));
        string baseUri = "http://localhost:59161/";

        HttpListener httpListener;
        string urlWebhook = "http://localhost:5000/api/notificacao/";

        // Define que este programa é responsável pela aplicação "prisao"
        string nomeAppEu = "prisao";

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
            IniciarServidorHTTP();

            // usa esta função no if pois assim garante que só tenta criar os recursos se a prisão existir ou for criada com sucesso
            if (verificarSeExistePrisaoECriar())
            {
                // 2. Verifica e Cria os contentores (celas/porta) e subscrições
                ConfigurarRecursos();

                // 3. Liga o MQTT para receber dados
                LigarMQTT();
            }
        }
        private void IniciarServidorHTTP()
        {
            try
            {
                httpListener = new HttpListener();
                httpListener.Prefixes.Add(urlWebhook);
                httpListener.Start();

                Task.Run(() =>
                {
                    while (httpListener.IsListening)
                    {
                        try
                        {
                            var context = httpListener.GetContext();
                            var request = context.Request;

                            if (request.HttpMethod == "POST")
                            {
                                using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                                {
                                    // 1. Ler o XML Bruto enviado pelo Middleware
                                    string xmlRecebido = reader.ReadToEnd();
                                    System.Diagnostics.Debug.WriteLine("XML HTTP RECEBIDO: " + xmlRecebido);

                                    // 2. VALIDAR E GRAVAR (Requisito do Projeto)
                                    // Usamos a mesma lógica que usas no MQTT
                                    ValidarEGravarNotificacao(xmlRecebido);

                                    if (_xmlValido)
                                    {
                                        // 3. Extrair o comando (ABRIR/FECHAR)
                                        string comando = ExtractFromXml(xmlRecebido);

                                        this.Invoke((MethodInvoker)delegate {
                                            // Nota: Como o HTTP POST não traz o tópico no corpo padrão,
                                            // ou assumimos que este webhook é genérico, ou aplicamos à porta de entrada
                                            // para efeitos de demonstração, ou verificamos se o XML traz o nome do recurso.

                                            // Por segurança, aplicamos à porta de entrada ou fazemos um log
                                            System.Diagnostics.Debug.WriteLine($"Comando HTTP validado: {comando}");

                                            // Exemplo: Atualizar visualmente (Podes ajustar a lógica aqui)
                                            AtualizarVisualCela1(comando);
                                            AtualizarVisualCela2(comando);
                                            AtualizarVisualPortaEntrada(comando);
                                        });
                                    }
                                }
                            }
                            context.Response.StatusCode = (int)HttpStatusCode.OK;
                            context.Response.Close();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Erro no loop HTTP: " + ex.Message);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERRO AO INICIAR SERVIDOR HTTP: " + ex.Message);
            }
        }

        private void VerificarECriarSubHTTP(RestClient client, string nomeApp, string nomeRecurso)
        {
            string nomeSub = "subHttp" + nomeRecurso;

            // Tenta criar diretamente (se já existir dá erro 409 ou ignoramos, para simplificar o código)
            try
            {
                var requestCreateSub = new RestRequest($"api/somiod/{nomeApp}/{nomeRecurso}", Method.Post);
                requestCreateSub.AddHeader("Content-Type", "application/json");

                var jsonSub = new JObject();
                jsonSub.Add("res-type", "subscription");
                jsonSub.Add("resource-name", nomeSub);
                jsonSub.Add("evt", "1"); // Criação
                                         // O TEU ENDPOINT HTTP AQUI:
                jsonSub.Add("endpoint", urlWebhook);

                requestCreateSub.AddParameter("application/json", jsonSub.ToString(), ParameterType.RequestBody);
                var response = client.Execute(requestCreateSub);

                if (response.IsSuccessful) Console.WriteLine($"Subscrição HTTP '{nomeSub}' criada!");
            }
            catch (Exception ex) { Console.WriteLine("Erro sub HTTP: " + ex.Message); }
        }

        private bool verificarSeExistePrisaoECriar()
        {
            var client = new RestClient(baseUri);

            // Verifica se a APP existe
            var requestGet = new RestRequest($"api/somiod/{nomeAppEu}", Method.Get);
            var responseGet = client.Execute(requestGet);

            if (responseGet.StatusCode == HttpStatusCode.NotFound)
            {
                // Se não existe, cria
                try
                {
                    var requestPost = new RestRequest("api/somiod", Method.Post);
                    requestPost.AddHeader("Content-Type", "application/json");
                    requestPost.AddParameter("application/json", $"{{ \"res-type\": \"application\", \"resource-name\": \"{nomeAppEu}\" }}", ParameterType.RequestBody);

                    var responsePost = client.Execute(requestPost);

                    if (!responsePost.IsSuccessful)
                    {
                        MessageBox.Show($"Erro crítico: Não consegui criar a aplicação '{nomeAppEu}'. API: {responsePost.StatusCode}");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erro de conexão (Monitor): " + ex.Message);
                    return false;
                }
            }
            return true;
        }

        // ==========================================
        // 1. VALIDAÇÃO XML
        // ==========================================

        private bool _xmlValido = true;

        private void ValidarEGravarNotificacao(string xmlConteudo)
        {
            _xmlValido = true; // Reset ao estado

            try
            {
                // Configurar as definições de validação
                XmlReaderSettings settings = new XmlReaderSettings();
                settings.ValidationType = ValidationType.Schema;

                // Carrega o XSD 
                settings.Schemas.Add(null, "notification.xsd");

                // Definir o evento que dispara em caso de erro
                settings.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);

                // Ler e Validar o XML
                using (StringReader sr = new StringReader(xmlConteudo))
                using (XmlReader reader = XmlReader.Create(sr, settings))
                {
                    // O método Read() avança pelo XML e dispara o evento se encontrar erros
                    while (reader.Read()) { }
                }

                // Se for válido, serializar para ficheiro (Gravar)
                if (_xmlValido)
                {
                    // Cria pasta de logs se não existir
                    string pathLogs = "NotificacoesRecebidas";
                    if (!Directory.Exists(pathLogs)) Directory.CreateDirectory(pathLogs);

                    // Gera nome único baseado na data
                    string filename = Path.Combine(pathLogs, $"notif_{DateTime.Now.Ticks}.xml");

                    // Grava o ficheiro (Serialização simples de string para ficheiro)
                    File.WriteAllText(filename, xmlConteudo);

                    Console.WriteLine($"[SUCESSO] XML Válido e gravado em: {filename}");
                }
                else
                {
                    Console.WriteLine("[FALHA] O XML recebido não cumpre o Schema XSD.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO SISTEMA] Falha na validação: {ex.Message}");
            }
        }

        // Callback para tratar os erros de validação 
        private void ValidationCallback(object sender, ValidationEventArgs args)
        {
            _xmlValido = false; // Marca como inválido
            Console.WriteLine($"[ERRO DE VALIDAÇÃO] {args.Message}");
        }

        // ==========================================
        // 2. CRIAÇÃO DE RECURSOS (COM VERIFICAÇÃO)
        // ==========================================
        private void ConfigurarRecursos()
        {
            var client = new RestClient(baseUri);

            // Define Strings dos tópicos
            topicoCela1 = $"{nomeAppEu}/cela1";
            topicoCela2 = $"{nomeAppEu}/cela2";
            topicoPortaEntrada = $"{nomeAppEu}/porta";

            // Chama a função que verifica e cria se necessário
            VerificarECriarContainerESub(client, nomeAppEu, "cela1");
            VerificarECriarContainerESub(client, nomeAppEu, "cela2");
            VerificarECriarContainerESub(client, nomeAppEu, "porta");

            VerificarECriarSubHTTP(client, nomeAppEu, "porta");
        }

        private void VerificarECriarContainerESub(RestClient client, string nomeApp, string nomeRecurso)
        {
            // ==============================================================
            // 1. OBTER A LISTA DE FILHOS DO CONTAINER
            // ==============================================================
            var requestGet = new RestRequest($"api/somiod/{nomeApp}/{nomeRecurso}", Method.Get);

            // CORREÇÃO 1: Usar o header igual ao teu Postman ("discovery" com 'y')
            requestGet.AddHeader("somiod-discovery", "subscription");

            var response = client.Execute(requestGet);

            // Se o container nem existir (404), cria-o primeiro
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                var requestCreateC = new RestRequest($"api/somiod/{nomeApp}", Method.Post);
                requestCreateC.AddHeader("Content-Type", "application/json");
                requestCreateC.AddParameter("application/json", $"{{ \"res-type\": \"container\", \"resource-name\": \"{nomeRecurso}\" }}", ParameterType.RequestBody);
                client.Execute(requestCreateC);
                System.Threading.Thread.Sleep(200); // Pausa técnica

                // Repete o pedido para ter a lista
                response = client.Execute(requestGet);
            }

            // ==============================================================
            // 2. VERIFICAR SE O NOME JÁ EXISTE NA LISTA (JSON ARRAY)
            // ==============================================================
            string nomeSub = "sub" + nomeRecurso; // Ex: "subporta"
            bool subExiste = false;

            if (response.IsSuccessful)
            {
                try
                {
                    // O servidor devolve: [".../subs/subporta", ".../subs/subporta_33"]
                    JArray listaSubs = JArray.Parse(response.Content);

                    foreach (var item in listaSubs)
                    {
                        string url = item.ToString();

                        // CORREÇÃO 2: Verifica se o URL termina com o nosso nome
                        // Ignora maiúsculas/minúsculas para ser seguro
                        if (url.EndsWith("/" + nomeSub, StringComparison.OrdinalIgnoreCase) ||
                            url.EndsWith(nomeSub, StringComparison.OrdinalIgnoreCase))
                        {
                            subExiste = true;
                            break; // Encontrámos! Pára de procurar.
                        }
                    }
                }
                catch
                {
                    // Se falhar a ler o JSON, tentamos "à bruta" com Contains
                    if (response.Content.Contains(nomeSub)) subExiste = true;
                }
            }

            // ==============================================================
            // 3. CRIAR (APENAS SE NÃO ENCONTROU NA LISTA)
            // ==============================================================
            if (subExiste)
            {
                Console.WriteLine($"[OK] A subscrição '{nomeSub}' já existe. Ignorando criação.");
            }
            else
            {
                try
                {
                    Console.WriteLine($"A criar: {nomeSub}...");
                    var requestCreateSub = new RestRequest($"api/somiod/{nomeApp}/{nomeRecurso}", Method.Post);
                    requestCreateSub.AddHeader("Content-Type", "application/json");

                    var jsonSub = new JObject();
                    jsonSub.Add("res-type", "subscription");
                    jsonSub.Add("resource-name", nomeSub);
                    jsonSub.Add("evt", "1");
                    jsonSub.Add("endpoint", "mqtt://127.0.0.1:1883");

                    requestCreateSub.AddParameter("application/json", jsonSub.ToString(), ParameterType.RequestBody);
                    client.Execute(requestCreateSub);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erro ao criar sub: " + ex.Message);
                }
            }
        }
        private void LigarMQTT()
        {
            try
            {
                string clientId = Guid.NewGuid().ToString();
                mClient.Connect(clientId);

                if (mClient.IsConnected)
                {
                    mClient.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
                    mClient.Subscribe(
                        new string[] { topicoCela1, topicoCela2, topicoPortaEntrada },
                        new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE }
                    );

                    this.Text = $"Monitor - Gerindo: {nomeAppEu}";

                    // Estado Inicial Visual
                    portaCela1.BackColor = Color.Red;
                    portaCela2.BackColor = Color.Red;
                    portaEntrada.BackColor = Color.Red;
                    estadoCela1.Text = "FECHADO";
                    estadoCela2.Text = "FECHADO";
                    estadoPortaEntrada.Text = "FECHADO";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro MQTT: " + ex.Message);
            }
        }

        // Helper para extrair o comando do XML
        private string ExtractFromXml(string xml)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xml);

                // Tenta encontrar a tag <resource> definida no nosso XSD
                // O "//resource" procura a tag em qualquer profundidade (útil se houver namespace ou root diferente)
                XmlNode node = doc.SelectSingleNode("//resource");

                if (node != null)
                {
                    string conteudo = node.InnerText;
                    return conteudo.Trim().ToUpper();
                }

                return "";
            }
            catch
            {
                // Em caso de erro no XML (ex: mal formado), devolve vazio para não crashar a app
                return "";
            }
        }

        // Receção de mensagens MQTT
        void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            // Converte os bytes recebidos para string XML
            string mensagemXml = Encoding.UTF8.GetString(e.Message);
            string topicoRecebido = e.Topic;

            // =========================================================
            // IMPLEMENTAÇÃO DO REQUISITO DE VALIDAÇÃO E SERIALIZAÇÃO
            // =========================================================
            ValidarEGravarNotificacao(mensagemXml);
            // =========================================================

            // Restante lógica de atualização da UI...
            string mensagem = ExtractFromXml(mensagemXml);

            this.Invoke((MethodInvoker)delegate {

                if (topicoRecebido == topicoCela1)
                {
                    AtualizarVisualCela1(mensagem);
                }
                else if (topicoRecebido == topicoCela2)
                {
                    AtualizarVisualCela2(mensagem);
                }
                else if (topicoRecebido == topicoPortaEntrada)
                {
                    AtualizarVisualPortaEntrada(mensagem);
                }
            });
        }

        // ==========================================
        // ATUALIZAÇÕES VISUAIS
        // ==========================================

        void AtualizarVisualCela1(string comando)
        {
            if (comando == "ABRIR")
            {
                portaCela1.BackColor = Color.Green;
                estadoCela1.Text = "ABERTO";
            }
            else if (comando == "FECHAR")
            {
                portaCela1.BackColor = Color.Red;
                estadoCela1.Text = "FECHADO";
            }
        }

        void AtualizarVisualCela2(string comando)
        {
            if (comando == "ABRIR")
            {
                portaCela2.BackColor = Color.Green;
                estadoCela2.Text = "ABERTO";
            }
            else if (comando == "FECHAR")
            {
                portaCela2.BackColor = Color.Red;
                estadoCela2.Text = "FECHADO";
            }
        }

        void AtualizarVisualPortaEntrada(string comando)
        {
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
    }
}