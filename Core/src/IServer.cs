﻿using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Zenet.Core
{
    public interface IServer
    {
        Socket Socket { get; }
        ZHost Host { get; }
        bool Opened { get; }

        // Controller
        void Open(ZHost host);
        void Close();
        void BroadcastToData(byte[] data);
        void BroadcastToEvent(string name, byte[] data);

        // Events        
        void OnOpen(Action callback);
        void OnError(Action<Exception> callback);
        void OnClose(Action callback);
        void BeforeOpen(Action<Socket> callback);
        void AfterOpen(Action<Socket> callback);

        // Client
        void OnEnter(Action<object> callback);
        void OnExit(Action<object> callback);
        void OnData(Action<object, byte[]> callback);
        void OnEvent(Action<object, string, byte[]> callback);
    }
}
