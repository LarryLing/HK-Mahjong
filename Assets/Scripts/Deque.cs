using System;
using System.Collections.Generic;

public class Deque<T>
{
    public LinkedList<T> elements;

    public int Count => elements.Count;

    public Deque()
    {
        elements = new LinkedList<T>();
    }

    public void PushFront(T newElement) => elements.AddFirst(newElement);

    public void PushBack(T newElement) => elements.AddLast(newElement);

    public bool TryPopFront(out T result)
    {
        if (elements.Count == 0)
        {
            result = default!;
            return false;
        }

        result = elements.First.Value;
        elements.RemoveFirst();
        return true;
    }

    public bool TryPopBack(out T result)
    {
        if (elements.Count == 0)
        {
            result = default!;
            return false;
        }

        result = elements.Last.Value;
        elements.RemoveLast();
        return true;
    }

    public bool TryPeekFront(out T result)
    {
        if (elements.Count == 0)
        {
            result = default!;
            return false;
        }

        result = elements.First.Value;
        return true;
    }

    public bool TryPeekBack(out T result)
    {
        if (elements.Count == 0)
        {
            result = default!;
            return false;
        }

        result = elements.Last.Value;
        return true;
    }

    public void Rotate(int rotateBy)
    {
        if (elements.Count == 0)
            return;

        rotateBy = ((rotateBy % elements.Count) + elements.Count) % elements.Count;

        for (int i = 0; i < rotateBy; i++)
        {
            LinkedListNode<T> firstNode = elements.First;
            elements.RemoveFirst();
            elements.AddLast(firstNode);
        }
    }
}
