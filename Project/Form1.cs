using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Project
{
    public partial class Form1 : Form
    {
        Main main;
        public Form1(Main m)
        {
            main = m;
            InitializeComponent();

        }
        int x = 0;

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (this.Width < 302)
                this.Size = new Size(this.Size.Width + 1, this.Size.Height + 1);
            else if (x < 80)
                x++;
            else
            {
                main.closeWindows();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.Location = new Point(100, 100);
            this.Size = new Size(1, 1);
        }
    }
}
