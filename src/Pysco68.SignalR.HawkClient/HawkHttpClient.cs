using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Http;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Thinktecture.IdentityModel.Hawk.Client;
using Thinktecture.IdentityModel.Hawk.Core;

namespace Pysco68.SignalR.HawkClient
{
    /// <summary>
    /// A IHttpClient implementation enabling Hawk authentication scheme with SignalR
    /// </summary>
    public class HawkHttpClient : IHttpClient
    {
        private HttpClient longRunningClient;
        private HttpClient shortRunningClient;
        private IConnection connection;

        private Credential credential;

        public ClientOptions HawkOptions { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="credential">Hawk credentials to use for authentication</param>
        public HawkHttpClient(Credential credential)
        {
            this.credential = credential;

            this.HawkOptions = new ClientOptions()
            {
                CredentialsCallback = () => credential,
                EnableResponseValidation = false,
                RequestPayloadHashabilityCallback = (r) => true,
                NormalizationCallback = (req) => null
            };
        }

        /// <summary>
        /// Initialize the Http Clients
        /// </summary>
        /// <param name="connection">Connection</param>
        public void Initialize(IConnection connection)
        {
            this.connection = connection;

            this.longRunningClient = HttpClientFactory.Create(
                new HawkValidationHandler(this.HawkOptions));
            this.longRunningClient.Timeout = TimeSpan.FromMilliseconds(-1.0);

            this.shortRunningClient = HttpClientFactory.Create(
                new HawkValidationHandler(this.HawkOptions));
            this.shortRunningClient.Timeout = TimeSpan.FromMilliseconds(-1.0);
        }

        /// <summary>
        /// Makes an asynchronous http GET request to the specified url.
        /// </summary>
        /// <param name="url">The url to send the request to.</param>
        /// <param name="prepareRequest">A callback that initializes the request with default values.</param>
        /// <param name="isLongRunning">Indicates whether the request is long running</param>
        /// <returns>A <see cref="T:Task{IResponse}"/>.</returns>
        public Task<IResponse> Get(string url, Action<IRequest> prepareRequest, bool isLongRunning)
        {
            if (prepareRequest == null)
            {
                throw new ArgumentNullException("prepareRequest");
            }
            var responseDisposer = new Disposer();
            var cts = new CancellationTokenSource();
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri(url));
            var request = new HttpRequestMessageWrapper(requestMessage, () =>
            {
                cts.Cancel();
                responseDisposer.Dispose();
            });

            prepareRequest(request);
            HttpClient httpClient = GetHttpClient(isLongRunning);

            return httpClient
                .SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cts.Token)
                .ContinueWith(prev =>
                {
                    var responseMessage = prev.Result;

                    if (responseMessage.IsSuccessStatusCode)
                    {
                        responseDisposer.Set(responseMessage);
                    }
                    else
                    {
                        throw new HttpClientException(responseMessage);
                    }
                    return (IResponse)new HttpResponseMessageWrapper(responseMessage);
                });
        }

        /// <summary>
        /// Makes an asynchronous http POST request to the specified url.
        /// </summary>
        /// <param name="url">The url to send the request to.</param>
        /// <param name="prepareRequest">A callback that initializes the request with default values.</param>
        /// <param name="postData">form url encoded data.</param>
        /// <param name="isLongRunning">Indicates whether the request is long running</param>
        /// <returns>A <see cref="T:Task{IResponse}"/>.</returns>
        public Task<IResponse> Post(string url, Action<IRequest> prepareRequest, IDictionary<string, string> postData, bool isLongRunning)
        {
            if (prepareRequest == null)
            {
                throw new ArgumentNullException("prepareRequest");
            }

            var responseDisposer = new Disposer();

            var cts = new CancellationTokenSource();
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, new Uri(url));

            if (postData == null)
            {
                requestMessage.Content = new StringContent(String.Empty);
            }
            else
            {
                requestMessage.Content = new ByteArrayContent(ProcessPostData(postData));
            }

            var request = new HttpRequestMessageWrapper(requestMessage, () =>
            {
                cts.Cancel();
                responseDisposer.Dispose();
            });

            prepareRequest(request);

            HttpClient httpClient = GetHttpClient(isLongRunning);

            return httpClient
                .SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cts.Token)
                .ContinueWith(prev =>
                {
                    var responseMessage = prev.Result;

                    if (responseMessage.IsSuccessStatusCode)
                    {
                        responseDisposer.Set(responseMessage);
                    }
                    else
                    {
                        throw new HttpClientException(responseMessage);
                    }
                    return (IResponse)new HttpResponseMessageWrapper(responseMessage);
                });                
        }

        /// <summary>
        /// Returns the appropriate client based on whether it is a long running request
        /// </summary>
        /// <param name="isLongRunning">Indicates whether the request is long running</param>
        /// <returns></returns>
        private HttpClient GetHttpClient(bool isLongRunning)
        {
            return isLongRunning ? longRunningClient : shortRunningClient;
        }

        /// <summary>
        /// Process post data
        /// </summary>
        /// <param name="postData"></param>
        /// <returns></returns>
        public static byte[] ProcessPostData(IDictionary<string, string> postData)
        {
            if (postData == null || postData.Count == 0)
            {
                return null;
            }
            var sb = new StringBuilder();
            foreach (var pair in postData)
            {
                if (sb.Length > 0)
                {
                    sb.Append("&");
                }
                if (String.IsNullOrEmpty(pair.Value))
                {
                    continue;
                }
                sb.AppendFormat("{0}={1}", pair.Key, WebUtility.UrlEncode(pair.Value));
            }
            return Encoding.UTF8.GetBytes(sb.ToString());
        }
    }
}
