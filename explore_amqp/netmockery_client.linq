<Query Kind="Program">
  <NuGetReference>AMQPNetLite</NuGetReference>
  <Namespace>Amqp</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

async Task Main()
{
	await SendMessage("q2", "hello world");
	await SendMessage("__netmockery", "shutdown");
}

async Task SendMessage(string queue, string message)
{
	var connection = await Connection.Factory.CreateAsync(new Address("amqp://localhost:9876/"));
	var session = new Session(connection);

	var msg = new Message(message);
	var sender = new SenderLink(session, "sender-link", queue);
	await sender.SendAsync(msg);
	await sender.CloseAsync();
	await session.CloseAsync();
	await connection.CloseAsync();
}

// Define other methods and classes here
