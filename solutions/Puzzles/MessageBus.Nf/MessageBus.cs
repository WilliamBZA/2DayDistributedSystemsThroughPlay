namespace SimonSays;
using Amqp;
using Amqp.Framing;
using nanoFramework.Json;
using System;
using System.Collections;
using System.Reflection;
using System.Text;
using System.Threading;

public class MessageBus
{
    public MessageBus(string connectionString, string topicName)
    {
        this.connectionString = connectionString;
        this.topicName = topicName;

        Console.WriteLine(connectionString);
        
        var connection = new Connection(new Address(connectionString));
        session = new Session(connection);
    }

    public delegate void MessageAction(Object message);

    public void Start()
    {
        var receiveLink = new ReceiverLink(session, topicName, topicName);

        receiveLink.Start(5, (link, msg) =>
        {
            var messageType = (msg.ApplicationProperties["messagetype"] as string) ?? "";
            var type = Type.GetType(messageType);

            if (!typeActionMaps.Contains(messageType))
            {
                throw new HandlerNotFoundException(messageType);
            }

            var handlerObj = typeActionMaps[messageType];

            if (handlerObj.GetType() == typeof(MessageAction))
            {
                var handler = (MessageAction)handlerObj;
                var bytes = (byte[])msg.Body;
                var messageBody = Encoding.UTF8.GetString(bytes, 0, bytes.Length);

                var message = JsonConvert.DeserializeObject(messageBody, type);
                Console.WriteLine(messageType);
                handler(message);
            }
            else
            {
                var handler = (Action)handlerObj;
                handler();
            }

            link.Accept(msg);
        });
    }

    public void Publish(object message)
    {
        new Thread(new ThreadStart(() =>
        {
            var destination = messageDestinations[message.GetType()] as string;
            var sender = senders[destination] as SenderLink;

            if (sender != null)
            {
                var json = JsonConvert.SerializeObject(message);

                Console.WriteLine($"Sending message type {message.GetType().ToString()} with body '{json}' to {sender.Name}");

                var msg = new Message(json);
                msg.ApplicationProperties = new Amqp.Framing.ApplicationProperties();
                msg.ApplicationProperties["messagetype"] = message.GetType().FullName;

                sender.Send(msg);
                Console.WriteLine("Sent");
            }
        })).Start();
    }

    public void On(Type type, Action showSequence)
    {
        typeActionMaps[type.FullName] = showSequence;
    }

    public void On(Type type, MessageAction messageHandler)
    {
        typeActionMaps[type.FullName] = messageHandler;
    }

    public void Route(Type type, string destination)
    {
        messageDestinations[type] = destination;

        if (!senders.Contains(destination))
        {
            var sender = new SenderLink(session, $"sender for {destination}", destination);
            senders.Add(destination, sender);
        }
    }

    Hashtable typeActionMaps = new Hashtable();
    Hashtable messageDestinations = new Hashtable();
    Hashtable senders = new Hashtable();
    private Session session;
    private string connectionString;
    private string topicName;
}