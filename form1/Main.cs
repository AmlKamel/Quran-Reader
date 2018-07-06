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
    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();
            
             
            
        }
        Form1 f1;
        Form2 f2;
        Form3 f3;
        Form4 f4;
        void LoadWindows()
        {
            this.Hide();
            f1 = new Form1(this);
            f1.Show();
             f2 = new Form2(this);
            f2.Show();
              f3 = new Form3(this);
            f3.Show();
              f4 = new Form4(this);
            f4.Show();          
        }
        public void closeWindows()
        {
            f1.Close();
            f2.Close();
            f3.Close();
            f4.Close();
            
            this.Size = new Size(600, 600);
            this.Location = new Point(100, 100);
            this.Show();
        }

        private void Main_Load(object sender, EventArgs e)
        {
            LoadWindows();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            this.Hide();
            timer1.Enabled = false;
        }
    }
}
