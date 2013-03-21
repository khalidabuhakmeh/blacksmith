﻿using System;
using System.Collections.Generic;
using System.Linq;
using Blacksmith.Core.Responses;
using Newtonsoft.Json;

namespace Blacksmith.Core
{
    public partial class Client
    {
        public class QueueWrapper<TMessage>
            where TMessage : class
        {
            private readonly Client _client;
            protected string Name { get; set; }

            private readonly JsonSerializerSettings _settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.None,
                DefaultValueHandling = DefaultValueHandling.Ignore
            };

            public QueueWrapper(Client client)
            {
                _client = client;
                Name = typeof(TMessage).GetQueueName();
            }

            /// <summary>
            /// Peeking at a queue returns the next messages on the queue, but it does not reserve them. Don't use this for processing messages, use Next or Get.
            /// </summary>
            /// <param name="numberOfDocuments"></param>
            /// <returns></returns>
            public IEnumerable<Message<TMessage>> Peek(int numberOfDocuments = 1)
            {
                var json = _client.Get(string.Format("queues/{0}/messages/peek?n={1}", Name, numberOfDocuments));
                var queue = JsonConvert.DeserializeObject<QueueMessages>(json, _settings);
                return queue.Messages.Select(message => new Message<TMessage>(message));
            }

            /// <summary>
            /// This call gets/reserves messages from the queue. The messages will not be deleted, but will be reserved until the timeout expires.
            /// If the timeout expires before the messages are deleted, the messages will be placed back onto the queue.
            /// As a result, be sure to delete the messages after you’re done with them. Using the consume pattern, will auto delete
            /// the message for you if the consumption is successful. Otherwise the message will be placed back into the queue due to timeout.
            /// </summary>
            /// <remarks>equivalent to Get(1)</remarks>
            /// <param name="timeout">How long should the timeout be, in case of errors.</param>
            /// <returns></returns>
            public MessageConsumer<TMessage> Next(int? timeout = 60)
            {
                return Get(1, timeout).FirstOrDefault();
            }

            /// <summary>
            /// This call gets/reserves messages from the queue. The messages will not be deleted, but will be reserved until the timeout expires.
            /// If the timeout expires before the messages are deleted, the messages will be placed back onto the queue.
            /// As a result, be sure to delete the messages after you’re done with them. Using the consume pattern, will auto delete
            /// the message for you if the consumption is successful. Otherwise the message will be placed back into the queue due to timeout.
            /// </summary>
            /// <param name="numberOfDocuments">number of documents you would like to process.</param>
            /// <param name="timeout">How long should the timeout be, in case of errors.</param>
            /// <returns></returns>
            public IEnumerable<MessageConsumer<TMessage>> Get(int? numberOfDocuments, int? timeout = 60)
            {
                var json = _client.Get(string.Format("queues/{0}/messages?n={1}&timeout={2}", Name, numberOfDocuments, timeout));
                var queue = JsonConvert.DeserializeObject<QueueMessages>(json, _settings);
                return queue.Messages.Select(message => new MessageConsumer<TMessage>(this, new Message<TMessage>(message)));
            }

            /// <summary>
            /// This call adds or pushes messages onto the queue.
            /// </summary>
            /// <param name="message">your message</param>
            /// <param name="delay">delays defer the visiblity of the message. Max 7 days.</param>
            /// <param name="timeout">timeout is the expected time it should take to process the message. Max 24 hours</param>
            /// <param name="expiration">when does the message become invalid. Max 30 days</param>
            public void Push(TMessage message, TimeSpan? delay = null, TimeSpan? timeout = null, TimeSpan? expiration = null)
            {
                Push(new[] { message }, delay, timeout, expiration);
            }

            /// <summary>
            /// This call adds or pushes messages onto the queue, you can batch them together. Note: they will have the same delay, timeout, and expiration values.
            /// </summary>
            /// <param name="messages">your messages</param>
            /// <param name="delay">delays defer the visiblity of the message. Max 7 days.</param>
            /// <param name="timeout">timeout is the expected time it should take to process the message. Max 24 hours</param>
            /// <param name="expiration">when does the message become invalid. Max 30 days</param>
            public void Push(IEnumerable<TMessage> messages, TimeSpan? delay = null, TimeSpan? timeout = null,
                             TimeSpan? expiration = null)
            {
                var serialized = messages.Select(JsonConvert.SerializeObject);
                var json = JsonConvert.SerializeObject(new QueueMessages
                {
                    Messages = serialized.Select(msg => new Message
                    {
                        Body = msg,
                        Timeout = (long)(timeout.HasValue ? timeout.Value.TotalSeconds : 0),
                        Delay = (long)(delay.HasValue ? delay.Value.TotalSeconds : 0),
                        ExpiresIn = (long)(expiration.HasValue ? expiration.Value.TotalSeconds : 0)
                    }
                    ).ToArray(),
                }, _settings);

                _client.Post(string.Format("queues/{0}/messages", Name), json);
            }

