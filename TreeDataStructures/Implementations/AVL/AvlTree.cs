using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.AVL;

public class AvlTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, AvlNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    protected override AvlNode<TKey, TValue> CreateNode(TKey key, TValue value) => new(key, value);
    
    protected override int GetHeight(AvlNode<TKey, TValue>? node) => node?.Height ?? 0;

    private void UpdateHeight(AvlNode<TKey, TValue> node)
    {
        node.Height = 1 + Math.Max(node.Left?.Height ?? 0, node.Right?.Height ?? 0);
    }

    private int BalanceFactor(AvlNode<TKey, TValue> node) => (node.Left?.Height ?? 0) - (node.Right?.Height ?? 0);

    private AvlNode<TKey, TValue> RotateLeftAndUpdate(AvlNode<TKey, TValue> x)
    {
        RotateLeft(x);
        UpdateHeight(x);
        UpdateHeight(x.Parent!);
        return x.Parent;
    }

    private AvlNode<TKey, TValue> RotateRightAndUpdate(AvlNode<TKey, TValue> y)
    {
        RotateRight(y);
        UpdateHeight(y);
        UpdateHeight(y.Parent!);
        return y.Parent;
        
    }

    private AvlNode<TKey, TValue>? Balance(AvlNode<TKey, TValue> node)
    {
        UpdateHeight(node);
        int balance = BalanceFactor(node);
        if (balance > 1) 
        {
            // левое пд перекос вправо
            if (BalanceFactor(node.Left) < 0)
            {
                node.Left = RotateLeftAndUpdate(node.Left!);
            }
            // п влево
            return RotateRightAndUpdate(node);
        }
        if (balance < -1)
        {
            // право пд перекос влево
            if (BalanceFactor(node.Right) > 0)
            {
                node.Right = RotateRightAndUpdate(node.Right!);
            }
            // перекос вправо
            return RotateLeftAndUpdate(node);
        }
        return node;
    }

    protected override void OnNodeAdded(AvlNode<TKey, TValue> newNode)
    {
        var node = newNode.Parent;
        while (node != null)
        {
            var parent = node.Parent;
            bool wasLeftChild = parent != null && parent.Left == node;
            var newRoot = Balance(node);
            if (parent == null)
            {
                Root = newRoot;
            }
            else if (wasLeftChild)
            {
                parent.Left = newRoot;
            }
            else
            {
                parent.Right = newRoot;
            }
            node = parent;
        }
    }

    protected override void OnNodeRemoved(AvlNode<TKey, TValue>? parent, AvlNode<TKey, TValue>? child)
    {
        var node = parent ?? child;
        while (node != null)
        {
            var parentOfNode = node.Parent;
            bool wasLeftChild = parentOfNode != null && parentOfNode.Left == node;
            var newRoot = Balance(node);
            if (parentOfNode == null)
            {
                Root = newRoot;
            }
            else if (wasLeftChild)
            {
                parentOfNode.Left = newRoot;
            }
            else
            {
                parentOfNode.Right = newRoot;
            }
            node = parentOfNode;
        }
    }
}