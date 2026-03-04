using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.RedBlackTree;

public class RedBlackTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, RbNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    protected override RbNode<TKey, TValue> CreateNode(TKey key, TValue value) => new(key, value);

    #region Insertion fixup

    protected override void OnNodeAdded(RbNode<TKey, TValue> newNode)
    {
        FixAfterInsertion(newNode);
    }

    private void FixAfterInsertion(RbNode<TKey, TValue> z)
    {
        while (z.Parent is RbNode<TKey, TValue> parent && parent.Color == RbColor.Red)
        {
            var grandparent = parent.Parent;
            if (grandparent == null)
            {
                break;
            }
            if (parent == grandparent.Left)
            {
                var uncle = grandparent.Right;
                if (uncle != null && uncle.Color == RbColor.Red)
                {
                    parent.Color = RbColor.Black;
                    uncle.Color = RbColor.Black;
                    grandparent.Color = RbColor.Red;
                    z = grandparent;
                }
                else
                {
                    if (z == parent.Right)
                    {
                        RotateLeft(parent);
                        z = parent;
                        parent = z.Parent;
                    }
                    RotateRight(grandparent);
                    (parent.Color, grandparent.Color) = (grandparent.Color, parent.Color);
                    break;
                }
            }
            else
            {
                var uncle = grandparent.Left;
                if (uncle != null && uncle.Color == RbColor.Red)
                {
                    parent.Color = RbColor.Black;
                    uncle.Color = RbColor.Black;
                    grandparent.Color = RbColor.Red;
                    z = grandparent;
                }
                else
                {
                    if (z == parent.Left)
                    {
                        RotateRight(parent);
                        z = parent;
                        parent = z.Parent;
                    }
                    RotateLeft(grandparent);
                    (parent.Color, grandparent.Color) = (grandparent.Color, parent.Color);
                    break;
                }
            }
        }
        if (Root is RbNode<TKey, TValue> root)
            root.Color = RbColor.Black;
    }

    #endregion
    
    #region Deletion fixup
    
    protected override void RemoveNode(RbNode<TKey, TValue> z)
    {
        RbNode<TKey, TValue>? y = z;
        RbNode<TKey, TValue>? x = null;
        RbNode<TKey, TValue>? xParent = null;
        RbColor yOriginalColor = y.Color;
        bool? xIsLeft = null; // true - x слева от xParent
        if (z.Left == null)
        {
            x = z.Right;
            xParent = z.Parent;
            xIsLeft = z.IsLeftChild; // x в сторону z
            Transplant(z, x);
        }
        else if (z.Right == null)
        {
            x = z.Left;
            xParent = z.Parent;
            xIsLeft = z.IsLeftChild;
            Transplant(z, x);
        }
        else // оба ребенка есть 
        {
            y = z.Right;
            while (y.Left != null)
            {
                y = y.Left;
            }
            yOriginalColor = y.Color;
            x = y.Right;
            if (y.Parent == z)
            {
                xParent = y; // y – правый ребёнок z
                xIsLeft = false; // x будет правым дитём y
                Transplant(z, y);
                y.Left = z.Left;
                y.Left!.Parent = y;
                y.Color = z.Color;
            }
            else
            {
                xParent = y.Parent; // y  глубже
                xIsLeft = y.IsLeftChild; // x на место y
                Transplant(y, x);
                y.Right = z.Right;
                y.Right!.Parent = y;
                Transplant(z, y);
                y.Left = z.Left;
                y.Left!.Parent = y;
                y.Color = z.Color;
            }
        }
        if (yOriginalColor == RbColor.Black)
        {
            FixAfterDeletion(x, xParent, xIsLeft);
        }
        if (Root is RbNode<TKey, TValue> root)
        {
            root.Color = RbColor.Black;
        }
        OnNodeRemoved(z.Parent, x);
    }
    
    private void FixAfterDeletion(RbNode<TKey, TValue>? x, RbNode<TKey, TValue>? parent, bool? xIsLeft) {
        while (x != Root && GetColor(x) == RbColor.Black)
        {
            bool isLeft; 
            if (x != null) // с какой стороны x от parent
            {
                isLeft = x == parent?.Left;
            }
            else
            {
                if (parent == null)
                {
                    break;
                }
                isLeft = xIsLeft!.Value;
            }
            if (isLeft)
            {
                var sibling = parent?.Right; // x – левое д
                if (GetColor(sibling) == RbColor.Red)
                {
                    sibling!.Color = RbColor.Black;
                    parent!.Color = RbColor.Red;
                    RotateLeft(parent);
                    sibling = parent.Right;
                }
                if (GetColor(sibling?.Left) == RbColor.Black && GetColor(sibling?.Right) == RbColor.Black)
                {
                    if (sibling != null)
                    {
                        sibling.Color = RbColor.Red;
                    }
                    x = parent;
                    parent = x?.Parent;
                }
                else
                {
                    if (GetColor(sibling?.Right) == RbColor.Black)
                    {
                        if (sibling?.Left is RbNode<TKey, TValue> leftChild)
                        {
                            leftChild.Color = RbColor.Black;
                        }
                        if (sibling != null)
                        {
                            sibling.Color = RbColor.Red;
                        }
                        RotateRight(sibling!);
                        sibling = parent?.Right;
                    }
                    sibling!.Color = parent!.Color;
                    parent.Color = RbColor.Black;
                    if (sibling.Right is RbNode<TKey, TValue> rightChild)
                    {
                        rightChild.Color = RbColor.Black;
                    }
                    RotateLeft(parent);
                    x = Root;
                }
            }
            else
            {
                var sibling = parent?.Left; // x - правое д
                if (GetColor(sibling) == RbColor.Red)
                {
                    sibling!.Color = RbColor.Black;
                    parent!.Color = RbColor.Red;
                    RotateRight(parent);
                    sibling = parent.Left;
                }
                if (GetColor(sibling?.Left) == RbColor.Black && GetColor(sibling?.Right) == RbColor.Black)
                {
                    if (sibling != null)
                    {
                        sibling.Color = RbColor.Red;
                    }
                    x = parent;
                    parent = x?.Parent;
                }
                else
                {
                    if (GetColor(sibling?.Left) == RbColor.Black)
                    {
                        if (sibling?.Right is RbNode<TKey, TValue> rightChild)
                        {
                            rightChild.Color = RbColor.Black;
                        }
                        if (sibling != null)
                        {
                            sibling.Color = RbColor.Red;
                        }
                        RotateLeft(sibling!);
                        sibling = parent?.Left;
                    }
                    sibling!.Color = parent!.Color;
                    parent.Color = RbColor.Black;
                    if (sibling.Left is RbNode<TKey, TValue> leftChild)
                    {
                        leftChild.Color = RbColor.Black;
                    }
                    RotateRight(parent);
                    x = Root;
                }
            }
        }
        if (x != null)
        {
            x.Color = RbColor.Black;
        }
    }

    private static RbColor GetColor(RbNode<TKey, TValue>? node) => node?.Color ?? RbColor.Black;

    #endregion

    protected override void OnNodeRemoved(RbNode<TKey, TValue>? parent, RbNode<TKey, TValue>? child)
    {
    }
}