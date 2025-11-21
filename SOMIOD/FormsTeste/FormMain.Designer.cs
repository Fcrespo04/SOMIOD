namespace ClientProductsApp
{
    partial class FormMain
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.buttonGetAll = new System.Windows.Forms.Button();
            this.richTextBoxShowProducts = new System.Windows.Forms.RichTextBox();
            this.buttonGetProductById = new System.Windows.Forms.Button();
            this.textBoxFilterById = new System.Windows.Forms.TextBox();
            this.textBoxOutput = new System.Windows.Forms.TextBox();
            this.textBoxID = new System.Windows.Forms.TextBox();
            this.textBoxName = new System.Windows.Forms.TextBox();
            this.textBoxCategory = new System.Windows.Forms.TextBox();
            this.textBoxPrice = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.buttonPost = new System.Windows.Forms.Button();
            this.buttonPut = new System.Windows.Forms.Button();
            this.buttonDelete = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // buttonGetAll
            // 
            this.buttonGetAll.Location = new System.Drawing.Point(28, 33);
            this.buttonGetAll.Margin = new System.Windows.Forms.Padding(4);
            this.buttonGetAll.Name = "buttonGetAll";
            this.buttonGetAll.Size = new System.Drawing.Size(148, 28);
            this.buttonGetAll.TabIndex = 0;
            this.buttonGetAll.Text = "Get All Products";
            this.buttonGetAll.UseVisualStyleBackColor = true;
            this.buttonGetAll.Click += new System.EventHandler(this.buttonGetAll_Click);
            // 
            // richTextBoxShowProducts
            // 
            this.richTextBoxShowProducts.Location = new System.Drawing.Point(28, 69);
            this.richTextBoxShowProducts.Margin = new System.Windows.Forms.Padding(4);
            this.richTextBoxShowProducts.Name = "richTextBoxShowProducts";
            this.richTextBoxShowProducts.Size = new System.Drawing.Size(423, 148);
            this.richTextBoxShowProducts.TabIndex = 1;
            this.richTextBoxShowProducts.Text = "";
            // 
            // buttonGetProductById
            // 
            this.buttonGetProductById.Location = new System.Drawing.Point(28, 240);
            this.buttonGetProductById.Margin = new System.Windows.Forms.Padding(4);
            this.buttonGetProductById.Name = "buttonGetProductById";
            this.buttonGetProductById.Size = new System.Drawing.Size(148, 28);
            this.buttonGetProductById.TabIndex = 2;
            this.buttonGetProductById.Text = "Get Product {?}";
            this.buttonGetProductById.UseVisualStyleBackColor = true;
            this.buttonGetProductById.Click += new System.EventHandler(this.buttonGetProductById_Click);
            // 
            // textBoxFilterById
            // 
            this.textBoxFilterById.Location = new System.Drawing.Point(185, 243);
            this.textBoxFilterById.Margin = new System.Windows.Forms.Padding(4);
            this.textBoxFilterById.Name = "textBoxFilterById";
            this.textBoxFilterById.Size = new System.Drawing.Size(132, 22);
            this.textBoxFilterById.TabIndex = 3;
            this.textBoxFilterById.Text = "1";
            // 
            // textBoxOutput
            // 
            this.textBoxOutput.Location = new System.Drawing.Point(28, 277);
            this.textBoxOutput.Margin = new System.Windows.Forms.Padding(4);
            this.textBoxOutput.Multiline = true;
            this.textBoxOutput.Name = "textBoxOutput";
            this.textBoxOutput.Size = new System.Drawing.Size(423, 68);
            this.textBoxOutput.TabIndex = 4;
            // 
            // textBoxID
            // 
            this.textBoxID.BackColor = System.Drawing.SystemColors.Info;
            this.textBoxID.Location = new System.Drawing.Point(557, 76);
            this.textBoxID.Margin = new System.Windows.Forms.Padding(4);
            this.textBoxID.Name = "textBoxID";
            this.textBoxID.Size = new System.Drawing.Size(132, 22);
            this.textBoxID.TabIndex = 5;
            this.textBoxID.TextChanged += new System.EventHandler(this.textBoxID_TextChanged);
            // 
            // textBoxName
            // 
            this.textBoxName.Location = new System.Drawing.Point(557, 109);
            this.textBoxName.Margin = new System.Windows.Forms.Padding(4);
            this.textBoxName.Name = "textBoxName";
            this.textBoxName.Size = new System.Drawing.Size(319, 22);
            this.textBoxName.TabIndex = 6;
            // 
            // textBoxCategory
            // 
            this.textBoxCategory.Location = new System.Drawing.Point(557, 143);
            this.textBoxCategory.Margin = new System.Windows.Forms.Padding(4);
            this.textBoxCategory.Name = "textBoxCategory";
            this.textBoxCategory.Size = new System.Drawing.Size(319, 22);
            this.textBoxCategory.TabIndex = 7;
            // 
            // textBoxPrice
            // 
            this.textBoxPrice.Location = new System.Drawing.Point(557, 176);
            this.textBoxPrice.Margin = new System.Windows.Forms.Padding(4);
            this.textBoxPrice.Name = "textBoxPrice";
            this.textBoxPrice.Size = new System.Drawing.Size(132, 22);
            this.textBoxPrice.TabIndex = 8;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(487, 85);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(21, 16);
            this.label1.TabIndex = 9;
            this.label1.Text = "Id:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(487, 118);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(47, 16);
            this.label2.TabIndex = 10;
            this.label2.Text = "Name:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(487, 152);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(65, 16);
            this.label3.TabIndex = 11;
            this.label3.Text = "Category:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(487, 184);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(41, 16);
            this.label4.TabIndex = 12;
            this.label4.Text = "Price:";
            // 
            // buttonPost
            // 
            this.buttonPost.Location = new System.Drawing.Point(557, 239);
            this.buttonPost.Margin = new System.Windows.Forms.Padding(4);
            this.buttonPost.Name = "buttonPost";
            this.buttonPost.Size = new System.Drawing.Size(121, 28);
            this.buttonPost.TabIndex = 13;
            this.buttonPost.Text = "POST (Create)";
            this.buttonPost.UseVisualStyleBackColor = true;
            this.buttonPost.Click += new System.EventHandler(this.buttonPost_Click);
            // 
            // buttonPut
            // 
            this.buttonPut.Location = new System.Drawing.Point(557, 277);
            this.buttonPut.Margin = new System.Windows.Forms.Padding(4);
            this.buttonPut.Name = "buttonPut";
            this.buttonPut.Size = new System.Drawing.Size(121, 28);
            this.buttonPut.TabIndex = 14;
            this.buttonPut.Text = "PUT (Update)";
            this.buttonPut.UseVisualStyleBackColor = true;
            this.buttonPut.Click += new System.EventHandler(this.buttonPut_Click);
            // 
            // buttonDelete
            // 
            this.buttonDelete.Location = new System.Drawing.Point(557, 314);
            this.buttonDelete.Margin = new System.Windows.Forms.Padding(4);
            this.buttonDelete.Name = "buttonDelete";
            this.buttonDelete.Size = new System.Drawing.Size(121, 28);
            this.buttonDelete.TabIndex = 15;
            this.buttonDelete.Text = "DEL (Delete)";
            this.buttonDelete.UseVisualStyleBackColor = true;
            this.buttonDelete.Click += new System.EventHandler(this.buttonDelete_Click);
            // 
            // FormMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(893, 416);
            this.Controls.Add(this.buttonDelete);
            this.Controls.Add(this.buttonPut);
            this.Controls.Add(this.buttonPost);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBoxPrice);
            this.Controls.Add(this.textBoxCategory);
            this.Controls.Add(this.textBoxName);
            this.Controls.Add(this.textBoxID);
            this.Controls.Add(this.textBoxOutput);
            this.Controls.Add(this.textBoxFilterById);
            this.Controls.Add(this.buttonGetProductById);
            this.Controls.Add(this.richTextBoxShowProducts);
            this.Controls.Add(this.buttonGetAll);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "FormMain";
            this.Text = "RESTFull client application - Products";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonGetAll;
        private System.Windows.Forms.RichTextBox richTextBoxShowProducts;
        private System.Windows.Forms.Button buttonGetProductById;
        private System.Windows.Forms.TextBox textBoxFilterById;
        private System.Windows.Forms.TextBox textBoxOutput;
        private System.Windows.Forms.TextBox textBoxID;
        private System.Windows.Forms.TextBox textBoxName;
        private System.Windows.Forms.TextBox textBoxCategory;
        private System.Windows.Forms.TextBox textBoxPrice;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button buttonPost;
        private System.Windows.Forms.Button buttonPut;
        private System.Windows.Forms.Button buttonDelete;
    }
}

