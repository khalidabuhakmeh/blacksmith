using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Blacksmith.Core;
using Blacksmith.Core.Attributes;
using FluentAssertions;
using Xunit;

namespace Blacksmith.Tests
{
    public class ClientTests
    {
        protected TestClient Client { get; set; }

        public ClientTests()
        {
            Client = new TestClient();
        }

        [Fact]
        public void Can_create_a_new_client()
        {
            Client.Should().NotBeNull();
        }

        [Fact]
        public void Can_get_a_queue()
        {
            Client.Queue<Stub>().Should().NotBeNull();
        }

        [Fact]
        public void Can_get_queues()
        {
            Client.GetResponse = e => "[ { name : 'test' } ]";
            var queues = Client.Queues();

            queues.Count.Should().Be(1);
            queues.First().Should().Be("test");
        }

        [Fact]
        public void Can_push_a_message_to_a_queue()
        {
            string body = string.Empty;
            string endpoint = string.Empty;

            Client.PostResponse = (e, b) =>
            {
                endpoint = e;
                body = b;

                return "{ 'ids': ['message 1 ID', 'message 2 ID'], 'msg': 'Messages put on queue.' }";
            };

            Client.Queue<Stub>()
                .Push(new Stub { Text = "hello" });

            body.Should().NotBeEmpty();
            endpoint.Should().Be("queues/Blacksmith.Tests.ClientTests+Stub/messages");
            Debug.WriteLine(body);
        }

        [Fact]
        public void Can_get_a_message_from_the_queue()
        {
            Client.GetResponse = (e) =>
            {
                return "{ 'messages': [ { 'id': 1, 'body':'{\"Text\":\"hello\"}', 'timeout': 600 } ] }";
            };

            Client.Queue<Stub>()
                .Next()
                .OnError(ex => ex.Should().BeNull())
                .Consume((m, ctx) => m.Target.Text.Should().NotBeEmpty());
        }

        [Fact]
        public void Can_get_multiple_messages_from_the_queue()
        {
            Client.GetResponse = (e) =>
            {
                return "{ 'messages': [ { 'id': 1, 'body':'{\"Text\":\"hello\"}', 'timeout': 600 }, { 'id': 2, 'body':'{\"Text\":\"hello again\"}', 'timeout': 600 } ] }";
            };

            var messages = Client.Queue<Stub>().Get(2).ToList();
            messages.Count().Should().Be(2);
            messages.ForEach(r => r.OnError(ex => ex.Should().BeNull())
                                   .Consume((m, ctx) => m.Target.Text.Should().NotBeEmpty()));
        }

        [Fact]
        public void Can_clear_all_messages_from_the_queue()
        {
            var endpoint = string.Empty; ;
            Client.PostResponse = (e, body) =>
            {
                endpoint = e;
                return "{ msg : 'Cleared' }";
            };

            Client.Queue<Stub>().Clear();
            endpoint.Should().Be("queues/Blacksmith.Tests.ClientTests+Stub/clear");
        }

        [Fact]
        public void Can_delete_a_message_by_id()
        {
            var endpoint = string.Empty;
            Client.DeleteResponse = e =>
            {
                endpoint = e;
                return "{ 'msg' : 'Deleted' }";
            };

            Client.Queue<Stub>().Delete("1");
            endpoint.Should().Be("queues/Blacksmith.Tests.ClientTests+Stub/messages/1");
        }

        [Fact]
        public void Can_destroy_a_queue()
        {
            var endpoint = string.Empty;
            Client.DeleteResponse = e =>
            {
                endpoint = e;
                return "{ 'msg': 'Deleted.' }";
            };

            Client.Queue<Stub>().Destroy();
            endpoint.Should().Be("queues/Blacksmith.Tests.ClientTests+Stub");
        }

        [Fact]
        public void Can_peek_into_queue()
        {
            var endpoint = string.Empty;
            Client.GetResponse = (e) =>
            {
                endpoint = e;
                return "{ 'messages': [ { 'id': 1, 'body':'{\"Text\":\"hello\"}', 'timeout': 600 }, { 'id': 2, 'body':'{\"Text\":\"hello again\"}', 'timeout': 600 } ] }";
            };

            var result = Client.Queue<Stub>().Peek(2);
            result.Count().Should().Be(2);
            endpoint.Should().Be("queues/Blacksmith.Tests.ClientTests+Stub/messages/peek?n=2");
        }

