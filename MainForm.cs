using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScanSRV
{
    public partial class MainForm : Form
    {
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [System.Runtime.InteropServices.DllImportAttribute("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImportAttribute("user32.dll")]
        public static extern bool ReleaseCapture();

        bool scanned_status = true;

        List<String> cnames = new List<string>();
        List<String> cdata = new List<string>();
        List<String> loglines = new List<string>();

        public static List<String> srv_names_info = new List<string>();

        int active_threads = 0;

        public static List<Tuple<string, int>> serverlist = new List<Tuple<string, int>>();

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            this.MouseDown += Form3_MouseDown;
            labelX.MouseEnter += new EventHandler(labelX_MouseEnter);
            labelX.MouseLeave += new EventHandler(labelX_MouseLeave);
            labelMinimize.MouseEnter += new EventHandler(labelX_MouseEnter);
            labelMinimize.MouseLeave += new EventHandler(labelX_MouseLeave);
        }

        private async void buttonScan_Click(object sender, EventArgs e)
        {
            if (active_threads > 0)
            {
                addlog("Not yet");
                return;
            }
            if (scanned_status == false)
            {
                addlog("Not yet[2]");
            }
            loglines.Clear();
            labelLog.Text = "";
            addlog("Preparing scan");
            scanned_status = false;
            labelNicknames.Text = "Nicknames:\n";
            labelConnect.Text = "";
            cdata.Clear();
            cnames.Clear();
            srv_names_info.Clear();
            buttonServers.Visible = false;
            if (!File.Exists("accounts.txt"))
            {
                addlog("accounts.txt - not found");
                scanned_status = true;
                return;
            }
            string[] lines = File.ReadAllLines("accounts.txt");
            Thread[] threads = new Thread[lines.Length];
            addlog("Found " + lines.Length.ToString() + " accounts");
            addlog("Launching threads");
            for (int _j = 0; _j < lines.Length; ++_j)
            {
                int j = _j;
                await Task.Delay(10);
                String clink = "http://steamcommunity.com/profiles/" + Web.SIDtoCom(lines[j]);
                ++active_threads;
                cdata.Add("");
                threads[j] = new Thread(() => get_cdata(j, clink));
                threads[j].Start();
            }
            while (active_threads > 0)
            {
                await Task.Delay(100);
                bool all_returned = true;
                for (int i = 0; i < lines.Length; ++i)
                {
                    if (cdata[i].Length == 0)
                        all_returned = false;
                }
                if (all_returned)
                    active_threads = 0;
            }
            addlog("Threads returned");
            for (int i = 0; i < lines.Length; ++i)
            {
                await Task.Delay(10);
                string name = Web.GetNameFromHTTP(cdata[i]);
                cnames.Add(name);
                labelNicknames.Text += name + "\n";
            }
            if (cnames.Count == lines.Length)
                addlog("[NICKNAMES] [Synced]");
            else
                addlog("[NICKNAMES] [NOT Synced]");
            if (Servers.Initialize() == false)
            {
                addlog("servers.txt - not found");
                scanned_status = true;
                return;
            }
            addlog("Servers - OK (" + serverlist.Count.ToString() + ")");
            addlog("Starting");
            await Task.Delay(300);
            bool not_found = true;
            ServerQuery.A2S_INFO fServer = default(ServerQuery.A2S_INFO);
            Tuple<string, int> server_address = new Tuple<string, int>("0.0.0.0", 0);
            string fName = "";
            int fScore = 0;
            int fMinutes = 0;
            for (int i = 0; i < serverlist.Count && not_found == true; ++i)
            {
                lastlog(".scanning server #" + (i + 1).ToString());
                await Task.Delay(70);
                string ip = serverlist[i].Item1;
                int port = serverlist[i].Item2;
                ServerQuery.A2S_INFO srvinfo = new ServerQuery.A2S_INFO(new IPEndPoint(IPAddress.Parse(ip), port));
                srv_names_info.Add(srvinfo.Name);
                if (srvinfo.Players > 3) // more than 3 players on the server
                {
                    ServerQuery.A2S_PLAYER1 challenge = new ServerQuery.A2S_PLAYER1(new IPEndPoint(IPAddress.Parse(ip), port));
                    ServerQuery.A2S_PLAYER2 p = new ServerQuery.A2S_PLAYER2(new IPEndPoint(IPAddress.Parse(ip), port), challenge.Challenge);
                    for (int j = 0; j < p.Players && not_found == true; ++j)
                    {
                        await Task.Delay(20);
                        for (int k = 0; k < cnames.Count && not_found == true; ++k)
                        {
                            if (p.Name[j] == cnames[k])
                            {
                                fName = p.Name[j];
                                fScore = p.Score[j];
                                fMinutes = Convert.ToInt32(p.Duration[j]) / 60;
                                fServer = srvinfo;
                                server_address = new Tuple<string, int>(ip, port);
                                not_found = false;
                            }
                        }
                    }
                }
            }
            buttonServers.Visible = true;
            lastlog("Server scan - Finished");
            if (not_found)
            {
                addlog("[Result - not found]");
            }
            else
            {
                addlog("[RESULT - FOUND]");
                labelConnect.Text += "Server  : " + fServer.Name +
                    "\nPlayers : " + fServer.Players + '/' + fServer.MaxPlayers +
                    "\nMap     : " + fServer.Map +
                    "\nJoin    : connect " + server_address.Item1 + ':' + server_address.Item2 +
                    "\n\nNickname: " + fName +
                    "\n" + fScore + " kills | " + fMinutes + " minutes";
            }
            scanned_status = true;
        }

        protected void get_cdata(int i, String clink)
        {
            cdata[i] = Web.GetHTTPfromURL(clink);
        }

        private void addlog(String line)
        {
            if (loglines.Count > 17)
                loglines.RemoveAt(0);
            loglines.Add(line);
            labelLog.Text = "";
            for (int i = 0; i < loglines.Count; ++i)
                labelLog.Text += loglines[i] + "\n";
        }

        private void lastlog(String line)
        {
            loglines.RemoveAt(loglines.Count - 1);
            loglines.Add(line);
            labelLog.Text = "";
            for (int i = 0; i < loglines.Count; ++i)
                labelLog.Text += loglines[i] + "\n";
        }

        private void Form3_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void labelX_MouseEnter(object sender, EventArgs e)
        {
            Label lbl = sender as Label;
            if (lbl != null)
            {
                lbl.ForeColor = Color.FromArgb(255, 255, 255, 255);
                lbl.BackColor = Color.FromArgb(255, 50, 50, 50);
            }
        }

        private void labelX_MouseLeave(object sender, EventArgs e)
        {
            Label lbl = sender as Label;
            if (lbl != null)
            {
                lbl.ForeColor = Color.FromArgb(255, 222, 222, 222);
                lbl.BackColor = Color.FromArgb(255, 45, 45, 45);
            }
        }

        private void labelX_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void labelMinimize_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void buttonServers_Click(object sender, EventArgs e)
        {
            ServersForm serversForm = new ServersForm();
            serversForm.Show();
        }
    }
}
