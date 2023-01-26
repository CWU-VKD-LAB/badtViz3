using System;
using Godot;

public class EditNodeThresholdWindow : WindowDialog
{
    private LineEdit minValueEdit = null;
    private LineEdit maxValueEdit = null;
    private HSlider thresholdSlider = null;
    private Label featureLabel = null;
    private LineEdit thresholdValueEdit = null;
    private Button closeBtn;

    private ITreeClassifier trackedTree;
    private BinaryTreeNode trackedNode;
    private Dataset targetDataset;
    private ProjectViewport projectViewport;

    public override void _Ready()
    {
        minValueEdit = GetNode<LineEdit>("MainMargins/MainHbox/SliderHbox/MinValueInput");
        maxValueEdit = GetNode<LineEdit>("MainMargins/MainHbox/SliderHbox/MaxValueInput");
        thresholdSlider = GetNode<HSlider>("MainMargins/MainHbox/SliderHbox/HSlider");
        thresholdSlider.Connect("drag_ended", this, nameof(onSliderDragEnded));

        featureLabel = GetNode<Label>("MainMargins/MainHbox/FeatureHbox/FeatureLabel");
        thresholdValueEdit = GetNode<LineEdit>("MainMargins/MainHbox/FeatureHbox/ThresholdValueEdit");
        closeBtn = GetNode<Button>("MainMargins/MainHbox/ButtonsHbox/CloseButton");
        resetUI();
    }

    public BinaryTreeNode TrackedNode
    {
        get
        {
            return trackedNode;
        }
    }

    public void SetProjectViewport(ProjectViewport viewport)
    {
        projectViewport = viewport;
    }

    public void SetTrackedNodeAndDataset(ITreeClassifier tree, BinaryTreeNode node, Dataset dataset)
    {
        trackedTree = tree;
        trackedNode = node;
        targetDataset = dataset;
        resetUI();
    }

    private void onSliderDragEnded(bool valueChanged)
    {
        if (valueChanged)
        {
            trackedNode.FeatureThreshold = thresholdSlider.Value;
            refreshThresholdText();
            projectViewport.DisplayTree(trackedTree);
            return;
        }
    }

    private void refreshThresholdText()
    {
        ThresholdOperator thresholdOperator = trackedNode.ThresholdOperator;
        string displayedFeatureName = trackedNode.FeatureName;
        double thresholdValue = trackedNode.FeatureThreshold;

        if (thresholdOperator == ThresholdOperator.LessThan)
        {
            displayedFeatureName += " < ";
        }
        else if (thresholdOperator == ThresholdOperator.LessThanOrEqual)
        {
            displayedFeatureName += " <= ";
        }
        else if (thresholdOperator == ThresholdOperator.GreaterThan)
        {
            displayedFeatureName += " > ";
        }
        else
        {
            displayedFeatureName += " >= ";
        }

        featureLabel.Text = displayedFeatureName;
        thresholdValueEdit.Text = thresholdValue.ToString();
    }

    private void resetUI()
    {
        if (trackedNode == null || targetDataset == null)
        {
            return;
        }

        IDatasetColumn datasetColumn = targetDataset.GetColumn(trackedNode.FeatureName);
        if (datasetColumn == null)
        {
            return;
        }

        if (minValueEdit == null)
        {
            return;
        }

        double minVal = (double)datasetColumn.GetMinValue();
        double maxVal = (double)datasetColumn.GetMaxValue();
        double thresholdValue = trackedNode.FeatureThreshold;

        ThresholdOperator thresholdOperator = trackedNode.ThresholdOperator;

        minValueEdit.Text = minVal.ToString();
        maxValueEdit.Text = maxVal.ToString();
        thresholdSlider.MinValue = minVal;
        thresholdSlider.MaxValue = maxVal;
        thresholdSlider.Value = thresholdValue;

        refreshThresholdText();
    }
}
