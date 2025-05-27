namespace TeamEnforcer.Collections;

internal class CustomQueue<T>(string name = "Nameless Queue") where T : notnull 
{
    public readonly string Name = name;
    private readonly LinkedList<T> _list = new();
    private readonly Dictionary<T, LinkedListNode<T>> _dictionary = [];
    public int Count => _list.Count;

    public void Enqueue(T item)
    {
        if (_dictionary.ContainsKey(item))
        {
            throw new InvalidOperationException("Item already exists in the queue.");
        }

        LinkedListNode<T> node = _list.AddLast(item);
        _dictionary[item] = node;
    }

    public T? Dequeue()
    {
        if (_list.Count == 0 || _list.First == null) throw new InvalidOperationException("Queue is empty");
        
        T first = _list.First.Value;
        _list.RemoveFirst();
        _dictionary.Remove(first);
        
        return first;
    }

    public List<T>? Dequeue(int count)
    {
        if (_list.Count < count || _list.First == null) throw new InvalidOperationException("Not enough in queue."); 
        List<T>? firstN = new(count);
        for(int i = 0; i < count; i++)
        {
            T? next = this.Dequeue();
            if (next != null) 
            {
                firstN.Add(next);
            }
        }
        return firstN;
    }

    public void Remove(T item)
    {
        if (_dictionary.TryGetValue(item, out LinkedListNode<T>? node))
        {  
            _list.Remove(node);
            _dictionary.Remove(item);
        }
        else
        {
            throw new InvalidOperationException("Item not found in queue.");
        }
    }

    public bool Contains(T item)
    {
        return _dictionary.ContainsKey(item);
    }

    public IEnumerable<T> GetAllItems()
    {
        return _list;
    }

    public T? Peek()
    {
        if (_list.First == null) throw new InvalidOperationException("Queue is empty.");
        return _list.First();
    }

    public void Clear()
    {
        _list.Clear();
        _dictionary.Clear();
    }

    public int GetQueuePosition(T item)
    {
        if (!_dictionary.TryGetValue(item, out LinkedListNode<T>? node))
        {
            throw new InvalidOperationException("Item not found in the queue.");
        }

        int position = 1;
        LinkedListNode<T>? current = _list.First;

        while (current != null)
        {
            if (current == node)
            {
                return position;
            }
            position++;
            current = current.Next;
        }

        throw new InvalidOperationException("Unexpected error: Item found in dictionary but not in list.");
    }
}