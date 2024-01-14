﻿using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Netly.Core;
using NativeHttpClient = System.Net.Http.HttpClient;

namespace Netly
{
    public class HttpClient : IHttpClient
    {
        private int _timeout;

        public int Timeout
        {
            get => _timeout;
            set
            {
                if (value <= -2) // -1 is null or not timeout
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                _timeout = value;
            }
        }

        public KeyValueContainer<string> Headers { get; }
        public KeyValueContainer<string> Queries { get; }
        public RequestBody Body { get; set; }
        public HttpMethod Method { get; private set; }


        private EventHandler<Request> _onSuccess;
        private EventHandler<Exception> _onError;
        private EventHandler<NativeHttpClient> _onModify;

        public HttpClient()
        {
            Headers = new KeyValueContainer<string>();
            Queries = new KeyValueContainer<string>();
            Body = new RequestBody();
            Method = HttpMethod.Get;
        }

        public void OnSuccess(Action<Request> callback)
        {
            if (callback == null) return;

            _onSuccess += (_, request) => MainThread.Add(() => callback?.Invoke(request));
        }

        public void OnError(Action<Exception> callback)
        {
            _onError += (_, exception) => MainThread.Add(() => callback?.Invoke(exception));
        }

        public void OnModify(Action<NativeHttpClient> callback)
        {
            _onModify += (_, httpClient) => MainThread.Add(() => callback?.Invoke(httpClient));
        }

        public void Send(string method, Uri uri)
        {
            Task.Run(async () =>
            {
                try
                {
                    if (uri == null) throw new NullReferenceException(nameof(uri));

                    string methodStr = string.IsNullOrWhiteSpace(method)
                        ? throw new ArgumentNullException(nameof(method))
                        : method.Trim().ToUpper();

                    if (Queries != null && Queries.Length > 0)
                    {
                        var uriBuilder = new UriBuilder(uri);
                        var queryBuilder = HttpUtility.ParseQueryString(uriBuilder.Query);

                        foreach (var query in Queries.AllKeyValue)
                        {
                            if (!string.IsNullOrWhiteSpace(query.Key))
                            {
                                queryBuilder.Add(query.Key, query.Value ?? string.Empty);
                            }
                        }

                        uriBuilder.Query = queryBuilder.ToString();
                        uri = new Uri(uriBuilder.ToString());
                    }
                    
                    Method = new HttpMethod(methodStr);

                    NativeHttpClient client = new NativeHttpClient();

                    // TODO: FIX IT. Its create exception
                    // client.Timeout = TimeSpan.FromMilliseconds(Timeout);

                    HttpRequestMessage req = new HttpRequestMessage(Method, uri);

                    req.Headers.Clear();

                    foreach (var header in Headers.AllKeyValue)
                    {
                        req.Headers.Add(header.Key, header.Value);
                    }


                    req.Content = Body.HttpContent;

                    req.Method = Method;

                    req.RequestUri = uri;

                    _onModify?.Invoke(null, client);

                    var httpResponseMessage = await client.SendAsync(req);

                    var request = new Request(httpResponseMessage);

                    _onSuccess?.Invoke(null, request);
                }
                catch (Exception e)
                {
                    _onError?.Invoke(null, e);
                }
            });
        }
    }
}