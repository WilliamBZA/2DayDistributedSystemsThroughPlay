namespace SimonSays;
using Amqp;
using System;
using System.Collections;

public class MessageBus(string topicName)
{
    public delegate void MessageAction(Object message);

    public void Start()
    {
        var connection = new Connection(new Address(""));
        var session = new Session(connection);
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

    public void On(Type type, Action showSequence)
    {
        typeActionMaps[type.FullName.ToLower()] = showSequence;
    }

    public void On(Type type, MessageAction messageHandler)
    {
        typeActionMaps[type.FullName.ToLower()] = messageHandler;
    }

    internal void Route(Type type, string v)
    {
        throw new NotImplementedException();
    }

    Hashtable typeActionMaps = new Hashtable();
}