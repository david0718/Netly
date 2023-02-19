﻿using Netly.Core;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Netly.Abstract
{
    public abstract class Client : IClient
    {
        #region Props

        public string UUID { get; protected set; }
        public Host Host { get; protected set; }

        public bool IsOpened => IsConnected();

        protected Socket m_socket;
        protected bool m_closed;
        protected bool m_closing;
        protected bool m_connecting;
        protected bool m_serverMode;

        protected private EventHandler onOpenHandler;
        protected private EventHandler<Exception> onErrorHandler;
        protected private EventHandler onCloseHandler;
        protected private EventHandler<byte[]> onDataHandler;
        protected private EventHandler<(string name, byte[] buffer)> onEventHandler;
        protected private EventHandler<Socket> onModifyHandler;

        #endregion
        protected virtual bool IsConnected()
        {
        }
        public virtual void Open(Host host)
        {
        }
        public virtual void Receive()
        {
        }
        public virtual void Close()
        {
        }
        public virtual void ToData(byte[] data)
        {
        }
        public virtual void ToData(string data)
        {
        }
        public virtual void ToEvent(string name, byte[] data)
        {
        }
        public virtual void ToEvent(string name, string data)
        {
        }
        public virtual void OnError(Action<Exception> callback)
        {
        }
        public virtual void OnOpen(Action callback)
        {
        }
        public virtual void OnClose(Action callback)
        {
        }
        public virtual void OnData(Action<byte[]> callback)
        {
        }
        public virtual void OnEvent(Action<string, byte[]> callback)
        {
        }
        public virtual void OnModify(Action<Socket> callback)
        {
            };
        }
    }
}
