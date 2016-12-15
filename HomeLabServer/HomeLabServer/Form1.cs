using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Messaging;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HomeLabServer
{
    public partial class Form1 : Form
    {
        //private static string player1 = "", player2 = "";

        //private static GameLogic sf = new GameLogic();
        //private string field = "";

        private MessageQueue mq = null;
        private Thread serverForClient = null;

        private Socket ClientSock;
        private TcpListener Listener;
        private Thread ServerForDispetcher = null;
        private bool _continue = false;

        private bool game = false;
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            _continue = true;
            ServerForDispetcherOn();
            //player1 = "Игрок 1";
            //player2 = "Игрок 2";
            //field = sf.generateField(player1, player2);
            //SendField("game123");
        }

        private void serverForClientOn(string gameNum)
        {
            string path = Dns.GetHostName() + "\\private$\\" + gameNum;  
            if (MessageQueue.Exists(path))
                mq = new MessageQueue(path);
            else
                mq = MessageQueue.Create(path);
            
            mq.Formatter = new XmlMessageFormatter(new Type[] { typeof(String) });
            game = true;
            serverForClient = new Thread(ReadField);
            serverForClient.Start();
        }

        private void SendField(string gameNum, string field)
        {
            MessageQueue message = null;
            string str = @".\private$\Server" + gameNum;
            if (MessageQueue.Exists(str))
                message = new MessageQueue(str);
            else
                message = MessageQueue.Create(str);
            // задаем форматтер сообщений в очереди
            message.Formatter = new XmlMessageFormatter(new Type[] { typeof(String) });

            //отправка сообщения
            message = new MessageQueue(str);
            message.Send(field, Dns.GetHostName());
        }

        private void ReadField()
        {
            while (game)
            {
                System.Messaging.Message msg = null;
                if (mq.Peek() != null)
                    msg = mq.Receive(TimeSpan.FromSeconds(5.0));
                string text = "";
                text += msg.Body;
                SendField(msg.Label, text);
                Thread.Sleep(500);
            }
        }

        private void ServerForDispetcherOn()
        {
            IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress IP = hostEntry.AddressList[0];
            int Port = 1011;

            foreach (IPAddress address in hostEntry.AddressList)
                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    IP = address;
                    break;
                }

            Listener = new TcpListener(IP, Port);
            Listener.Start();

            ServerForDispetcher = new Thread(WaitForNewGameRequest);
            ServerForDispetcher.Start();
        }

        private void WaitForNewGameRequest()
        {
            while (_continue)
            {
                ClientSock = Listener.AcceptSocket();
                Thread t = new Thread(RecieveNewGameRequest);
                t.Start();
            }
        }

        private void RecieveNewGameRequest()
        {
            string msg = "";

            while (_continue)
            {
                byte[] buff = new byte[1024];
                ClientSock.Receive(buff);
                msg = Encoding.Unicode.GetString(buff);
                msg = msg.Replace("\0", "");
                if (!string.IsNullOrEmpty(msg))
                    CreateNewGame(msg);
                Thread.Sleep(500);
            }
        }

        private void CreateNewGame(string msg)
        {
            GameLogic gl = new GameLogic();
            gl.GameNum = msg.Split('|').ToArray()[0];
            gl.Field = gl.generateField(msg.Split('|').ToArray()[1], msg.Split('|').ToArray()[2]);
            serverForClientOn(gl.GameNum);
            SendField(gl.GameNum, gl.Field);
        }
    }
}
