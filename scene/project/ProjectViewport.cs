using System;
using System.Collections.Generic;
using Godot;

public enum NodeSpritePosition
{
    LeftChild,
    RightChild
}

struct CreateNodeResult
{
    public TreeNodeSprite sprite;
    public int childrenSum;
    public float leftMostPos;
    public float rightMostPos;

    public CreateNodeResult(TreeNodeSprite sprite, int childrenSum)
    {
        this.sprite = sprite;
        this.childrenSum = childrenSum;
        this.leftMostPos = float.PositiveInfinity;
        this.rightMostPos = float.NegativeInfinity;
    }

    public CreateNodeResult(TreeNodeSprite sprite, int childrenSum, float leftMostPos, float rightMostPos)
    {
        this.sprite = sprite;
        this.childrenSum = childrenSum;
        this.leftMostPos = leftMostPos;
        this.rightMostPos = rightMostPos;
    }
}

public class ProjectViewport : Viewport
{
    [Signal] public delegate void TreeNodeSpriteClicked(ITreeClassifier classifier, TreeNodeSprite sprite, Dataset dataset);
    private MainUICanvas mainUICanvas = null;
    private static PackedScene nodeSpriteScene = null;
    private static PackedScene sampleSplineScene = null;
    private static PackedScene viewportLabelScene = null;
    private Node2D contentRootNode = null;
    private Camera2D mainCamera = null;
    private Vector2 mousePos = new Vector2();
    private bool cameraIsPanning = false;
    private Vector2 cameraPanningOrigin = new Vector2();
    private Vector2 nodeSpriteSpacing = new Vector2(400f, 400f);
    private Vector2 nodePadding = new Vector2(200f, 100f);

    private List<Tuple<ITreeClassifier, TreeNodeSprite, Dataset>> currentNodeSprites = null;

    public override void _Ready()
    {
        mainUICanvas = GetTree().Root.GetNode<MainUICanvas>("MainUICanvas");
        currentNodeSprites = new List<Tuple<ITreeClassifier, TreeNodeSprite, Dataset>>();

        if (nodeSpriteScene == null)
        {
            nodeSpriteScene = GD.Load<PackedScene>("res://scene/tree/TreeNodeSprite.tscn");
        }

        if (sampleSplineScene == null)
        {
            sampleSplineScene = GD.Load<PackedScene>("res://scene/tree/SampleSpline.tscn");
        }

        if (viewportLabelScene == null)
        {
            viewportLabelScene = GD.Load<PackedScene>("res://scene/ui/ViewportLabel.tscn");
        }

        contentRootNode = GetNode<Node2D>("ContentRoot");
        mainCamera = GetNode<Camera2D>("MainCamera");
        mainCamera.Zoom = new Vector2(1.0f, 1.0f);

        // SampleSpline testSpline = sampleSplineScene.Instance<SampleSpline>();
        // testSpline.Curve.AddPoint(new Vector2(0, 0));
        // testSpline.Curve.AddPoint(new Vector2(200, 50));
        // testSpline.Curve.AddPoint(new Vector2(0, 100));
        // testSpline.Color = Color.Color8(0, 0, 0, 255);
        // contentRootNode.AddChild(testSpline);
        // testSpline.Smooth();
    }

    public override void _PhysicsProcess(float delta)
    {
        if (cameraIsPanning)
        {
            mainCamera.Position += (cameraPanningOrigin - mousePos) * mainCamera.Zoom;
            cameraPanningOrigin = mousePos;
        }
    }

    public Vector2 MousePos
    {
        get { return mousePos; }
        set
        {
            mousePos = value;
        }
    }

    public Node2D ContentRootNode
    {
        get { return contentRootNode; }
    }

    public void onGuiInputEvent(InputEvent ev)
    {
        Vector2 localMousePos = mainCamera.Position + ((mousePos - (Size / 2)) * mainCamera.Zoom);

        if (ev.GetType() == typeof(InputEventMouseButton))
        {
            InputEventMouseButton mouseButtonEvent = (InputEventMouseButton)ev;
            if (mouseButtonEvent.IsActionPressed("viewport_primary") && cameraIsPanning == false)
            {
                Tuple<ITreeClassifier, TreeNodeSprite, Dataset> spriteUnderMouse = getNodeAtPos(localMousePos);
                if (spriteUnderMouse != null)
                {
                    cameraIsPanning = false;
                    EmitSignal("TreeNodeSpriteClicked", spriteUnderMouse.Item1, spriteUnderMouse.Item2, spriteUnderMouse.Item3);
                }
                else
                {
                    cameraPanningOrigin = mousePos;
                    cameraIsPanning = true;
                }
            }
            else if (mouseButtonEvent.IsActionReleased("viewport_primary") && cameraIsPanning == true)
            {
                cameraIsPanning = false;
            }

            if (mouseButtonEvent.ButtonIndex == (int)ButtonList.WheelUp)
            {
                mainCamera.Zoom /= 1.1f;
                if (mainCamera.Zoom.x < 0.1f)
                {
                    mainCamera.Zoom = new Vector2(0.1f, 0.1f);
                }
            }

            if (mouseButtonEvent.ButtonIndex == (int)ButtonList.WheelDown)
            {
                mainCamera.Zoom *= 1.1f;
                if (mainCamera.Zoom.x > 10.0f)
                {
                    mainCamera.Zoom = new Vector2(10.0f, 10.0f);
                }
            }
        }
    }

