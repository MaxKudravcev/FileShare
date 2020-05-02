using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace FileShare1.Model.FTPServer
{
    class FTPServer
    {
        private TcpListener listener;

        public FTPServer()
        {

        }

        public void Start()
        {
            listener = new TcpListener(IPAddress.Any, 2121);
            listener.Start();
            listener.BeginAcceptTcpClient(HandleAcceptTcpClient, listener);
        }

        public void Stop()
        {
            if (listener != null)
                listener.Stop();
        }

        private void HandleAcceptTcpClient(IAsyncResult result)
        {
            TcpClient client = listener.EndAcceptTcpClient(result);
            listener.BeginAcceptTcpClient(HandleAcceptTcpClient, listener);

            ClientConnection connection = new ClientConnection(client);
            ThreadPool.QueueUserWorkItem(connection.HandleClient, client);
        }
    }
}
