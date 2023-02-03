using System;
using System.Collections.Generic;
using Godot;

public class ProjectViewportContainer : ViewportContainer
{
    private MainUICanvas mainUICanvas = null;
    private static PackedScene editNodeThresholdWindowScene = null;
    private ProjectViewport viewport = null;
    private ColorRect viewportBackgroundColorRect = null;
    private EditNodeThresholdWindow editNodeThresholdWindow = null;

    public override void _Ready()
    {
        mainUICanvas = GetTree().Root.GetNode<MainUICanvas>("MainUICanvas");
        if (editNodeThresholdWindowScene == null)
        {
            editNodeThresholdWindowScene = GD.Load<PackedScene>("res://scene/ui/EditNodeThresholdWindow.tscn");
        }

        viewport = GetNode<ProjectViewport>("ProjectViewport");
        viewport.Connect("TreeNodeSpriteClicked", this, nameof(onTreeNodeSpriteClicked));
        viewport.Connect("PredictedResultsChanged", this, nameof(onPredictedResultsChanged));
        viewportBackgroundColorRect = GetNode<ColorRect>("ProjectViewport/CanvasLayer/BackgroundColor");

        Connect("gui_input", this, nameof(onGuiInputEvent));
    }

    public ColorRect BackgroundColorRect
    {
        get { return viewportBackgroundColorRect; }
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        Vector2 mousePos = GetLocalMousePosition();

        if (viewport != null)
        {
            viewport.MousePos = mousePos;
        }
    }

    public void DisplayTree(ITreeClassifier tree)
    {
        viewport.DisplayTree(tree);
    }

    private void onGuiInputEvent(InputEvent inputEvent)
    {
        viewport.onGuiInputEvent(inputEvent);
    }

    private void onTreeNodeSpriteClicked(ITreeClassifier tree, TreeNodeSprite sprite, Dataset dataset)
    {
        if (editNodeThresholdWindow != null)
        {
            mainUICanvas.RemoveChild(editNodeThresholdWindow);
            editNodeThresholdWindow.QueueFree();
        }

        BinaryTreeNode targetNode = (BinaryTreeNode)sprite.SourceNode;
        editNodeThresholdWindow = editNodeThresholdWindowScene.Instance<EditNodeThresholdWindow>();
        mainUICanvas.AddChild(editNodeThresholdWindow);
        editNodeThresholdWindow.SetTrackedNodeAndDataset(tree, targetNode, dataset);
        editNodeThresholdWindow.SetProjectViewport(viewport);
        editNodeThresholdWindow.PopupCentered();
    }

    private void onPredictedResultsChanged(Godot.Collections.Array<PredictedResult> predictedResults)
    {
        mainUICanvas.UpdateStats(predictedResults);
    }
}
