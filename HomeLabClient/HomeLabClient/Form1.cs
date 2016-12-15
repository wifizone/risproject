using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Messaging;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HomeLabClient
{
    public partial class Form1 : Form
    {
        MessageQueue mq = null;
        private Thread serverForServer = null;
        private string field = "";
        private bool gameContinue = false;
        private int clientX = 1, clientY = 1;
        private string player = "";
        private static string nameOfGame = "";
        public Form1()
        {            
            InitializeComponent();            
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //Random rn = new Random();
            //int m = rn.Next(1, 5000);
            int m = 1020;
            player = "Player" + m.ToString();
            this.Text += m.ToString();
        }

        private void ServerForGameServerOn(string gameName)
        {
            string path = Dns.GetHostName() + "\\private$\\Server" + gameName;
            if (MessageQueue.Exists(path))
                mq = new MessageQueue(path);
            else
                mq = MessageQueue.Create(path);

            mq.Formatter = new XmlMessageFormatter(new Type[] { typeof(String) });
        }

        private void ReadField()
        {
            while (gameContinue == true)
            {
                System.Messaging.Message msg = null;
                if (mq.Peek() != null)
                    msg = mq.Receive(TimeSpan.FromSeconds(5.0));
                field += msg.Body;
                button2.Invoke((MethodInvoker)delegate
                {
                    button2.Enabled = true;
                });
                label1.Invoke((MethodInvoker)delegate
                {
                    label1.Text = field.Split('/').ToArray()[0];
                    label1.Visible = true;
                });
                label2.Invoke((MethodInvoker)delegate
                {
                    label2.Text = field.Split('/').ToArray()[1];
                    label2.Visible = true;
                });
                this.Invalidate();

                Thread.Sleep(500);
            }
        }

        private void SendNewField(string gameNum)
        {
            MessageQueue message = null;
            string str = @".\private$\" + gameNum;
            if (MessageQueue.Exists(str))
                message = new MessageQueue(str);
            else
                message = MessageQueue.Create(str);
            // задаем форматтер сообщений в очереди
            message.Formatter = new XmlMessageFormatter(new Type[] { typeof(String) });

            //отправка сообщения
            message = new MessageQueue(str);
            message.Send(field, gameNum);
        }

        private string GetGameName (string player) //получаем имя игры, для создания очереди сообщений с сервером
        {
            Thread t = new Thread(WaitForGameName);
            t.Start();
            SendRequestToDispetcher(player);
            while (string.IsNullOrEmpty(nameOfGame)) ;
            return nameOfGame;
        }

        private void SendRequestToDispetcher(string player)//посылаем запрос на игру
        {
            TcpClient Client = new TcpClient();
            IPAddress IP;
            IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName());   
            IP = hostEntry.AddressList[0];                                  
            Client = new TcpClient();
            
            foreach (IPAddress address in hostEntry.AddressList)
                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    IP = address;
                    break;
                }
            try
            {
                int Port = 1010;                              
                Client.Connect(IP, Port);
            }
            catch
            {
                MessageBox.Show("Нет подключения к диспетчеру серверов");
                return;
            }
            byte[] buff = Encoding.Unicode.GetBytes(player); 
            Stream stm = Client.GetStream();                                                  
            stm.Write(buff, 0, buff.Length);
        }

        private void WaitForGameName()
        {
            IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress IP = hostEntry.AddressList[0];

            foreach (IPAddress address in hostEntry.AddressList)
                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    IP = address;
                    break;
                }

            TcpListener Listener = new TcpListener(IP, Convert.ToInt32(player.Substring(6)));
            Listener.Start();
            Socket ClientSock = Listener.AcceptSocket();

            while (string.IsNullOrEmpty(nameOfGame))
            {
                byte[] buff = new byte[1024];
                ClientSock.Receive(buff);
                nameOfGame = Encoding.Unicode.GetString(buff);
                nameOfGame = nameOfGame.Replace("\0", "");
            }
            Listener.Stop();
        }

        private void Vystrel()
        {
            string[] temp = field.Split('/').ToArray();
            if (temp[2 + (clientX - 1) * 5 + clientY - 1].ToCharArray()[2] == '1')
            {
                if (temp[0].Contains(player))
                {
                    int ochki = Convert.ToInt32(temp[0].Split(':').ToArray()[1].Trim());
                    ochki++;
                    temp[0] = temp[0].Split(':').ToArray()[0] + ": " + ochki.ToString();
                    label1.Text = temp[0];
                }
                else
                {
                    int ochki = Convert.ToInt32(temp[1].Split(':').ToArray()[1].Trim());
                    ochki++;
                    temp[1] = temp[1].Split(':').ToArray()[0] + ": " + ochki.ToString();
                    label2.Text = temp[1];
                }
            }
            temp[2 + (clientX - 1) * 5 + clientY - 1] = clientX.ToString() + clientY.ToString() + temp[2 + (clientX - 1) * 5 + clientY - 1].ToCharArray()[2].ToString() + 1;
            field = "";
            foreach (var str in temp)
                field += str + "/";
        }

        private void button1_Click(object sender, EventArgs e) //начать игру
        {
            string gameName = GetGameName(player);
            if (string.IsNullOrEmpty(gameName)) return;
            button1.Enabled = false;
            ServerForGameServerOn(gameName);
            gameContinue = true;
            button2.Visible = true;
            serverForServer = new Thread(ReadField);
            serverForServer.Start();
        }

        private void button2_Click(object sender, EventArgs e) //выстрел
        {
            button2.Enabled = false;
            Vystrel();
            SendNewField(nameOfGame);
            //gameContinue = false;
            this.Invalidate();
        }

        private void Form1_Paint(object sender, PaintEventArgs e) //прорисовка
        {
            Graphics g = e.Graphics;
            Rectangle blackRect = new Rectangle(18, 78, 104, 104);
            g.FillRectangle(Brushes.Black, blackRect);
            if (!string.IsNullOrEmpty(field))
                for (int n = 2; n < 27; n++)
                {
                    string str = field.Split('/').ToArray()[n];
                    if (str.ToCharArray()[2].Equals('1') && str.ToCharArray()[3].Equals('1')) //меткий выстрел
                    {
                        int x = Convert.ToInt32(str.ToCharArray()[0].ToString());
                        int y = Convert.ToInt32(str.ToCharArray()[1].ToString());
                        Rectangle greenRect = new Rectangle(20 * x, 60 + 20 * y, 20, 20);
                        g.FillRectangle(Brushes.Green, greenRect);
                    }
                    else if (str.ToCharArray()[2].Equals('0') && str.ToCharArray()[3].Equals('1')) //промах
                    {
                        int x = Convert.ToInt32(str.ToCharArray()[0].ToString());
                        int y = Convert.ToInt32(str.ToCharArray()[1].ToString());
                        Rectangle redRect = new Rectangle(20 * x, 60 + 20 * y, 20, 20);
                        g.FillRectangle(Brushes.Red, redRect);
                    }
                    else
                    {
                        int x = Convert.ToInt32(str.ToCharArray()[0].ToString());
                        int y = Convert.ToInt32(str.ToCharArray()[1].ToString());
                        Rectangle rect = new Rectangle(20 * x, 60 + 20 * y, 20, 20);
                        g.FillRectangle(Brushes.Aqua, rect);
                    }
                }
            if (!string.IsNullOrEmpty(field))
            {
                g.FillRectangle(Brushes.Yellow, new Rectangle((clientX * 20), (60 + clientY * 20), 20, 20));
            }
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e) //шаги
        {
            if (e.KeyCode == Keys.S)
            {                
                if (clientY != 5)
                {
                    clientY++;
                    this.Invalidate();
                }
            }
            if (e.KeyCode == Keys.W)
            {
                if (clientY != 1)
                {
                    clientY--;
                    this.Invalidate();
                }
            }
            if (e.KeyCode == Keys.A)
            {
                if (clientX != 1)
                {
                    clientX--;
                    this.Invalidate();
                }
            }
            if (e.KeyCode == Keys.D)
            {
                if (clientX != 5)
                {
                    clientX++;
                    this.Invalidate();
                }
            }
        }
    }
}
