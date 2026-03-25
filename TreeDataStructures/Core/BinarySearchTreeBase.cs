using System.Collections;
using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Interfaces;

namespace TreeDataStructures.Core;

public abstract class BinarySearchTreeBase<TKey, TValue, TNode>(IComparer<TKey>? comparer = null) : ITree<TKey, TValue> where TNode : Node<TKey, TValue, TNode>
{
    protected TNode? Root;
    public IComparer<TKey> Comparer { get; protected set; } = comparer ?? Comparer<TKey>.Default;

    public int Count
    {
        get; protected set;
    }
    
    public bool IsReadOnly => false;
    
    
    protected virtual int GetHeight(TNode? node)
    {
        if (node == null)
        {
            return 0;
        }
        return 1 + Math.Max(GetHeight(node.Left), GetHeight(node.Right));
    }
    
    public virtual void Add(TKey key, TValue value)
    {
        var existing = FindNode(key);
        if (existing != null)
        {
            existing.Value = value; // ключ есть, обн знач
            return;
        }
        var newNode = CreateNode(key, value);
        if (Root == null)
        {
            Root = newNode;
            Count++;
            OnNodeAdded(newNode);
            return;
        }
        TNode current = Root;
        while (true)
        {
            int cmp = Comparer.Compare(key, current.Key);
            if (cmp < 0)
            {
                if (current.Left == null)
                {
                    current.Left = newNode;
                    newNode.Parent = current;
                    Count++;
                    OnNodeAdded(newNode);
                    return;
                }
                current = current.Left;
            }
            else
            {
                if (current.Right == null)
                {
                    current.Right = newNode;
                    newNode.Parent = current;
                    Count++;
                    OnNodeAdded(newNode);
                    return;
                }
                current = current.Right;
            }
        }
    }

    
    public virtual bool Remove(TKey key)
    {
        TNode? node = FindNode(key);
        if (node == null)
        {
            return false;
        }
        RemoveNode(node);
        Count--;
        return true;
    }
    
    
    protected virtual void RemoveNode(TNode node)
    {
        TNode? replacement;
        TNode? parentOfRemoved = node.Parent;
        if (node.Left == null)
        {
            replacement = node.Right;
            Transplant(node, node.Right);
        }
        else if (node.Right == null)
        {
            replacement = node.Left;
            Transplant(node, node.Left);
        }
        else
        { // ищем мин в ппд
            TNode successor = node.Right;
            while (successor.Left != null)
            {
                successor = successor.Left;
            }
            replacement = successor;
            TNode? successorParent = successor.Parent;
            if (successorParent != node)
            {
                Transplant(successor, successor.Right); // замена преемника правым дитем
                successor.Right = node.Right;
                successor.Right.Parent = successor;
            }
            Transplant(node, successor);
            successor.Left = node.Left;
            successor.Left.Parent = successor;
            parentOfRemoved = successor.Parent;
        }
        OnNodeRemoved(parentOfRemoved, replacement);
    }

    public virtual bool ContainsKey(TKey key) => FindNode(key) != null;
    
