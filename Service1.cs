using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data;
using MySql.Data.MySqlClient;

namespace GoldFmCollector
{
    public partial class Service1 : ServiceBase
    {
        BackgroundWorker backgroundWorker1 = new BackgroundWorker();
        public Service1()
        {
            InitializeComponent();
        }

        private void BackgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void BackgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {

            MySqlConnection con = new MySqlConnection("server=localhost;database=gold;uid=gold;pwd=localhost");
            MySqlCommand command;
            try
            {
                con.Open();
            }
            catch (MySqlException)
            {
                Console.WriteLine("Couldn't Connect to MySQL database. Check if database is online");
                Console.ReadKey();
                return;
            }
            if (con.State == ConnectionState.Open)
            {
                Console.WriteLine("Succesfully connected To MySQL Database");
            }
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
                    Console.WriteLine("Failed To Connect to MySQL Database. Retrying");
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
                    Console.WriteLine("Connected Succesfully");
                }
                if (nextdatetime <= DateTime.Now)
                {
                    response =
                       client.DownloadString("http://goldfm.lt/wp-content/themes/goldfm/radio/php/radio-get.php");
                    nextdatetime = DateTime.Now.AddSeconds(60);
                    if (md5h(response) != md5h(previuos))
                    {
                        nextdatetime = DateTime.Now.AddSeconds(60 + random.Next(0, 30));
                        previuos = response;
                        string cmdstring = "INSERT INTO goldfm (Date, Hash, Name) VALUES (now(),'" + md5h(response) + "','" + MySqlHelper.EscapeString(response) + "')";
                        command = new MySqlCommand(cmdstring, con);
                        command.ExecuteNonQuery();
                    }
                }
                Thread.Sleep(1000);
            }
        }

        protected override void OnStart(string[] args)
        {
           main();
        }
        public void main()
        {
            backgroundWorker1.DoWork += BackgroundWorker1_DoWork;
            backgroundWorker1.RunWorkerCompleted += BackgroundWorker1_RunWorkerCompleted;
            backgroundWorker1.WorkerReportsProgress = true;
            backgroundWorker1.WorkerSupportsCancellation = true;

            backgroundWorker1.RunWorkerAsync();
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
        }
    }
}
