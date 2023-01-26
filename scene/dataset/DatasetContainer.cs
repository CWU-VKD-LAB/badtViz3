using System;
using System.Collections.Generic;
using Godot;

public class DatasetContainer : ScrollContainer
{
    private PackedScene datasetListItemScene = null;
    private PackedScene sampleListItemScene = null;
    private MainUICanvas mainUICanvas = null;
    private VBoxContainer datasetsVbox = null;
    private VBoxContainer samplesVbox = null;

    public override void _Ready()
    {
        mainUICanvas = GetTree().Root.GetNode<MainUICanvas>("MainUICanvas");
        mainUICanvas.Connect("ProjectDatasetCollectionChanged", this, nameof(onProjectDatasetCollectionChanged));

        datasetListItemScene = GD.Load<PackedScene>("res://scene/ui/DatasetListItem.tscn");
        sampleListItemScene = GD.Load<PackedScene>("res://scene/ui/SampleListItem.tscn");
        datasetsVbox = GetNode<VBoxContainer>("VSplitter/DatasetsVbox/DatasetsPanel/DatasetsScrollContainer/DatasetsVbox");
        samplesVbox = GetNode<VBoxContainer>("VSplitter/SamplesVbox/SamplesPanel/ScrollContainer/SamplesVbox");
    }

    private void onProjectDatasetCollectionChanged(Project project)
    {
        if (project != mainUICanvas.ActiveProject)
        {
            return;
        }

        RefreshDatasetList();
        RefreshSamplesList();
    }

    public void RefreshDatasetList()
    {
        List<Dataset> datasets = mainUICanvas.ActiveProject.GetAllDatasets();
        clearDatasetsList();

        foreach (Dataset d in datasets)
        {
            DatasetListItem newListItem = datasetListItemScene.Instance<DatasetListItem>();
            newListItem.Dataset = d;
            datasetsVbox.AddChild(newListItem);
        }
    }

    public void RefreshSamplesList()
    {
        List<Dataset> datasets = mainUICanvas.ActiveProject.GetAllDatasets();
        clearSamplesList();

        foreach (Dataset d in datasets)
        {
            if (d.Display)
            {
                for (int i = 0; i < d.Count; i++)
                {
                    SampleListItem newListItem = sampleListItemScene.Instance<SampleListItem>();
                    newListItem.Sample = d[i];
                    samplesVbox.AddChild(newListItem);
                }
            }
        }
    }

    private void clearDatasetsList()
    {
        foreach (Node node in datasetsVbox.GetChildren())
        {
            datasetsVbox.RemoveChild(node);
            node.QueueFree();
        }
    }

    private void clearSamplesList()
    {
        foreach (Node node in samplesVbox.GetChildren())
        {
            samplesVbox.RemoveChild(node);
            node.QueueFree();
        }
    }
}
