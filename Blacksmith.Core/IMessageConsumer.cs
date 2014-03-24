using System;
using Blacksmith.Core.Responses;

namespace Blacksmith.Core
{
    public interface IMessageConsumer<TMessage> where TMessage : class
    {
        Message<TMessage> Payload { get; }

        /// <summary>
        /// If there is an exception you can use this to handle it. Your message will be added back to the queue after the timeout.
        /// </summary>
        /// <param name="exceptionHandler"></param>
        /// <returns></returns>
        IMessageConsumer<TMessage> OnError(Action<Exception> exceptionHandler);

        /// <summary>
        /// If something goes wrong, don't wait for the timeout to kick in, just release the message immediately.
        /// </summary>
        /// <returns></returns>
        IMessageConsumer<TMessage> ReleaseImmediatelyOnError();

        /// <summary>
        /// For long running tasks, you might want to hold onto the message for longer than the original timeout.
        /// This will start a timer and send a Touch request at the end of each interval.
        /// </summary>
        /// <param name="seconds"></param>
        /// <param name="milliseconds"></param>
        /// <param name="minutes"></param>
        /// <returns></returns>
        IMessageConsumer<TMessage> KeepTouching(int seconds = 0, int milliseconds = 0, int minutes = 0);

        /// <summary>
        /// Use this method to consume your message. If this is successful (no exceptions), the message will be auto-deleted from the queue.
        /// </summary>
        /// <param name="action"></param>
        void Consume(Action<Message<TMessage>, Client.MessageConsumer<TMessage>.ConsumingContext> action);
    }
}