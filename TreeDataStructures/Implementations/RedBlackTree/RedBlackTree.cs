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
                        parent = z.Parent!;
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
    protected override void OnNodeRemoved(RbNode<TKey, TValue>? parent, RbNode<TKey, TValue>? child)
    {
        bool deletedWasBlack = (child == null || child.Color == RbColor.Black);
        if (!deletedWasBlack)
        {
            return;
        }
        bool? isLeft = null;
        if (parent != null && child != null)
        {
            if (parent.Left == child)
            {
                isLeft = true;
            }
            else if (parent.Right == child)
            {
                isLeft = false;
            }
        }
        if (parent == null || isLeft == null)
        {
            return;
        }
        FixAfterDeletion(child, parent, isLeft.Value);
    }

    private void FixAfterDeletion(RbNode<TKey, TValue>? x, RbNode<TKey, TValue> parent, bool xIsLeft)
    {
        while (x != Root && GetColor(x) == RbColor.Black)
        {
            bool isLeft = (x != null) ? (x == parent.Left) : xIsLeft;

            if (isLeft)
            {
                var sibling = parent.Right;
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
                    parent = x?.Parent ?? parent;
                    xIsLeft = (x != null) ? (x == parent.Left) : xIsLeft;
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
                        sibling = parent.Right;
                    }
                    sibling!.Color = parent.Color;
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
                var sibling = parent.Left;
                if (GetColor(sibling) == RbColor.Red)
                {
                    sibling!.Color = RbColor.Black;
                    parent.Color = RbColor.Red;
                    RotateRight(parent);
                    sibling = parent.Left;
                }
                if (GetColor(sibling?.Left) == RbColor.Black && GetColor(sibling?.Right) == RbColor.Black)
                {
                    if (sibling != null) sibling.Color = RbColor.Red;
                    x = parent;
                    parent = x?.Parent ?? parent;
                    xIsLeft = (x != null) ? (x == parent.Left) : xIsLeft;
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
                        sibling = parent.Left;
                    }
                    sibling!.Color = parent.Color;
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
}