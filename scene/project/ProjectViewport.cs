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

public class PredictedResult : Reference
{
    public List<Tuple<Sample, string>> SamplePredictions;
    public Dataset SourceDataset;
}

public class ProjectViewport : Viewport
{
    [Signal] public delegate void TreeNodeSpriteClicked(ITreeClassifier classifier, TreeNodeSprite sprite, Dataset dataset);
    [Signal] public delegate void PredictedResultsChanged(Godot.Collections.Array<PredictedResult> predictedResults);
    private MainUICanvas mainUICanvas = null;
    private static PackedScene nodeSpriteScene = null;
    private static PackedScene sampleSplineScene = null;
    private static PackedScene viewportLabelScene = null;
    private static PackedScene treeNodeColorRect = null;
    private Node2D contentRootNode = null;
    private Camera2D mainCamera = null;
    private Vector2 mousePos = new Vector2();
    private bool cameraIsPanning = false;
    private Vector2 cameraPanningOrigin = new Vector2();
    private Vector2 nodeSpriteSpacing = new Vector2(400f, 400f);
    private Vector2 nodePadding = new Vector2(200f, 100f);
    private Tuple<ITreeClassifier, TreeNodeSprite, Dataset> clickedSprite = null;
    private float clickedSpriteHoldDuration = 0.0f;
    private Vector2 clickedSpriteSourceMousePos = Vector2.Zero;
    private ColorRect draggedColorRect = null;

    private List<Tuple<ITreeClassifier, TreeNodeSprite, Dataset>> currentNodeSprites = null;
    private Godot.Collections.Array<PredictedResult> predictionResults = null;