    private Tuple<ITreeClassifier, TreeNodeSprite, Dataset> getNodeAtPos(Vector2 pos)
    {
        if (currentNodeSprites.Count == 0)
        {
            return null;
        }

        foreach (Tuple<ITreeClassifier, TreeNodeSprite, Dataset> spriteTuple in currentNodeSprites)
        {
            if (spriteTuple.Item2.GetCollisionRect().HasPoint(pos))
            {
                return spriteTuple;
            }
        }

        return null;
    }

    public void DisplayTree(ITreeClassifier tree)
    {
        try
        {
            clearTree();

            ITreeNode rootNode = tree.GetTreeRoot();
            if (rootNode == null)
            {
                return;
            }

            if (rootNode.GetIsLeafClass())
            {
                mainUICanvas.ShowMessageBox("Error", "Root tree node cannot be a leaf class node.", true);
                return;
            }

            List<Dataset> datasets = mainUICanvas.ActiveProject.GetAllDatasets();
            float treeWidthSum = 0.0f;

            foreach (Dataset d in datasets)
            {
                if (d.Display)
                {
                    CreateNodeResult createResult = createNodeRecursive(tree, d, rootNode, null);
                    TreeNodeSprite rootNodeSprite = createResult.sprite;
                    if (rootNodeSprite != null)
                    {
                        Label datasetNameLabel = viewportLabelScene.Instance<Label>();
                        datasetNameLabel.Text = d.Name;
                        rootNodeSprite.AddChild(datasetNameLabel);
                        datasetNameLabel.RectPosition = new Vector2(-300, -256);

                        createSamplePredictions(rootNodeSprite, d);

                        float treeWidth = (createResult.rightMostPos - createResult.leftMostPos);
                        rootNodeSprite.Position =
                            new Vector2((treeWidthSum + Math.Abs(rootNodeSprite.GlobalPosition.x - createResult.leftMostPos)), rootNodeSprite.GlobalPosition.y);

                        treeWidthSum += (treeWidth + (nodeSpriteSpacing.x * 2) + nodePadding.x);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            mainUICanvas.ShowMessageBox("Error", ex.GetType().ToString() + "\n\n" + ex.Message + "\n\n" + ex.StackTrace);
        }
    }

    private void clearTree()
    {
        currentNodeSprites.Clear();
        foreach (Node child in contentRootNode.GetChildren())
        {
            contentRootNode.CallDeferred("remove_child", child);
            child.CallDeferred("queue_free");
        }
    }

    private CreateNodeResult createNodeRecursive(ITreeClassifier classifier, Dataset targetDataset, ITreeNode node, TreeNodeSprite parent, NodeSpritePosition position = NodeSpritePosition.LeftChild)
    {
        if (node == null)
        {
            return new CreateNodeResult(null, 0);
        }

        TreeNodeSprite newNodeSprite = nodeSpriteScene.Instance<TreeNodeSprite>();
        newNodeSprite.SourceNode = node;
        newNodeSprite.NodeSpacing = nodeSpriteSpacing;
        newNodeSprite.ParentSprite = parent;
        currentNodeSprites.Add(new Tuple<ITreeClassifier, TreeNodeSprite, Dataset>(classifier, newNodeSprite, targetDataset));

        if (parent == null)
        {
            contentRootNode.AddChild(newNodeSprite);
        }

        ITreeNode leftTreeNode = node.GetLeftmostChild();
        ITreeNode rightTreeNode = node.GetRightmostChild();
        CreateNodeResult leftChild = new CreateNodeResult(null, 0);
        CreateNodeResult rightChild = new CreateNodeResult(null, 0);

        if (leftTreeNode != null && leftTreeNode.GetIsLeafClass())
        {
            newNodeSprite.LeftTargetClass = leftTreeNode.GetLeafClass();
        }
        else
        {
            leftChild = createNodeRecursive(classifier, targetDataset, leftTreeNode, newNodeSprite, NodeSpritePosition.LeftChild);
        }

        if (rightTreeNode != null && rightTreeNode.GetIsLeafClass())
        {
            newNodeSprite.RightTargetClass = rightTreeNode.GetLeafClass();
        }
        else
        {
            rightChild = createNodeRecursive(classifier, targetDataset, rightTreeNode, newNodeSprite, NodeSpritePosition.RightChild);
        }

        int childrenSum = leftChild.childrenSum + rightChild.childrenSum;

        if (parent != null)
        {
            parent.AddChild(newNodeSprite);
            if (position == NodeSpritePosition.LeftChild)
            {
                newNodeSprite.Position = new Vector2(-nodeSpriteSpacing.x + (-nodeSpriteSpacing.x * rightChild.childrenSum) + (-nodePadding.x * (rightChild.childrenSum + 1)), nodeSpriteSpacing.y + nodePadding.y);
                parent.LeftChildSprite = newNodeSprite;
            }
            else
            {
                newNodeSprite.Position = new Vector2(nodeSpriteSpacing.x + (nodeSpriteSpacing.x * leftChild.childrenSum) + (nodePadding.x * (leftChild.childrenSum + 1)), nodeSpriteSpacing.y + nodePadding.y);
                parent.RightChildSprite = newNodeSprite;
            }
        }

        float leftMostPos = Mathf.Min(Mathf.Min(leftChild.leftMostPos, rightChild.leftMostPos), newNodeSprite.GlobalPosition.x);
        float rightMostPos = Mathf.Max(Mathf.Max(leftChild.rightMostPos, rightChild.rightMostPos), newNodeSprite.GlobalPosition.x);

        return new CreateNodeResult(newNodeSprite, 1 + childrenSum, leftMostPos, rightMostPos);
    }

    private void createSamplePredictions(TreeNodeSprite root, Dataset d)
    {
        Vector2 startPointGlobal = root.ToGlobal(root.Position - new Vector2(0, 150f));

        for (int i = 0; i < d.Count; i++)
        {
            Sample s = d[i];
            if (s.Visible)
            {
                createSamplePredictionsRecursive(root, startPointGlobal, s).Smooth(true);
            }
        }
    }

    private SampleSpline createSamplePredictionsRecursive(TreeNodeSprite sprite, Vector2 startPointGlobal, Sample s, SampleSpline newSpline = null)
    {
        if (sprite == null || sprite.SourceNode.GetIsLeafClass())
        {
            return newSpline;
        }

        if (newSpline == null)
        {
            newSpline = sampleSplineScene.Instance<SampleSpline>();
            newSpline.Color = s.Color;
            sprite.AddChild(newSpline);
        }

        Tuple<Vector2, int> result = createSampleSpline(sprite, startPointGlobal, s, newSpline);
        if (result.Item2 == -1)
        {
            return newSpline;
        }

        else if (result.Item2 == 0)
        {
            createSamplePredictionsRecursive(sprite.LeftChildSprite, result.Item1, s, newSpline);
        }
        else
        {
            createSamplePredictionsRecursive(sprite.RightChildSprite, result.Item1, s, newSpline);
        }

        return newSpline;
    }

    private Tuple<Vector2, int> createSampleSpline(TreeNodeSprite sprite, Vector2 startPointGlobal, Sample s, SampleSpline newSpline)
    {
        Vector2 startPoint = sprite.ToLocal(startPointGlobal);
        Vector2 middlePoint = Vector2.Zero;
        Vector2 terminatingPoint = Vector2.Zero;

        int terminatingPrediction = sprite.PredictSample(s);
        terminatingPoint = sprite.GetSamplePositionOnLine(terminatingPrediction, s);
        Vector2 delta = terminatingPoint - startPoint;
        middlePoint = startPoint + (delta / 2);
        float lengthFactor = 1.0f + startPoint.DistanceTo(terminatingPoint) / 500.0f;

        if (terminatingPrediction == 0)
        {
            middlePoint += delta.Normalized().Rotated(Mathf.Pi / 2) * (20 + (GD.Randf() * 50)) * lengthFactor;
        }
        else
        {
            middlePoint += delta.Normalized().Rotated(-(Mathf.Pi / 2)) * (20 + (GD.Randf() * 50)) * lengthFactor;
        }

        if (newSpline.Curve.GetPointCount() == 0)
        {
            newSpline.Curve.AddPoint(newSpline.ToLocal(startPointGlobal));
        }

        newSpline.Curve.AddPoint(newSpline.ToLocal(sprite.ToGlobal(middlePoint)));
        newSpline.Curve.AddPoint(newSpline.ToLocal(sprite.ToGlobal(terminatingPoint)));

        bool isDeadEnd = true;
        ITreeNode terminatingChild = sprite.SourceNode.GetChildren()[terminatingPrediction];

        if (terminatingChild != null && !terminatingChild.GetIsLeafClass())
        {
            isDeadEnd = false;
        }

        return new Tuple<Vector2, int>(sprite.ToGlobal(terminatingPoint), isDeadEnd ? -1 : terminatingPrediction);
    }

    private void predictSample(Sample s, TreeNodeSprite parent, List<TreeNodeSprite> path = null)
    {
        if (path == null)
        {
            path = new List<TreeNodeSprite>();
            path.Add(parent);
        }

        ITreeNode predChild = parent.SourceNode.Predict(s);

        if (predChild != null)
        {

        }
    }
}