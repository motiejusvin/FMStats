using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.Net.Sockets;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Security;

namespace GoldFmCollector
{
    public partial class Service1 : ServiceBase
    {

        StreamWriter sw;
        BackgroundWorker goldworker = new BackgroundWorker();
        BackgroundWorker m1plus = new BackgroundWorker();

        public void OpenDB()
        {

            MySqlConnection con = new MySqlConnection("server=localhost;database=fmstats;uid=gold;pwd=localhost");
            try
            {
                con.Open();
            }
            catch (MySqlException)
            {
                using (StreamWriter writes = new StreamWriter(@"C:\Log.txt", true))
                {
                    writes.WriteLine(DateTime.Now + " " + "Couldn't Connect to MySQL database. Check if database is online");
                }
                return;
            }
            if (con.State == ConnectionState.Open)
            {
                using (StreamWriter writes = new StreamWriter(@"C:\Log.txt", true))
                {
                    writes.Write(DateTime.Now + " " + "Succesfully connected To MySQL Database");
                }
                main();
            }
           
        }
        public Service1()
        {
            InitializeComponent();
        }

        private void m1plus_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            throw new NotImplementedException();
        }
        private void BackgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            throw new NotImplementedException();
        }

        public void goldworker_DoWork(object sender, DoWorkEventArgs e)
        {
            MySqlConnection con = new MySqlConnection("server=localhost;database=fmstats;uid=gold;pwd=localhost");
            MySqlCommand goldcom;
            string previuos = "";
            System.Net.WebClient client = new WebClient();
            Random random = new Random();
            string response = "";
            bool isconnected = true;
            DateTime nextdatetime = DateTime.Now;
            while (true)
            {
                if (!con.Ping())
                {

                    isconnected = false;
                    using (StreamWriter writes = new StreamWriter(@"C:\Log.txt", true))
                    {
                        writes.WriteLine(DateTime.Now + " " + "Failed To Connect to MySQL Database. Retrying");
                    }
                    try
                    {
                        con.Close();
                        con.Open();
                    }
                    catch (MySqlException)
                    {

                    }
                }

                if (!isconnected && con.Ping())
                {
                    isconnected = true;
                    using (StreamWriter writes = new StreamWriter(@"C:\Log.txt", true))
                    {
                        writes.WriteLine(DateTime.Now + " " + "Connected Succesfully", 0);
                    }
                    }
                if (nextdatetime <= DateTime.Now)
                {
                    try
                    {
                        response =
                           client.DownloadString("http://goldfm.lt/wp-content/themes/goldfm/radio/php/radio-get.php");

                    }
                    catch
                    {
                        using (StreamWriter writes = new StreamWriter(@"C:\Log.txt",true))
                        {
                            writes.WriteLine(DateTime.Now + " " + "Cannot Connect to GoldFM");
                        }
                    }
                    if (md5h(response) != md5h(previuos))
                    {
                        nextdatetime = DateTime.Now.AddSeconds(32 + random.Next(0, 15));
                        previuos = response;
                        string cmdstring = "INSERT INTO goldfm (Date, Hash, Name) VALUES (now(),'" + md5h(response) + "','" + MySqlHelper.EscapeString(response) + "')";
                        goldcom = new MySqlCommand(cmdstring, con);
                        goldcom.ExecuteNonQuery();
                    }
                }
                Thread.Sleep(1000);
            }
        }
        public void m1plus_DoWork(object sender, DoWorkEventArgs e)
        {
            JObject o;
            MySqlConnection con = new MySqlConnection("server=localhost;database=fmstats;uid=gold;pwd=localhost");
            MySqlCommand m1pCom;
            string prev = "";
            Random random = new Random();
            WebClient clent = new WebClient();
            string response = "";
            DateTime nexdatetime = DateTime.Now;
            bool isconnected = true;
            while (true)
            {
                if (!con.Ping())
                {
                    isconnected = false;
                    using (StreamWriter writes = new StreamWriter(@"C:\Log.txt", true))
                    {
                        writes.WriteLine(DateTime.Now + " " + "Failed To Connect to MySQL Database. Retrying");
                    }
                    try
                    {
                        con.Close();
                        con.Open();
                    }
                    catch (MySqlException)
                    {
                    }
                }
                if (!isconnected && con.Ping())
                {
                    isconnected = true;
                    using (StreamWriter writes = new StreamWriter(@"C:\Log.txt", true))
                    {
                        writes.WriteLine(DateTime.Now + " " + "Connected Succesfully", 0);
                    }
                }
                //
                if (nexdatetime <= DateTime.Now)
                {
                    try
                    {
                        response = clent.DownloadString("http://localhost/m1plusdat.php");

                    }
                    catch(Exception ex)
                    {
                        using (StreamWriter writes = new StreamWriter(@"C:\Log.txt", true))
                        {
                            writes.WriteLine(DateTime.Now + " " + "Cannot Connect to M1 " + ex);
                        }
                           
                    }
                    try
                    {
                         o = JObject.Parse(response);
                    }
                    catch(Exception ex)
                     {
                        using (StreamWriter writes = new StreamWriter(@"C:\Log.txt", true))
                        {
                            writes.WriteLine(DateTime.Now + " " + "Cannot Connect to M1 " + ex);
                        }

                     }
                    o = JObject.Parse(response);
                    string track = o["track"].ToString();
                     if (track != "")
                     {
                        string result = o["author"] + " - " + o["track"];
                            if (md5h(result) != md5h(prev))
                            {
 
                                nexdatetime = DateTime.Now.AddSeconds(30 + random.Next(0, 15));
                                string artist = o["author"].ToString();
                                prev = result;
                                string cmd = "INSERT INTO m1plus(Date, Hash, Artist, Song) VALUES(now(), '" + md5h(response) + "', '" + MySqlHelper.EscapeString(artist) + "', '" + MySqlHelper.EscapeString(track) + "')";
                                m1pCom = new MySqlCommand(cmd, con);
                                m1pCom.ExecuteNonQuery();
                            }
                        }
                    
                }
                Thread.Sleep(1000);
            }
        }

        protected override void OnStart(string[] args)
        {
            using (StreamWriter writes = new StreamWriter(@"C:\Log.txt", true))
            {
                writes.WriteLine(DateTime.Now + " " + "Starting");
            }
            OpenDB();
        }
        public void main()
        {
            //GoldFM worker
            goldworker.DoWork += goldworker_DoWork;
            goldworker.RunWorkerCompleted += BackgroundWorker1_RunWorkerCompleted;
            goldworker.WorkerReportsProgress = true;
            goldworker.WorkerSupportsCancellation = true;
            goldworker.RunWorkerAsync();

            //M1+ Worker
            m1plus.DoWork += m1plus_DoWork;
            m1plus.RunWorkerCompleted += m1plus_RunWorkerCompleted;
            m1plus.WorkerReportsProgress = true;
            m1plus.WorkerSupportsCancellation = true;
            m1plus.RunWorkerAsync();
        }

        public static string md5h(string input)
        {
            using (var md5 = MD5.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(input);
                var hashb = md5.ComputeHash(bytes);
                var hash = BitConverter.ToString(hashb).Replace("-", String.Empty);
                //Console.WriteLine(hash);
                return hash;
            }
        }

        protected override void OnStop()
        {
            using (StreamWriter writes = new StreamWriter(@"C:\Log.txt", true))
            {
                writes.WriteLine(DateTime.Now + " " + "Stopping");
            }
        }
    }
}
