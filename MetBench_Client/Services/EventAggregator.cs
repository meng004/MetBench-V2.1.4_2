using System;
using System.Collections.Generic;
using System.Linq;

namespace MetBench_Client.Services;

public interface IHandle<in TMessage>
{
    void Handle(TMessage message);
}

public interface IEventAggregator
{
    void Subscribe(object subscriber);

    void Publish<TMessage>(TMessage message);
}

public sealed class EventAggregator : IEventAggregator
{
    private readonly List<object> _subscribers = new();

    public void Subscribe(object subscriber)
    {
        ArgumentNullException.ThrowIfNull(subscriber);

        if (!_subscribers.Contains(subscriber))
        {
            _subscribers.Add(subscriber);
        }
    }

    public void Publish<TMessage>(TMessage message)
    {
        var handlers = _subscribers.OfType<IHandle<TMessage>>().ToArray();
        foreach (var handler in handlers)
        {
            handler.Handle(message);
        }
    }
}
