#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core
{

public sealed class EventQueueState
{
    private readonly List<EventState> events = new();

    public IReadOnlyList<EventState> Events
    {
        get { return events; }
    }

    public EventState? Current
    {
        get
        {
            foreach (var eventState in events)
            {
                if (!eventState.IsResolved)
                {
                    return eventState;
                }
            }

            return null;
        }
    }

    public int PendingCount
    {
        get
        {
            var count = 0;
            foreach (var eventState in events)
            {
                if (!eventState.IsResolved)
                {
                    count += 1;
                }
            }

            return count;
        }
    }

    public void Enqueue(EventState eventState)
    {
        if (eventState == null)
        {
            throw new ArgumentNullException(nameof(eventState));
        }

        events.Add(eventState);
    }

    public EventState? Find(string eventId)
    {
        foreach (var eventState in events)
        {
            if (eventState.Id == eventId)
            {
                return eventState;
            }
        }

        return null;
    }

    public void Clear()
    {
        events.Clear();
    }
}
}
