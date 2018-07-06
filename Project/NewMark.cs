using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using System.Speech.Synthesis;

namespace Project
{
    public partial class NewMark : Form
    {
        //open connection with data base 
        MySqlConnection con = new MySqlConnection("server=localhost;database=quran;uid=root;pwd=root");
        MySqlCommand cmd;
        Main form;
        public NewMark(Main f)
        {
            InitializeComponent();
            form = f;
        }
        private void btnOk_Click(object sender, EventArgs e)
        {
            // ok click
            if (textBox1.Text != "")
            {
                MessageBox.Show("تم إضافة العلامة بنجاح");
                cmd = new MySqlCommand("insert into Marked(Name,page) values('" + textBox1.Text + "'," + 1 + ");", con);
                con.Open();
                cmd.ExecuteNonQuery();
                con.Close();
                form.settings.GetMenueItems();

                this.Close();
            }
            else
            {
                MessageBox.Show("قم بإدخال اسم العلامة للإضافة", "تحذير", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        private void NewMark_FormClosed(object sender, FormClosedEventArgs e)
        {
            form.Enabled = true;
        }
        private void btnOk_MouseHover(object sender, EventArgs e)
        {         
            form.MouseHoverControls(sender, e);
        }
        private void btnCancel_MouseHover(object sender, EventArgs e)
        {
            form.MouseHoverControls(sender, e);
        }
        private void btnCancel_MouseLeave(object sender, EventArgs e)
        {
            form.MouseLeaveControls(sender, e);
        }
        private void btnOk_MouseLeave(object sender, EventArgs e)
        {
            form.MouseLeaveControls(sender, e);
        }
    }
}
