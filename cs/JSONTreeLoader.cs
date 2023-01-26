using System;
using Godot;

public class JSONTreeLoader : TreeLoader
{
    public override ITreeClassifier LoadTreeFromFile(string path)
    {
        File gdFile = new File();
        Error error = gdFile.Open(path, File.ModeFlags.Read);
        if (error != Error.Ok)
        {
            return null;
        }

        JSONParseResult jsonResult = JSON.Parse(gdFile.GetAsText());
        if (jsonResult.Error != Error.Ok)
        {
            return null;
        }

        if (!(jsonResult.Result is Godot.Collections.Dictionary))
        {
            return null;
        }

        return parseJson(jsonResult.Result as Godot.Collections.Dictionary, System.IO.Path.GetFileNameWithoutExtension(path));
    }

    private ITreeClassifier parseJson(Godot.Collections.Dictionary jsonData, string treeName)
    {
        if (jsonData == null || jsonData.Count <= 0)
        {
            return null;
        }

        if (!jsonData.Contains("tree_root") || !(jsonData["tree_root"] is Godot.Collections.Dictionary))
        {
            return null;
        }

        TreeClassifierType treeType = TreeClassifierType.ID3_CLASSIFIER;
        if (jsonData.Contains("tree_type"))
        {
            treeType = (TreeClassifierType)Enum.Parse(typeof(TreeClassifierType), jsonData["tree_type"].ToString());
        }

        ITreeClassifier tree = TreeClassifierFactory.Create(treeType, treeName);
        ITreeNode treeRoot = parseNodeRecursive(jsonData["tree_root"] as Godot.Collections.Dictionary, treeType, "root");
        tree.CreateTreeRoot(treeRoot, true);
        return tree;
    }

    private ITreeNode parseNodeRecursive(Godot.Collections.Dictionary nodeData, TreeClassifierType treeType, string nodeName)
    {
        if (nodeData == null) { return null; }

        ITreeNode newNode = TreeNodeFactory.Create(treeType, nodeName);
        setNodeData(newNode, nodeData, treeType);

        if (nodeData.Contains("child_left") && nodeData["child_left"] is Godot.Collections.Dictionary)
        {
            ITreeNode leftChild = parseChildNode(nodeData["child_left"] as Godot.Collections.Dictionary, treeType, "left_child");
            if (leftChild != null)
            {
                newNode.AddChild(leftChild, 0);
            }
        }

        if (nodeData.Contains("child_right") && nodeData["child_right"] is Godot.Collections.Dictionary)
        {
            ITreeNode leftChild = parseChildNode(nodeData["child_right"] as Godot.Collections.Dictionary, treeType, "right_child");
            if (leftChild != null)
            {
                newNode.AddChild(leftChild, 1);
            }
        }

        return newNode;
    }

    private void setNodeData(ITreeNode node, Godot.Collections.Dictionary nodeData, TreeClassifierType treeType)
    {
        if (node == null) { return; }

        if (treeType == TreeClassifierType.ID3_CLASSIFIER)
        {
            BinaryTreeNode btreeNode = node as BinaryTreeNode;

            if (nodeData.Contains("attribute"))
            {
                btreeNode.FeatureName = nodeData["attribute"] as string;
            }

            if (nodeData.Contains("threshold"))
            {
                if (nodeData["threshold"] is string)
                {
                    btreeNode.FeatureThreshold = double.Parse(nodeData["threshold"] as string);
                }
                else
                {
                    object doubleVariant = GD.Convert(nodeData["threshold"], Variant.Type.Real);
                    btreeNode.FeatureThreshold = double.Parse(doubleVariant.ToString());
                }
            }

            if (nodeData.Contains("operator"))
            {
                string thresholdOperator = (nodeData["operator"] as string).Trim();
                ThresholdOperator parsedOpererator = ThresholdOperator.LessThan;

                if (thresholdOperator == "=<" || thresholdOperator == "<=")
                {
                    parsedOpererator = ThresholdOperator.LessThanOrEqual;
                }
                else if (thresholdOperator == ">")
                {
                    parsedOpererator = ThresholdOperator.GreaterThan;
                }
                else if (thresholdOperator == "=>" || thresholdOperator == ">=")
                {
                    parsedOpererator = ThresholdOperator.GreaterThanOrEqual;
                }
                btreeNode.ThresholdOperator = parsedOpererator;
            }
        }
    }

    private ITreeNode parseChildNode(Godot.Collections.Dictionary nodeData, TreeClassifierType treeType, string nodeName)
    {
        if (nodeData == null) { return null; }

        if (nodeData.Contains("leaf_class"))
        {
            string leafClass = nodeData["leaf_class"] as string;
            ITreeNode leafChild = TreeNodeFactory.Create(treeType, leafClass);
            leafChild.SetLeafClass(leafClass);
            return leafChild;
        }
        else
        {
            return parseNodeRecursive(nodeData, treeType, nodeName);
        }
    }
}
