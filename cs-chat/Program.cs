using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Text;
using cs_chat;

IPAddress ipAddress = IPAddress.Loopback;

using Server server = new(ipAddress, 666);

server.Start();

