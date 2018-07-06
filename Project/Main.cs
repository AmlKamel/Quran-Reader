using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Speech.Synthesis;
using MySql.Data.MySqlClient;
using System.IO;
using iTextSharp.text.pdf.parser;
using System.Text.RegularExpressions;
using System.Speech.AudioFormat;
using System.Diagnostics;
//using Word = Microsoft.Office.Interop.Word;

namespace Project
{
    public partial class Main : Form
    {
        //open connection with data base 
       private  MySqlConnection con = new MySqlConnection("server=localhost;database=quran;uid=root;pwd=root");
       private  MySqlCommand cmd;
        private MySqlDataReader dr;
        //intialize speaker 
        public   SpeechSynthesizer speaker = new SpeechSynthesizer();//for richbox
        private SpeechSynthesizer speakerSilent = new SpeechSynthesizer(); //for record
        public   SpeechSynthesizer SettingsSpeaker = new SpeechSynthesizer();//for setting
        private int current_index = 0; // to know where we are at rich box
        private Microsoft.Office.Interop.Word.Application wordobject = new Microsoft.Office.Interop.Word.Application();
        private List<string> words = new List<string>();             
        private List<string> wordsSpeak = new List<string>();
        private string speak = "";
        public  SettingClass settings;
        private bool   shown = false;
        public  int      CurrentPage;
        private int    CurrentGoza;
        private int    CurrentSura;
        private int CurrentAya;
        private string CurrentSuraText = "";
        private string Playstate = "stop";
        public  string ChangeFrom="function";//to know if the code changed in combo box or user
        private int indexOfLists = 0;
        private bool automateSelection = false;
        private bool selectionMode = false;
        private bool record;
        private List<string> recordedWords = new List<string>();
        private bool RecordFinished = false;
        private bool firstTime = true;
        private string savefile = "";
        private string wavefile = "";
        private string readingMode = "quraan";
        private string pdfFileName = "";
        private bool ReadSettings = false;
        private bool automate = false; //to ignore some events fired automaticly
       private  int pdfPage;
        private struct SuraCount
        {
            public string sura;
            public int count;
        };
        public  Main()
        {
            InitializeComponent();
            settings = new SettingClass(this);
           settings.LoadvoiceSettings();
            CurrentPage = settings.GetMainBookmarkedPage();

            txtShow.SelectionAlignment = HorizontalAlignment.Center;
            txtTafser.SelectionAlignment = HorizontalAlignment.Right;
            ChangeFrom = "function";
            FillComponentsWithPage(CurrentPage);
            ChangeFrom = "";
            settings.GetMenueItems();
            FillListView();//الفهرس
            SettingPanel.Hide();
            textBox1.Hide();
            speaker.SpeakCompleted += new EventHandler<SpeakCompletedEventArgs>(speaker_SpeakCompleted);   //it is uesed in wav fuction to ensure the speaker complete its reading
            speaker.SpeakProgress += new EventHandler<SpeakProgressEventArgs>(speechSynthesizer_SpeakProgress); // used in progress function to highlight the word the speaker speaking now
        }
        public void FillComponentsWithPage(int page)
        {
            if(readingMode!="quraan")
            {
                lblGoza.Show();
                lblSura.Show();
            }
            readingMode = "quraan";
            //disable buttons when fill components to avoid conflict
            disableButtons();
            disableComboBoxes();
            if (ChangeFrom=="function")
            {
                //fill for first time need to fill Goza comboBox
                if (GozaComboBox.Items.Count == 0)
                {
                    cmd = new MySqlCommand("select GozaName from Goza", con); // add Gozas names to Goza ComboBox
                    con.Open();
                    dr = cmd.ExecuteReader(); //execute command to database
                    while (dr.Read())   //loop reader and fill the combobox
                    {
                        GozaComboBox.Items.Add(dr[0].ToString());
                    }
                    dr.Close(); // close data reader
                    con.Close(); // close connection
                }
                //get page sura and goza
                cmd = new MySqlCommand("select sura,SuraName from quran_text,Sura where Sura.SuraID=quran_text.sura and  page_number =" + page + " limit 1", con);
                con.Open();
                dr = cmd.ExecuteReader();
                dr.Read();
                CurrentSura = Int32.Parse(dr["sura"].ToString());
                CurrentSuraText = dr["SuraName"].ToString();
                CurrentGoza = calculateGoza(page);
                con.Close();
                if (GozaComboBox.SelectedIndex != 0)
                {
                    GozaComboBox.SelectedIndex = -1;
                    GozaComboBox.SelectedIndex = CurrentGoza;
                }
                else//goza compoBox will not change so no change in suraCompo too ,change it here
                {
                    SuraComboBox.SelectedIndex = -1;
                   SuraComboBox.SelectedItem = CurrentSuraText;
                }
            }
            SetTxtShowSizeAndBackground(page);
            GetFromWord(page + "");
           
            //change page label 
            //txtPage.Enabled = true;
            lblPage.Text = page + "  ";
            //set goza to label Goza
            if (GozaComboBox.SelectedIndex == 0)
                lblGoza.Text = GozaComboBox.Items[CurrentGoza].ToString();
            else
            lblGoza.Text = GozaComboBox.SelectedItem.ToString();

            //set page to text box
            con.Open();
            cmd = new MySqlCommand("select text,quran_text.type,SuraName,aya from quran_text,Sura where page_number=" + CurrentPage + " and Sura.SuraID=sura", con);
            dr = cmd.ExecuteReader();//execute command
            textBox1.Clear();
            List<string> li = new List<string>();
            List<SuraCount> suracounts = new List<SuraCount>();
            bool new_sura;
            string type = "";
            bool addToReadText = false;
            if (ChangeFrom == "function") addToReadText = true;
            while (dr.Read())   //loop reader and fill the txt box
            {
                if (ChangeFrom == "suraCombo" && !addToReadText && dr["SuraName"].ToString() == CurrentSuraText)
                    addToReadText = true;
              else  if (ChangeFrom == "ayaCombo" && !addToReadText && dr["aya"].ToString() == CurrentAya+"")
                    addToReadText = true;
                //to set label suras    suras in page
                new_sura = true; 
                for(int i=0;i<suracounts.Count;i++)
                {
                    if (dr["SuraName"].ToString() == suracounts[i].sura)
                    {
                        SuraCount s  = suracounts[i];
                        s.count += 1;
                        suracounts[i] = s;
                        new_sura = false;
                        break;
                    }
                }
                if(new_sura)
                {
                    SuraCount s = new SuraCount();
                    s.sura = dr["SuraName"].ToString();
                    s.count = 1;
                    suracounts.Add(s);
                }             
                li.Clear();
                li = dr[1].ToString().Split(new char[] { ' ' }).ToList();
                // label type  
                for (int i = 0; i < li.Count; i += 2)
                {
                    if (li[i] == "Goza")
                    {
                        if (ChangeFrom == "gozaCombo"&& !addToReadText)
                            addToReadText = true;
                        type += "الجزء" + "  " + li[i + 1] + "\n";
                    }
                    else if (li[i] == "RobHzb")
                        type += "ربع حزب" + "\n" + li[i + 1];
                    else if (li[i] == "NosHzb")
                        type += "نص حزب" + "\n" + li[i + 1];
                    else if (li[i] == "ThlthRobHzb")
                        type += "ثلاث أرباع" + "\n حزب " + li[i + 1];
                    else if (li[i] == "Hzb")
                        type += " حزب   " + li[i + 1] + "\n";
                    else if (li[i] == "Sgda")
                    {
                        type += "سجدة" + "\n";
                        i--;
                    }
                }
                // text to read from it
                if (addToReadText )
                    textBox1.Text += dr[0].ToString() + " \r\n ";
            }
            if (type.Length == 0)
                PType.Hide();
            else
            {
                lblType.Text = type; PType.Show();
                int x=  txtShow.Find("۞");
                if (x > 0)
                {
                    Point t = txtShow.GetPositionFromCharIndex(x);
                    t = new Point(PType.Location.X, t.Y-10);
                    PType.Location = t;
                }
                else
                {
                     x = txtShow.Find("۩");
                    if (x > 0)
                    {
                        Point t = txtShow.GetPositionFromCharIndex(x);
                        t = new Point(PType.Location.X, t.Y - 10);
                        PType.Location = t;
                    }
                    else PType.Location = new Point(558, 42);
                }
                
            }

            //label sura
            if (suracounts.Count == 1)// page has only one sura
                lblSura.Text = " سورة " + suracounts[0].sura;
            else if (suracounts.Count == 2)// page has two suras  write sura has most number of ayas in page
            {
                if (suracounts[0].count >= suracounts[1].count)
                    lblSura.Text = " سورة " + suracounts[0].sura;
                else
                    lblSura.Text = " سورة " + suracounts[1].sura;
            }
            else
            {
                lblSura.Text = " سورة " + suracounts[0].sura + " سورة " + suracounts[1].sura;
            }
            dr.Close(); // close data reader
            con.Close();
            enableButtons();
            enableComboBoxes();
            ChangeFrom = "";
   
        }
        void SetTxtShowSizeAndBackground(int page)
        {
            //draw txtShow and change its background according to page
            Point l = new Point(80, 90);
            if (page == 1 || page == 2)
            {
                if (page == 1)
                {
                    this.Show.BackgroundImage = global::Project.Properties.Resources._1;
                }
                else
                {
                    this.Show.BackgroundImage = global::Project.Properties.Resources._2;
                }
                this.txtShow.Font = new System.Drawing.Font("KFGQPC Uthmanic Script HAFS", 20F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(178)));
                this.txtShow.Location = l;
                this.txtShow.Size = new System.Drawing.Size(440, 500);
            }
            else if (txtShow.Location == l)
            {
                this.Show.BackgroundImage = global::Project.Properties.Resources._3;
                this.txtShow.Font = new System.Drawing.Font("KFGQPC Uthmanic Script HAFS", 15F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(178)));
                this.txtShow.Location = new System.Drawing.Point(40, 33);
                this.txtShow.Size = new System.Drawing.Size(520, 580);
            }
        }
        int calculateGoza(int page)
        {
            float x;
            if (page == 602 || page == 603 || page == 604) return 30;
            else if (page == 1) return 1;
            else
            {
                x = ((float)page / 20) - (page / 20);
                if (x >= 0.06)
                    return page / 20 + 1;
                else return page / 20;
            }
        }
        int calculatePage(int goza)
        {
            if (goza == 1)
                return 1;
            else
            {
                return (goza - 1) * 20 + 2;
            }
        }
        // get page data from word
        void GetFromWord(string fileName)
        {
            //get quraan file for current page
            object File = Application.StartupPath + @"\wordfiles\" + fileName + ".docx"; //this is the file name with the path
            object nullobject = System.Reflection.Missing.Value;
            object readOnly = true;
            wordobject.DisplayAlerts = Microsoft.Office.Interop.Word.WdAlertLevel.wdAlertsNone;
            Microsoft.Office.Interop.Word.Document docs = wordobject.Documents.Open(ref File, ref nullobject, ref readOnly, ref nullobject, ref nullobject, ref nullobject, ref nullobject, ref nullobject, ref nullobject, ref nullobject, ref nullobject, ref nullobject, ref nullobject, ref nullobject, ref nullobject, ref nullobject);
            txtShow.Clear();
            txtShow.Text = docs.Content.Text;
            docs.Close(ref nullobject, ref nullobject, ref nullobject);
            //get tafser file for current page
            File = Application.StartupPath + @"\tafser\" + fileName + ".docx";
           docs = wordobject.Documents.Open(ref File, ref nullobject, ref readOnly, ref nullobject, ref nullobject, ref nullobject, ref nullobject, ref nullobject, ref nullobject, ref nullobject, ref nullobject, ref nullobject, ref nullobject, ref nullobject, ref nullobject, ref nullobject);
            txtTafser.Clear();
            txtTafser.Text = docs.Content.Text;
            docs.Close(ref nullobject, ref nullobject, ref nullobject);
        }
        void disableButtons()
        {
            btnPlay.Enabled = false;
            btnStop.Enabled = false;
            BookMark.Enabled = false;
            btnNextPage.Enabled = false;
            btnLastPage.Enabled = false;
            btnBrowse.Enabled = false;
            btnRecord.Enabled = false;
        }
        void enableButtons()
        {
            btnPlay.Enabled = true;
            btnStop.Enabled = true;
            BookMark.Enabled = true;
            btnNextPage.Enabled = true;
            btnLastPage.Enabled = true;
            btnBrowse.Enabled = true;
            btnRecord.Enabled = true;
        }
        void disableComboBoxes()
        {
            GozaComboBox.Enabled = false;
            SuraComboBox.Enabled = false;
            AyaComboBox.Enabled = false;
            VoicesComboBox.Enabled = false;
            speechRate.Enabled = false;
            speechVolume.Enabled = false;
            listView1.Enabled = false;
            listView2.Enabled = false;
            txtToSearch.Enabled = false;
        }
        void enableComboBoxes()
        {
            GozaComboBox.Enabled = true;
            SuraComboBox.Enabled = true;
            AyaComboBox.Enabled = true;
            VoicesComboBox.Enabled = true;
            speechRate.Enabled = true;
            speechVolume.Enabled = true;
            listView1.Enabled = true;
            listView2.Enabled = true;
            txtToSearch.Enabled = true;
        }
        private void Setting_Click(object sender, EventArgs e)
        {
            if (!shown)
            {
                this.Size = new Size(1150, 730);
                SettingPanel.Show();
                SettingPanel.Location = new Point(0, 40);
                Show.Location = new Point(530, 40);
                shown = true;
            }
            else
            {
                this.Size = new Size(620, 730);
                SettingPanel.Hide();
                Show.Location = new Point(0, 40);
                shown = false;
            }
        }
        private void speechVolume_ValueChanged(object sender, EventArgs e)
        {
            settings.ChangeVolume(Int32.Parse(speechVolume.Value.ToString()));
        }
        private void speechRate_ValueChanged(object sender, EventArgs e)
        {
            settings.ChangeRate(Int32.Parse(speechRate.Value.ToString()));
        }
        private void btnStop_Click(object sender, EventArgs e)
        {
            if (Playstate == "play" || Playstate == "pause")
            {
               
                this.btnPlay.BackgroundImage = global::Project.Properties.Resources.play;
                //stop play code 
                if (Playstate == "pause")
                {
                    speaker.Resume();

                }
                speaker.SpeakAsyncCancelAll(); // cancel any speaking know
                if (record)
                {
                    toWavAndMp3();
                    record = false;
                    //speaker.SpeakAsyncCancelAll();
                    RecordFinished = true;
                }
                enableButtons();
                enableComboBoxes();

            }
        }
        private void btnPlay_Click(object sender, EventArgs e)
        {
            if (Playstate == "pause")
            {
                this.btnPlay.BackgroundImage = global::Project.Properties.Resources.pause;
                Playstate = "play";
                //start paused play code 
                speaker.Resume();  //resuming speaker
            }
            else if (Playstate == "play")
            {
                this.btnPlay.BackgroundImage = global::Project.Properties.Resources.play;
                Playstate = "pause";
                //pause play code                
                speaker.Pause();  //pausing speaker
            }
            else
            {
                SettingsSpeaker.SpeakAsyncCancelAll();
                this.btnPlay.BackgroundImage = global::Project.Properties.Resources.pause;
                Playstate = "play";
                //start play code 
                if (readingMode=="quraan")
                {
                    string text = "";
                    if (selectionMode)
                    {
                        words.Clear();
                        wordsSpeak.Clear();
                        speak = "";
                        text = txtShow.SelectedText;
                        text = ignoreNumbers(text);
                        text = text.Replace("\r\n", " ");
                    }
                    else
                    {
                        PrepareTospeak();
                        text = textBox1.Text; //get text from text box
                        text = text.Replace("\r\n", "\n");
                    }
                    text = text.Replace("بِسۡمِ ٱللَّهِ ٱلرَّحۡمَٰنِ ٱلرَّحِيمِ ", " \n  بِسۡمِ ٱللَّهِ ٱلرَّحۡمَٰنِ ٱلرَّحِيمِ  \n ");
                    string[] alonewords = text.Split(' ');
                    
                    words = alonewords.ToList();
                    wordsSpeak = alonewords.ToList();
                    ToSpeakRight();
                    play();
                }
                else
                {
                    speak = txtShow.Text;
                    play();
                }
            }
        }
        private void btnNextPage_Click(object sender, EventArgs e)
        {
            if (readingMode == "quraan")
            {
                if (CurrentPage != 604)
                {
                    ChangeFrom = "function";
                    //decrease current page number
                    CurrentPage++;
                    FillComponentsWithPage(CurrentPage);
                    ChangeFrom = "";
                }
            }
            else if (readingMode == "pdf")
            {
                iTextSharp.text.pdf.PdfReader reader = new iTextSharp.text.pdf.PdfReader(pdfFileName);
                ITextExtractionStrategy strategy = new SimpleTextExtractionStrategy();

                pdfPage--;
                if (pdfPage >= 1)
                {
                    string text = PdfTextExtractor.GetTextFromPage(reader, pdfPage, strategy);
                    txtShow.Clear();
                    txtShow.Text = text;
                    lblPage.Text = pdfPage.ToString();
                    lblPage.Enabled = false;
                }
            }
        }
        private void btnLastPage_Click(object sender, EventArgs e)
        {
            if (readingMode == "quraan")
            {
                if (CurrentPage != 1)
                {
                    ChangeFrom = "function";
                    //decrease current page number
                    CurrentPage--;
                    FillComponentsWithPage(CurrentPage);
                    ChangeFrom = "";
                }
            }
            else if(readingMode=="pdf")
            {
                iTextSharp.text.pdf.PdfReader reader = new iTextSharp.text.pdf.PdfReader(pdfFileName);
                ITextExtractionStrategy strategy = new SimpleTextExtractionStrategy();

                pdfPage++;
                if (pdfPage < reader.NumberOfPages)
                {
                    string text = PdfTextExtractor.GetTextFromPage(reader, pdfPage, strategy);
                    txtShow.Clear();
                    txtShow.Text = text;
                    lblPage.Text= pdfPage.ToString();
                    lblPage.Enabled = false;
                }
            }
        }
        private void GozaComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (GozaComboBox.SelectedIndex == -1) return;
            if (ChangeFrom=="")//change from goza
            {
                ChangeFrom = "gozaCombo";
                if (GozaComboBox.SelectedIndex != 0)
                {
                    CurrentGoza = GozaComboBox.SelectedIndex;
                }
                //else current goza and sura still the sam
            }
            // get suras in goza
            if (GozaComboBox.SelectedIndex == 0)
                cmd = new MySqlCommand("select SuraName,SuraID from Sura;", con);
            else
                cmd = new MySqlCommand("select SuraName,SuraID from Sura where SuraID in (select SuraID from Goza_Sura where GozaID =" + CurrentGoza + ");", con);//select Suras names
            con.Open();
            dr = cmd.ExecuteReader(); //execute command to database
            SuraComboBox.Items.Clear();
            while (dr.Read())   //loop reader and fill the combobox
            {
                SuraComboBox.Items.Add(dr[0].ToString());
            }
            dr.Close(); // close data reader
            con.Close();
            //remove selected item
            SuraComboBox.SelectedIndex = -1;
            if (ChangeFrom == "gozaCombo")//change from gozaCombo
            {
                if (GozaComboBox.SelectedIndex == 0)//select all goza not change current sura
                    SuraComboBox.SelectedItem = CurrentSuraText;
                else
                {
                    SuraComboBox.SelectedIndex = 0;
                    CurrentSuraText = SuraComboBox.SelectedItem.ToString();
                }
            }
            else //called from function that calculated the current goza and sura
                SuraComboBox.SelectedItem = CurrentSuraText;
        }
        private void SuraComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (SuraComboBox.SelectedIndex == -1) return;
            if (ChangeFrom == "")//change from sura comboBox
            {
                con.Open();
                cmd = new MySqlCommand("select sura, page_number from quran_text,Sura where Sura.SuraID=quran_text.sura and Sura.SuraName ='" + SuraComboBox.SelectedItem.ToString() + "' limit 1", con);
                ChangeFrom = "suraCombo";
                dr = cmd.ExecuteReader();
                dr.Read();
                CurrentSura = Int32.Parse(dr["sura"].ToString());
                CurrentPage = Int32.Parse(dr["page_number"].ToString());
                CurrentSuraText = SuraComboBox.SelectedItem.ToString();
                dr.Close();
                con.Close();
                FillAyaComboBox();
                AyaComboBox.SelectedIndex = -1;//remove selected index
                AyaComboBox.SelectedIndex = 0;//Select first aya  in   ayaComboBox
            }
            else if (ChangeFrom == "gozaCombo")
            {
                //if all goza selected load all ayas to aya combo,no change to current page
                con.Open();
                cmd = new MySqlCommand("select SuraID from  Sura where   Sura.SuraName ='" + SuraComboBox.SelectedItem.ToString() + "'  ;", con);
                CurrentSura = Int32.Parse(cmd.ExecuteScalar().ToString());
                con.Close();
                //ChangeFrom = "suraCombo";
                string aya = AyaComboBox.SelectedItem.ToString();
                FillAyaComboBox();
                if (GozaComboBox.SelectedIndex != 0)
                //  Select first aya as it first aya in goza
                {
                    AyaComboBox.SelectedIndex = -1;
                    AyaComboBox.SelectedIndex = 0;
                }
                //else no change only load all ayas in current sura
                else
                {
                    AyaComboBox.SelectedIndex = -1;
                    AyaComboBox.SelectedItem = aya;
                }
            }
            else //change from function
            {
                FillAyaComboBox();
                // Select first aya in selected page
                cmd = new MySqlCommand("select aya from  quran_text where page_number=" + CurrentPage + " and  sura=" + CurrentSura + " limit 1;", con);
                con.Open();
                AyaComboBox.SelectedItem = cmd.ExecuteScalar().ToString();
                con.Close();
                //function will load the page
            }
        }
        void FillAyaComboBox()
        {
            //load all aya in sura if all goza selected
            if (GozaComboBox.SelectedIndex == 0)
                cmd = new MySqlCommand("select aya,page_number from quran_text where sura=" + CurrentSura + ";", con);
            else//load ayas only in current sura and current goza
            {
                int startPage = calculatePage(GozaComboBox.SelectedIndex);
                if (GozaComboBox.SelectedIndex == 30)
                    cmd = new MySqlCommand("select aya,page_number from quran_text where sura=" + CurrentSura + " and page_number>=" + startPage + "  and page_number <=" + (startPage + 22) + "  ;", con);
                else if (GozaComboBox.SelectedIndex == 1)
                    cmd = new MySqlCommand("select aya,page_number from quran_text where sura=" + CurrentSura + " and page_number>=" + startPage + "  and page_number <=" + (startPage + 20) + "  ;", con);
                else
                    cmd = new MySqlCommand("select aya,page_number from quran_text where sura=" + CurrentSura + " and page_number>=" + startPage + "  and page_number <" + (startPage + 20) + "  ;", con);

            }
            con.Open();
            dr = cmd.ExecuteReader();
            AyaComboBox.Items.Clear();
            while (dr.Read())
            {
                AyaComboBox.Items.Add(dr[0].ToString());
            }
            con.Close();
        }
        private void AyaComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (AyaComboBox.SelectedIndex ==-1)  return;
            //if called by change in goza
            if (ChangeFrom == "gozaCombo")
            {
                //all goza chnged no change will occure
                if (GozaComboBox.SelectedIndex == 0)
                {ChangeFrom = ""; return; }
                else //select page has first aya in goza
                {
                    //con.Open();
                    CurrentPage = calculatePage(CurrentGoza);
                    if (CurrentGoza == 7 || CurrentGoza == 11)
                        CurrentPage--;
                    //  cmd = new MySqlCommand("select page_number from  quran_text  where type like  \"%Goza "+CurrentGoza+"%\" ", con);
                    // CurrentPage = (Int32.Parse(cmd.ExecuteScalar().ToString()));
                    //  con.Close();
                    FillComponentsWithPage(CurrentPage);
                }
            }
            else if(ChangeFrom=="suraCombo")
            {
                //load page with first aya in sura
                con.Open();
                cmd = new MySqlCommand("select page_number from  quran_text  where aya= 1   and  sura ="+CurrentSura+ ";", con);
                CurrentPage = (Int32.Parse(cmd.ExecuteScalar().ToString()));
                con.Close();
                FillComponentsWithPage(CurrentPage);
                
            }
            else if (ChangeFrom == "")
            {
                ChangeFrom ="ayaCombo";
                //load page with selected aya in sura
                con.Open();
                cmd = new MySqlCommand("select page_number from  quran_text  where aya=  "+Int32.Parse(AyaComboBox.SelectedItem.ToString())+"   and  sura =" + CurrentSura + ";", con);
                CurrentPage = (Int32.Parse(cmd.ExecuteScalar().ToString()));
                con.Close();
                CurrentAya = Int32.Parse(AyaComboBox.SelectedItem.ToString());
                FillComponentsWithPage(CurrentPage);
               
            }
        }
        private void CheckKeyword(string word, Color color, int startIndex)
        {
            if (this.txtShow.Text.Contains(word))
            {
                int index = -1;
                int selectStart = this.txtShow.SelectionStart;

                while ((index = this.txtShow.Text.IndexOf(word, (index + 1))) != -1)
                {
                    automateSelection = true;//prevent to continue in selection changed
                    this.txtShow.Select((index + startIndex), word.Length);
                    this.txtShow.SelectionColor = color;
                    this.txtShow.Select(selectStart, 0);
                    this.txtShow.SelectionColor = Color.Black;
                    automateSelection = false;//prevent to continue in selection changed
                }
            }
        }
        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            if (automate)
            {
                return;
            }
            for (int i = 0; i < SuraComboBox.Items.Count; i++)
            {
                this.CheckKeyword("سُورَةُ " + SuraComboBox.Items[i].ToString(), Color.Green, 0);
            }

            this.CheckKeyword("١", Color.Green, 0);
            this.CheckKeyword("٢", Color.Green, 0);
            this.CheckKeyword("٣", Color.Green, 0);
            this.CheckKeyword("٤", Color.Green, 0);
            this.CheckKeyword("٥", Color.Green, 0);
            this.CheckKeyword("٦", Color.Green, 0);
            this.CheckKeyword("٧", Color.Green, 0);
            this.CheckKeyword("٨", Color.Green, 0);
            this.CheckKeyword("٩", Color.Green, 0);
            this.CheckKeyword("٠", Color.Green, 0);
            this.CheckKeyword("آ", Color.Blue, 0);
        }
        private void FillListView()
        {
            listView2.Items.Clear();
            ImageList image = new ImageList();
            image.ImageSize = new Size(50, 50);
            image.Images.Add(global::Project.Properties.Resources.images__1_);
            image.Images.Add(global::Project.Properties.Resources.download__2_);
            listView2.SmallImageList = image;

            cmd = new MySqlCommand("select SuraID,SuraName,Count,Type,Start from Sura;", con);
            con.Open();
            dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                ListViewItem colume = new ListViewItem(dr[0].ToString());
                //colume.SubItems.Add(dr[0].ToString(), Color.White, Color.DarkGray, new Font(this.Font, FontStyle.Bold));
                colume.SubItems.Add(dr[1].ToString());
                colume.SubItems.Add(dr[2].ToString());
                if (dr[3].ToString() == "1")//مكية
                {

                    colume.SubItems.Add("مكيه");
                }
                else// مدنية
                {
                    colume.SubItems.Add("مدنية");
                }
                colume.SubItems.Add(dr[4].ToString(), Color.White, Color.DarkGray, new Font(this.Font, FontStyle.Bold));
                listView2.Items.Add(colume);
            }
            dr.Close();
            con.Close();

        }
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            wordobject.Quit();
        }      
        private void btnBrowse_Click(object sender, EventArgs e)
        {
            OpenFileDialog browse = new OpenFileDialog();

            browse.Filter = "TXT|*.txt|PDF files|*.pdf|DOCX|*.docx";
            browse.Multiselect = false;


            if (browse.ShowDialog() == DialogResult.OK)
            {
                //change background
                this.Show.BackgroundImage = global::Project.Properties.Resources._4;
                Point l = new Point(80, 90);
                this.txtShow.Location = l;
                this.txtShow.Size = new System.Drawing.Size(440, 500);
                txtTafser.Text = "";
                lblGoza.Hide();
                lblSura.Hide();
                PType.Hide();
                string ext = System.IO.Path.GetExtension(browse.FileName);
                if (ext == ".txt")
                {

                    StreamReader reader = new StreamReader(browse.FileName);
                    txtShow.Clear();
                    txtShow.Text = reader.ReadToEnd();
                    reader.Close();
                    readingMode = "txt";
                }
                else if (ext == ".pdf")
                {
                    try
                    {
                        pdfFileName = browse.FileName;
                        iTextSharp.text.pdf.PdfReader reader = new iTextSharp.text.pdf.PdfReader(browse.FileName);
                        ITextExtractionStrategy strategy = new SimpleTextExtractionStrategy();
                        string text = PdfTextExtractor.GetTextFromPage(reader, 1, strategy);
                        pdfPage = 1;
                        bool arabic = false;
                        if (text.Contains("ا"))
                        {
                            arabic = true;
                            if (text.Contains("e"))
                            {
                                arabic = false;
                                MessageBox.Show("can not detect if that english or arabic document");
                                return;
                            }
                        }
                        if (arabic)
                        {
                            text = string.Join(string.Empty, text.Reverse());
                        }
                        txtShow.Clear();
                        //richTextBox1.Text = sb.ToString();
                        txtShow.Text = text;
                        reader.Close();
                        readingMode = "pdf";
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Message", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                }
                else if (ext == ".docx")
                {
                    Microsoft.Office.Interop.Word.Application newWordObject = new Microsoft.Office.Interop.Word.Application();

                    //var wordObject = new Microsoft.Office.Interop.Word.Application();

                    //Microsoft.Office.Interop.Word.Application wordobject = new Microsoft.Office.Interop.Word.Application();
                    object File = browse.FileName; //this is the path
                    object nullobject = System.Reflection.Missing.Value;

                    Microsoft.Office.Interop.Word._Document docs = newWordObject.Documents.Open(ref File, ref nullobject, ref nullobject, ref nullobject, ref nullobject, ref nullobject, ref nullobject, ref nullobject, ref nullobject, ref nullobject, ref nullobject, ref nullobject, ref nullobject, ref nullobject, ref nullobject, ref nullobject);
                    //wordobject.DisplayAlerts = Microsoft.Office.Interop.Word.WdAlertLevel.wdAlertsNone;

                    docs.Activate();
                    txtShow.Text = docs.Content.Text;
                    docs.Close(ref nullobject, ref nullobject, ref nullobject);
                    newWordObject.Quit();
                    readingMode = "docs";
                }
            }
        }
        private void txtToSearch_TextChanged(object sender, EventArgs e)
        {
            if (txtToSearch.Text == "")
                listView1.Items.Clear();
            else
            {

                cmd = new MySqlCommand("select sura,SuraName,page_number,aya from quran_text,Sura where Sura.SuraID=quran_text.sura and text like  @s", con);
                cmd.Parameters.AddWithValue("@s", "%" + txtToSearch.Text + "%");
                con.Open();
                dr = cmd.ExecuteReader();
                listView1.Items.Clear();
                int id = 1;
                while (dr.Read())
                {
                    ListViewItem colume = new ListViewItem((id++) + "");
                    colume.SubItems.Add(dr[0].ToString());
                    colume.SubItems.Add(dr[1].ToString());
                    colume.SubItems.Add(dr[2].ToString());
                    colume.SubItems.Add(dr[3].ToString());
                    listView1.Items.Add(colume);
                }
                dr.Close();
                con.Close();
            }
        }
        private void listView1_SelectedIndexChanged(object sender, EventArgs e)//البحث
        {
            if (listView1.SelectedItems.Count == 0 || listView1.SelectedItems[0].SubItems[0].Text == "") return;
            else
            {
                disableButtons();
                disableComboBoxes();
                GozaComboBox.Enabled = true;
                SuraComboBox.Enabled = true;
                AyaComboBox.Enabled = true;
                ChangeFrom = "function";
                CurrentPage = Int32.Parse(listView1.SelectedItems[0].SubItems[3].Text);
                FillComponentsWithPage(CurrentPage);
                ChangeFrom = "";
                int index = 0;
                while (index <= txtShow.Text.LastIndexOf(txtToSearch.Text))
                {
                    automateSelection = true; //to prevent going to  selection changed
                    index = txtShow.Find(txtToSearch.Text, index, txtShow.TextLength, RichTextBoxFinds.None);
                    automateSelection = false;
                    automate = true;//to prevent going to text changed
                    txtShow.SelectionBackColor = Color.Yellow;
                    automate = false;
                    index++;
                }
                enableButtons();
                enableComboBoxes();
            }
        }
        private void listView2_SelectedIndexChanged(object sender, EventArgs e)//الفهرس
        {
            if (listView2.SelectedItems.Count == 0 || listView2.SelectedItems[0].SubItems[0].Text == "")return ;
            else
            {
                disableButtons();
                disableComboBoxes();
                GozaComboBox.Enabled = true;
                SuraComboBox.Enabled = true;
                AyaComboBox.Enabled = true;
                ChangeFrom = "function";
                CurrentPage = Int32.Parse(listView2.SelectedItems[0].SubItems[4].Text);
                FillComponentsWithPage(CurrentPage);
                ChangeFrom = "";
                enableButtons();
                enableComboBoxes();
            }
        }
        private void richTextBox1_SelectionChanged(object sender, EventArgs e)
        {
            if (automateSelection)
                return;
            if (Playstate!= "stop")
            {

                return;
            }
            string selectedText = txtShow.SelectedText;
            char[] unWantedChars = { '١', '٢', '٣', '٤', '٥', '٦', '٧', '٨', '٩', '٠', '\n', ' ' };
            selectedText = selectedText.Trim();
            selectedText = selectedText.Trim(unWantedChars);
            if (selectedText != "" && selectedText != " ")
            {
                current_index = txtShow.SelectionStart;
                selectionMode = true;
            }
        }
        private void VoicesComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            settings.SetVoice(VoicesComboBox.Text);
        }
        void speechSynthesizer_SpeakProgress(object sender, SpeakProgressEventArgs e)
        {
            try
            {
                int start_index = 0;
                string targetWord = "";
                if (selectionMode)
                {
                    automate = true; //because after next line it go to txt changed event and i don't know why
                    txtShow.SelectionBackColor = Color.Tomato;
                    automate = false;
                }
                if (readingMode == "quraan")
                {


                    int indexOfWord = Array.IndexOf(wordsSpeak.ToArray(), e.Text, indexOfLists);
                    targetWord = words[indexOfWord];
                    indexOfLists = indexOfWord;

                    SpeakRightThroughtSpeaking(e.Text, indexOfLists);

                    automateSelection = true;
                    start_index = txtShow.Find(targetWord, current_index, RichTextBoxFinds.None); //get the index of the noun e.text we will search from current index which is intialy by 0 to get it
                    automateSelection = false;


                    if (record)
                    {
                        recordedWords.Add(wordsSpeak[indexOfWord]);
                    }
                }
                else
                {
                    targetWord = e.Text;
                    automateSelection = true;
                    start_index = txtShow.Find(targetWord, current_index, RichTextBoxFinds.None); //get the index of the noun e.text we will search from current index which is intialy by 0 to get it
                    automateSelection = false;

                    if (record)
                    {
                        recordedWords.Add(targetWord);
                    }

                }
                //making mark to the noun
                txtShow.Focus();
                txtShow.Select(start_index, targetWord.Length);
                current_index = start_index + targetWord.Length; // make current index = start index to know where is the noun we read know
            }
            catch(Exception ex)
            {
                
            }
            }
        void speaker_SpeakCompleted(object sender, SpeakCompletedEventArgs e)
        {
            if (automate)
            {
                automate = false;
                return;
            }
            if (record)
            {
                toWavAndMp3();
                record = false;
                RecordFinished = true;
            }
            if (RecordFinished)
            {
                speakerSilent.SetOutputToNull();
                recordedWords.Clear();
                RecordFinished = false;
            }
            if (selectionMode)
            {
                automateSelection = true; //next twe line go to selection changed
                txtShow.SelectionStart = 0;
                txtShow.SelectionLength = txtShow.TextLength;
                automate = true; // next line go to txt changed
                txtShow.SelectionBackColor = Color.White;
                automate = false;
                txtShow.SelectionStart = 0;
                txtShow.SelectionLength = 0;
                selectionMode = false;
                automateSelection = false;
            }
            PrepareTospeak();//make current index =0, empty words,wordsSpeak lists,empty speak string
            enableComboBoxes();
            enableButtons();
            Playstate = "stop";
            this.btnPlay.BackgroundImage = global::Project.Properties.Resources.play;
        }
        void PrepareTospeak()//make current index =0, empty words,wordsSpeak lists,empty speak string
        {
            current_index = 0;// make current index equal zero as it is a new sura
            indexOfLists = 0;
            words = new List<string>(); //empty words array
            wordsSpeak = new List<string>();//empty word that will be prepared to speak
            speak = ""; //empty string that will be send to be spoken
        }
        void play()
        {
            disableButtons();
            disableComboBoxes();
            btnPlay.Enabled = true;
            btnStop.Enabled = true;
            speaker.SpeakAsync(speak);
        }
        string ignoreNumbers(string text)
        {
            text = text.Replace('\n', ' ');
            text = text.Replace('١', '\n');
            text = text.Replace('٢', '\n');
            text = text.Replace('٣', '\n');
            text = text.Replace('٤', '\n');
            text = text.Replace('٥', '\n');
            text = text.Replace('٦', '\n');
            text = text.Replace('٧', '\n');
            text = text.Replace('٨', '\n');
            text = text.Replace('٩', '\n');
            text = text.Replace('٠', '\n');

            return text;
        }
        string fromArabicToEnglis(string arbNum)
        {
            string engNum = arbNum;
            engNum = engNum.Replace("\n", " ");
            engNum = engNum.Replace('١', '1');
            engNum = engNum.Replace('٢', '2');
            engNum = engNum.Replace('٣', '3');
            engNum = engNum.Replace('٤', '4');
            engNum = engNum.Replace('٥', '5');
            engNum = engNum.Replace('٦', '6');
            engNum = engNum.Replace('٧', '7');
            engNum = engNum.Replace('٨', '8');
            engNum = engNum.Replace('٩', '9');
            engNum = engNum.Replace('٠', '0');

            return engNum;
        }
        string fromEngToArb(string engNum)
        {

            engNum = engNum.Replace("\n", " ");
            engNum = engNum.Replace('1', '١');
            engNum = engNum.Replace('2', '٢');
            engNum = engNum.Replace('3', '٣');
            engNum = engNum.Replace('4', '٤');
            engNum = engNum.Replace('5', '٥');
            engNum = engNum.Replace('6', '٦');
            engNum = engNum.Replace('7', '٧');
            engNum = engNum.Replace('8', '٨');
            engNum = engNum.Replace('9', '٩');
            engNum = engNum.Replace('0', '٠');

            return engNum;
        }
        // fellow to speak right
        void PutEnterAfter(int index)
        {
            wordsSpeak.Insert(index + 1, "\n");
            words.Insert(index + 1, "\n");
        }
        void ToSpeakRight()
        {
            for (int i = 0; i < wordsSpeak.Count; i++)
            {
                //مد بدايه السور
                if (wordsSpeak[i] == "الٓمٓصٓ")
                {
                    wordsSpeak[i] = "ألِفْ";
                    words[i] = "ا";

                    wordsSpeak.Insert(i + 1, "لاام");
                    words.Insert(i + 1, "لٓ");

                    wordsSpeak.Insert(i + 2, "ميّمْ");
                    words.Insert(i + 2, "مٓ");

                    wordsSpeak.Insert(i + 3, "صااد");
                    words.Insert(i + 3, "صٓ");
                }
                else if (wordsSpeak[i] == "الٓمٓرۚ")
                {
                    wordsSpeak[i] = "ألِفْ";
                    words[i] = "ا";

                    wordsSpeak.Insert(i + 1, "لاام");
                    words.Insert(i + 1, "لٓ");

                    wordsSpeak.Insert(i + 2, "ميّمْ");
                    words.Insert(i + 2, "مٓ");

                    wordsSpeak.Insert(i + 3, "راء");
                    words.Insert(i + 3, "ر");
                }

                else if (wordsSpeak[i] == "الٓمٓ")
                {
                    wordsSpeak[i] = "ألِفْ";
                    words[i] = "ا";

                    wordsSpeak.Insert(i + 1, "لاام");
                    words.Insert(i + 1, "لٓ");

                    wordsSpeak.Insert(i + 2, "ميّمْ");
                    words.Insert(i + 2, "مٓ");
                }

                else if (wordsSpeak[i] == "الٓرۚ")
                {
                    wordsSpeak[i] = "ألِفْ";
                    words[i] = "ا";

                    wordsSpeak.Insert(i + 1, "لاام");
                    words.Insert(i + 1, "لٓ");

                    wordsSpeak.Insert(i + 2, "راء");
                    words.Insert(i + 2, "ر");
                }

                else if (wordsSpeak[i] == "كٓهيعٓصٓ")
                {
                    wordsSpeak[i] = "كااف";
                    words[i] = "كٓ";

                    wordsSpeak.Insert(i + 1, "هي");
                    words.Insert(i + 1, "هي");
                    wordsSpeak.Insert(i + 2, "عيين");
                    words.Insert(i + 2, "عٓ");

                    wordsSpeak.Insert(i + 3, "صااد");
                    words.Insert(i + 3, "صٓ ");
                }

                else if (wordsSpeak[i] == "طسٓمٓ")
                {
                    wordsSpeak[i] = "طا";
                    words[i] = "ط";

                    wordsSpeak.Insert(i + 1, "سْيّن");
                    words.Insert(i + 1, "سٓ");

                    wordsSpeak.Insert(i + 2, "ميّمْ");
                    words.Insert(i + 2, "مٓ");
                }

                else if (wordsSpeak[i] == "طسٓۚ")
                {
                    wordsSpeak[i] = "طا";
                    words[i] = "ط";

                    wordsSpeak.Insert(i + 1, "سْيّن");
                    words.Insert(i + 1, "سٓ");
                }
                else if (wordsSpeak[i] == "عٓسٓقٓ")
                {
                    wordsSpeak[i] = "عيّن";
                    words[i] = "عٓ";

                    wordsSpeak.Insert(i + 1, "سْيّن");
                    words.Insert(i + 1, "سٓ");

                    wordsSpeak.Insert(i + 2, "قااف");
                    words.Insert(i + 2, "قٓ");
                }




                if (wordsSpeak[i] == "ألِفْ" || wordsSpeak[i] == "لاام" || wordsSpeak[i] == "ميّمْ" || wordsSpeak[i] == "صااد" || wordsSpeak[i] == "راء" || wordsSpeak[i] == "عيين" || wordsSpeak[i] == "كااف" || wordsSpeak[i] == "هي")
                {
                    speak += wordsSpeak[i] + " ";
                    continue;
                }
                //::ۛ
                if (wordsSpeak[i] == "فِيهِۛ")
                {
                    speak += wordsSpeak[i] + "\n";
                    continue;
                }
                else if (wordsSpeak[i] == "هَادُواْۛ")
                {
                    speak += wordsSpeak[i] + "\n";
                    continue;
                }

                //الهمزه ع نبره 
                //wordsSpeak[i] = wordsSpeak[i].Replace("‍", "‍");
                if (wordsSpeak[i].Contains("‍ٔ"))
                {
                    string[] thesplitword = wordsSpeak[i].Split('‍');
                    wordsSpeak[i] = thesplitword[0];
                    words[i] = thesplitword[0];
                    wordsSpeak.Insert(i + 1, thesplitword[1]);
                    words.Insert(i + 1, thesplitword[1]);
                }

                //واو لاتنطق 
                wordsSpeak[i] = wordsSpeak[i].Replace("وْ", "");
                //حروف تنطق الالف
                wordsSpeak[i] = wordsSpeak[i].Replace("وٰ", "ا");

                //ياء عليها الالف صغيره
                wordsSpeak[i] = wordsSpeak[i].Replace("ىٰ", "ا");

                //الالف اللينه
                wordsSpeak[i] = wordsSpeak[i].Replace("ى", "ا");

                //طه
                wordsSpeak[i] = wordsSpeak[i].Replace("طه", "طاها");

                //يسٓ
                wordsSpeak[i] = wordsSpeak[i].Replace("يسٓ", "ياسْيّن");

                //صٓ
                wordsSpeak[i] = wordsSpeak[i].Replace("صٓۚ", "صااد");

                //حمٓ
                wordsSpeak[i] = wordsSpeak[i].Replace("حمٓ", "حمييمْ");
                //قٓۚ
                wordsSpeak[i] = wordsSpeak[i].Replace("قٓۚ", "قااف");

                //نٓۚ
                wordsSpeak[i] = wordsSpeak[i].Replace("نٓۚ", "نوٌن");
                //الالف و المد
                wordsSpeak[i] = wordsSpeak[i].Replace('ٱ', 'ا');
                wordsSpeak[i] = wordsSpeak[i].Replace("آ", "ا");

                wordsSpeak[i] = wordsSpeak[i].Replace(" ", " ");
                wordsSpeak[i] = wordsSpeak[i].Replace(" ", " ");
                wordsSpeak[i] = wordsSpeak[i].Replace(" ", " ");


                //السكون
                wordsSpeak[i] = wordsSpeak[i].Replace('ۡ', 'ْ');

                //الضمتين
                wordsSpeak[i] = wordsSpeak[i].Replace('ٞ', 'ٌ');

                //wordsSpeak[i] = wordsSpeak[i].Replace("بۡ", "بْ");
                //wordsSpeak[i] = wordsSpeak[i].Replace("حۡ", "حْ");
                //wordsSpeak[i] = wordsSpeak[i].Replace("سۡ", "سْ");
                //wordsSpeak[i] = wordsSpeak[i].Replace("لۡ", "لْ");
                //wordsSpeak[i] = wordsSpeak[i].Replace("مۡ", "مْ");
                //wordsSpeak[i] = wordsSpeak[i].Replace("وۡ", "وْ");
                //wordsSpeak[i] = wordsSpeak[i].Replace("ؤۡ", "ؤْ");
                //wordsSpeak[i] = wordsSpeak[i].Replace("هۡ", "هْ");
                //wordsSpeak[i] = wordsSpeak[i].Replace("عۡ", "عْ");
                //wordsSpeak[i] = wordsSpeak[i].Replace("نۡ", "نْ");
                //wordsSpeak[i] = wordsSpeak[i].Replace("يۡ", "يْ");
                //wordsSpeak[i] = wordsSpeak[i].Replace("غۡ", "غْ");
                //wordsSpeak[i] = wordsSpeak[i].Replace("قۡ", "قْ");
                wordsSpeak[i] = wordsSpeak[i].Replace(" ", " ");
                wordsSpeak[i] = wordsSpeak[i].Replace(" ", " ");
                //الالف الصغيره
                wordsSpeak[i] = wordsSpeak[i].Replace('ٰ', 'ا');

                //wordsSpeak[i] = wordsSpeak[i].Replace("عَٰ", "عَا");
                //wordsSpeak[i] = wordsSpeak[i].Replace("نَٰ", "نَا");
                //wordsSpeak[i] = wordsSpeak[i].Replace("مَٰ", "مَا");
                //wordsSpeak[i] = wordsSpeak[i].Replace("رَٰ", "رَا");
                //wordsSpeak[i] = wordsSpeak[i].Replace("ذَٰ", "ذَا");
                //wordsSpeak[i] = wordsSpeak[i].Replace("تَٰ", "تَا");
                wordsSpeak[i] = wordsSpeak[i].Replace(" ", " ");
                wordsSpeak[i] = wordsSpeak[i].Replace(" ", " ");
                wordsSpeak[i] = wordsSpeak[i].Replace(" ", " ");
                wordsSpeak[i] = wordsSpeak[i].Replace(" ", " ");
                wordsSpeak[i] = wordsSpeak[i].Replace(" ", " ");


                wordsSpeak[i] = wordsSpeak[i].Replace(" ", " ");
                wordsSpeak[i] = wordsSpeak[i].Replace(" ", " ");
                wordsSpeak[i] = wordsSpeak[i].Replace(" ", " ");
                wordsSpeak[i] = wordsSpeak[i].Replace(" ", " ");
                wordsSpeak[i] = wordsSpeak[i].Replace(" ", " ");
                wordsSpeak[i] = wordsSpeak[i].Replace(" ", " ");
                wordsSpeak[i] = wordsSpeak[i].Replace(" ", " ");
                wordsSpeak[i] = wordsSpeak[i].Replace(" ", " ");

                wordsSpeak[i] = wordsSpeak[i].Replace(" ", " ");
                wordsSpeak[i] = wordsSpeak[i].Replace(" ", " ");
                wordsSpeak[i] = wordsSpeak[i].Replace(" ", " ");
                wordsSpeak[i] = wordsSpeak[i].Replace(" ", " ");
                wordsSpeak[i] = wordsSpeak[i].Replace(" ", " ");




                //حاجات محتاجه حوار
                if (wordsSpeak[i] == "ٱللَّهِ")
                {
                    wordsSpeak[i] = "ٱللَّهِ";
                    wordsSpeak.Insert(i + 1, " ");
                    words.Insert(i + 1, " ");
                }
                //if (wordsSpeak[i] == "‍ٔ")
                //{
                //    string[] thesplitword = wordsSpeak[i].Split(' '); 
                //    wordsSpeak[i] = thesplitword[0];
                //    words[i] = thesplitword[0];
                //    wordsSpeak.Insert(i + 1, thesplitword[1]);
                //    words.Insert(i + 1, thesplitword[1]);
                //}
                // محناجه تنزل سطر بعد الكلمه
                //in this case we always use SpeakRightThroughtSpeaking()
                if (wordsSpeak[i] == "لِلَّهِ")
                {
                    PutEnterAfter(i);
                }
                else if (wordsSpeak[i] == "الصَّلَاةَ")
                {
                    PutEnterAfter(i);
                }
                else if (wordsSpeak[i] == "هُدٗا")
                {
                    PutEnterAfter(i);
                }
                speak += wordsSpeak[i] + " ";
            }
        }
            // to change the rate of the speaker throught speaking
            void chageRate(int rate , int index ,int duration)
        {
            System.Threading.Thread.Sleep(duration);
            automate = true; //speak completed called after the end of the method 
            speaker.SpeakAsyncCancelAll();

            speaker.Rate = rate;

            index++;

            speak = "";
            for (int i = index; i < wordsSpeak.Count; i++)
            {
                speak += wordsSpeak[i] + " ";
            }
            speaker.SpeakAsync(speak);
        }
        //fellow Speak right throught speaking
        void RemoveEnter(int index ,int duration)
        {
            System.Threading.Thread.Sleep(duration);
            automate = true; //speak completed called after the end of the method 
            speaker.SpeakAsyncCancelAll();
            index = index + 2;
            speak = "";
            for (int i = index; i < wordsSpeak.Count; i++)
            {
                speak += wordsSpeak[i] + " ";
            }
            speaker.SpeakAsync(speak);
        }
        void SpeakRightThroughtSpeaking(string spokenWord, int index)
        {
            if (spokenWord == "لِلَّهِ")
            {
                RemoveEnter(index,400);
                //wordsSpeak[index] = "ٱللَّهِ";
                //wordsSpeak.Insert(i + 1, "\n");
                //words.Insert(i + 1, "\n");
            }
            else if (spokenWord == "الصَّلَاةَ")
            {
                RemoveEnter(index,400);
            }
            else if (spokenWord == "هُدٗا")
            {
                RemoveEnter(index,300);
            }
            else if (spokenWord == "ألِفْ")
            {
                //chageRate(-10,index,400);
            }
            else if (spokenWord == "ميّمْ")
            {
                //chageRate(0, index,1057);
            }

        }
        private void txtPage_TextChanged(object sender, EventArgs e)
        {
            if(ChangeFrom=="")
            {
             //   System.Threading.Thread.Sleep(1000);
                ChangeFrom = "function";
                int p;
                try
                {
                    p = Int32.Parse(lblPage.Text);
                    if (p >= 1 || p <= 604)
                    {
                        CurrentPage = p;
                     //  FillComponentsWithPage(CurrentPage);
                    }
                    else lblPage.Text = CurrentPage + "";
                    ChangeFrom = "";
                }
                catch
                {
                    lblPage.Text = CurrentPage + "";
                    ChangeFrom = "";
                }
            }
           

        }
        void toWavAndMp3()
        {
            automate = true; //as speak completed called before folder browse dialog

            if (firstTime)
            {
                speakerSilent.SpeakProgress -= new EventHandler<SpeakProgressEventArgs>(speechSynthesizer_SpeakProgress);
            }

            firstTime = false;
            savefile = "";
            wavefile = @"E:\" + CurrentPage + ".wav";
            FolderBrowserDialog location = new FolderBrowserDialog();
            if (location.ShowDialog() == DialogResult.OK)
            {
                savefile = location.SelectedPath.ToString();
            }
            wordsSpeak = recordedWords;
            string recordedString = "";
            for (int i = 0; i < wordsSpeak.Count; i++)
            {
                recordedString += wordsSpeak[i] + " ";
            }
            speakerSilent.Volume = (int)speechVolume.Value; // adjust volume
            speakerSilent.Rate = (int)speechRate.Value;    // adjust rate 
            string[] voice = VoicesComboBox.Text.Split(':'); //voice selected at combo box
            speakerSilent.SelectVoice(voice[1]); // select voice of the speaker
            speakerSilent.SetOutputToWaveFile(wavefile);
            PromptBuilder builder = new PromptBuilder(new System.Globalization.CultureInfo(voice[0]));
            builder.AppendText(recordedString);
            speakerSilent.SpeakAsync(builder);
            //to mp3
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            psi.FileName = Application.StartupPath + @"\lame.exe";
            psi.Arguments = "-b" + (128).ToString() + " --resample " + (22.05).ToString() + " -m j " +
                                 "\"" + wavefile + "\"" + " " +
                                  "\"" + savefile + "\\" + System.IO.Path.GetFileNameWithoutExtension(wavefile) + ".mp3" + "\"";
            Process p = Process.Start(psi);
            p.Close();
            p.Dispose();

            //speakerSilent.SpeakProgress += new EventHandler<SpeakProgressEventArgs>(speechSynthesizer_SpeakProgress);
            automate = false; //back to default
        }
        private void richTextBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (Playstate !="stop")
            {
                Cursor.Position = new Point(0, 0);
            }
        }
        private void btnRecord_Click(object sender, EventArgs e)
        {
            record = true;
            btnPlay_Click(sender, e);
        }
        private void BoxReadSettings_CheckedChanged(object sender, EventArgs e)
        {
            if (BoxReadSettings.CheckState == CheckState.Checked)
                ReadSettings = true;
            else ReadSettings = false;
        }
        public void MouseHoverControls(object sender, EventArgs e)
        {
            if (Playstate == "stop" && ReadSettings)
            {
                string type = sender.GetType().ToString();
                string tospeak = "";
                if (type == "System.Windows.Forms.ToolStripSplitButton")
                {
                    tospeak = sender.ToString();
                }
              else  if (type == "System.Windows.Forms.PictureBox")
                {
                    tospeak = "فهرس القرآن الكريم";
                }
                if (type == "System.Windows.Forms.ToolStripButton")
                {
                    tospeak = sender.ToString();
                }
                else if (type == "System.Windows.Forms.ToolStripComboBox")
                {
                    ToolStripComboBox c = (ToolStripComboBox)sender;
                    if (c.Name == "GozaComboBox")
                        tospeak = (c.SelectedItem.ToString());
                    else if (c.Name == "SuraComboBox")
                        tospeak = "سورة " + c.SelectedItem.ToString();
                }
                else if (type == "System.Windows.Forms.ToolStripMenuItem")
                {
                    tospeak = ((ToolStripMenuItem)sender).Text;
                }
                else if (type == "System.Windows.Forms.TextBox")
                {
                    if (((TextBox)sender).Name == "txtPage")
                        tospeak = "الصفحة " + ((TextBox)sender).Text;
                    else
                        tospeak = ((TextBox)sender).Text;
                }
                else if (type == "System.Windows.Forms.Button")
                {
                    Button b = (Button)sender;
                    if (b.Name == "btnStop")
                        tospeak = "ايقاف القراءة";
                    else if (b.Name == "btnPlay")
                        tospeak = "بدء القراءة";
                    else if (b.Name == "btnLastPage")
                        tospeak = "الصفحة السابقة";
                    else if (b.Name == "btnNextPage")
                        tospeak = "الصفحة التالية";
                    else if (b.Name == "btnSound")
                        tospeak = "مستوى الصوت";
                    else tospeak = b.Text;

                }
                else if (type == "System.Windows.Forms.Label")
                    tospeak = ((Label)sender).Text;
                    
                else if(type == "System.Windows.Forms.RichTextBox")
                {
                   if(((RichTextBox)sender).Name== "txtTafser")
                        tospeak = ((RichTextBox)sender).Text;
                }
                    SettingsSpeaker.SpeakAsync(tospeak);
            }

        }
        public void MouseLeaveControls(object sender, EventArgs e)
        {
            SettingsSpeaker.SpeakAsyncCancelAll();
            if (txtTafser.SelectedText != "" || txtTafser.SelectedText != " ")
            {
               //rremove selection from txtTafser
            }
        }
        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl1.SelectedIndex == -1) return;
            if (Playstate == "stop" && ReadSettings)
            {
                string tospeak = ((TabPage)tabControl1.SelectedTab).ToolTipText;
                SettingsSpeaker.SpeakAsyncCancelAll();
                SettingsSpeaker.SpeakAsync(tospeak);
            }

        }
        private void txtTafser_SelectionChanged(object sender, EventArgs e)
        {          
            if (Playstate == "stop" && ReadSettings)
            {
                SettingsSpeaker.SpeakAsyncCancelAll();
                SettingsSpeaker.SpeakAsync(txtTafser.SelectedText);
            }

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LoadWindows();
        }

        private void btnGoToPage_Click(object sender, EventArgs e)
        {
            try
            {
                int x = Int32.Parse(txtGotoPage.Text);
                if(x>=1 ||x<=604)
                {
                    txtGotoPage .Text= "";
                    CurrentPage = x;
                    ChangeFrom = "function";
                    FillComponentsWithPage(CurrentPage);
                }
            }
            catch
            {
                txtGotoPage.Text = "";
                MessageBox.Show("فشل التعرف على رقم الصفحة", "تحذير", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void lblType_Click(object sender, EventArgs e)
        {
            MessageBox.Show(lblType.Text);
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

            this.Location = new Point(0, 0);
            this.Show();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            this.Hide();
            timer1.Enabled = false;
        }
    }
}
