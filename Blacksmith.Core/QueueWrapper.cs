using System;
using System.Collections.Generic;
using System.Linq;
using Blacksmith.Core.Responses;
using Newtonsoft.Json;

namespace Blacksmith.Core
{
    public partial class Client
    {
        public class QueueWrapper<TMessage> 
            : IQueueWrapper<TMessage> where TMessage : class
        {
            private readonly Client _client;
            private Action _emptyHandler;

            private readonly JsonSerializerSettings _settings = new JsonSerializerSettings {
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.None,
                DefaultValueHandling = DefaultValueHandling.Ignore
            };

            
            public QueueWrapper(Client client) : this(client, typeof(TMessage).GetQueueName()) {}
            public QueueWrapper(Client client, string queueName)
            {
                _client = client;
                Name = queueName;
            } 


            /// <summary>
            /// Name of queue in iron.io project
            /// </summary>
            public virtual string Name { get; protected set; }


            /// <summary>
            /// Use this method to handle scenarios where the queue is perceived to be empty. There may still be some defered messages in the queue or messages waiting to timeout.
            /// </summary>
            /// <param name="emptyHandler"></param>
            /// <returns></returns>
            public virtual QueueWrapper<TMessage> OnEmpty(Action emptyHandler)
            {
                _emptyHandler = emptyHandler;
                return this;
            }

            /// <summary>
            /// Simple response to whether the queue is empty or not. Will make a request for a message, prefer using the OnEmpty construct.
            /// </summary>
            /// <returns></returns>
            public virtual bool IsEmpty()
            {
                return Get(1).FirstOrDefault() == null;
            }

            /// <summary>
            /// Returns information on the queue. Will tell you the size of the queue including messages of all states (queued, reserved, and delayed).
            /// </summary>
            /// <returns></returns>
            public virtual int Size()
            {
                var json = _client.Get(string.Format("queues/{0}", Name));

                return JsonConvert.DeserializeObject<QueueInfo>(json, ConfigurationWrapper.JsonSettings).Size;
            }

            /// <summary>
            /// This allows you to change the properties of a queue including setting subscribers and the push type if you want it to be a push queue.
            /// </summary>
            /// <param name="retries"></param>
            /// <param name="retriesDelay"></param>
            /// <param name="pushType"></param>
            /// <param name="errorQueue"></param> 
            /// <param name="subscriberUrls"></param>
            /// <returns></returns>
            public virtual QueueSettings Update(int retries = 3, int retriesDelay = 60, string pushType = "multicast", string errorQueue = null,
                                              string[] subscriberUrls = null)
            {
                var request = new QueueUpdate {
                    PushType = pushType,
                    Retries = retries,
                    RetriesDelay = retriesDelay,
                    ErrorQueue = errorQueue,
                    Subscribers = (subscriberUrls ?? new string[0]).Select(x => new Subscriber(x)).ToArray()
                };

                var body = JsonConvert.SerializeObject(request, ConfigurationWrapper.JsonSettings);
                var json = _client.Post(string.Format("queues/{0}", Name), body);

                return JsonConvert.DeserializeObject<QueueSettings>(json, ConfigurationWrapper.JsonSettings);
            }

            /// <summary>
            /// Peeking at a queue returns the next messages on the queue, but it does not reserve them. Don't use this for processing messages, use Next or Get.
            /// </summary>
            /// <param name="numberOfDocuments"></param>
            /// <returns></returns>
            public virtual IEnumerable<Message<TMessage>> Peek(int numberOfDocuments = 1)
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
            /// 
            /// Note: If the queue is empty, then your consume method will not run. Look at using OnEmpty on the queue to react appropriately.
            /// </summary>
            /// <remarks>equivalent to Get(1)</remarks>
            /// <param name="timeout">How long should the timeout be, in case of errors.</param>
            /// <returns></returns>
            public virtual MessageConsumer<TMessage> Next(int? timeout = 60)
            {
                var message = Get(1, timeout).FirstOrDefault() ??
                              new MessageConsumer<TMessage>(this, null);

                return message;
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
            public virtual IEnumerable<MessageConsumer<TMessage>> Get(int? numberOfDocuments, int? timeout = 60)
            {
                var json = _client.Get(string.Format("queues/{0}/messages?n={1}&timeout={2}", Name, numberOfDocuments, timeout));
                var queue = JsonConvert.DeserializeObject<QueueMessages>(json, _settings);
                var messages = queue.Messages.Select(message => new MessageConsumer<TMessage>(this, new Message<TMessage>(message))).ToList();

                if (!messages.Any() && _emptyHandler != null)
                    _emptyHandler();

                return messages;
            }

            /// <summary>
            /// This call adds or pushes messages onto the queue.
            /// </summary>
            /// <param name="message">your message</param>
            /// <param name="delay">delays defer the visiblity of the message. Max 7 days.</param>
            /// <param name="timeout">timeout is the expected time it should take to process the message. Max 24 hours</param>
            /// <param name="expiration">when does the message become invalid. Max 30 days</param>
            public virtual void Push(TMessage message, TimeSpan? delay = null, TimeSpan? timeout = null, TimeSpan? expiration = null)
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
            public virtual void Push(IEnumerable<TMessage> messages, TimeSpan? delay = null, TimeSpan? timeout = null,
                             TimeSpan? expiration = null)
            {
                var serialized = messages.Select(msg => JsonConvert.SerializeObject(msg, ConfigurationWrapper.JsonSettings));

                var json = JsonConvert.SerializeObject(
                    new QueueMessages {
                        Messages = serialized.Select(msg => new Message {
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
            public virtual void Clear()
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
            public virtual void Destroy()
            {
                _client.Delete(string.Format("queues/{0}", Name));
            }

            /// <summary>
            /// Delete a message from the queue
            /// </summary>
            /// <param name="id">Message Identifier</param>
            /// <exception cref="System.Web.HttpException">Thown if the IronMQ service returns a status other than 200 OK. </exception>
            /// <exception cref="System.IO.IOException">Thrown if there is an error accessing the IronMQ server.</exception>
            public virtual void Delete(string id)
            {
                _client.Delete(string.Format("queues/{0}/messages/{1}", Name, id));
            }

            /// <summary>
            /// Touching a reserved message extends its timeout by the duration specified when the message was created, which is 60 seconds by default.
            /// </summary>
            /// <param name="id"></param>
            public virtual void Touch(string id)
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
            public virtual void Release(string id)
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
            public virtual Subscription Subscribe(params string[] urls)
            {
                if (urls == null || !urls.Any())
                    throw new ArgumentException("at least one url is required", "urls");
                var request = new Subscriptions { Subscribers = urls.Select(x => new Subscriber { Url = x }).ToArray() };

                var json = JsonConvert.SerializeObject(request);
                var response = _client.Post(string.Format("queues/{0}/subscribers", Name), json);

                return JsonConvert.DeserializeObject<Subscription>(response);
            }

            /// <summary>
            /// Removes subscribers (HTTP endpoints) to a queue. This is for Push Queues only.
            /// </summary>
            /// <param name="urls"></param>
            /// <returns></returns>
            public virtual Subscription Unsubscribe(params string[] urls)
            {
                if (urls == null || !urls.Any())
                    throw new ArgumentException("at least one url is required", "urls");
                var request = new Subscriptions { Subscribers = urls.Select(x => new Subscriber { Url = x }).ToArray() };

                var json = JsonConvert.SerializeObject(request);
                var response = _client.DeleteWithBody(string.Format("queues/{0}/subscribers", Name), json);

                return JsonConvert.DeserializeObject<Subscription>(response);
            }



        }
    }
}