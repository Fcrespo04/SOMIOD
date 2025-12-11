using System;
using System.Text;
using System.Windows.Forms;
using System.Net;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace ControladorPrisao
{
    public partial class FormControlador : Form
    {
        // Cliente MQTT
        MqttClient mClient;
        string topicoCela1 = "prisao/cela1";

        public FormControlador()
        {
            InitializeComponent();

            // Inicializa o cliente MQTT (não conecta logo, só cria o objeto)
            mClient = new MqttClient(IPAddress.Parse("127.0.0.1"));
        }

        private void FormControlador_Load(object sender, EventArgs e)
        {
            try
            {
                // Conecta assim que o Form abre
                string clientId = Guid.NewGuid().ToString();
                mClient.Connect(clientId);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao conectar ao Broker: " + ex.Message);
            }
        }

        // =========================================================
        // BOTÃO ABRIR
        // =========================================================
        private void buttonAbrirCela1_Click(object sender, EventArgs e)
        {
            EnviarComandoMqtt("ABRIR");
        }

        // =========================================================
        // BOTÃO FECHAR
        // =========================================================
        private void buttonFecharCela1_Click(object sender, EventArgs e)
        {
            EnviarComandoMqtt("FECHAR");
        }

        // Função auxiliar simples para publicar
        private void EnviarComandoMqtt(string mensagem)
        {
            if (mClient.IsConnected)
            {
                // Publica diretamente no tópico (sem XML, sem JSON, só texto cru)
                mClient.Publish(
                    topicoCela1,
                    Encoding.UTF8.GetBytes(mensagem),
                    MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE,
                    false // Retained message (false = não guarda a última msg para novos subscritores)
                );
            }
            else
            {
                MessageBox.Show("Não estás conectado ao MQTT broker!");
                // Tenta reconectar caso tenha caído
                try { mClient.Connect(Guid.NewGuid().ToString()); } catch { }
            }
        }

        
    }
}