using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace Chatter
{
    class User
    {
        public string Name;
        public IPAddress IP;
        public int tcpPort;

        private TcpClient tcpClient;
        public NetworkStream messageStream;

        public User(string login, IPEndPoint endPoint)
        {
            IP = endPoint.Address;
            tcpPort = 8002;
            this.Name = login;
        }
        public User(TcpClient tcpClient, int port)
        {
            this.tcpClient = tcpClient;
            tcpPort = port;
            IP = ((IPEndPoint)this.tcpClient.Client.RemoteEndPoint).Address;
            messageStream = this.tcpClient.GetStream();
        }
        public void EstablishConnection()
        {
            tcpClient = new TcpClient();
            tcpClient.Connect(new IPEndPoint(IP, tcpPort));
            messageStream = tcpClient.GetStream();
        }

        public void SendMessage(string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            this.messageStream.Write(data, 0, data.Length);
        }

        public string ReceiveMessage()
        {
            StringBuilder message = new StringBuilder();
            byte[] buff = new byte[1024];
            do
            {
                try
                {
                    int size = messageStream.Read(buff, 0, buff.Length);
                    message.Append(Encoding.UTF8.GetString(buff, 0, size));
                }
                catch
                {
                    return "1";
                }

            }
            while (messageStream.DataAvailable);

            string receiveMessage = message.ToString();

            return receiveMessage;
        }

        public void Disconnect()
        {
            tcpClient.Close();
        }
    }
}