    public override void _Ready()
    {
        mainUICanvas = GetTree().Root.GetNode<MainUICanvas>("MainUICanvas");
        currentNodeSprites = new List<Tuple<ITreeClassifier, TreeNodeSprite, Dataset>>();
        predictionResults = new Godot.Collections.Array<PredictedResult>();

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

        if (treeNodeColorRect == null)
        {
            treeNodeColorRect = GD.Load<PackedScene>("res://scene/tree/TreeNodeColorRect.tscn");
        }

        if (draggedColorRect == null)
        {
            draggedColorRect = treeNodeColorRect.Instance<ColorRect>();
            AddChild(draggedColorRect);
            draggedColorRect.Visible = false;
        }

        contentRootNode = GetNode<Node2D>("ContentRoot");
        mainCamera = GetNode<Camera2D>("MainCamera");
        mainCamera.Zoom = new Vector2(4.0f, 4.0f);

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
        else if (clickedSprite != null)
        {
            if (clickedSpriteHoldDuration <= 0.25 && clickedSprite.Item2.SourceNode.GetParentNode() != null)
            {
                clickedSpriteHoldDuration += delta;
            }
            else if (clickedSpriteHoldDuration > 0.25)
            {
                draggedColorRect.Visible = true;
                draggedColorRect.SetPosition(getLocalMousePos() - (draggedColorRect.GetRect().Size / 2));
            }
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

    private Vector2 getLocalMousePos()
    {
        return mainCamera.Position + ((mousePos - (Size / 2)) * mainCamera.Zoom);
    }

    public void onGuiInputEvent(InputEvent ev)
    {
        Vector2 localMousePos = getLocalMousePos();

        if (ev.GetType() == typeof(InputEventMouseButton))
        {
            InputEventMouseButton mouseButtonEvent = (InputEventMouseButton)ev;
            if (mouseButtonEvent.IsActionPressed("viewport_primary") && cameraIsPanning == false)
            {
                var spriteUnderMouse = getNodeAtPos(localMousePos);

                if (clickedSprite == null && spriteUnderMouse != null)
                {
                    clickedSprite = spriteUnderMouse;
                    cameraIsPanning = false;
                    clickedSpriteHoldDuration = 0.0f;
                    clickedSpriteSourceMousePos = localMousePos;
                }
                else if (clickedSprite == null && spriteUnderMouse == null)
                {
                    cameraPanningOrigin = mousePos;
                    cameraIsPanning = true;
                }
            }
            else if (mouseButtonEvent.IsActionReleased("viewport_primary") && cameraIsPanning == true)
            {
                cameraIsPanning = false;

            }
            else if (mouseButtonEvent.IsActionReleased("viewport_primary") && cameraIsPanning == false)
            {
                var spriteUnderMouse = getNodeAtPos(localMousePos);
                if (clickedSprite != null && spriteUnderMouse != null && spriteUnderMouse.Item2 == clickedSprite.Item2 && clickedSpriteHoldDuration < 0.25f)
                {
                    EmitSignal("TreeNodeSpriteClicked", clickedSprite.Item1, clickedSprite.Item2, clickedSprite.Item3);
                }
                else if (clickedSprite != null && draggedColorRect.Visible)
                {
                    BinaryTreeNode btreeNode = (BinaryTreeNode)clickedSprite.Item2.SourceNode;
                    clickedSprite.Item2.GlobalPosition = localMousePos;
                    btreeNode.CustomDisplayPosition = clickedSprite.Item2.Position;
                    btreeNode.UseCustomDisplayPosition = true;
                    DisplayTree(clickedSprite.Item1);
                }
                clickedSprite = null;
                cameraIsPanning = false;
                clickedSpriteHoldDuration = 0.0f;
                draggedColorRect.Visible = false;
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
            if (tree == null)
            {
                return;
            }

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

            predictionResults.Clear();

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

                        treeWidthSum += (treeWidth + (nodeSpriteSpacing.x) + nodePadding.x);
                    }
                }
            }

            EmitSignal("PredictedResultsChanged", predictionResults);
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
            contentRootNode.RemoveChild(child);
            child.QueueFree();
            // contentRootNode.CallDeferred("remove_child", child);
            // child.CallDeferred("queue_free");
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
        newNodeSprite.SourceDataset = targetDataset;
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

        BinaryTreeNode sourceBTreeNode = (BinaryTreeNode)node;
        if (sourceBTreeNode.UseCustomDisplayPosition)
        {
            newNodeSprite.Position = sourceBTreeNode.CustomDisplayPosition;
        }
        else
        {
            sourceBTreeNode.CustomDisplayPosition = newNodeSprite.Position;
        }

        float leftMostPos = Mathf.Min(Mathf.Min(leftChild.leftMostPos, rightChild.leftMostPos), newNodeSprite.GlobalPosition.x);
        float rightMostPos = Mathf.Max(Mathf.Max(leftChild.rightMostPos, rightChild.rightMostPos), newNodeSprite.GlobalPosition.x);

        return new CreateNodeResult(newNodeSprite, 1 + childrenSum, leftMostPos, rightMostPos);
    }

    private void createSamplePredictions(TreeNodeSprite root, Dataset d)
    {
        Vector2 startPointGlobal = root.ToGlobal(root.Position - new Vector2(0, 150f));

        List<Tuple<Sample, string>> predictions = new List<Tuple<Sample, string>>();

        for (int i = 0; i < d.Count; i++)
        {
            Sample s = d[i];
            if (s.Visible)
            {
                Tuple<SampleSpline, string> result = createSamplePredictionsRecursive(root, startPointGlobal, s);
                result.Item1.Smooth(true);
                predictions.Add(new Tuple<Sample, string>(s, result.Item2));
            }
        }
        PredictedResult newResult = new PredictedResult();
        newResult.SamplePredictions = predictions;
        newResult.SourceDataset = d;
        predictionResults.Add(newResult);
    }

    private Tuple<SampleSpline, string> createSamplePredictionsRecursive(TreeNodeSprite sprite, Vector2 startPointGlobal, Sample s, SampleSpline newSpline = null)
    {
        if (sprite == null || sprite.SourceNode == null || sprite.SourceNode.GetIsLeafClass())
        {
            return new Tuple<SampleSpline, string>(newSpline, sprite != null ? sprite.SourceNode.GetLeafClass() : null);
        }

        if (newSpline == null)
        {
            newSpline = sampleSplineScene.Instance<SampleSpline>();
            newSpline.Color = s.Color;
            sprite.AddChild(newSpline);
        }

        ITreeNode sourceNode = sprite.SourceNode;
        Tuple<Vector2, int> result = createSampleSpline(sprite, startPointGlobal, s, newSpline);
        Tuple<SampleSpline, string> childResult = null;

        if (result.Item2 == 0)
        {
            ITreeNode leftChild = sourceNode.GetLeftmostChild();
            if (leftChild != null && leftChild.GetIsLeafClass() && s.ParentDataset.Schema.HasTargetClassName(leftChild.GetLeafClass()))
            {
                childResult = new Tuple<SampleSpline, string>(newSpline, leftChild.GetLeafClass());
            }
            else
            {
                childResult = createSamplePredictionsRecursive(sprite.LeftChildSprite, result.Item1, s, newSpline);
            }

        }
        else if (result.Item2 == 1)
        {
            ITreeNode rightChild = sourceNode.GetRightmostChild();
            if (rightChild != null && rightChild.GetIsLeafClass() && s.ParentDataset.Schema.HasTargetClassName(rightChild.GetLeafClass()))
            {
                childResult = new Tuple<SampleSpline, string>(newSpline, rightChild.GetLeafClass());
            }
            else
            {
                childResult = createSamplePredictionsRecursive(sprite.RightChildSprite, result.Item1, s, newSpline);
            }
        }
        else
        {
            childResult = createSamplePredictionsRecursive(null, result.Item1, s, newSpline);
        }

        return childResult;
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

        ITreeNode terminatingChild = sprite.SourceNode.GetChildren()[terminatingPrediction];
        return new Tuple<Vector2, int>(sprite.ToGlobal(terminatingPoint), terminatingPrediction);
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
