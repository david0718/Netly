﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Netly.Core;
using Netly.Interfaces;

namespace Netly
{
    public partial class UDP
    {
        public partial class Server
        {
            private class ServerTo : IUDP.ServerTo
            {
                private readonly Server _server;

                private bool _isClosed, _isOpeningOrClosing;

                private Socket _socket;

                private ServerTo()
                {
                    _server = null;
                    _socket = null;
                    _isClosed = true;
                    _isOpeningOrClosing = false;
                    Host = Host.Default;
                    Clients = new List<Client>();
                }

                public ServerTo(Server server) : this()
                {
                    _server = server;
                }

                private ServerOn On => _server._on;
                public bool IsOpened => !_isClosed && _socket != null;
                public Host Host { get; set; }
                public List<Client> Clients { get; set; }

                public Task Open(Host host)
                {
                    if (_isOpeningOrClosing || !_isClosed) return Task.CompletedTask;

                    _isOpeningOrClosing = true;

                    return Task.Run(() =>
                    {
                        try
                        {
                            _socket = new Socket(host.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

                            On.OnModify?.Invoke(null, _socket);

                            _socket.Bind(host.EndPoint);

                            Host = new Host(_socket.LocalEndPoint);

                            _isClosed = false;

                            InitAccept();

                            On.OnOpen?.Invoke(null, null);
                        }
                        catch (Exception e)
                        {
                            _isClosed = true;
                            On.OnError?.Invoke(null, e);
                        }
                        finally
                        {
                            _isOpeningOrClosing = false;
                        }
                    });
                }

                public Task Close()
                {
                    if (_isClosed || _isOpeningOrClosing) return Task.CompletedTask;

                    _isOpeningOrClosing = true;

                    return Task.Run(async () =>
                    {
                        try
                        {
                            _socket?.Shutdown(SocketShutdown.Both);

                            foreach (var client in Clients) await client.To.Close();

                            Clients.Clear();

                            _socket?.Close();
                            _socket?.Dispose();
                        }
                        catch
                        {
                            // Ignored
                        }
                        finally
                        {
                            _socket = null;
                            _isClosed = true;
                            _isOpeningOrClosing = false;
                            On.OnClose?.Invoke(null, null);
                        }
                    });
                }

                public void DataBroadcast(byte[] data)
                {
                    if (!IsOpened || data == null) return;

                    Broadcast(data);
                }

                public void DataBroadcast(string data, NE.Encoding encoding = NE.Encoding.UTF8)
                {
                    if (!IsOpened || data == null) return;

                    Broadcast(NE.GetBytes(data, encoding));
                }

                public void EventBroadcast(string name, byte[] data)
                {
                    if (!IsOpened || name == null || data == null) return;

                    Broadcast(EventManager.Create(name, data));
                }

                public void EventBroadcast(string name, string data, NE.Encoding encoding = NE.Encoding.UTF8)
                {
                    if (!IsOpened || name == null || data == null) return;

                    Broadcast(EventManager.Create(name, NE.GetBytes(data, encoding)));
                }

                public void Data(Host targetHost, byte[] data)
                {
                    if (!IsOpened || targetHost == null || data == null) return;

                    Send(targetHost, data);
                }

                public void Data(Host targetHost, string data, NE.Encoding encoding = NE.Encoding.UTF8)
                {
                    if (!IsOpened || targetHost == null || data == null) return;

                    Send(targetHost, NE.GetBytes(data, encoding));
                }

                public void Event(Host targetHost, string name, byte[] data)
                {
                    if (!IsOpened || targetHost == null || name == null || data == null) return;

                    Send(targetHost, EventManager.Create(name, data));
                }

                public void Event(Host targetHost, string name, string data, NE.Encoding encoding = NE.Encoding.UTF8)
                {
                    if (!IsOpened || targetHost == null || name == null || data == null) return;

                    Send(targetHost, EventManager.Create(name, NE.GetBytes(data, encoding)));
                }

                private void Broadcast(byte[] data)
                {
                    try
                    {
                        if (Clients.Count > 0)
                            foreach (var client in Clients)
                                client?.To.Data(data);
                    }
                    catch (Exception e)
                    {
                        NETLY.Logger.PushError(e);
                    }
                }

                private void Send(Host host, byte[] bytes)
                {
                    if (bytes == null || bytes.Length <= 0 || !IsOpened || host == null) return;

                    try
                    {
                        _socket?.BeginSendTo
                        (
                            bytes,
                            0,
                            bytes.Length,
                            SocketFlags.None,
                            host.EndPoint,
                            null,
                            null
                        );
                    }
                    catch (Exception e)
                    {
                        NETLY.Logger.PushError(e);
                    }
                }


                private void InitAccept()
                {
                    new Thread(AcceptJob)
                    {
                        IsBackground = true,
                        Priority = ThreadPriority.Highest
                    }.Start();
                }

                private void AcceptJob()
                {
                    var length = (int)_socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer);
                    var buffer = new byte[length];
                    var point = Host.EndPoint;

                    while (IsOpened)
                        try
                        {
                            var size = _socket.ReceiveFrom
                            (
                                buffer,
                                0,
                                buffer.Length,
                                SocketFlags.None,
                                ref point
                            );

                            if (size <= 0) continue;

                            var data = new byte[size];

                            Array.Copy(buffer, 0, data, 0, data.Length);

                            var newHost = new Host(point);

                            // Find a client
                            var client = Clients.FirstOrDefault(x => Host.Equals(newHost, x.Host));

                            if (client == null)
                            {
                                // Create new client
                                client = new Client(ref newHost, ref _socket);

                                Clients.Add(client);

                                On.OnAccept?.Invoke(null, client);

                                client.On.Close(() => Clients.Remove(client));

                                client.InitServerSide();
                            }

                            // publish data for client
                            client.OnServerBuffer(ref data);
                        }
                        catch (Exception e)
                        {
                            NETLY.Logger.PushError(e);
                        }
                }
            }
        }
    }
}