        [Fact]
        public void Can_touch_a_message_by_id()
        {
            var endpoint = string.Empty;
            Client.PostResponse = (e, b) =>
            {
                endpoint = e;
                return "{ 'msg' : 'Touched' }";
            };

            Client.Queue<Stub>().Touch("1");
            endpoint.Should().Be("queues/Blacksmith.Tests.ClientTests+Stub/messages/1/touch");
        }

        [Fact]
        public void Can_release_a_message_by_id()
        {
            var endpoint = string.Empty;
            Client.PostResponse = (e, b) =>
            {
                endpoint = e;
                return "{ 'msg' : 'Released' }";
            };

            Client.Queue<Stub>().Release("1");
            endpoint.Should().Be("queues/Blacksmith.Tests.ClientTests+Stub/messages/1/release");
        }

        [Fact]
        public void Can_subscribe_to_a_queue()
        {
            var endpoint = string.Empty;
            var body = string.Empty;
            Client.PostResponse = (e, b) =>
                {
                    endpoint = e;
                    body = b;
                    return
                    @"{
                      'id':'50eb546d3264140e8638a7e5',
                      'name':'pushq-demo-1',
                      'size':7,
                      'total_messages':7,
                      'project_id':'4fd2729368a0197d1102056b',
                      'retries':3,
                      'push_type':'multicast',
                      'retries_delay':60,
                      'subscribers':[
                        {'url':'http://mysterious-brook-1807.herokuapp.com/ironmq_push_1'},
                        {'url':'http://mysterious-brook-1807.herokuapp.com/ironmq_push_2'}
                      ]
                    }";
                };


            var subscriptions = Client.Queue<Stub>().Subscribe("http://localhost");
            endpoint.Should().Be("queues/Blacksmith.Tests.ClientTests+Stub/subscribers");
            subscriptions.Should().NotBeNull();
            subscriptions.TotalMessages.Should().Be(7);
            subscriptions.Subscribers.Count().Should().Be(2);
            Debug.WriteLine(body);
        }

        [Fact]
        public void Can_unsubscribe_to_a_queue()
        {
            var endpoint = string.Empty;
            var body = string.Empty;
            Client.DeleteWithBodyResponse = (e, b) =>
            {
                endpoint = e;
                body = b;
                return
                @"{
                      'id':'50eb546d3264140e8638a7e5',
                      'name':'pushq-demo-1',
                      'size':7,
                      'total_messages':7,
                      'project_id':'4fd2729368a0197d1102056b',
                      'retries':3,
                      'push_type':'multicast',
                      'retries_delay':60,
                      'subscribers':[
                        {'url':'http://mysterious-brook-1807.herokuapp.com/ironmq_push_1'},
                        {'url':'http://mysterious-brook-1807.herokuapp.com/ironmq_push_2'}
                      ]
                    }";
            };

