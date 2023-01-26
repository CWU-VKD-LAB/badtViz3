using System;
using System.Collections.Generic;
using Godot;

public class ID3Classifier : BinaryTreeClassifier
{
    private string name;
    private BinaryTreeNode rootNode = null;

    public ID3Classifier(string name)
    {
        this.name = name;
    }

    public override string GetName()
    {
        return this.name;
    }

    public override ITreeNode GetTreeRoot()
    {
        return rootNode;
    }

    public override void CreateTreeRoot(ITreeNode newRoot, bool overwrite = false)
    {
        if (newRoot == null)
        {
            throw new NullReferenceException("Cannot create tree root node: New root cannot be null");
        }
        else if (rootNode != null && !overwrite)
        {
            throw new InvalidOperationException("Cannot create tree root node: A tree root already exists for tree: " + name);
        }
        else if (newRoot.GetType() != typeof(BinaryTreeNode))
        {
            throw new InvalidNodeTypeException("Cannot create tree root node: Expected " + typeof(BinaryTreeNode).ToString() + " but recieved " + newRoot.GetType().ToString());
        }

        rootNode = (BinaryTreeNode)newRoot;
    }

    public override void Train(List<Dataset> trainingDatasets)
    {
        List<Sample> trainingSamples = new List<Sample>();

        foreach (Dataset dataset in trainingDatasets)
        {
            foreach (Sample sample in dataset.GetAll())
            {
                trainingSamples.Add(sample);
            }
        }
    }

    public override EvaluationMetrics Evaluate()
    {
        return new EvaluationMetrics();
    }
    public override string Predict(Sample s)
    {
        return "";
    }
}
