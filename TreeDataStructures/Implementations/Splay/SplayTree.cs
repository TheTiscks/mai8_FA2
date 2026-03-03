using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Implementations.BST;

namespace TreeDataStructures.Implementations.Splay;

public class SplayTree<TKey, TValue> : BinarySearchTree<TKey, TValue>
    where TKey : IComparable<TKey>
{
    protected override BstNode<TKey, TValue> CreateNode(TKey key, TValue value)
        => new(key, value);

    #region Splay operation
    
    private void Splay(BstNode<TKey, TValue> x)
    {
        while (x.Parent != null)
        {
            var p = x.Parent;
            var g = p.Parent;
            if (g == null)
            {
                // Zig – x дитя корня
                if (x.IsLeftChild)
                    RotateRight(p);
                else
                    RotateLeft(p);
            }
            else if (x.IsLeftChild && p.IsLeftChild)
            { // Zig‑zig ll
                RotateRight(g);
                RotateRight(p);
            }
            else if (x.IsRightChild && p.IsRightChild)
            { // rr
                RotateLeft(g);
                RotateLeft(p);
            }
            else if (x.IsLeftChild && p.IsRightChild)
            { // Zig-zag rl
                RotateRight(p);
                RotateLeft(g);
            }
            else
            { // lr
                RotateLeft(p);
                RotateRight(g);
            }
        }
    }

    #endregion

    #region Insertion

    public override void Add(TKey key, TValue value)
    {
        var existing = FindNode(key); // ключ уже есть
        if (existing != null)
        {
            existing.Value = value;
            Splay(existing);
            return;
        }
        base.Add(key, value); // бдп вставка
    }

    protected override void OnNodeAdded(BstNode<TKey, TValue> newNode)
    {
        Splay(newNode);
    }

    #endregion

    #region Search

    public override bool ContainsKey(TKey key)
    {
        var node = FindNode(key);
        if (node != null)
        {
            Splay(node);
        }
        return node != null;
    }

    public override bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        var node = FindNode(key);
        if (node != null)
        {
            value = node.Value;
            Splay(node);
            return true;
        }
        value = default;
        return false;
    }

    #endregion

    #region Deletion

    public override bool Remove(TKey key)
    {
        var node = FindNode(key);
        if (node == null)
        {
            return false;
        }
        Splay(node);
        var left = node.Left;
        var right = node.Right;
        if (left == null)
        {
            Root = right;
            if (right != null)
                right.Parent = null;
        }
        else
        { // левое пд есть
            var rightmost = left;
            while (rightmost.Right != null)
                rightmost = rightmost.Right;
            rightmost.Right = right; // пр пд к самой правой ноде
            if (right != null)
                right.Parent = rightmost;
            Root = left; // левое пд корень
            left.Parent = null;
        }
        Count--;
        return true;
    }

    protected override void OnNodeRemoved(BstNode<TKey, TValue>? parent, BstNode<TKey, TValue>? child)
    {
    }

    #endregion
}