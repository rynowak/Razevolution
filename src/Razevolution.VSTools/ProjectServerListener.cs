using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Razevolution.Tooling.Messages;

namespace Razevolution.VSTools
{
    public class ProjectServerListener
    {
        private readonly ConcurrentBag<ConnectionState> _connections;
        private readonly Action<ProjectServerClient> _onConnected;

        private TcpListener _listener;
        private Trace _trace;

        public ProjectServerListener(Trace trace, Action<ProjectServerClient> onConnected)
        {
            _trace = trace;
            _onConnected = onConnected;
            _connections = new ConcurrentBag<ConnectionState>();
        }

        public int Port
        {
            get
            {
                if (_listener == null)
                {
                    throw new InvalidOperationException();
                }

                return ((IPEndPoint)_listener.LocalEndpoint).Port;
            }
        }

        public void Start()
        {
            _listener = new TcpListener(IPAddress.Loopback, port: 0);
            _listener.Start();

            _trace.WriteLine($"listening on {_listener.LocalEndpoint}");

            _listener.BeginAcceptTcpClient(OnAccepted, state: null);
        }

        public void Broadcast(string type, Message message)
        {
            foreach (var client in _connections)
            {
                client.Client.Send(type, message);
            }
        }

        private void OnAccepted(IAsyncResult result)
        {
            try
            {
                var tcpClient = _listener.EndAcceptTcpClient(result);
                var stream = tcpClient.GetStream();

                var client = new ProjectServerClient(new BinaryWriter(stream));

                _onConnected(client);

                _connections.Add(new ConnectionState(tcpClient, stream, client));

                _trace.WriteLine($"accepted client on {tcpClient.Client.LocalEndPoint}");
            }
            catch (ObjectDisposedException)
            {
                _trace.WriteLine("listener disposed");

                // This will happen because we disposed the listener while we had a pending 'accept'.
                return;
            }

            _listener.BeginAcceptTcpClient(OnAccepted, state: null);
        }

        private class ConnectionState
        {
            public ConnectionState(TcpClient tcpClient, NetworkStream stream, ProjectServerClient client)
            {
                TcpClient = tcpClient;
                Stream = stream;
                Client = client;
            }

            public ProjectServerClient Client { get; }

            public NetworkStream Stream { get; }

            public TcpClient TcpClient { get; }
        }
    }
}
