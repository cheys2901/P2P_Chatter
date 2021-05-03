using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Chatter
{
    public partial class frmMain : Form
    {
        private string MyName;
        public IPAddress MyIP;
        private bool alive = true;
        public int UDPPort = 8001;
        public int TCPPort = 8002;

        private static Task receiveThreadUDP;
        private static Task receiveThreadTCP;

        private Chatter chatter = new Chatter();

        public frmMain()
        {
            InitializeComponent();
            btnSend.Enabled = false;
            txtBoxMyMessage.Enabled = false;
        }

        private void SendMessageUDP(string message)
        {
            UdpClient sender = new UdpClient(); // создаем UdpClient для отправки сообщений
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                sender.Send(data, data.Length, GetBroadcastAdr(MyIP).ToString(), UDPPort); // отправка
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                sender.Close();
            }
        }

        private void ReceiveMessageUDP()
        {
            IPEndPoint remoteIp = new IPEndPoint(IPAddress.Any, UDPPort);
            UdpClient receiver = new UdpClient(new IPEndPoint(MyIP, UDPPort));
            while (alive)
            {
                byte[] data = receiver.Receive(ref remoteIp);
                string message = Encoding.UTF8.GetString(data);

                string toPrint = chatter.WhatIsThis(message, remoteIp);

                User newUser = new User(message.Substring(1), remoteIp);
                newUser.EstablishConnection();
                chatter.UserList.Add(newUser);
                newUser.SendMessage("0" + MyName);
                this.Invoke(new MethodInvoker(() =>
            {
                string time = DateTime.Now.ToShortTimeString();
                txtBoxChat.Text = $"{time} [{remoteIp.Address}] {toPrint}\r\n {txtBoxChat.Text}";
            }));
                Task.Factory.StartNew(() => ListenClient(newUser));

            }
            receiver.Close();
            receiver.Dispose();
        }

        private void ReceiveTCP()
        {
            TcpListener tcpListener = new TcpListener(MyIP, TCPPort);
            tcpListener.Start();

            while (true)
            {
                TcpClient tcpNewClient = tcpListener.AcceptTcpClient();
                User newUser = new User(tcpNewClient, TCPPort);

                Task.Factory.StartNew(() => ListenClient(newUser));
            }
        }

        private void ListenClient(User client)
        {
            while (alive)
            {
                string tcpMessage = client.ReceiveMessage();
                switch (tcpMessage[0])
                {
                    case '0':
                        {
                            client.Name = tcpMessage.Substring(1);
                            chatter.UserList.Add(client);
                            break;
                        }
                    case '1':
                        this.Invoke(new MethodInvoker(() =>
                        {
                            txtBoxChat.Text = $"{DateTime.Now.ToShortTimeString()} :  {client.Name} [{client.IP}] покинул чат\r\n" + txtBoxChat.Text;
                        }));

                        chatter.UserList.Remove(client);
                        return;

                    case '2':
                        this.Invoke(new MethodInvoker(() =>
                        {
                            txtBoxChat.Text = $"{DateTime.Now.ToShortTimeString()} :  {client.Name} [{client.IP}]: {tcpMessage.Substring(1)}\r\n" + txtBoxChat.Text;
                        }));
                        break;
                }
            }
        }

        public void SendMessageToAllClients(string tcpMessage)
        {
            foreach (var user in chatter.UserList)
            {
                try
                {
                    user.SendMessage(tcpMessage);
                }
                catch
                {
                    MessageBox.Show($"Не удалось отправить сообщение пользователю {user.Name}.");
                }
            }

            if (tcpMessage[0] == '2')
            {
                this.Invoke(new MethodInvoker(() =>
                {
                    txtBoxChat.Text = $"{DateTime.Now.ToShortTimeString()} :  Вы [{MyIP}]: {tcpMessage.Substring(1)}\r\n" + txtBoxChat.Text;
                }));
            }

        }

        public IPAddress GetIP()
        {
            string hostName = Dns.GetHostName();
            IPAddress[] ipAddrs = Dns.GetHostAddresses(hostName);
            //List<IPAddress> ipList = new List<IPAddress>();
            foreach (var item in ipAddrs)
            {
                if (item.AddressFamily == AddressFamily.InterNetwork)
                    return item;
            }
            return ipAddrs[0];
        }
        private void txtBoxMyMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (txtBoxMyMessage.Text == "")
                {
                    MessageBox.Show("Пустое сообщение");
                }
                else
                {
                    SendMessage();
                }
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            btnConnect.Enabled = false;
            txtBoxMyName.ReadOnly = true;
            btnSend.Enabled = true;
            txtBoxMyMessage.Enabled = true;

            MyName = txtBoxMyName.Text;
            MyIP = GetIP();
            alive = true;

            SendMessageUDP("0" + MyName);

            receiveThreadUDP = new Task(ReceiveMessageUDP);
            receiveThreadUDP.Start();

            txtBoxChat.Text = $"{DateTime.Now.ToShortTimeString()} : Вы [{MyIP}] подключились к чату\r\n" + txtBoxChat.Text;
            receiveThreadTCP = new Task(ReceiveTCP);
            receiveThreadTCP.Start();
        }
        private IPAddress GetBroadcastAdr(IPAddress ip)
        {
            string broadcastAddr = ip.ToString();
            broadcastAddr = broadcastAddr.Substring(0, broadcastAddr.LastIndexOf('.') + 1) + "255";

            return IPAddress.Parse(broadcastAddr);
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            alive = false;
            SendMessageToAllClients("1");
            foreach (var user in chatter.UserList)
            {
                user.Disconnect();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SendMessage();
        }

        private void SendMessage() {
            SendMessageToAllClients("2" + txtBoxMyMessage.Text);
            txtBoxMyMessage.Text = "";
        }
    }
}
