using System.Net;
using System.Net.Sockets;
using Netly;
using Netly.Core;
using test;

// ReSharper disable once CheckNamespace
namespace com.netly.udp;

public class Client
{
    private static readonly Host MyHost = new Host("127.0.0.1", 12444);

    [Fact]
    public void Dependencies()
    {
        Assert.NotNull(MyHost);
    }

    [Fact]
    public void Instantiate()
    {
        UDP.Client client = new UDP.Client();

        Assert.NotNull(client);
    }

    [Fact]
    public void Proprieties()
    {
        UDP.Client client = new UDP.Client();

        Assert.NotNull(client.On);
        Assert.NotNull(client.To);
        Assert.NotNull(client.Id);
        Assert.NotNull(client.Host);

        Assert.False(client.IsOpened);

        Assert.NotEmpty(client.Id);

        Assert.Equal(Host.Default.ToString(), client.Host.ToString());
    }

    [Fact]
    public async void Connect()
    {
        bool error = false;
        bool connected = false;
        bool modify = false;

        UDP.Client client = new UDP.Client();

        client.On.Modify(_ => modify = true);
        client.On.Open(() => connected = true);
        client.On.Error(_ => error = true);

        await client.To.Open(MyHost);
        await client.To.Close();

        Assert.True(modify);
        Assert.True(connected);
        Assert.False(error);
    }

    [Fact]
    public async void Disconnect()
    {
        bool modify = false;
        bool error = false;
        bool connected = false;
        bool disconnected = false;

        UDP.Client client = new UDP.Client();

        client.On.Modify(_ => modify = true);
        client.On.Open(() => connected = true);
        client.On.Error(_ => error = true);
        client.On.Close(() => disconnected = true);

        await client.To.Open(MyHost);
        await client.To.Close();

        Assert.True(modify);
        Assert.True(connected);
        Assert.True(disconnected);
        Assert.False(error);
    }

    [Fact]
    public async void Error()
    {
        bool error = false;
        bool connected = false;

        UDP.Client client = new UDP.Client();

        client.On.Open(() => connected = true);
        client.On.Error(_ => error = true);


        if (!OperatingSystem.IsWindows())
        {
            /*
             * WARNING:
             * windows allow open udp connection at 0 port.
             * this test is disabled for run in windows because will fail.
             */
            await client.To.Open(new Host("127.0.0.1", 0));
            await client.To.Close();
        }

        await client.To.Open(null);
        await client.To.Close();

        Assert.True(error);
        Assert.False(connected);
    }

    [Fact]
    public async void Modify()
    {
        bool connected = false;
        bool modify = false;
        bool disconnect = false;
        bool initSocketConnectedState = false;
        bool connectedSocketConnectedState = false;
        bool disconnectedSocketConnectedState = true;

        Socket? mySocket = null;

        UDP.Client client = new UDP.Client();

        client.On.Modify(socket =>
        {
            modify = true;
            mySocket = socket ?? null;
            initSocketConnectedState = socket?.Connected ?? false;
        });

        client.On.Open(() =>
        {
            connected = true;
            connectedSocketConnectedState = mySocket?.Connected ?? false;
        });

        client.On.Close(() =>
        {
            disconnect = true;
            disconnectedSocketConnectedState = mySocket?.Connected ?? true;
        });

        await client.To.Open(MyHost);
        await client.To.Close();

        Assert.True(modify);
        Assert.True(connected);
        Assert.True(disconnect);
        Assert.NotNull(mySocket);
        Assert.True(connectedSocketConnectedState);
        Assert.False(initSocketConnectedState);
        Assert.False(disconnectedSocketConnectedState);

        // check if object is disposable
        try
        {
            await mySocket.ConnectAsync(MyHost.EndPoint);
            Assert.Fail("Socket isn't disposable");
        }
        catch
        {
            // success: is disposed
        }
    }