    public virtual bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        TNode? node = FindNode(key);
        if (node != null)
        {
            value = node.Value;
            return true;
        }
        value = default;
        return false;
    }

    public TValue this[TKey key]
    {
        get => TryGetValue(key, out TValue? val) ? val : throw new KeyNotFoundException();
        set => Add(key, value);
    }

    
    #region Hooks
    
    protected virtual void OnNodeAdded(TNode newNode) { }
    
    protected virtual void OnNodeRemoved(TNode? parent, TNode? child) { }
    
    #endregion
    
    
    #region Helpers
    protected abstract TNode CreateNode(TKey key, TValue value);
    
    
    protected TNode? FindNode(TKey key)
    {
        TNode? current = Root;
        while (current != null)
        {
            int cmp = Comparer.Compare(key, current.Key);
            if (cmp == 0)
            {
                return current;
            }
            current = cmp < 0 ? current.Left : current.Right;
        }
        return null;
    }

    protected void RotateLeft(TNode x)
    {
        if (x.Right == null)
        {
            throw new InvalidOperationException("Right child must exist for left rotation");
        }
        TNode y = x.Right;
        x.Right = y.Left;
        if (y.Left != null)
        {
            y.Left.Parent = x;
        }
        y.Parent = x.Parent;
        if (x.Parent == null)
        {
            Root = y;
        }
        else if (x.IsLeftChild)
        {
            x.Parent.Left = y;
        }
        else
        {
            x.Parent.Right = y;
        }
        y.Left = x;
        x.Parent = y;
    }

    protected void RotateRight(TNode y)
    {
        if (y.Left == null)
        {
            throw new InvalidOperationException("Left child must exist for right rotation");
        }
        TNode x = y.Left;
        y.Left = x.Right;
        if (x.Right != null)
        {
            x.Right.Parent = y;
        }
        x.Parent = y.Parent;
        if (y.Parent == null)
        {
            Root = x;
        }
        else if (y.IsLeftChild)
        {
            y.Parent.Left = x;
        }
        else
        {
            y.Parent.Right = x;
        }
        x.Right = y;
        y.Parent = x;
    }
    
    protected void RotateDoubleLeft(TNode x)
    {
        RotateRight(x.Right!);
        RotateLeft(x);
    }
    
    protected void RotateDoubleRight(TNode y)
    {
        RotateLeft(y.Left!);
        RotateRight(y);
    }

    protected void RotateBigLeft(TNode x) => RotateDoubleLeft(x);
    
    protected void RotateBigRight(TNode y) => RotateDoubleRight(y);
    
    protected void Transplant(TNode u, TNode? v)
    {
        if (u.Parent == null)
        {
            Root = v;
        }
        else if (u.IsLeftChild)
        {
            u.Parent.Left = v;
        }
        else
        {
            u.Parent.Right = v;
        }

        if (v != null)
        {
            v.Parent = u.Parent;
        }
    }
    #endregion
    
    public IEnumerable<TreeEntry<TKey, TValue>> InOrder() => 
        new TreeIterator(this, TraversalStrategy.InOrder);
    
    public IEnumerable<TreeEntry<TKey, TValue>> PreOrder() => 
        new TreeIterator(this, TraversalStrategy.PreOrder);
    
    public IEnumerable<TreeEntry<TKey, TValue>> PostOrder() => 
        new TreeIterator(this, TraversalStrategy.PostOrder);
    
    public IEnumerable<TreeEntry<TKey, TValue>> InOrderReverse() => 
        new TreeIterator(this, TraversalStrategy.InOrderReverse);
    
    public IEnumerable<TreeEntry<TKey, TValue>> PreOrderReverse() => 
        new TreeIterator(this, TraversalStrategy.PreOrderReverse);

    public IEnumerable<TreeEntry<TKey, TValue>> PostOrderReverse() => 
        new TreeIterator(this, TraversalStrategy.PostOrderReverse);
    
    
    private struct TreeIterator : 
        IEnumerable<TreeEntry<TKey, TValue>>,
        IEnumerator<TreeEntry<TKey, TValue>>
    {
        private readonly BinarySearchTreeBase<TKey, TValue, TNode> _tree;
        private readonly TraversalStrategy _strategy;
        private Stack<object>? _stack; // храним узлы и сост
        private List<TreeEntry<TKey, TValue>>? _cachedEntries;
        private int _reverseIndex;
        private TreeEntry<TKey, TValue> _current;

        public TreeIterator(BinarySearchTreeBase<TKey, TValue, TNode> tree, TraversalStrategy strategy)
        {
            _tree = tree;
            _strategy = strategy;
            _stack = null;
            _cachedEntries = null;
            _reverseIndex = -1;
            _current = default;
            Initialize();
        }

        private void Initialize()
        {
            if (_tree.Root == null)
            {
                return;
            }
            switch (_strategy)
            {
                case TraversalStrategy.PreOrder:
                    _stack = new Stack<object>();
                    _stack.Push(_tree.Root);
                    break;
                case TraversalStrategy.PreOrderReverse:
                    _cachedEntries = new List<TreeEntry<TKey, TValue>>();
                    CollectPreOrder(_tree.Root, _cachedEntries);
                    _reverseIndex = _cachedEntries.Count - 1;
                    break;
                case TraversalStrategy.InOrder:
                    _stack = new Stack<object>();
                    PushLeftChain(_tree.Root);
                    break;
                case TraversalStrategy.InOrderReverse:
                    _cachedEntries = new List<TreeEntry<TKey, TValue>>();
                    CollectInOrder(_tree.Root, _cachedEntries);
                    _reverseIndex = _cachedEntries.Count - 1;
                    break;
                case TraversalStrategy.PostOrder:
                    _stack = new Stack<object>();
                    _stack.Push(new PostOrderState { Node = _tree.Root, Visited = false });
                    break;
                case TraversalStrategy.PostOrderReverse:
                    _cachedEntries = new List<TreeEntry<TKey, TValue>>();
                    CollectPostOrder(_tree.Root, _cachedEntries);
                    _reverseIndex = _cachedEntries.Count - 1;
                    break;
            }
        }

        private void CollectPreOrder(TNode? node, List<TreeEntry<TKey, TValue>> list)
        {
            if (node == null)
            {
                return;
            }
            list.Add(CreateEntry(node));
            CollectPreOrder(node.Left, list);
            CollectPreOrder(node.Right, list);
        }

        private void CollectInOrder(TNode? node, List<TreeEntry<TKey, TValue>> list)
        {
            if (node == null)
            {
                return;
            }
            CollectInOrder(node.Left, list);
            list.Add(CreateEntry(node));
            CollectInOrder(node.Right, list);
        }

        private void CollectPostOrder(TNode? node, List<TreeEntry<TKey, TValue>> list)
        {
            if (node == null)
            {
                return;
            }
            CollectPostOrder(node.Left, list);
            CollectPostOrder(node.Right, list);
            list.Add(CreateEntry(node));
        }

        private void PushLeftChain(TNode? node)
        {
            while (node != null)
            {
                _stack!.Push(node);
                node = node.Left;
            }
        }

        private void PushRightChain(TNode? node)
        {
            while (node != null)
            {
                _stack!.Push(node);
                node = node.Right;
            }
        }

        public IEnumerator<TreeEntry<TKey, TValue>> GetEnumerator()
        {
            var fresh = new TreeIterator(_tree, _strategy);
            fresh.Initialize();
            return fresh;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public TreeEntry<TKey, TValue> Current => _current;
        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (_cachedEntries != null)
            {
                if (_reverseIndex >= 0)
                {
                    _current = _cachedEntries[_reverseIndex];
                    _reverseIndex--;
                    return true;
                }
                return false;
            }
            if (_stack == null || _stack.Count == 0)
            {
                return false;
            }
            switch (_strategy)
            {
                case TraversalStrategy.PreOrder:
                    return MoveNextPreOrder();
                case TraversalStrategy.InOrder:
                    return MoveNextInOrder();
                case TraversalStrategy.PostOrder:
                    return MoveNextPostOrder();
                default:
                    throw new NotSupportedException("Strategy not implemented");
            }
        }

        private bool MoveNextPreOrder()
        {
            var node = (TNode)_stack!.Pop();
            _current = CreateEntry(node);
            if (node.Right != null)
            {
                _stack.Push(node.Right);
            }
            if (node.Left != null)
            {
                _stack.Push(node.Left);
            }
            return true;
        }

        private bool MoveNextInOrder()
        {
            var node = (TNode)_stack!.Pop();
            _current = CreateEntry(node);
            if (node.Right != null)
            {
                PushLeftChain(node.Right);
            }
            return true;
        }

        private bool MoveNextPostOrder()
        {
            while (true)
            {
                var state = (PostOrderState)_stack!.Peek();
                if (!state.Visited)
                {
                    state.Visited = true;
                    _stack.Pop();
                    _stack.Push(state);
                    if (_strategy == TraversalStrategy.PostOrder)
                    {
                        if (state.Node.Right != null)
                        {
                            _stack.Push(new PostOrderState { Node = state.Node.Right, Visited = false });
                        }
                        if (state.Node.Left != null)
                        {
                            _stack.Push(new PostOrderState { Node = state.Node.Left, Visited = false });
                        }
                    }
                    else // PostOrderReverse
                    {
                        if (state.Node.Left != null)
                        {
                            _stack.Push(new PostOrderState { Node = state.Node.Left, Visited = false });
                        }
                        if (state.Node.Right != null)
                        {
                            _stack.Push(new PostOrderState { Node = state.Node.Right, Visited = false });
                        }
                    }
                }
                else
                {
                    _stack.Pop();
                    _current = CreateEntry(state.Node);
                    return true;
                }
            }
        }

        private TreeEntry<TKey, TValue> CreateEntry(TNode node)
        {
            int height = _tree.GetHeight(node); 
            return new TreeEntry<TKey, TValue>(node.Key, node.Value, height);
        }

        public void Reset()
        {
            _stack = null;
            _cachedEntries = null;
            Initialize();
        }

        public void Dispose() { }

        private struct PostOrderState
        {
            public TNode Node;
            public bool Visited;
        }
    }
    
    private struct InOrderPairEnumerator : IEnumerator<KeyValuePair<TKey, TValue>>
    {
        private TreeIterator _innerIterator;
        private KeyValuePair<TKey, TValue> _current;
        public InOrderPairEnumerator(BinarySearchTreeBase<TKey, TValue, TNode> tree)
        {
            _innerIterator = new TreeIterator(tree, TraversalStrategy.InOrder);
            _current = default;
        }

        public bool MoveNext()
        {
            if (!_innerIterator.MoveNext())
            {
                return false;
            }
            var entry = _innerIterator.Current;
            _current = new KeyValuePair<TKey, TValue>(entry.Key, entry.Value);
            return true;
        }

        public KeyValuePair<TKey, TValue> Current => _current;
        object IEnumerator.Current => Current;
        public void Reset()
        {
            _innerIterator.Reset();
            _current = default;
        }
        public void Dispose()
        {
            _innerIterator.Dispose();
        }
    }
    
    
    private enum TraversalStrategy { InOrder, PreOrder, PostOrder, InOrderReverse, PreOrderReverse, PostOrderReverse }
    
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return new InOrderPairEnumerator(this);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
    public void Clear() { Root = null; Count = 0; }
    public bool Contains(KeyValuePair<TKey, TValue> item) => ContainsKey(item.Key);
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        if (array == null)
        {
            throw new ArgumentNullException(nameof(array));
        }
        if (arrayIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(arrayIndex), "index must be >=0");
        }
        if (array.Length - arrayIndex < Count)
        {
            throw new ArgumentException("dest doesnt have enough space");
        }
        int i = arrayIndex;
        foreach (var entry in InOrder())
        {
            array[i++] = new KeyValuePair<TKey, TValue>(entry.Key, entry.Value);
        }
    }
    public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);
    
    public ICollection<TKey> Keys => InOrder().Select(e => e.Key).ToList();
    public ICollection<TValue> Values => InOrder().Select(e => e.Value).ToList();
}