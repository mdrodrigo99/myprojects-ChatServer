using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Windows_Forms_CORE_CHAT_UGH
{
    public partial class Form2 : Form
    {
        public string data;
        public string username;

        public Form2()
        {
            InitializeComponent();
        }

        private void Form2_Load(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            username = textBox1.Text;
            data = DatabaseServer.LoginUser(textBox1.Text, textBox2.Text);

            if (data != null) 
            {
                // close the second form
                this.Close();
            }  
            else 
            {
                textBox2.Text = null;
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            
        }

        private void button2_Click(object sender, EventArgs e)
        {
            textBox1.Text = null;
            textBox2.Text = null;
        }
    }
}
