using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Web;
using Blacksmith.Core.Responses;
using Newtonsoft.Json;

namespace Blacksmith.Core
{
    public partial class Client
    {
        private const string Protocol = "https";
        private const string HOST = "mq-aws-us-east-1.iron.io";
        private const int PORT = 443;
        private const string ApiVersion = "1";

        private readonly string _projectId = string.Empty;
        private readonly string _token = string.Empty;

        public string Host { get; private set; }
        public int Port { get; private set; }

        /// <summary>
        /// The starting point to using IronMQ. Instantiate here and make sure you set your AppSettings accordingly.
        /// 
        /// blacksmith.project = your product id
        /// blacksmith.token = your OAuth token
        /// blacksmith.host = host of your queue (optional | mq-aws-us-east-1.iron.io )
        /// blacksmith.port = the port (optional | 443 )
        /// </summary>
        public Client()
            : this(
                ConfigurationManager.AppSettings["blacksmith.projectId"],
                ConfigurationManager.AppSettings["blacksmith.token"],
                ConfigurationManager.AppSettings.GetValueOrDefault("blacksmith.host", HOST),
                ConfigurationManager.AppSettings.GetValueOrDefault("blacksmith.port", PORT)
            )
        { }

        /// <summary>
        /// Constructs a new Client using the specified project ID and token.
        /// The network is not accessed during construction and this call will
        /// succeed even if the credentials are invalid.
        /// </summary>
        /// <param name="projectId">projectId A 24-character project ID.</param>
        /// <param name="token">token An OAuth token.</param>
        /// <param name="host"></param>
        /// <param name="port"></param>
        public Client(string projectId, string token, string host = HOST, int port = PORT)
        {
            _projectId = projectId;
            _token = token;
            Host = host;
            Port = port;
        }

        /// <summary>
        /// Get a list of all queues in a project. By default, 30 queues are listed at a time. To see more, use the page parameter or the per_page parameter. Up to 100 queues may be listed on a single page.
        /// </summary>
        /// <param name="page"></param>
        /// <param name="perPage"></param>
        /// <returns></returns>
        public IList<string> Queues(int page = 0, int perPage = 30)
        {
            var url = string.Format("queues?page={0}&per_page={1}", page, perPage);

            var json = Get(url);
            var template = new[] { new { id = string.Empty, name = string.Empty, projectId = string.Empty } };
            var queues = JsonConvert.DeserializeAnonymousType(json, template);
            return queues.Select(q => q.name).ToList();
        }

        /// <summary>
        /// Queues are based on message types, where TMessage is your class. Your class can contain anything that is serializable.
        /// </summary>
        /// <typeparam name="TMessage">your message</typeparam>
        /// <returns>a fluent interface to let you construct a request.</returns>
        public QueueWrapper<TMessage> Queue<TMessage>()
            where TMessage : class
        {
            return new QueueWrapper<TMessage>(this);
        }

        private string Request(string method, string endpoint, string body)
        {
            string path = string.Format("/{0}/projects/{1}/{2}", ApiVersion, _projectId, endpoint);
            string uri = string.Format("{0}://{1}:{2}{3}", Protocol, this.Host, this.Port, path);
            var request = (HttpWebRequest)HttpWebRequest.Create(uri);
            request.ContentType = "application/json";
            request.Headers.Add("Authorization", "OAuth " + _token);
            request.UserAgent = "Blacksmith .Net Client";
            request.Method = method;
            if (body != null)
            {
                using (var write = new System.IO.StreamWriter(request.GetRequestStream()))
                {
                    write.Write(body);
                    write.Flush();
                }
            }

            var response = (HttpWebResponse)request.GetResponse();
            string json;
            using (var reader = new System.IO.StreamReader(response.GetResponseStream()))
            {
                json = reader.ReadToEnd();
            }
            if (response.StatusCode == HttpStatusCode.OK) return json;

            var error = JsonConvert.DeserializeObject<Error>(json);
            throw new HttpException((int)response.StatusCode, error.Message);
        }

        protected virtual string Delete(string endpoint)
        {
            return Request("DELETE", endpoint, null);
        }

        protected virtual string DeleteWithBody(string endpoint, string body)
        {
            return Request("DELETE", endpoint, body);
        }

        protected virtual string Get(string endpoint)
        {
            return Request("GET", endpoint, null);
        }

        protected virtual string Post(string endpoint, string body)
        {
            return Request("POST", endpoint, body);
        }
    }
}
