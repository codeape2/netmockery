using Amqp;
using Amqp.Listener;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace netmockery
{
    public class LogToConsoleMessageProcessor : IMessageProcessor
    {
        private string address;
        public LogToConsoleMessageProcessor(string address)
        {
            this.address = address;
        }
        public void ProcessMessage(Message message)
        {
            Debug.Assert(message != null);
            Log("Message received");
            Log("Body:" + message.Body.ToString());
        }

        public void Log(string s)
        {
            Console.WriteLine($"{DateTime.Now} {address} {s}");
        }

        int IMessageProcessor.Credit
        {
            get
            {
                return 1;
            }
        }

        void IMessageProcessor.Process(MessageContext messageContext)
        {
            try
            {
                ProcessMessage(messageContext.Message);
            }
            finally
            {
                messageContext.Complete();
            }
        }
    }


    public class CommandAndControlMessageProcessor : IMessageProcessor
    {
        private ContainerHost containerHost;
        public CommandAndControlMessageProcessor(ContainerHost containerHost)
        {
            this.containerHost = containerHost;
        }

        public int Credit => 1;

        public void Process(MessageContext messageContext)
        {
            var command = messageContext.Message.GetBody<string>();
            messageContext.Complete();

            Log($"Command: {command}");
            switch (command)
            {
                case "shutdown":
                    Log("Shutting down");
                    containerHost.Close();
                    break;
                default:
                    Log("Unknown command");
                    break;
            }
        }

        public void Log(string s)
        {
            Console.WriteLine($"{DateTime.Now} *netmockery* {s}");
        }

    }

    public class AMQPConfig
    {
        public Uri Uri { get; set; }
        public IEnumerable<string> QueueAddresses { get; set; }

        private ContainerHost CreateContainerHost()
        {
            var containerHost = new ContainerHost(Uri);
            foreach (var address in QueueAddresses)
            {
                containerHost.RegisterMessageProcessor(address, new LogToConsoleMessageProcessor(address));
            }

            containerHost.RegisterMessageProcessor("__netmockery", new CommandAndControlMessageProcessor(containerHost));

            return containerHost;
        }

        public Action StartContainerHost()
        {
            var containerHost = CreateContainerHost();
            containerHost.Open();
            Action closer = () =>
            {
                containerHost.Close();
            };
            return closer;
        }

        static public AMQPConfig ReadFromDirectory(string directory)
        {
            var amqpJsonFile = Path.Combine(directory, "amqp.json");

            var mainConfig = JsonConvert.DeserializeObject<AMQPConfigJson>(File.ReadAllText(amqpJsonFile));

            var queues = new List<QueueConfigJson>();
            foreach (var subdir in Directory.GetDirectories(directory))
            {
                var queueFile = Path.Combine(subdir, "queue.json");
                if (File.Exists(queueFile))
                {
                    queues.Add(JsonConvert.DeserializeObject<QueueConfigJson>(File.ReadAllText(queueFile)));
                }
            }

            return new AMQPConfig
            {
                Uri = new Uri(mainConfig.uri),
                QueueAddresses = from q in queues select q.address
            };
        }
    }

    public class AMQPConfigJson
    {
        public string uri;
    }

    public class QueueConfigJson
    {
        public string address;
    }
}
