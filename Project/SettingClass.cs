using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Speech.Synthesis;

namespace Project
{
    public class SettingClass
    {
        //open connection with data base 
        MySqlConnection con = new MySqlConnection("server=localhost;database=quran;uid=root;pwd=root");
        MySqlCommand cmd;
        MySqlDataReader dr;
        Main form;
        public SettingClass(Main f)
        {
            form = f;
        }
        public void LoadvoiceSettings()
        {
            con.Open();
            cmd = new MySqlCommand("select * from settings;", con);
            dr = cmd.ExecuteReader();
            dr.Read();
            int rate = Int32.Parse(dr["rate"].ToString());
            int volume = Int32.Parse(dr["volume"].ToString());
            string voice = dr["voice"].ToString();
            dr.Close();
            con.Close();
            form.speechRate.Value = rate;
            form.speechVolume.Value = volume;
            AddInstalledVoicesToList(voice);
            if ( volume==0)
                form.btnSound.BackgroundImage = global::Project.Properties.Resources.noSound;
            else
                form.btnSound.BackgroundImage = global::Project.Properties.Resources.Sound;
               form.SettingsSpeaker.Rate = 2;
        }
        public int GetVolume()
        {
            con.Open();
            cmd = new MySqlCommand("select volume from settings;", con);
            int x = Int32.Parse(cmd.ExecuteScalar().ToString());
            con.Close();
            return x;
        }
        public void ChangeVolume(int volume)
        {
            //form.speaker.Volume = (int)form.speechVolume.Value;
            con.Open();
            cmd = new MySqlCommand("update settings set volume=" + volume, con);
            if(volume==0)
                form.btnSound.BackgroundImage = global::Project.Properties.Resources.noSound;
            else
                form.btnSound.BackgroundImage = global::Project.Properties.Resources.Sound;
            form.speaker.Volume = volume; // adjust volume
            cmd.ExecuteNonQuery();
            con.Close();
            form.SettingsSpeaker.Volume = volume;
        }
        public void ChangeRate(int rate)
        {
            con.Open();
            //form.speaker.Rate = (int)form.speechRate.Value;
            cmd = new MySqlCommand("update settings set rate=" + rate, con);
            form.speaker.Rate = rate;    // adjust rate 
            cmd.ExecuteNonQuery();
            con.Close();
        }
        public int GetMainBookmarkedPage()
        {
            cmd = new MySqlCommand("select page from Marked where Name='العلامة الرئيسية';", con);
            con.Open();
            int page = Int32.Parse(cmd.ExecuteScalar().ToString());
            con.Close();
            return page;

        }
        public void GetBookmarkedPages(object sender, EventArgs e)
        {
            cmd = new MySqlCommand("select page from Marked where Name='" + sender.ToString() + "';", con);
            con.Open();
            form.ChangeFrom = "function";
            form.CurrentPage = Int32.Parse(cmd.ExecuteScalar().ToString());
            form.FillComponentsWithPage(form.CurrentPage);
            form.ChangeFrom = "";
            con.Close();

        }
        public void SetBookmarkedPages(object sender, EventArgs e)
        {

            //System.Windows.Forms.MessageBox.Show("sender=  " + sender.ToString() + "  event = " + e.ToString());          
            cmd = new MySqlCommand("update Marked set page=" + form.CurrentPage + " where Marked.Name ='" + sender.ToString() + "' ;", con);
            con.Open();
            cmd.ExecuteNonQuery();
            con.Close();
        }
        public void GetMenueItems()
        {
            System.Windows.Forms.ToolStripMenuItem[] menue = new System.Windows.Forms.ToolStripMenuItem[3];
            System.Windows.Forms.ToolStripMenuItem saveMarkToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            saveMarkToolStripMenuItem.Text = "حفظ علامة";
            saveMarkToolStripMenuItem.MouseHover += new EventHandler(form.MouseHoverControls);
            saveMarkToolStripMenuItem.MouseLeave += new EventHandler(form.MouseLeaveControls);
            System.Windows.Forms.ToolStripMenuItem goToMarkToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            goToMarkToolStripMenuItem.Text = "ذهاب لعلامة";
            goToMarkToolStripMenuItem.MouseHover+= new EventHandler(form.MouseHoverControls);
            goToMarkToolStripMenuItem.MouseLeave += new EventHandler(form.MouseLeaveControls);

            con.Open();
            cmd = new MySqlCommand("select * from Marked", con);
            dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                System.Windows.Forms.ToolStripMenuItem saveMark = new System.Windows.Forms.ToolStripMenuItem();
                saveMark.Name = dr[0].ToString();
                saveMark.Text = dr[1].ToString();
                saveMark.Click += new EventHandler(SetBookmarkedPages);
                saveMark.MouseHover += new EventHandler(form.MouseHoverControls);
                saveMark.MouseLeave += new EventHandler(form.MouseLeaveControls);
                System.Windows.Forms.ToolStripMenuItem goToMark = new System.Windows.Forms.ToolStripMenuItem();
                goToMark.Name = dr[0].ToString();
                goToMark.Text = dr[1].ToString();
                goToMark.Click += new EventHandler(GetBookmarkedPages);
                goToMark.MouseHover += new EventHandler(form.MouseHoverControls);
                goToMark.MouseLeave += new EventHandler(form.MouseLeaveControls);
                saveMarkToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
             saveMark});

                goToMarkToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
             goToMark });

            }

            con.Close();
            //this.BookMark.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            //this.saveMarkToolStripMenuItem,
            //this.goToMarkToolStripMenuItem});
            //this.goToMarkToolStripMenuItem.Name = "goToMarkToolStripMenuItem";
            //this.goToMarkToolStripMenuItem.Size = new System.Drawing.Size(135, 22);
            //this.goToMarkToolStripMenuItem.Text = "Go To Mark";
            //this.goToMarkToolStripMenuItem.Click += new System.EventHandler(this.goToMarkToolStripMenuItem_Click);

            menue[0] = saveMarkToolStripMenuItem;
            menue[1] = goToMarkToolStripMenuItem;
            System.Windows.Forms.ToolStripMenuItem NewMarkToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            NewMarkToolStripMenuItem.Text = "علامة جديدة";
            NewMarkToolStripMenuItem.Name = "newMark";
            NewMarkToolStripMenuItem.Click += new EventHandler(createNewMark);
            NewMarkToolStripMenuItem.MouseHover+= new EventHandler(form.MouseHoverControls);
            NewMarkToolStripMenuItem.MouseLeave += new EventHandler(form.MouseLeaveControls);
            menue[2] = NewMarkToolStripMenuItem;
            form.BookMark.DropDownItems.Clear();
            form.BookMark.DropDownItems.AddRange(menue);
        }
        public void SetSoundState(bool sound)
        {
            con.Open();
            cmd = new MySqlCommand("update settings set sound=" + sound, con);
            cmd.ExecuteNonQuery();
            con.Close();
            if (sound && form.speechVolume.Value!=0)
                form.btnSound.BackgroundImage = global::Project.Properties.Resources.Sound;
            else
                form.btnSound.BackgroundImage = global::Project.Properties.Resources.noSound;

        }
        void createNewMark(object sender, EventArgs e)
        {
            form.Enabled = false;
            NewMark n = new NewMark(form);
            n.Show();
        }
        private void AddInstalledVoicesToList(string v) // to get the installed voice (prof)
        {
            using (SpeechSynthesizer synth = new SpeechSynthesizer())
            {
                foreach (var voice in synth.GetInstalledVoices())
                {
                    string item = voice.VoiceInfo.Culture + ":" + voice.VoiceInfo.Name;
                    form.VoicesComboBox.Items.Add(item);
                }
            }

            form.VoicesComboBox.SelectedItem = v;
            if (form.VoicesComboBox.SelectedIndex == -1)
            {
                form.VoicesComboBox.SelectedIndex = 1;
                cmd = new MySqlCommand("update settings set voice ='" + form.VoicesComboBox.Text + " '; ", con);
                con.Open();
                cmd.ExecuteNonQuery();
                con.Close();
            }
            string[] voices = form.VoicesComboBox.Text.Split(':'); //voice selected at combo box
            form.speaker.SelectVoice(voices[1]); // select voice of the speaker
            form.SettingsSpeaker.SelectVoice(voices[1]);
        }
        public void SetVoice(string v)
        {
            cmd = new MySqlCommand("update settings set voice ='" + v + "'; ", con);
            string[] voice = form.VoicesComboBox.Text.Split(':'); //voice selected at combo box
            form.speaker.SelectVoice(voice[1]); // select voice of the speaker
            con.Open();
            cmd.ExecuteNonQuery();
            con.Close();
            form.SettingsSpeaker.SelectVoice(voice[1]); // select voice of the speaker
        }
    }
}
