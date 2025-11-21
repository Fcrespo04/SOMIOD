using RestSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO; //Stream
using System.Linq;
using System.Net; //HttpWebRequest
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

//JavaScriptSerializer --> necessário criar referencia para System.Web.Extensions caso pretendam usar para serializar objetos em JSON

namespace ClientProductsApp
{
    public partial class FormMain : Form
    {

        string baseRUI = @"http://localhost:59161"; //TODO: needs to be updated!

        public FormMain()
        {
            InitializeComponent();
        }

        private void buttonGetAll_Click(object sender, EventArgs e)
        {
            var client = new RestClient(baseRUI);
            
            var request = new RestRequest("api/products", Method.Get);
            request.RequestFormat = DataFormat.Json;
            
            var response = client.Execute<List<Product>>(request).Data;

            richTextBoxShowProducts.Clear();
            foreach (Product item in response)
            {
                richTextBoxShowProducts.AppendText($" {item.Id} : {item.Name} \t {item.Category} : {item.Price}\n");
            }


        }

        private void textBoxID_TextChanged(object sender, EventArgs e)
        {

        }

        private void buttonGetProductById_Click(object sender, EventArgs e)
        {
            int id = int.Parse(textBoxFilterById.Text);

            var client = new RestClient(baseRUI);

            var request = new RestRequest("api/products/{idProd}", Method.Get);
            request.AddUrlSegment("idProd", id);
            // esta linha faz "api/products/ + {id}"

            request.AddHeader("Accept", "application/json");

            var response = client.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var serializer = new JavaScriptSerializer();
                Product prod = serializer.Deserialize<Product>(response.Content);

                textBoxOutput.Text = $" {prod.Id} : {prod.Name} : {prod.Category}\n";
            }
            else
            {
                textBoxOutput.Text = "";
                MessageBox.Show("Product not found!");  
            }
        }

        private void buttonPost_Click(object sender, EventArgs e)
        {
            Product prod = new Product
            {
                Id = 0,
                Name = textBoxName.Text,
                Category = textBoxCategory.Text,
                Price = decimal.Parse(textBoxPrice.Text)
            };

            var client = new RestClient(baseRUI);

            var request = new RestRequest("api/products", Method.Post);

            request.AddHeader("Accept", "application/json");
            request.AddHeader("Content-Type", "application/json");

            // igual a -> request.AddObject(prod);
            request.AddBody(prod);

            var response = client.Execute(request);

            MessageBox.Show($"{response.StatusCode} : {response.StatusDescription}");
        }

        private void buttonPut_Click(object sender, EventArgs e)
        {
            Product prod = new Product
            {
                Name = textBoxName.Text,
                Category = textBoxCategory.Text,
                Price = decimal.Parse(textBoxPrice.Text)
            };

            int id = int.Parse(textBoxID.Text);

            var client = new RestClient(baseRUI);

            var request = new RestRequest("api/products/{idProd}", Method.Put);
            request.AddUrlSegment("idProd", id);
            // esta linha faz "api/products/ + {id}"

            request.AddHeader("Accept", "application/json");
            request.AddHeader("Content-Type", "application/json");

            // igual a -> request.AddObject(prod);
            request.AddBody(prod);

            var response = client.Execute(request);

            MessageBox.Show($"{response.StatusCode} : {response.StatusDescription}");
        }

        private void buttonDelete_Click(object sender, EventArgs e)
        {
            int id = int.Parse(textBoxID.Text);

            var client = new RestClient(baseRUI);

            var request = new RestRequest("api/products/{idProd}", Method.Delete);
            request.AddUrlSegment("idProd", id);
            // esta linha faz "api/products/ + {id}"

            request.AddHeader("Accept", "application/json");
            request.AddHeader("Content-Type", "application/json");

            var response = client.Execute(request);

            MessageBox.Show($"{response.StatusCode} : {response.StatusDescription}");
        }
    }
}
