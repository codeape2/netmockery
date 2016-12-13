<Query Kind="Program">
  <NuGetReference>AMQPNetLite</NuGetReference>
  <Namespace>Amqp</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

async Task Main()
{
	var connection = await Connection.Factory.CreateAsync(new Address("amqp://localhost:1234/"));
	var session = new Session(connection);
	
	var msg = new Message("Hello world!");
	var sender = new SenderLink(session, "sender-link", "q2");
	await sender.SendAsync(msg);
	await sender.CloseAsync();
	await session.CloseAsync();
	await connection.CloseAsync();
}

// Define other methods and classes here
