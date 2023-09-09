using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace cs_chat
{
    interface IChatServer
    {
        public void AcceptCallback(IAsyncResult ar);
        public void ReceiveCallback(IAsyncResult ar);
        public void NotifyAll(MessageWrapper msg, string response);
        public void NotifyGroup(MessageWrapper msg, string response, string groupName);
        public void NotifyUser(MessageWrapper msg, string response, string username);

    }

    public class Server : IChatServer, IDisposable
    {
        private Socket _listener;
        private IPEndPoint _endpoint;
        private Dictionary<string,ClientWrapper> _clients;
        private Dictionary<string, GroupWrapper> _groups;
        public Server(IPAddress ip, int port) {
            _endpoint = new IPEndPoint(ip, port);
            _listener = new(_endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _clients = new Dictionary<string, ClientWrapper>();
            _groups = new Dictionary<string, GroupWrapper>();
        }
        /// <summary>
        /// Server main loop
        /// </summary>
        public void Start()
        {
            _listener.Bind(_endpoint);
            _listener.Listen(100);
            Console.WriteLine("Server started.");
            while (true)
            {
                _listener.BeginAccept(AcceptCallback, null);
            }
        }

        /// <summary>
        /// Callback method for new client connection
        /// </summary>
        /// <param name="ar">Connection information</param>
        public void AcceptCallback(IAsyncResult ar)
        {
            Socket handler = _listener.EndAccept(ar);
            
            byte[] userBytes = new byte[MessageWrapper.BUFF_SIZE/4];
            handler.Receive(userBytes);
            var username = Encoding.UTF8.GetString(userBytes).Replace("\0", string.Empty);
            Console.WriteLine("Client " + username + " connected.");
            var client = new ClientWrapper() { Handler = handler, Username = username };
            ServerToUser(client, "Connected to cs-chat! Welcome!");
            while (_clients.ContainsKey(username))
            {
                ServerToUser(client, "Username already taken, try another one");
                handler.Receive(userBytes);
                username = Encoding.UTF8.GetString(userBytes).Replace("\0", string.Empty);
                client.Username = username;
            }
            _clients.Add(client.Username, client);
            var msg = new MessageWrapper(client);
            handler.BeginReceive(msg.Data, 0, MessageWrapper.BUFF_SIZE, SocketFlags.None, ReceiveCallback, msg);
        }

        /// <summary>
        /// Callback method for new message received by the client
        /// </summary>
        /// <param name="ar">Message information and data</param>
        public void ReceiveCallback(IAsyncResult ar)
        {
            var msg = ar.AsyncState as MessageWrapper;
            if (!msg.Client.Handler.Connected)
            {
                return;
            }
            int read = msg.Client.Handler.EndReceive(ar);
            var msgText = Encoding.UTF8.GetString(msg.Data,0,read);

            Console.WriteLine("received message: " + msgText);
            if (msgText.StartsWith('/'))
            {
                Console.WriteLine("Command detected");
                ProcessCommand(msg, msgText.Substring(1));
            }
            else
                NotifyAll(msg, msgText);
            msg.Client.Handler.BeginReceive(msg.Data, 0, MessageWrapper.BUFF_SIZE, SocketFlags.None, ReceiveCallback, msg);

        }
        
        /// <summary>
        /// Method for processing client commands
        /// </summary>
        /// <param name="msg">Message information and data</param>
        /// <param name="msgText">Text of the message</param>
        private void ProcessCommand(MessageWrapper msg, string msgText)
        {
            var cmd = msgText.Split(' ')[0];
            if (cmd == "pm")
            {
                ProcessPmCommand(msg, msgText);
                
            }
            else if (cmd == "gm")
            {
                ProcessGmCommand(msg, msgText);
            }
            else if (cmd == "group")
            {
                ProcessGroupCommand(msg, msgText);
            }
            else if (cmd == "help")
                ProvideHelp(msg.Client.Handler);
            else
            {
                ServerToUser(msg.Client, "Wrong command, try /help");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="msgText"></param>
        private void ProcessGroupCommand(MessageWrapper msg, string msgText)
        {
            var tmp = msgText.Split(' ');
            if (tmp.Length < 2)
            {
                ProvideHelp(msg.Client.Handler);
                return;
            }
            var groupname = tmp[2];
            switch (tmp[1])
            {
                case "create":
                    if (!_groups.ContainsKey(groupname))
                    {
                        var grouptype = GroupType.PUBLIC;
                        if (tmp.Length >= 4 && tmp[3] == "-p")
                            grouptype = GroupType.PRIVATE;
                        GroupCreateCmd(groupname, msg.Client, grouptype);
                    }
                    else
                        ServerToUser(msg.Client, "Group " + groupname + " already exists");
                    break;
                case "join":
                    if (_groups.ContainsKey(groupname))
                        GroupJoinCmd(groupname, msg.Client);
                    else
                        ServerToUser(msg.Client, "Group "+groupname+" does not exist");
                    break;
                case "leave":
                    if (_groups.ContainsKey(groupname))
                        GroupLeaveCmd(groupname, msg.Client);
                    else
                        ServerToUser(msg.Client, "Group " + groupname + " does not exist");
                    break;
                case "add":
                    if (_groups.ContainsKey(groupname))
                        if (tmp.Length >= 4)
                        {
                            var username = tmp[3];
                            GroupAddCmd(groupname, username, msg.Client);
                        }
                        else
                            ProvideHelp(msg.Client.Handler);
                    else
                        ServerToUser(msg.Client, "Group " + groupname + " does not exist");
                    break;
                case "promote":
                    if (_groups.ContainsKey(groupname))
                        if (tmp.Length >= 4)
                        {
                            var username = tmp[3];
                            GroupPromote(groupname, username, msg.Client);
                        }
                        else
                            ProvideHelp(msg.Client.Handler);
                    else
                        ServerToUser(msg.Client, "Group " + groupname + " does not exist");
                    break;
                case "kick":
                    if (_groups.ContainsKey(groupname))
                        if (tmp.Length >= 4)
                        {
                            var username = tmp[3];
                            GroupKick(groupname, username, msg.Client);
                        }
                        else
                            ProvideHelp(msg.Client.Handler);
                    else
                        ServerToUser(msg.Client, "Group " + groupname + " does not exist");
                    break;
                default:
                    ProvideHelp(msg.Client.Handler);
                    break;
            }
        }

        private void GroupKick(string groupname, string username, ClientWrapper admin)
        {
            var group = _groups[groupname];
            if (group.HasAdmin(admin))
                group.Remove(username);
            else
                ServerToUser(admin, "You have no admin rights in this group");
        }

        private void GroupPromote(string groupname, string username, ClientWrapper admin)
        {
            var group = _groups[groupname];
            if (group.HasAdmin(admin))
                group.Promote(username);
            else
                ServerToUser(admin, "You have no admin rights in this group");
        }  

        private void GroupAddCmd(string groupname, string username, ClientWrapper client)
        {
            var group = _groups[groupname];
            if (group.Type == GroupType.PUBLIC || group.HasAdmin(client))
                _groups[groupname].Add(_clients[username]);
            else
                ServerToUser(client, "You have no rights to add people to this group");
        }

        private void GroupLeaveCmd(string groupname, ClientWrapper client)
        {
            _groups[groupname].Remove(client.Username);
        }

        private void GroupJoinCmd(string groupname, ClientWrapper client)
        {
            GroupWrapper group = _groups[groupname];
            if (group.Type == GroupType.PUBLIC)
                _groups[groupname].Add(client);
            else
                ServerToUser(client, "This group is private, ask admin of the group");
        }

        private void ServerToUser(ClientWrapper client, string msg)
        {
            client.Handler.SendAsync(Encoding.UTF8.GetBytes(msg), SocketFlags.None);
        }

        private void GroupCreateCmd(string groupname, ClientWrapper client, GroupType type = GroupType.PUBLIC)
        {
            GroupWrapper group = new GroupWrapper(groupname, client);
            _groups.Add(groupname, group);
        }

        /// <summary>
        /// Method for private message processing
        /// </summary>
        /// <param name="msg">Message info</param>
        /// <param name="msgText">Message text</param>
        private void ProcessPmCommand(MessageWrapper msg, string msgText)
        {
            var tmp = msgText.Split(' ');
            if (tmp.Length < 3) 
            {
                ProvideHelp(msg.Client.Handler);
                return;
            }
            NotifyUser(msg, msgText.Substring(tmp[0].Length + tmp[1].Length + 2), tmp[1]);
        }

        /// <summary>
        /// Method for group message processing
        /// </summary>
        /// <param name="msg">Message info</param>
        /// <param name="msgText">Message text</param>
        private void ProcessGmCommand(MessageWrapper msg, string msgText)
        {
            var tmp = msgText.Split(' ');
            if (tmp.Length < 3)
            {
                ProvideHelp(msg.Client.Handler);
                return;
            }
            NotifyGroup(msg, msgText.Substring(tmp[0].Length + tmp[1].Length + 2), tmp[1]);
        }

        /// <summary>
        /// Wrapper for help message
        /// </summary>
        /// <param name="handler"></param>
        private void ProvideHelp(Socket handler)
        {
            handler.SendAsync(Encoding.UTF8.GetBytes("Help: \n" +
                "'/pm <username> <msg>' to send private message \n" +
                "'/gm <groupname> <msg>' to send group message \n" +
                "'/help' to show this help"), SocketFlags.None);
        }
       
        /// <summary>
        /// Sends the <paramref name="msgText"/> to all users connected
        /// </summary>
        /// <param name="msg">Message information and data</param>
        /// <param name="msgText">Text of the message</param>
        public void NotifyAll(MessageWrapper msg, string msgText)
        {
            foreach (var client in _clients)
            {
                ServerToUser(client.Value, msg.Client.Username + ": " + msgText);
                //client.Value.Handler.SendAsync(Encoding.UTF8.GetBytes(msg.Client.Username + ": " + msgText), SocketFlags.None);
            }
        }
        
        /// <summary>
        /// Sends the <paramref name="msgText"/> to users in the group with <paramref name="groupName"/>
        /// </summary>
        /// <param name="msg">Message information and data</param>
        /// <param name="msgText">Text of the message</param>
        /// <param name="groupName">Target groupname</param>
        public void NotifyGroup(MessageWrapper msg, string msgText, string groupName)
        {
            foreach (var client in _groups[groupName])
            {
                ServerToUser(client, msg.Client.Username + "(" + groupName + "): " + msgText);
            }
        }
        
        /// <summary>
        /// Sends the <paramref name="msgText"/> to user with the <paramref name="username"/>
        /// </summary>
        /// <param name="msg">Message information and data</param>
        /// <param name="msgText">Text of the message</param>
        /// <param name="username">Target username</param>
        public void NotifyUser(MessageWrapper msg, string msgText, string username)
        {
            if (_clients.ContainsKey(username))
                ServerToUser(_clients[username], msg.Client.Username + "(private): " + msgText);
        }
        
        public void Dispose()
        {
            foreach (var client in _clients)
            {
                client.Value.Handler.Disconnect(false);
                client.Value.Handler.Dispose();
            }
            _listener.Dispose();
        }
    }
}
