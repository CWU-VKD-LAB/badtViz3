using System;
using Godot;

public class TreeNodeSprite : Node2D
{
    private ITreeNode sourceNode = null;
    private Dataset sourceDataset = null;
    private ColorRect nodeColorRect = null;
    private TreeNodeLine leftLine = null;
    private TreeNodeLine rightLine = null;
    private TreeNodeSprite parentSprite = null;
    private TreeNodeSprite leftChildSprite = null;
    private TreeNodeSprite rightChildSprite = null;
    private Node2D leftClassLabelNode = null;
    private Label leftClassLabel = null;
    private Node2D rightClassLabelNode = null;
    private Label rightClassLabel = null;
    private Label featureLabel = null;
    private Vector2 nodeSpacing = Vector2.Zero;
    private Vector2 leftLineDest = Vector2.Zero;
    private Vector2 rightLineDest = Vector2.Zero;
    private string leftTargetClass = null;
    private string rightTargetClass = null;
    private float sampleTickPading = 0.2f;
    private bool proportionalLines = true;

    public TreeNodeSprite()
    {
        UpdateLineDestinations();
    }

    public override void _EnterTree()
    {
        nodeColorRect = GetNode<ColorRect>("TreeNodeColorRect");
        leftLine = GetNode<TreeNodeLine>("LeftChildLine");
        rightLine = GetNode<TreeNodeLine>("RightChildLine");
        leftClassLabelNode = GetNode<Node2D>("LeftClassLabelNode");
        leftClassLabel = GetNode<Label>("LeftClassLabelNode/ClassLabel");
        rightClassLabelNode = GetNode<Node2D>("RightClassLabelNode");
        rightClassLabel = GetNode<Label>("RightClassLabelNode/ClassLabel");
        featureLabel = GetNode<Label>("FeatureLabelNode/FeatureLabel");
        if (sourceNode != null)
        {
            featureLabel.Text = ((BinaryTreeNode)sourceNode).FeatureName;
        }
        UpdateLines();
    }

    public Rect2 GetCollisionRect()
    {
        return new Rect2(nodeColorRect.RectGlobalPosition, nodeColorRect.RectSize);
    }

    public ITreeNode SourceNode
    {
        get
        {
            return sourceNode;
        }
        set
        {
            sourceNode = value;
            if (featureLabel != null)
            {
                featureLabel.Text = ((BinaryTreeNode)sourceNode).FeatureName;
            }
        }
    }

    public Dataset SourceDataset
    {
        get
        {
            return sourceDataset;
        }
        set
        {
            sourceDataset = value;
        }
    }

    public string LeftTargetClass
    {
        get
        {
            return leftTargetClass;
        }
        set
        {
            leftTargetClass = value;
            UpdateLines();
        }
    }

    public string RightTargetClass
    {
        get
        {
            return rightTargetClass;
        }
        set
        {
            rightTargetClass = value;
            UpdateLines();
        }
    }

    public TreeNodeSprite ParentSprite
    {
        get { return parentSprite; }
        set { parentSprite = value; }
    }

    public TreeNodeSprite LeftChildSprite
    {
        get { return leftChildSprite; }
        set { leftChildSprite = value; }
    }

    public TreeNodeSprite RightChildSprite
    {
        get { return rightChildSprite; }
        set { rightChildSprite = value; }
    }

    public int PredictSample(Sample s)
    {
        if (sourceNode.GetIsLeafClass())
        {
            return -1;
        }

        return sourceNode.PredictIndex(s);
    }

