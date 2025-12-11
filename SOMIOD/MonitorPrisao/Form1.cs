using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace MonitorPrisao
{
    public partial class Monitor_Prisao : Form
    {
        // IP do Broker (Mosquitto)
        MqttClient mClient = new MqttClient(IPAddress.Parse("127.0.0.1"));
        
        // Tópico onde vamos "escutar" as ordens
        string topicoCela1 = "prisao/cela1"; 

        public Monitor_Prisao()
        {
            InitializeComponent();
        }

        private void Monitor_Prisao_Load(object sender, EventArgs e)
        {
            try 
            {
                // Conectar ao Broker
                string clientId = Guid.NewGuid().ToString();
                mClient.Connect(clientId);

                if (mClient.IsConnected)
                {
                    // Subscreve o evento para saber quando chega mensagem
                    mClient.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
                    
                    // Subscreve o tópico
                    mClient.Subscribe(
                        new string[] { topicoCela1 }, 
                        new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE }
                    );

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao ligar ao MQTT: " + ex.Message);
            }
        }

        void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            // Converte os bytes recebidos para Texto
            string mensagem = Encoding.UTF8.GetString(e.Message);

            // Invoke para mexer na UI (cores/texto)
            this.Invoke((MethodInvoker)delegate {

                // Já não vem XML, vem texto limpo ("ABRIR" ou "FECHAR")
                AtualizarVisualCela1(mensagem);

            });
        }

        void AtualizarVisualCela1(string estado)
        {
            if (estado == "ABRIR")
            {
                portaCela1.BackColor = Color.Green; 
                estadoCela1.Text = "ABERTO";
            }
            else if (estado == "FECHAR")
            {
                portaCela1.BackColor = Color.Red; 
                estadoCela1.Text = "FECHADO";
            }
        }
        
        // Quando fechares o form, é boa prática desconectar
        private void Monitor_Prisao_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (mClient.IsConnected)
            {
                mClient.Disconnect();
            }
        }
    }
}