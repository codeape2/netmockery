<Query Kind="Program">
  <NuGetReference>AMQPNetLite</NuGetReference>
  <Namespace>Amqp</Namespace>
  <Namespace>Amqp.Listener</Namespace>
</Query>

void Main()
{
	var containerHost = new ContainerHost(new Uri("amqp://localhost:1234/"));
	containerHost.RegisterMessageProcessor("q2", new MP());
	containerHost.Open();

	//containerHost.RegisterLinkProcessor(new LinkProcessor());
	
	Console.ReadLine();
	containerHost.Close();
}

class LinkProcessor : ILinkProcessor
{
	public void Process(AttachContext context)
	{
		Debug.Assert(context != null);
		Console.WriteLine("LinkProcessor.Process");
		context.Dump();
	}
}

class MP : IMessageProcessor
{
	public void Process(MessageContext context)
	{
		Debug.Assert(context != null);
		Console.WriteLine("MessageProcessor.Process(context)");
		context.Message.Dump();
		context.Complete();
	}

	public int Credit
	{
		get
		{
			Console.WriteLine("Credit.get");
			return 1;
		}
	}
}
// Define other methods and classes here
