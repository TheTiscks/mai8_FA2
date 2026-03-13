using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.Treap;

public class Treap<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, TreapNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    protected virtual (TreapNode<TKey, TValue>? Left, TreapNode<TKey, TValue>? Right) Split(TreapNode<TKey, TValue>? root, TKey key)
    {
        var (left, right) = SplitCore(root, key, leftInclusive: true);
        if (left != null)
        {
            left.Parent = null;
        }
        
        if (right != null)
        {
            right.Parent = null;
        }
        return (left, right);
    }
    
    private (TreapNode<TKey, TValue>? Left, TreapNode<TKey, TValue>? Right) SplitCore(
        TreapNode<TKey, TValue>? root, TKey key, bool leftInclusive)
    {
        if (root == null)
        {
            return (null, null);
        }
        bool rootInLeft = leftInclusive ? Comparer.Compare(root.Key, key) <= 0 : Comparer.Compare(root.Key, key) < 0;
        if (rootInLeft)
        {
            // рекурсивно сплитуем правое
            var (leftOfRight, rightOfRight) = SplitCore(root.Right, key, leftInclusive);
            root.Right = leftOfRight;
            if (leftOfRight != null)
            {
                leftOfRight.Parent = root;
            }
            return (root, rightOfRight);
        }
        else
        {
            // левое
            var (leftOfLeft, rightOfLeft) = SplitCore(root.Left, key, leftInclusive);
            root.Left = rightOfLeft;
            if (rightOfLeft != null)
            {
                rightOfLeft.Parent = root;
            }
            return (leftOfLeft, root);
        }
    }
    
    protected virtual TreapNode<TKey, TValue>? Merge(TreapNode<TKey, TValue>? left, TreapNode<TKey, TValue>? right)
    {
        if (left == null)
        {
            if (right != null)
            {
                right.Parent = null;
            }
            return right;
        }
        if (right == null)
        {
            if (left != null)
            {
                left.Parent = null;
            }
            return left;
        }
        if (left.Priority > right.Priority)
        {
            left.Right = Merge(left.Right, right);
            if (left.Right != null)
            {
                left.Right.Parent = left;
            }
            left.Parent = null;
            return left;
        }
        else
        {
            right.Left = Merge(left, right.Left);
            if (right.Left != null)
            {
                right.Left.Parent = right;
            }
            right.Parent = null;
            return right;
        }
    }

    public override void Add(TKey key, TValue value)
    {
        var existing = FindNode(key); // ключ есть - обн знач
        if (existing != null)
        {
            existing.Value = value;
            return;
        }
        var newNode = CreateNode(key, value);
        var (left, right) = Split(Root, key);
        Root = Merge(Merge(left, newNode), right);
        Count++;
        OnNodeAdded(newNode);
    }

    public override bool Remove(TKey key)
    {
        if (!ContainsKey(key))
        {
            return false;
        }
        var (left, right) = Split(Root, key);
        // отделим ключ
        var (leftStrict, middle) = SplitCore(left, key, leftInclusive: false);
        Root = Merge(leftStrict, right); // mid - ключ, мердж без него
        Count--;
        OnNodeRemoved(null, null);
        return true;
    }

    protected override TreapNode<TKey, TValue> CreateNode(TKey key, TValue value)
    {
        return new TreapNode<TKey, TValue>(key, value);
    }
    
    protected override void OnNodeAdded(TreapNode<TKey, TValue> newNode) { }
    protected override void OnNodeRemoved(TreapNode<TKey, TValue>? parent, TreapNode<TKey, TValue>? child) { }
}