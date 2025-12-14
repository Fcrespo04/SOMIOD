using System;
using System.Windows.Forms;

namespace ControladorPrisao
{
    public partial class FormControlador : Form
    {
        public FormControlador()
        {
            InitializeComponent();

        }

        // =========================================================
        // BOTÃO ABRIR
        // =========================================================
        private async void buttonAbrirCela1_Click(object sender, EventArgs e)
        {
            try
            {
                // Envia comando silenciosamente. Se der erro, o catch apanha.
                await RestHelper.SendCommand("prisao", "cela1", "ABRIR");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao enviar comando: " + ex.Message, "Erro");
            }
        }

        // =========================================================
        // BOTÃO FECHAR
        // =========================================================
        private async void buttonFecharCela1_Click(object sender, EventArgs e)
        {
            try
            {
                await RestHelper.SendCommand("prisao", "cela1", "FECHAR");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao enviar comando: " + ex.Message, "Erro");
            }
        }
    
        // =========================================================
        // BOTÃO ABRIR
        // =========================================================
    

        private async void buttonAbrirPortaEntrada_Click(object sender, EventArgs e)
        {
            try
            {
                // Envia para o container "portaentrada"
                await RestHelper.SendCommand("prisao", "portaentrada", "ABRIR");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao enviar comando: " + ex.Message, "Erro");
            }
        }

        private async void buttonFecharPortaEntrada_Click(object sender, EventArgs e)
        {
            try
            {
                // Envia para o container "portaentrada"
                await RestHelper.SendCommand("prisao", "portaentrada", "FECHAR");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao enviar comando: " + ex.Message, "Erro");
            }
        }

        private async void buttonAbrirCela2_Click_1(object sender, EventArgs e)
        {

            try
            {
                await RestHelper.SendCommand("prisao", "cela2", "ABRIR");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao enviar comando: " + ex.Message, "Erro");
            }
        }

        private async void buttonFecharCela2_Click_1(object sender, EventArgs e)
        {
            try
            {
                await RestHelper.SendCommand("prisao", "cela2", "FECHAR");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao enviar comando: " + ex.Message, "Erro");
            }
        }

        private async void FormControlador_Load(object sender, EventArgs e)
        {
            // O Controlador regista a sua própria existência na BD
            // Isto cria a SEGUNDA aplicação na tabela Application (ex: ID 2)
            try
            {
                await RestHelper.CreateApplication("controlador");
            }
            catch { }
        }
    }
}