    public Vector2 GetSamplePositionOnLine(int childIndex, Sample s)
    {
        Vector2 startLeftLine = leftLineDest.LimitLength(leftLine.TickPadding);
        Vector2 endLeftLine = leftLineDest.LimitLength(leftLineDest.Length() - (leftLine.TickPadding));
        Vector2 leftLinePadded = endLeftLine - startLeftLine;

        Vector2 startRightLine = rightLineDest.LimitLength(rightLine.TickPadding);
        Vector2 endRightLine = rightLineDest.LimitLength(rightLineDest.Length() - (rightLine.TickPadding));
        Vector2 rightLinePadded = endRightLine - startRightLine;

        if (childIndex == 0)
        {
            string featureName = ((BinaryTreeNode)SourceNode).FeatureName;
            double minValue = (double)s.GetMinValue(featureName);
            double thresholdValue = ((BinaryTreeNode)SourceNode).FeatureThreshold;
            double sampleValue = (double)s[featureName];
            float delta = (float)((thresholdValue - sampleValue) / (thresholdValue - minValue));
            delta = Mathf.Clamp(delta, 0.0f, 1.0f);
            return startLeftLine + (leftLinePadded * (float)delta);
        }
        else if (childIndex == 1)
        {
            string featureName = ((BinaryTreeNode)SourceNode).FeatureName;
            double maxValue = (double)s.GetMaxValue(featureName);
            double thresholdValue = ((BinaryTreeNode)SourceNode).FeatureThreshold;
            double sampleValue = (double)s[featureName];
            float delta = (float)((sampleValue - thresholdValue) / (maxValue - thresholdValue));
            delta = Mathf.Clamp(delta, 0.0f, 1.0f);
            return startRightLine + (rightLinePadded * delta);
        }
        else
        {
            throw new Exception("Invalid child index, this shouldn't happen.");
        }
    }

    public void UpdateLineDestinations()
    {
        Vector2 maxLeftLineDest = new Vector2(-nodeSpacing.x, nodeSpacing.y) * 0.75f;
        Vector2 maxRightLineDest = nodeSpacing * 0.75f;
        float leftTickPadding = maxLeftLineDest.Length() * sampleTickPading;
        float rightTickPadding = maxRightLineDest.Length() * sampleTickPading;

        if (leftLine != null)
        {
            leftLine.TickPadding = maxLeftLineDest.Length() * sampleTickPading;
        }
        if (rightLine != null)
        {
            rightLine.TickPadding = maxRightLineDest.Length() * sampleTickPading;
        }

        if (sourceNode != null && sourceDataset != null &&
            proportionalLines && !sourceNode.GetIsLeafClass())
        {
            BinaryTreeNode binaryTreeNode = (BinaryTreeNode)sourceNode;
            IDatasetColumn datasetColumn = sourceDataset.GetColumn(binaryTreeNode.FeatureName);
            float rightLineRatio = (float)(((double)datasetColumn.GetMaxValue() - binaryTreeNode.FeatureThreshold) /
                ((double)datasetColumn.GetMaxValue() - (double)datasetColumn.GetMinValue()));

            rightLineRatio = Mathf.Clamp(rightLineRatio, 0.05f, 0.95f);
            float leftLineRatio = 1.0f - rightLineRatio;

            maxLeftLineDest = maxLeftLineDest.LimitLength(((maxLeftLineDest.Length() - (leftTickPadding * 2)) * leftLineRatio) + (leftTickPadding * 2));
            maxRightLineDest = maxRightLineDest.LimitLength(((maxRightLineDest.Length() - (rightTickPadding * 2)) * rightLineRatio) + (rightTickPadding * 2));
        }

        leftLineDest = maxLeftLineDest;
        rightLineDest = maxRightLineDest;
    }

    public void UpdateLines()
    {
        UpdateLineDestinations();

        if (leftLine != null)
        {
            leftLine.Destination = leftLineDest;
            if (leftTargetClass != null)
            {
                leftClassLabelNode.Position = leftLine.Destination + new Vector2(0.0f, 50.0f);
                leftClassLabelNode.Visible = true;
                leftClassLabel.Text = leftTargetClass;
            }
            else
            {
                leftClassLabelNode.Visible = false;
            }
        }
        if (rightLine != null)
        {
            rightLine.Destination = rightLineDest;
            if (rightTargetClass != null)
            {
                rightClassLabelNode.Position = rightLine.Destination + new Vector2(0.0f, 50.0f);
                rightClassLabelNode.Visible = true;
                rightClassLabel.Text = rightTargetClass;
            }
            else
            {
                rightClassLabelNode.Visible = false;
            }
        }
    }

    public Vector2 NodeSpacing
    {
        get { return nodeSpacing; }
        set
        {
            nodeSpacing = value;
            UpdateLines();
        }
    }
}
