using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace cs_chat
{
    public class MessageWrapper
    {
        public ClientWrapper Client { get; set; }
        public byte[] Data { get; set; }
        public const int BUFF_SIZE = 1_024;
        public MessageWrapper(ClientWrapper client)
        {
            Client = client;
            Data = new byte[BUFF_SIZE];
        }
    }
    public class ClientWrapper
    {
        public Socket Handler { get; set; }
        public string Username { get; set; }
    }
    public enum GroupType
    {
        PRIVATE, PUBLIC
    }
    public class GroupWrapper : IEnumerable<ClientWrapper>
    {
        public string Groupname { get; set; }
        public GroupType Type { get; set; }
        private Dictionary<string, ClientWrapper> _clients = new Dictionary<string, ClientWrapper>();
        private Dictionary<string, ClientWrapper> _admins = new Dictionary<string, ClientWrapper>();
        public GroupWrapper(string groupname, ClientWrapper admin, GroupType type = GroupType.PUBLIC)
        {
            Groupname = groupname;
            Type = type;
            Add(admin);
            Promote(admin);
        }
        public void Add(ClientWrapper client)
        {
            _clients.Add(client.Username, client);
        }
        public void Remove(string username)
        {
            _clients.Remove(username);
        }
        public void Promote(ClientWrapper client)
        {
            _admins.Add(client.Username, client);
        }
        public void Promote(string username)
        {
            _admins.Add(username, _clients[username]);
        }

        public IEnumerator<ClientWrapper> GetEnumerator()
        {
            return _clients.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _clients .GetEnumerator();
        }

        public bool HasAdmin(ClientWrapper client)
        {
            return _admins.ContainsKey(client.Username);
        }
    }
}
