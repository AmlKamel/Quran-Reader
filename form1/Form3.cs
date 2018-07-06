using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace form1
{
    public partial class Form3 : Form
    {
        Main main;
        public Form3(Main m)
        {
            main = m;
            InitializeComponent();
           
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (this.Width < 302)
            {
                this.Size = new Size(this.Size.Width + 1, this.Size.Height + 1);
                this.Location = new Point(this.Location.X, this.Location.Y - 1);
            }
        }

        private void Form3_Load(object sender, EventArgs e)
        {
            this.Size = new Size(1, 1);
            this.Location = new Point(100, 700);
        }
    }
}