    [Fact]
    public async void SendData()
    {
        // server
        bool sConnected = false;
        bool sError = false;
        bool sClosed = false;
        bool sAccept = false;
        // client
        bool cConnected = false;
        bool cError = false;
        bool cClosed = false;

        string sentData1 = Guid.NewGuid().ToString();
        string sentData2 = Guid.NewGuid().ToString();
        string recvData1 = string.Empty;
        string recvData2 = string.Empty;

        UDP.Server server = new();

        server.On.Open(() => sConnected = true);
        server.On.Close(() => sClosed = true);
        server.On.Error(e => sError = true);
        server.On.Accept((client) =>
        {
            int count = 0;
            client.On.Open(() => sAccept = true);

            client.On.Data((data) =>
            {
                count++;

                switch (count)
                {
                    case 1:
                        recvData1 = NE.GetString(data);
                        break;
                    case 2:
                        recvData2 = NE.GetString(data);
                        break;
                    default:
                        Assert.Fail(new NotSupportedException().ToString());
                        break;
                }
            });
        });

        // connect in random port
        await server.To.Open(new Host(IPAddress.Loopback, 0));

        Assert.True(sConnected);
        Assert.False(sError);
        Assert.False(sClosed);

        // connect a client
        UDP.Client client = new UDP.Client();

        client.On.Open(() => cConnected = true);
        client.On.Close(() => cClosed = true);
        client.On.Error((e) => cError = true);

        await client.To.Open(server.Host);

        Assert.True(cConnected);
        Assert.False(cError);
        Assert.False(cClosed);

        client.To.Data(sentData1);
        await Task.Delay(50); // prevent unreceived data and inverse data order
        client.To.Data(sentData2);
        await Task.Delay(50); // prevent unreceived data and inverse data order


        // server receive client connection
        Assert.True(sAccept);

        Assert.Equal(sentData1, recvData1);
        //    ^^^^^^ server receive data successful

        Assert.Equal(sentData2, recvData2);
        //    ^^^^^^ server receive data successful

        Assert.True(server.IsOpened);
        Assert.True(client.IsOpened);

        await server.To.Close();
        await client.To.Close();

        Assert.False(server.IsOpened);
        Assert.False(client.IsOpened);
    }

    [Fact]
    public async void SendEvent()
    {
        // server
        bool sConnected = false;
        bool sError = false;
        bool sClosed = false;
        bool sAccept = false;
        // client
        bool cConnected = false;
        bool cError = false;
        bool cClosed = false;

        var sentData1 = new KeyValuePair<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
        var sentData2 = new KeyValuePair<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
        var recvData1 = new KeyValuePair<string, string>(string.Empty, string.Empty);
        var recvData2 = new KeyValuePair<string, string>(string.Empty, string.Empty);

        UDP.Server server = new();

        server.On.Open(() => sConnected = true);
        server.On.Close(() => sClosed = true);
        server.On.Error(e => sError = true);
        server.On.Accept((client) =>
        {
            int count = 0;
            client.On.Open(() => sAccept = true);

            client.On.Event((name, data) =>
            {
                count++;

                switch (count)
                {
                    case 1:
                        recvData1 = new(name, NE.GetString(data));
                        break;
                    case 2:
                        recvData2 = new(name, NE.GetString(data));
                        break;
                    default:
                        Assert.Fail(new NotSupportedException().ToString());
                        break;
                }
            });
        });

        // connect in random port
        await server.To.Open(new Host(IPAddress.Loopback, 0));

        Assert.True(sConnected);
        Assert.False(sError);
        Assert.False(sClosed);

        // connect a client
        UDP.Client client = new UDP.Client();

        client.On.Open(() => cConnected = true);
        client.On.Close(() => cClosed = true);
        client.On.Error((e) => cError = true);

        await client.To.Open(server.Host);

        Assert.True(cConnected);
        Assert.False(cError);
        Assert.False(cClosed);

        client.To.Event(sentData1.Key, sentData1.Value);
        await Task.Delay(50); // prevent unreceived data and inverse data order
        client.To.Event(sentData2.Key, sentData2.Value);
        await Task.Delay(50); // prevent unreceived data and inverse data order


        // server receive client connection
        Assert.True(sAccept);

        Assert.Equal(sentData1.Key, recvData1.Key);
        Assert.Equal(sentData1.Value, recvData1.Value);
        //    ^^^^^^ server receive data successful

        Assert.Equal(sentData2.Key, recvData2.Key);
        Assert.Equal(sentData2.Value, recvData2.Value);
        //    ^^^^^^ server receive data successful

        Assert.True(server.IsOpened);
        Assert.True(client.IsOpened);

        await server.To.Close();
        await client.To.Close();

        Assert.False(server.IsOpened);
        Assert.False(client.IsOpened);
    }
    {
        throw new NotImplementedException();
    }
}