Blacksmith - IronMQ Client for .Net
==========================================

Description
--------------

IronMQ client that has some sensible assumptions of how .NET developers would want to use IronMQ. IronMQ is a promising REST based queueing system. Before you get started, you will need an account. Accounts are free and offer 10 million free API requests a month (pretty sweet).

Sign Up Here: http://www.iron.io/mq


Nuget
------------

    > Install-Package Blacksmith


Getting Started
--------------------

If you want to build from source, you will need Visual Studio 2012 and a minimum of .NET 4.0. You will also need Nuget Package Restore enabled, as there is a dependency on JSON.NET that needs to be pulled down for the Core. There is also a dependency on XUnit and FluentAssertions for tests.

The Blacksmith API tried to stay as true to the IronMQ API documentation. So consult the IronMQ documentation to see what you need.

http://dev.iron.io/mq/

** Note: The API is documented as well, so you will get some helpful hints in Visual Studio. **


Usage - Settings
----------------------

Blacksmith gives you two options to initialize your client. You can set it in code, or you can use the default AppSetting keys to set the client. Add the following keys to your AppSettings with the values from your IronMQ account (look for iron.json).

	blacksmith.projectId 
	blacksmith.token
	blacksmith.port  // optional
	blacksmith.host  // optional


Usage - Hello World
---------------------

Blacksmith has a fluent based interface that gives you a simple and straight forward way of accessing your IronMQ queues. IronMQ suggests you follow this patter if you were going to use the REST API directly.

1. Push Item to Queue
2. Pull Item from Queue
3. Process Item (in your code)
4. Delete the Item from the Queue

** Blacksmith encapsulates that logic for you, so all you need to do is consume the message. Successful consumption will delete the item for you. **

Let's take a look at how you would create a Queue, and then process a message from that Queue.

    // pulling projectId and OAuth token from AppSettings
	var client = new Client();
	// create the new queue, push the message
	client.Queue<MyMessage>().Push(new MyMessage { Text = "Hello World!" });

	// getting the message from IronMQ
	client.Queue<MyMessage>().Next().Consume((message, ctx) => Console.WriteLine(message.Target.Text));

There are a couple assumptions in the above code:

1. The Queue name is the type's FullName. You can override this using the QueueNameAttribute on the class.
2. Your messages are serialized to JSON and messages are expected to be classes because of this.
3. When you "Consume" a message, you have access to the message itself, the body (which is a JSON string), and the Target which is nicely deserialized back to your type.
4. When Consume completes executing, Blacksmith will make another request to IronMQ for you automatically to delete the message from the queue.

This is the simplest example, but all IronMQ Queue API methods are supported in Blacksmith. We also have some really nice features built right in, so you don't have to reimplement them yourself.

Usage - Error Handling
--------------------------

When you consume a message, things are bound to go wrong. You have several options to handle exceptions. We decided that to give you a way to do it in a descriptive way. Have a look.

	client.
		Queue<MyMessage>()
		.Next()
		.OnError(ex => HandleError(ex))
		.Consume((m, ctx) => throw new Exception("oops!"));

OnError will catch all unhandled exceptions that occur in your consumption of the message. If there is an unhandled exception, the message will not be deleted from your queue and you will have another oppurtunity to process it.


Usage - Touching
--------------------------

When consuming a message, you are passed two things that are important. The message (duh!), and the message context. The message context allows you to Touch the message you are working on so that it doesn't time out.

	client
		.Next()
		.Consume((m,ctx) =>{
            ctx.Touch(); // long running process
		})

You can also have us touch your message for you on a timed interval, that way you don't have to worry about it.

	client
		.Next()
		.KeepTouching(seconds:30) // make a request every 30 seconds
		.Consume((m,ctx) => TakingForever(m));

Make sure you pay attention to what the timeout is for your message or else you might get unexpected results. The default timeout is 60 seconds.


Usage - Releasing
-------------------------------

When consuming a message, you might realize you don't want to process it just yet maybe because a third party resource is down, but you may have pulled it off of the queue already. You might want to release it back to the queue and let something else pick it up.

 ** Note: calling release will not delete the message from the queue, so we suggest you release the message early before doing something. i.e. Charging for an order, sending emails, or deleting a resource. **

	client
		.Next()
		.Consume((m,ctx) =>{
            ctx.Release(); // Not yet
		})

	client
		.Next()
		.ReleaseImmediatelyOnError()
		.Consume((m, ctx) => throw new Exception());