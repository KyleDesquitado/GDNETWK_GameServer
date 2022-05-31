﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace GDNETWK_GameServer
{
    class Server
    {
        public static RiddleGenerator rg = new RiddleGenerator();
        public static int MaxPlayers { get; private set; }
        public static int Port { get; private set; }
        public static Dictionary<int, Client> clients = new Dictionary<int, Client>();
        public delegate void PacketHandler(int _fromClient, Packet _packet);
        public static Dictionary<int, PacketHandler> packetHandlers;

        private static TcpListener tcListener;
        private static UdpClient udpListener;

        

        public static void Start(int _maxPLayers, int _port)
        {

            MaxPlayers = _maxPLayers;
            Port = _port;

            Console.WriteLine("Starting Server..");
            InitializeServerData();

            tcpListener = new TcpListener(IPAddress.Any, Port);
            tcpListener.Start();
            tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);

            udpListener = new UdpClient(Port);
            udpListener.BeginReceive(UDPReceiveCallback, null);

            Console.WriteLine($"Server started on {Port}.");
            

        }

        private static void TCPConnectCallback(IAsyncResult _result)
        {
            
            TcpClient _client = tcpListener.EndAcceptTcpClient(_result);
            tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);
            Console.WriteLine($"Incoming connection from {_client.Client.RemoteEndPoint}");
            for (int i = 1; i <= MaxPlayers; i++)
            {
                if(clients[i].tcp.socket == null)
                {
                    clients[i].tcp.Connect(_client);
                    return;
                }
            }

            Console.WriteLine($"{_client.Client.RemoteEndPoint} failed to connect: Server full");
        }

        private static void UDPReceiveCallback(IAsyncResult _result)
        {
            try
            {
                IPEndPoint _clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] _data = udpListener.EndReceive(_result, ref _clientEndPoint);
                udpListener.BeginReceive(UDPReceiveCallback, null);

                if(_data.Length < 4)
                {
                    return;
                }

                using (Packet _packet = new Packet(_data))
                {
                    int _clientId = _packet.ReadInt();

                    if(_clientId == 0)
                    {
                        return;
                    }

                    if (clients[_clientId].udp.endPoint == null)
                    {
                        clients[_clientId].udp.Connect(_clientEndPoint);
                        return;
                    }

                    if(clients[_clientId].udp.endPoint.ToString() == _clientEndPoint.ToString())
                    {
                        clients[_clientId].udp.HandleData(_packet);
                    }
                }
            }

            catch(Exception _ex)
            {
                Console.WriteLine($"Error receiving udp data: {_ex}");
            }

        }

        public static void SendUDPData(IPEndPoint _cliendEndPoint,Packet _packet)
        {
            try
            {
                if(_cliendEndPoint != null)
                {
                    udpListener.BeginSend(_packet.ToArray(), _packet.Length(), _cliendEndPoint, null, null);
                }
            }

            catch(Exception _ex)
            {
                Console.WriteLine($"Error sending data to server via udp: {_ex}");
            }
        }
        private static void InitializeServerData()
        {
            for(int i = 1; i <=MaxPlayers; i++)
            {
                clients.Add(i, new Client(i));
            }

            packetHandlers = new Dictionary<int, PacketHandler>()
            {
                { (int)ClientPackets.welcomeReceived, ServerHandler.TCPTestReceived },
                { (int)ClientPackets.udpTestReceived, ServerHandler.UDPTestReceived }

            };

            Console.WriteLine("Initialized Packets");
        }
    }


}
