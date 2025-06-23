namespace SimonSays;
using Amqp;
using System;
using System.Collections;

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
            var messageType = (msg.ApplicationProperties["messagetype"] as string)?.ToLower() ?? "";

            if (!typeActionMaps.Contains(messageType))
            {
                throw new HandlerNotFoundException(messageType);
            }

            var handlerObj = typeActionMaps[messageType];

            if (handlerObj.GetType() == typeof(MessageAction))
            {
                var handler = (MessageAction)handlerObj;
                handler(msg!); // Todo: Convert to the type
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
        var destination = outgoingMessages[message.GetType()] as string;
        var sender = senders[destination] as SenderLink;

        var msg = new Message(message);
        msg.ApplicationProperties = new Amqp.Framing.ApplicationProperties();
        msg.ApplicationProperties["messagetype"] = message.GetType().FullName.ToLower();
    }

    public void On(Type type, Action showSequence)
    {
        typeActionMaps[type.FullName.ToLower()] = showSequence;
    }

    public void On(Type type, MessageAction messageHandler)
    {
        typeActionMaps[type.FullName.ToLower()] = messageHandler;
    }

    public void Route(Type type, string destination)
    {
        outgoingMessages[type] = destination;

        if (!senders.Contains(destination))
        {
            var sender = new SenderLink(session, $"sender for {destination}", destination);
            senders.Add(destination, sender);
        }
    }

    Hashtable typeActionMaps = new Hashtable();
    Hashtable outgoingMessages = new Hashtable();
    Hashtable senders = new Hashtable();
    private Session session;
    private string connectionString;
    private string topicName;
}