            var subscriptions = Client.Queue<Stub>().Unsubscribe("http://localhost");
            endpoint.Should().Be("queues/Blacksmith.Tests.ClientTests+Stub/subscribers");
            subscriptions.Should().NotBeNull();
            subscriptions.TotalMessages.Should().Be(7);
            subscriptions.Subscribers.Count().Should().Be(2);
            Debug.WriteLine(body);
        }

        [Fact]
        public void Can_catch_error_when_consuming()
        {
            Client.GetResponse = (e) =>
            {
                return "{ 'messages': [ { 'id': 1, 'body':'{\"Text\":\"hello\"}', 'timeout': 600 } ] }";
            };

            Client.Queue<Stub>()
                .Next()
                .OnError(ex => ex.Message.Should().Be("woo!"))
                .Consume((m, ctx) =>
                {
                    throw new Exception("woo!");
                });
        }

        [Fact]
        public void Can_touch_a_message_from_within_consuming()
        {
            var endpoint = string.Empty;

            Client.GetResponse = (e) => "{ 'messages': [ { 'id': 1, 'body':'{\"Text\":\"hello\"}', 'timeout': 600 } ] }";

            Client.PostResponse = (e, b) =>
            {
                endpoint = e;
                return "{ 'msg' : 'Touched' }";
            };

            Client.Queue<Stub>()
              .Next()
              .Consume((m, ctx) => ctx.Touch());

            endpoint.Should().Be("queues/Blacksmith.Tests.ClientTests+Stub/messages/1/touch");
        }

        [Fact]
        public void Can_release_a_message_from_within_consuming()
        {
            var endpoint = string.Empty;

            Client.GetResponse = (e) => "{ 'messages': [ { 'id': 1, 'body':'{\"Text\":\"hello\"}', 'timeout': 600 } ] }";

            Client.PostResponse = (e, b) =>
            {
                endpoint = e;
                return "{ 'msg' : 'Released' }";
            };

            Client.Queue<Stub>()
              .Next()
              .Consume((m, ctx) => ctx.Release());

            endpoint.Should().Be("queues/Blacksmith.Tests.ClientTests+Stub/messages/1/release");
        }

        [Fact]
        public void Can_keep_a_message_while_it_is_being_consumed()
        {
            var endpoint = string.Empty;
            var touched = 0;
            Client.GetResponse = (e) => "{ 'messages': [ { 'id': 1, 'body':'{\"Text\":\"hello\"}', 'timeout': 600 } ] }";

            Client.PostResponse = (e, b) =>
            {
                touched++;
                endpoint = e;
                return "{ 'msg' : 'Touched' }";
            };

            Client.Queue<Stub>()
              .Next()
              .KeepTouching(milliseconds: 10)
              .Consume((m, ctx) => Thread.Sleep(100));

            touched.Should().BeGreaterThan(1);
        }

        [Fact]
        public void Can_override_default_queue_name_with_attribute()
        {
            var result = typeof(Overriding).GetQueueName();
            result.Should().Be("HammerTime");
        }

        [Fact]
        public void Can_tell_if_queue_is_just_empty()
        {
            Client.GetResponse = (e) => "{ 'messages': [ ] }";
            Client.Queue<Stub>().IsEmpty().Should().BeTrue();
        }

        [Fact]
        public void Can_tell_if_queue_is_empty_and_react()
        {
            Client.GetResponse = (e) => "{ 'messages': [ ] }";

            var isEmpty = false;
            Client.Queue<Stub>()
                  .OnEmpty(() => isEmpty = true)
                      .Next()
                      .Consume((m, ctx) => Console.WriteLine("Should not be here!"));

            isEmpty.Should().BeTrue();
        }

        [Fact]
        public void Can_tell_if_a_batch_request_is_empty()
        {
            Client.GetResponse = (e) => "{ 'messages': [ ] }";

            var isEmpty = false;
            Client.Queue<Stub>()
                  .OnEmpty(() => isEmpty = true)
                      .Get(5)
                      .ForEach(x => Console.WriteLine("should not get here"));

            isEmpty.Should().BeTrue();
        }

        [Fact]
        public void Can_get_size_of_the_queue()
        {
            Client.GetResponse = (e) =>  "{ 'size' : '5' }";
            Client.Queue<Stub>().Size().Should().Be(5);
        }

        [Fact]
        public void Can_update_a_queue()
        {
            var endpoint = string.Empty;
            var body = string.Empty;
            Client.PostResponse = (e, b) => {
                    endpoint = e;
                    body = b;

                    return
                        @"{
                          'id':'50eb546d3264140e8638a7e5',
                          'name':'pushq-demo-1',
                          'size':7,
                          'total_messages':7,
                          'project_id':'4fd2729368a0197d1102056b',
                          'retries':3,
                          'push_type':'multicast',
                          'retries_delay':60,
                          'subscribers':[
                            {'url':'http://mysterious-brook-1807.herokuapp.com/ironmq_push_1'},
                            {'url':'http://mysterious-brook-1807.herokuapp.com/ironmq_push_2'}
                          ]
                        }";
                };

            var result = Client.Queue<Stub>().Update(retries: 6);

            result.Should().NotBeNull();
            body.Should().NotBeEmpty();
            endpoint.Should().Be("queues/Blacksmith.Tests.ClientTests+Stub");

        }

        public class Stub
        {
            public string Text { get; set; }
        }

        [QueueName("HammerTime")]
        public class Overriding
        {
            public string Text { get; set; }
        }

        public class TestClient : Client
        {
            public Func<string, string> GetResponse = (e) => string.Empty;
            public Func<string, string, string> PostResponse = (e, b) => string.Empty;
            public Func<string, string> DeleteResponse = (e) => string.Empty;
            public Func<string, string, string> DeleteWithBodyResponse = (e, b) => string.Empty;

            protected override string Get(string endpoint)
            {
                return GetResponse(endpoint);
            }

            protected override string Delete(string endpoint)
            {
                return DeleteResponse(endpoint);
            }

            protected override string DeleteWithBody(string endpoint, string body)
            {
                return DeleteWithBodyResponse(endpoint, body);
            }

            protected override string Post(string endpoint, string body)
            {
                return PostResponse(endpoint, body);
            }
        }
    }
}
