using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MTConnectAgentCore;

namespace MTConnectAgentCoreWindows
{
    public partial class Form1 : Form
    {
        Agent agent;
        public Form1()
        {
            InitializeComponent();
            agent = new Agent();
            this.button2.Enabled = false;
            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try 
            {
                agent.Start(80);
            }
            catch (AgentException exp )
            {
                String msg = exp.Message;
                if (exp.InnerException != null)
                    msg = msg + "\n" + exp.InnerException.Message;
                MessageBox.Show(this, msg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Dispose();
            }
            this.button1.Enabled = false;
            this.button2.Enabled = true;
            //this.textBox1.Text = "Agent Started";
        
        }

        private void button2_Click(object sender, EventArgs e)
        {
            agent.Stop();
            this.button1.Enabled = true;
            this.button2.Enabled = false;
            //this.textBox2.Text = "Agent Stopped";

        }

             

       
    }
}
