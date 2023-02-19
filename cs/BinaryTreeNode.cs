using System;
using System.Collections.Generic;
using Godot;

public enum ThresholdOperator
{
    LessThan,
    LessThanOrEqual,
    GreaterThan,
    GreaterThanOrEqual
}

public class BinaryTreeNode : ITreeNode
{
    private string name = null;
    private BinaryTreeNode parentNode = null;
    private BinaryTreeNode[] children = null;
    private string featureName = null;
    private double featureThreshold = 0.0;
    private ThresholdOperator featureThresholdOperator = ThresholdOperator.LessThan;
    private string leafClass = null;
    bool useCustomDisplayPos = false;
    private Vector2 customDisplayPos = Vector2.Zero;

    public BinaryTreeNode(string name)
    {
        this.name = name;
        this.children = new BinaryTreeNode[2];
    }

    public BinaryTreeNode(string name, string featureName, double featureThreshold, ThresholdOperator featureThresholdOperator)
    {
        this.name = name;
        this.children = new BinaryTreeNode[2];
        this.featureName = featureName.Trim();
        this.featureThreshold = featureThreshold;
        this.featureThresholdOperator = featureThresholdOperator;
    }

    public BinaryTreeNode(string name, string leafClass)
    {
        this.name = name;
        this.children = new BinaryTreeNode[2];
        this.leafClass = leafClass;
    }

    public override string GetName()
    {
        return name;
    }

    public override void SetName(string newName)
    {
        name = newName;
    }

    public bool UseCustomDisplayPosition
    {
        get => useCustomDisplayPos;
        set => useCustomDisplayPos = value;
    }

    public Vector2 CustomDisplayPosition
    {
        get => customDisplayPos;
        set => customDisplayPos = value;
    }

    public override ITreeNode GetParentNode()
    {
        return parentNode;
    }

    public override ITreeNode[] GetChildren()
    {
        return children;
    }

    public override int GetChildrenCount()
    {
        return (children[0] != null ? 1 : 0) + (children[1] != null ? 1 : 0);
    }

    public override ITreeNode GetLeftmostChild()
    {
        return children[0];
    }

    public override ITreeNode GetRightmostChild()
    {
        return children[1];
    }

    public string FeatureName
    {
        get
        {
            return featureName;
        }
        set
        {
            featureName = value.Trim();
        }
    }

    public double FeatureThreshold
    {
        get
        {
            return featureThreshold;
        }
        set
        {
            featureThreshold = value;
        }
    }
    public ThresholdOperator ThresholdOperator
    {
        get
        {
            return featureThresholdOperator;
        }
        set
        {
            featureThresholdOperator = value;
        }
    }

    public override bool GetIsLeafClass()
    {
        return leafClass != null;
    }

    public override string GetLeafClass()
    {
        return leafClass;
    }

    public override void SetLeafClass(string className)
    {
        leafClass = className;
    }

    public override void AddChild(ITreeNode child, int index = -1)
    {
        if (GetChildrenCount() >= 2)
        {
            throw new NodeCountOutOfBoundsException("Error adding child node to binary tree: Parent node already has two children.");
        }
        else if (index == -1)
        {
            index = (LeftChild != null ? 1 : 0);
        }

        if (child.GetType() != typeof(BinaryTreeNode))
        {
            throw new InvalidNodeTypeException("Error adding child node to binary tree: Child is not type BinaryTreeNode");
        }
        else if (index < 0 || index > 1)
        {
            throw new NodeCountOutOfBoundsException(index.ToString() + " is not a valid binary tree child index");
        }
        else if (children[index] != null)
        {
            throw new NodeCountOutOfBoundsException("Error adding child node to binary tree: Child already exists at index " + index.ToString());
        }
        else
        {
            BinaryTreeNode btChild = (BinaryTreeNode)child;
            children[index] = btChild;
            btChild._updateParent(this);
        }
    }

    public override void RemoveChild(ITreeNode child)
    {
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] != null && children[i].Equals(child))
            {
                children[i]._updateParent(null);
                children[i] = null;
            }
        }
    }

    public BinaryTreeNode LeftChild
    {
        get { return children[0]; }
        set
        {
            if (children[0] != null)
            {
                children[0]._updateParent(null);
            }

            children[0] = value;

            if (children[0] != null)
            {
                children[0]._updateParent(this);
            }
        }
    }

    public BinaryTreeNode RightChild
    {
        get { return children[1]; }
        set
        {
            if (children[1] != null)
            {
                children[1]._updateParent(null);
            }

            children[1] = value;

            if (children[1] != null)
            {
                children[1]._updateParent(this);
            }
        }
    }

    public bool IsLeaf
    {
        get { return leafClass != null; }
    }

    public override int PredictIndex(Sample s)
    {
        if (IsLeaf)
        {
            // SamplePrediction p = new SamplePrediction();
            // p.parentNode = this;
            // p.childNode = null;
            // p.sample = s;
            // p.leafClass = leafClass;

            // this.predictedSamples[s.Index] = p;
            return -1;
        }

        object feature = s[featureName];
        int childIndex = 0;

        switch (featureThresholdOperator)
        {
            case ThresholdOperator.LessThan:
                childIndex = (double)feature < featureThreshold ? 0 : 1;
                break;
            case ThresholdOperator.LessThanOrEqual:
                childIndex = (double)feature <= featureThreshold ? 0 : 1;
                break;
            case ThresholdOperator.GreaterThan:
                childIndex = (double)feature > featureThreshold ? 0 : 1;
                break;
            case ThresholdOperator.GreaterThanOrEqual:
                childIndex = (double)feature >= featureThreshold ? 0 : 1;
                break;
        }

        return childIndex;
    }

    public override ITreeNode Predict(Sample s)
    {
        int index = PredictIndex(s);
        return children[index];
    }

    public void _updateParent(BinaryTreeNode parent)
    {
        parentNode = parent;
    }
}
