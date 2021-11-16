using System;
using System.Windows.Forms;

namespace CheckConsecutiveStudioRuns
{
    public partial class Form1 : Form
    {
        public Form1(string displayText, string helpURL=null)
        {
            InitializeComponent();
            this.label1.Text = displayText;
            if (string.IsNullOrEmpty(helpURL))
                this.lnkHelp.Visible = false;
            else
                this.lnkHelp.Text = helpURL;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MinimizeBox = false;
            this.MaximizeBox = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
