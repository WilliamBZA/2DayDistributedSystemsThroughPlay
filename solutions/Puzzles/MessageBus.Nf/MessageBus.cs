namespace SimonSays;
using Amqp;
using System;
using System.Collections;

public class MessageBus(string connectionString, string topicName)
{
    public delegate void MessageAction(Object message);

    public void Start()
    {
        var connection = new Connection(new Address(connectionString));
        session = new Session(connection);
        var receiveLink = new ReceiverLink(session, "Simon Says receiver link", topicName);

        receiveLink.Start(1, (link, msg) =>
        {
            var messageType = ((string)msg?.MessageAnnotations["messagetype"]).ToLower();

            if (!typeActionMaps.Contains(messageType))
            {
                throw new HandlerNotFoundException(messageType);
            }

            var handlerObj = typeActionMaps[messageType];

            if (handlerObj.GetType() == typeof(MessageAction))
            {
                var handler = (MessageAction)handlerObj;
                handler(msg); // Todo: Convert to the type
            }
            else
            {
                var handler = (Action)handlerObj;
                handler();
            }
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
}