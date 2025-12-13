using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace MonitorPrisao
{
    public partial class Monitor_Prisao : Form
    {
        MqttClient mClient;

        public Monitor_Prisao()
        {
            InitializeComponent();
        }

        private async void Monitor_Prisao_Load(object sender, EventArgs e)
        {
            try
            {
                // 1. Criar a Aplicação Principal
                await RestHelper.CreateApplication("prisao");

                // 2. Criar Contentores e Subscrições para CADA porta
                // CELA 1
                await RestHelper.CreateContainer("prisao", "cela1");
                await RestHelper.CreateSubscription("prisao", "cela1", "subCela1", "mqtt://127.0.0.1/prisao/cela1", "1");

                // CELA 2
                await RestHelper.CreateContainer("prisao", "cela2");
                await RestHelper.CreateSubscription("prisao", "cela2", "subCela2", "mqtt://127.0.0.1/prisao/cela2", "1");

                // PORTA ENTRADA
                await RestHelper.CreateContainer("prisao", "portaentrada");
                await RestHelper.CreateSubscription("prisao", "portaentrada", "subEntrada", "mqtt://127.0.0.1/prisao/portaentrada", "1");
            }
            catch
            {
                // Ignora erros de criação se já existirem
            }

            ConectarMQTT();
        }

        private void ConectarMQTT()
        {
            try
            {
                // Usar "localhost" no Monitor (IPv6/IPv4 friendly)
                mClient = new MqttClient("localhost");
                mClient.Connect(Guid.NewGuid().ToString());

                if (mClient.IsConnected)
                {
                    mClient.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
                    // Subscreve a TUDO (#) para apanhar mensagens de todas as celas
                    mClient.Subscribe(new string[] { "#" }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao ligar ao MQTT: " + ex.Message);
            }
        }

        void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            string msg = Encoding.UTF8.GetString(e.Message).ToUpper(); // Converter logo para maiúsculas
            string topico = e.Topic;

            this.Invoke((MethodInvoker)delegate
            {
                // Lógica para CELA 1
                if (topico.Contains("cela1"))
                {
                    if (msg.Contains("ABRIR"))
                    {
                        portaCela1.BackColor = Color.Green;
                        estadoCela1.Text = "ABERTO";
                    }
                    else if (msg.Contains("FECHAR"))
                    {
                        portaCela1.BackColor = Color.Red;
                        estadoCela1.Text = "FECHADO";
                    }
                }
                // Lógica para CELA 2
                else if (topico.Contains("cela2"))
                {
                    if (msg.Contains("ABRIR"))
                    {
                        portaCela2.BackColor = Color.Green;
                        estadoCela2.Text = "ABERTO";
                    }
                    else if (msg.Contains("FECHAR"))
                    {
                        portaCela2.BackColor = Color.Red;
                        estadoCela2.Text = "FECHADO";
                    }
                }
                // Lógica para PORTA DA ENTRADA
                else if (topico.Contains("portaentrada"))
                {
                    if (msg.Contains("ABRIR"))
                    {
                        // Certifica-te que tens este painel no Design com este nome
                        portaEntrada.BackColor = Color.Green;
                        estadoPortaEntrada.Text = "ABERTO";
                    }
                    else if (msg.Contains("FECHAR"))
                    {
                        portaEntrada.BackColor = Color.Red;
                        estadoPortaEntrada.Text = "FECHADO";
                    }
                }
            });
        }
        private void estadoCela1_Click(object sender, EventArgs e) { }

    
        private void Monitor_Prisao_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (mClient != null && mClient.IsConnected)
            {
                mClient.Disconnect();
            }
        }
    }
}