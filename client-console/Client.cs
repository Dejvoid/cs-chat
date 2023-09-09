using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace client_console
{
    internal class Client : IDisposable
    {
        private bool _disposed;
        private IPEndPoint _endpoint;
        private Socket _socket;

        public Client(IPAddress ipAddress, int port)
        {
            _endpoint = new IPEndPoint(ipAddress, port);
            _socket = new(_endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _disposed = false;
        }

        /// <summary>
        /// Server connection establishment
        /// </summary>
        /// <returns></returns>
        public async Task ConnectAsync()
        {
            try
            {
                await _socket.ConnectAsync(_endpoint);
            }
            catch (SocketException)
            {
                await Console.Out.WriteLineAsync("> Couldn't connect to the server");
                return;
            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync(ex.ToString());
                throw;
            }
        }

        public void Dispose()
        {
            _disposed = true;
            _socket.Shutdown(SocketShutdown.Both);
            _socket.Dispose();
        }
        /// <summary>
        /// Loop for recieving messages from the server
        /// </summary>
        /// <returns></returns>
        public async Task ReceivingAsync()
        {
            try
            {
                while (!_disposed)
                {
                    var buffer = new byte[1_024];
                    var received = await _socket.ReceiveAsync(buffer, SocketFlags.None);
                    var response = Encoding.UTF8.GetString(buffer, 0, received);
                    Console.WriteLine(response);
                }
            }
            catch (SocketException)
            {
                //await Console.Out.WriteLineAsync(ex.ToString());
                await Console.Out.WriteLineAsync("> Lost connection to the server");
            }
        }
        /// <summary>
        /// Sends message <paramref name="msg"/> to the server
        /// </summary>
        /// <param name="msg">Text of the message</param>
        /// <returns></returns>
        internal async Task SendAsync(string msg)
        {
            try
            {
                var messageBytes = Encoding.UTF8.GetBytes(msg);
                _ = await _socket.SendAsync(messageBytes, SocketFlags.None);
            }
            catch (SocketException)
            {
                await Console.Out.WriteLineAsync("> Couldn't send the message");
            }
        }
    }
}
