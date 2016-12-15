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

namespace HomeLabDispetcher
{
    public partial class Form1 : Form
    {
        private Socket ClientSock;                      
        private TcpListener Listener;
        private Thread ServerForClient = null;
        private bool _continue = false;
        int gameNumber = 0;
        private static Game newGame = new Game();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            _continue = true;
            ServerForClientOn();
        }

        private void ServerForClientOn() //сокет, ожидающий запросов на игру от клиентов
        {
            IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName());    
            IPAddress IP = hostEntry.AddressList[0];                        
            int Port = 1010;                                                
            
            foreach (IPAddress address in hostEntry.AddressList)
                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    IP = address;
                    break;
                }
            
            Listener = new TcpListener(IP, Port);
            Listener.Start();
            
            ServerForClient = new Thread(WaitRequestForGame);
            ServerForClient.Start();
        }

        private void WaitRequestForGame()
        {
            while (_continue)
            {
                ClientSock = Listener.AcceptSocket();         
                Thread t = new Thread(RecieveRequest);
                t.Start();
            }
        }

        private void RecieveRequest()
        {
            string msg = "";
            while (_continue)
            {
                byte[] buff = new byte[1024];
                ClientSock.Receive(buff);
                msg = Encoding.Unicode.GetString(buff);
                msg = msg.Replace("\0", "");
                if (!string.IsNullOrEmpty(msg))
                    DefineParticipantsInGame(msg);
                Thread.Sleep(500);
            }
        }

        private void DefineParticipantsInGame(string msg)
        {
            if (string.IsNullOrEmpty(newGame.Player1))
            {
                newGame.Player1 = msg;
                gameNumber++;
                newGame.Name = "game" + gameNumber.ToString();
                CreateNewGame(newGame);
            }
            else if (string.IsNullOrEmpty(newGame.Player2))
            {
                newGame.Player2 = msg;
                CreateNewGame(newGame);
                return;
            }
            else
            {
                newGame = new Game();
                newGame.Player1 = msg;
                gameNumber++;
                newGame.Name = "game" + gameNumber.ToString();
            }
        }

        private void CreateNewGame(Game game)
        {
            SendGameNameToClient(game.Name, game.Player1);
            //SendGameNameToClient(game.Name, game.Player2);
            SendGameNameToServer(game.Name, game.Player1, "Player2");
        }

        private void SendGameNameToClient(string gameName, string player)
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
                int Port = Convert.ToInt32(player.Substring(6));
                Client.Connect(IP, Port);
            }
            catch
            {
                MessageBox.Show("Нет подключения диспетчера к клиенту");
                return;
            }
            byte[] buff = Encoding.Unicode.GetBytes(gameName);
            Stream stm = Client.GetStream();
            stm.Write(buff, 0, buff.Length);
        }

        private void SendGameNameToServer(string gameName, string player1, string player2)
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
                int Port = 1011;
                Client.Connect(IP, Port);
            }
            catch
            {
                MessageBox.Show("Нет подключения диспетчера к серверу");
                return;
            }
            byte[] buff = Encoding.Unicode.GetBytes(gameName + "|" + player1 + "|" + player2);
            Stream stm = Client.GetStream();
            stm.Write(buff, 0, buff.Length);
        }
    }
}
