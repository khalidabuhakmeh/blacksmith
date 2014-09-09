using System;
using System.Collections.Generic;
using Blacksmith.Core.Responses;

namespace Blacksmith.Core
{
    public interface IQueueWrapper<TMessage> where TMessage : class
    {
        /// <summary>
        /// Name of queue in iron.io project
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Use this method to handle scenarios where the queue is perceived to be empty. There may still be some defered messages in the queue or messages waiting to timeout.
        /// </summary>
        /// <param name="emptyHandler"></param>
        /// <returns></returns>
        Client.QueueWrapper<TMessage> OnEmpty(Action emptyHandler);

        /// <summary>
        /// Simple response to whether the queue is empty or not. Will make a request for a message, prefer using the OnEmpty construct.
        /// </summary>
        /// <returns></returns>
        bool IsEmpty();

        /// <summary>
        /// Returns information on the queue. Will tell you the size of the queue including messages of all states (queued, reserved, and delayed).
        /// </summary>
        /// <returns></returns>
        int Size();

        /// <summary>
        /// This allows you to change the properties of a queue including setting subscribers and the push type if you want it to be a push queue.
        /// </summary>
        /// <param name="retries"></param>
        /// <param name="retriesDelay"></param>
        /// <param name="pushType"></param>
        /// <param name="errorQueue"></param>
        /// <param name="subscriberUrls"></param>
        /// <returns></returns>
        QueueSettings Update(int retries = 3, int retriesDelay = 60, string pushType = "multicast", string errorQueue = null,
            Subscriber[] subscribers = null);

        /// <summary>
        /// Peeking at a queue returns the next messages on the queue, but it does not reserve them. Don't use this for processing messages, use Next or Get.
        /// </summary>
        /// <param name="numberOfDocuments"></param>
        /// <returns></returns>
        IEnumerable<Message<TMessage>> Peek(int numberOfDocuments = 1);

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
        Client.MessageConsumer<TMessage> Next(int? timeout = 60);

        /// <summary>
        /// This call gets/reserves messages from the queue. The messages will not be deleted, but will be reserved until the timeout expires.
        /// If the timeout expires before the messages are deleted, the messages will be placed back onto the queue.
        /// As a result, be sure to delete the messages after you’re done with them. Using the consume pattern, will auto delete
        /// the message for you if the consumption is successful. Otherwise the message will be placed back into the queue due to timeout.
        /// </summary>
        /// <param name="numberOfDocuments">number of documents you would like to process.</param>
        /// <param name="timeout">How long should the timeout be, in case of errors.</param>
        /// <returns></returns>
        IEnumerable<Client.MessageConsumer<TMessage>> Get(int? numberOfDocuments, int? timeout = 60);

        /// <summary>
        /// This call adds or pushes messages onto the queue.
        /// </summary>
        /// <param name="message">your message</param>
        /// <param name="delay">delays defer the visiblity of the message. Max 7 days.</param>
        /// <param name="timeout">timeout is the expected time it should take to process the message. Max 24 hours</param>
        /// <param name="expiration">when does the message become invalid. Max 30 days</param>
        void Push(TMessage message, TimeSpan? delay = null, TimeSpan? timeout = null, TimeSpan? expiration = null);

        /// <summary>
        /// This call adds or pushes messages onto the queue, you can batch them together. Note: they will have the same delay, timeout, and expiration values.
        /// </summary>
        /// <param name="messages">your messages</param>
        /// <param name="delay">delays defer the visiblity of the message. Max 7 days.</param>
        /// <param name="timeout">timeout is the expected time it should take to process the message. Max 24 hours</param>
        /// <param name="expiration">when does the message become invalid. Max 30 days</param>
        void Push(IEnumerable<TMessage> messages, TimeSpan? delay = null, TimeSpan? timeout = null,
            TimeSpan? expiration = null);

        /// <summary>
        /// This call deletes all messages on a queue, whether they are reserved or not.
        /// </summary>
        void Clear();

        /// <summary>
        /// This call deletes a message queue and all its messages. DANGER!
        /// </summary>
        void Destroy();

        /// <summary>
        /// Delete a message from the queue
        /// </summary>
        /// <param name="id">Message Identifier</param>
        /// <exception cref="System.Web.HttpException">Thown if the IronMQ service returns a status other than 200 OK. </exception>
        /// <exception cref="System.IO.IOException">Thrown if there is an error accessing the IronMQ server.</exception>
        void Delete(string id);

        /// <summary>
        /// Touching a reserved message extends its timeout by the duration specified when the message was created, which is 60 seconds by default.
        /// </summary>
        /// <param name="id"></param>
        void Touch(string id);

        /// <summary>
        /// Releasing a reserved message unreserves the message and puts it back on the queue as if the message had timed out.
        /// </summary>
        /// <param name="id"></param>
        void Release(string id);

        /// <summary>
        /// Add subscribers (HTTP endpoints) to a queue. This is for Push Queues only.
        /// </summary>
        /// <param name="urls"></param>
        /// <returns></returns>
        Subscription Subscribe(params Subscriber[] subscribers);

        /// <summary>
        /// Removes subscribers (HTTP endpoints) to a queue. This is for Push Queues only.
        /// </summary>
        /// <param name="urls"></param>
        /// <returns></returns>
        Subscription Unsubscribe(params string[] urls);
    }
}