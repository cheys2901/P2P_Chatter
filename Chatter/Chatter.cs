using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace Chatter
{
    // Коды сообщений:
    // 0 - вошёл в сеть, ищу собеседников
    // 1 - вышел из сети
    // 2 - сообщение обычное
    class Chatter
    {
        public List<User> UserList = new List<User>();

        public string AddUser(string Name, IPEndPoint IP)
        {
            UserList.Add(new User(Name, IP));
            return Name;
        }

        public void RemoveUser(IPEndPoint endPoint)
        {
            for (int i = 0; i < UserList.Count; i++)
            {
                if ((endPoint.Address == UserList[i].IP) && (endPoint.Port == UserList[i].tcpPort))
                {
                    UserList.RemoveAt(i);
                }
            }
        }

        public string WhatIsThis(string message, IPEndPoint adress)
        {
            if (message[0] == '0')
            {
                string name;
                name = message.Substring(1);
                //AddUser(name, adress);
                return name + " подключился к чату";
            }
            return "";
        }
    }
}