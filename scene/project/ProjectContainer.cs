using System;
using Godot;

public class ProjectContainer : VBoxContainer
{
    private MainUICanvas mainUICanvas = null;
    private Project project;
    private ProjectViewportContainer projectViewport = null;
    private Button refreshBtn = null;
    private ColorPickerButton viewportBackgroundColorPicker = null;

    public override void _Ready()
    {
        mainUICanvas = GetTree().Root.GetNode<MainUICanvas>("MainUICanvas");
        projectViewport = GetNode<ProjectViewportContainer>("ProjectViewportContainer");
        refreshBtn = GetNode<Button>("ViewportMenuBar/RefreshBtn");
        refreshBtn.Connect("pressed", this, nameof(onRefreshBtnPressed));
        viewportBackgroundColorPicker = GetNode<ColorPickerButton>("ViewportMenuBar/BackgroundColorPicker");
        viewportBackgroundColorPicker.Connect("color_changed", this, nameof(onViewportBackgroundColorPickerChanged));
    }

    public Project GetProject()
    {
        return this.project;
    }

    public void SetProject(Project project)
    {
        this.project = project;
    }

    public void RefreshViewport(bool suppressErrors = false)
    {
        ITreeClassifier selectedTree = mainUICanvas.ActiveTree;
        if (!suppressErrors && selectedTree == null)
        {
            mainUICanvas.ShowMessageBox("Error", "You must select a tree before you can display it.");
            return;
        }
        else if (!suppressErrors && !project.HasTree(selectedTree.GetName()))
        {
            mainUICanvas.ShowMessageBox("Error", "Selected tree is not valid for current project.");
            return;
        }

        projectViewport.DisplayTree(selectedTree);
    }

    private void onViewportBackgroundColorPickerChanged(Color newColor)
    {
        projectViewport.BackgroundColorRect.Color = newColor;
    }

    private void onRefreshBtnPressed()
    {
        RefreshViewport();
    }
}
