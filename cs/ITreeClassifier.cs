using System;
using System.Collections.Generic;
using Godot;

public enum TreeClassifierType
{
    ID3_CLASSIFIER
}

public class TreeClassifierFactory : Reference
{
    public static ITreeClassifier Create(TreeClassifierType treeType, string treeName)
    {
        if (treeType == TreeClassifierType.ID3_CLASSIFIER)
        {
            return new ID3Classifier(treeName);
        }

        return null;
    }
}

public abstract class ITreeClassifier : Reference
{
    public abstract string GetName();
    public abstract ITreeNode GetTreeRoot();
    public abstract void CreateTreeRoot(ITreeNode root, bool overwrite = false);
    public abstract void Train(List<Dataset> trainingDatasets);
    public abstract EvaluationMetrics Evaluate();
    public abstract string Predict(Sample s);
}
