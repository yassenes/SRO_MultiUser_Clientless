using SilkroadSecurityAPI;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SRO_Clientless
{
    public class Client
    {
        private readonly object _sendLock;

        public ClientType _clientType;

        private bool _isReady;

        private Timer _pingTimer;

        private byte[] _recvBytes;

        private int _recvRef;

        public string _username;

        public string _password;

        public uint _sessionId;

        public string charname;

        private Security _security;

        private Socket _socket;

        private Random rnd;

        public int _index;

        private Func<Client, Packet, bool> _packetProcessor;


        public string _serverIP;

        public ushort _serverPort;

        public Client(int i, ClientType clienttype, string username, string password, uint sessionId, string serverIP, ushort serverPort)
        {
            _serverIP = serverIP;
            _serverPort = serverPort;
            _index = i;
            _username = username;
            _password = password;
            _clientType = clienttype;
            _sessionId = sessionId;
            _recvBytes = new byte[4096];
            _security = null;
            _socket = null;
            _recvRef = 0;
            _isReady = false;
            _sendLock = new object();
            rnd = new Random();
        }

        public void Connect()
        {
            if (_socket != null)
            {
                throw new Exception("_socket is not null");
            }
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint lep = new IPEndPoint(IPAddress.Parse(_serverIP), _serverPort);
            try
            {
                _socket.BeginConnect(lep, new AsyncCallback(ConnectCallBack), null);
            }
            catch { }
            _security = new Security();
            WaitTillReady();

        }

        private void ConnectCallBack(IAsyncResult ar)
        {
            try
            {
                _socket.EndConnect(ar);
                Receive();
                Interlocked.Increment(ref Data.Users);
            }
            catch
            {
                new Client(_index, ClientType.GatewayServer, _username, _password, 0, _serverIP, 15779).Connect();
            }

        }

        public void WaitTillReady()
        {
            while (!_isReady)
            {
                Thread.Sleep(1);
            }
        }

        private void Receive()
        {
            IncreaseRef();

            try
            {
                _socket.BeginReceive(_recvBytes, 0, _recvBytes.Length, SocketFlags.None, EndReceive, null);
            }
            catch {
                DecreaseRef();
            }

        }

        private void EndReceive(IAsyncResult iar)
        {
            try
            {
                int num = _socket.EndReceive(iar);
                if (num > 0)
                {
                    _security.Recv(_recvBytes, 0, num);
                    List<Packet> list = _security.TransferIncoming();
                    if (list != null)
                    {
                        foreach (Packet item in list)
                        {
                            if (item.Opcode == 8193)
                            {
                                _isReady = true;
                                _pingTimer = new Timer(delegate
                                {
                                    SendPing();
                                }, null, 1000, 5000);

                                Console.WriteLine($"[{_clientType.ToString()}]: [{_index}] has connected sucesssfully.");
                            }
                            if (PacketHandler.Handler[item.Opcode] != null)
                            {
                                _packetProcessor = PacketHandler.Handler[item.Opcode];
                            }
                            else
                            {
                                _packetProcessor = PacketHandler.Handler[0];
                            }
                            _packetProcessor(this, item);
                        }
                    }
                    SendQueue();
                    Receive();
                }
            }
            catch
            {
            }
            finally
            {
                DecreaseRef();
            }
        }

        private void SendPing()
        {
            if (_socket == null || !_socket.Connected)
            {
                _pingTimer.Dispose();
            }
            else
            {
                SendPacket(new Packet(8194));
            }
        }

        public void SendPacket(Packet packet)
        {
            _security.Send(packet);
            SendQueue();
        }

        private void SendQueue()
        {
            try
            {
                lock (_sendLock)
                {
                    _security.TransferOutgoing()?.ForEach(delegate (KeyValuePair<TransferBuffer, Packet> kvp)
                    {
                        TransferBuffer key = kvp.Key;
                        PostSend(key.Buffer, key.Offset, key.Size);
                    });
                }
            }
            catch { }
        }

        private void PostSend(byte[] buffer, int offset, int size)
        {
            IncreaseRef();
            try
            {
                _socket.BeginSend(buffer, offset, size, SocketFlags.None, OnSend, null);
            }
            catch {
                DecreaseRef();
            }
        }

        private void OnSend(IAsyncResult iar)
        {
            try
            {
                _socket.EndSend(iar);
            }
            catch
            {
            }
            finally
            {
                DecreaseRef();
            }
        }

        private void IncreaseRef()
        {
            Interlocked.Increment(ref _recvRef);
        }

        private void DecreaseRef()
        {
            Interlocked.Decrement(ref _recvRef);
            if (_recvRef == 0)
            {
                try
                {
                    if (_socket != null)
                    {
                        _socket.Close();
                        Interlocked.Decrement(ref Data.Users);
                        if (_clientType == ClientType.AgentServer)
                        {
                            //Thread.Sleep(5000);
                            new Client(_index, ClientType.GatewayServer, _username, _password, 0, _serverIP, 15779).Connect();
                        }
                    }
                }
                catch
                {
                }
            }
        }
    }
}