            /// <summary>
            /// This call deletes all messages on a queue, whether they are reserved or not.
            /// </summary>
            public void Clear()
            {
                const string emptyJsonObject = "{}";
                var response = _client.Post("queues/" + Name + "/clear", emptyJsonObject);
                var responseObject = JsonConvert.DeserializeObject<Dictionary<string, string>>(response, _settings);
                if (responseObject["msg"] != "Cleared")
                    throw new Exception(string.Format("Unknown response from REST Endpoint : {0}", response));
            }

            /// <summary>
            /// This call deletes a message queue and all its messages. DANGER!
            /// </summary>
            public void Destroy()
            {
                _client.Delete(string.Format("queues/{0}", Name));
            }

            /// <summary>
            /// Delete a message from the queue
            /// </summary>
            /// <param name="id">Message Identifier</param>
            /// <exception cref="System.Web.HttpException">Thown if the IronMQ service returns a status other than 200 OK. </exception>
            /// <exception cref="System.IO.IOException">Thrown if there is an error accessing the IronMQ server.</exception>
            public void Delete(string id)
            {
                _client.Delete(string.Format("queues/{0}/messages/{1}", Name, id));
            }

            /// <summary>
            /// Touching a reserved message extends its timeout by the duration specified when the message was created, which is 60 seconds by default.
            /// </summary>
            /// <param name="id"></param>
            public void Touch(string id)
            {
                var response = _client.Post(string.Format("queues/{0}/messages/{1}/touch", Name, id), "{}");
                var responseObject = JsonConvert.DeserializeObject<Dictionary<string, string>>(response, _settings);
                if (responseObject["msg"] != "Touched")
                    throw new Exception(string.Format("Unknown response from REST Endpoint : {0}", response));
            }

            /// <summary>
            /// Releasing a reserved message unreserves the message and puts it back on the queue as if the message had timed out.
            /// </summary>
            /// <param name="id"></param>
            public void Release(string id)
            {
                var response = _client.Post(string.Format("queues/{0}/messages/{1}/release", Name, id), "{}");
                var responseObject = JsonConvert.DeserializeObject<Dictionary<string, string>>(response, _settings);
                if (responseObject["msg"] != "Released")
                    throw new Exception(string.Format("Unknown response from REST Endpoint : {0}", response));
            }

            /// <summary>
            /// Add subscribers (HTTP endpoints) to a queue. This is for Push Queues only.
            /// </summary>
            /// <param name="urls"></param>
            /// <returns></returns>
            public Subcsription Subscribe(params string[] urls)
            {
                if (urls == null || !urls.Any())
                    throw new ArgumentException("at least one url is required", "urls");
                var request = new Subscriptions { Subscribers = urls.Select(x => new Subscriber { Url = x }).ToArray() };

                var json = JsonConvert.SerializeObject(request);
                var response = _client.Post(string.Format("queues/{0}/subscribers", Name), json);

                return JsonConvert.DeserializeObject<Subcsription>(response);
            }

            /// <summary>
            /// Removes subscribers (HTTP endpoints) to a queue. This is for Push Queues only.
            /// </summary>
            /// <param name="urls"></param>
            /// <returns></returns>
            public Subcsription Unsubscribe(params string[] urls)
            {
                if (urls == null || !urls.Any())
                    throw new ArgumentException("at least one url is required", "urls");
                var request = new Subscriptions { Subscribers = urls.Select(x => new Subscriber { Url = x }).ToArray() };

                var json = JsonConvert.SerializeObject(request);
                var response = _client.DeleteWithBody(string.Format("queues/{0}/subscribers", Name), json);

                return JsonConvert.DeserializeObject<Subcsription>(response);
            }

        }
    }
}