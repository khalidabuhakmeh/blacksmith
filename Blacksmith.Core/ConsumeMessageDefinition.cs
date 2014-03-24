using System;
using System.Timers;
using Blacksmith.Core.Responses;

namespace Blacksmith.Core
{
    public partial class Client
    {
        public class MessageConsumer<TMessage> : IMessageConsumer<TMessage> where TMessage : class
        {
            private readonly IQueueWrapper<TMessage> _queue;
            private bool _releaseOnError;
            private double? _touchInterval;
            public Message<TMessage> Payload { get; private set; }
            protected Action<Exception> ExceptionHandler { get; set; }

            public MessageConsumer(IQueueWrapper<TMessage> queue, Message<TMessage> payload)
            {
                _queue = queue;
                Payload = payload;
            }

            /// <summary>
            /// If there is an exception you can use this to handle it. Your message will be added back to the queue after the timeout.
            /// </summary>
            /// <param name="exceptionHandler"></param>
            /// <returns></returns>
            public virtual IMessageConsumer<TMessage> OnError(Action<Exception> exceptionHandler)
            {
                ExceptionHandler = exceptionHandler;
                return this;
            }

            /// <summary>
            /// If something goes wrong, don't wait for the timeout to kick in, just release the message immediately.
            /// </summary>
            /// <returns></returns>
            public virtual IMessageConsumer<TMessage> ReleaseImmediatelyOnError()
            {
                _releaseOnError = true;
                return this;
            }

            /// <summary>
            /// For long running tasks, you might want to hold onto the message for longer than the original timeout.
            /// This will start a timer and send a Touch request at the end of each interval.
            /// </summary>
            /// <param name="seconds"></param>
            /// <param name="milliseconds"></param>
            /// <param name="minutes"></param>
            /// <returns></returns>
            public virtual IMessageConsumer<TMessage> KeepTouching(int seconds = 0, int milliseconds = 0, int minutes = 0)
            {
                _touchInterval = new TimeSpan(0, 0, minutes, seconds, milliseconds).TotalMilliseconds;
                return this;
            }

            /// <summary>
            /// Use this method to consume your message. If this is successful (no exceptions), the message will be auto-deleted from the queue.
            /// </summary>
            /// <param name="action"></param>
            public virtual void Consume(Action<Message<TMessage>, ConsumingContext> action)
            {
                if (Payload == null)
                    return;
                
                using (var timer = new Timer { AutoReset = true, Enabled = _touchInterval.HasValue, Interval = _touchInterval ?? 60 })
                {
                    var context = new ConsumingContext(_queue, Payload);
                    timer.Elapsed += (sender, args) => context.Touch();

                    try
                    {
                        action.Invoke(Payload, context);
                        timer.Stop();
                        
                        if (!context.WasReleased)
                            _queue.Delete(Payload.Id);
                    }
                    catch (Exception e)
                    {
                        timer.Stop();

                        if (ExceptionHandler != null)
                            ExceptionHandler.Invoke(e);

                        if (_releaseOnError && Payload != null)
                            _queue.Release(Payload.Id);
                    }
                }
            }

            /// <summary>
            /// Use the consuming context to touch and release the message. Don't release the message if you plan to process it.
            /// </summary>
            public class ConsumingContext
            {
                private readonly IQueueWrapper<TMessage> _queue;
                private readonly Message<TMessage> _message;

                public bool WasReleased { get; private set; }

                public ConsumingContext(IQueueWrapper<TMessage> queue, Message<TMessage> message)
                {
                    _queue = queue;
                    _message = message;
                }

                public void Release()
                {
                    _queue.Release(_message.Id);
                    WasReleased = true;
                }

                public void Touch()
                {
                    _queue.Touch(_message.Id);
                }
            }
        }
    }
}