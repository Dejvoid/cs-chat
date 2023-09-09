using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Security.Cryptography;
using client_console;


var ipAddress = IPAddress.Loopback;

using Client cClient = new(ipAddress, 666);

string username = "";
while (username == "" || username.Contains(' '))
{
    Console.Write("Enter your username (must not contain spaces or emptystring): ");
    username = Console.ReadLine();
}
await cClient.ConnectAsync();
await cClient.SendAsync(username);
Console.WriteLine("> Successfully connected to server");

Thread t = new Thread(async () => await cClient.ReceivingAsync());
t.Start();

string msg = "";
msg = Console.ReadLine();
while (msg != "/exit")
{
    await cClient.SendAsync(msg);
    msg = Console.ReadLine();
}