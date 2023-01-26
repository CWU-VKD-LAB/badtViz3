using System;
using System.Collections.Generic;
using Godot;

public class NodeIsNotLeafException : Exception
{
    public NodeIsNotLeafException(string message) : base(message)
    {
    }
}

public class InvalidNodeTypeException : Exception
{
    public InvalidNodeTypeException(string message) : base(message)
    {
    }
}

public class NodeCountOutOfBoundsException : Exception
{
    public NodeCountOutOfBoundsException(string message) : base(message)
    {
    }
}

public class TreeNodeFactory
{
    public static ITreeNode Create(TreeClassifierType treeType, string name)
    {
        if (treeType == TreeClassifierType.ID3_CLASSIFIER)
        {
            return new BinaryTreeNode(name);
        }

        return null;
    }
}

public struct SamplePrediction
{
    public ITreeNode parentNode;
    public ITreeNode childNode;
    public Sample sample;
    public string leafClass;

    public bool isLeafPrediction()
    {
        return leafClass != null && !leafClass.Empty();
    }
}

public abstract class ITreeNode : Reference
{
    public abstract string GetName();
    public abstract void SetName(string newName);
    public abstract ITreeNode GetParentNode();

    public abstract ITreeNode[] GetChildren();

    public abstract int GetChildrenCount();

    public abstract ITreeNode GetLeftmostChild();

    public abstract ITreeNode GetRightmostChild();

    public abstract bool GetIsLeafClass();

    public abstract string GetLeafClass();

    public abstract void SetLeafClass(string className);

    public abstract void AddChild(ITreeNode child, int index);

    public abstract void RemoveChild(ITreeNode child);

    public abstract ITreeNode Predict(Sample s);

    public abstract int PredictIndex(Sample s);
}
