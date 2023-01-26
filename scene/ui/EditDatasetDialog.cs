using System;
using System.Collections.Generic;
using Godot;

public class EditDatasetDialog : WindowDialog
{
    private static Stack<EditDatasetHeaderCell> headerCellCache;
    private static Stack<EditDatasetSampleCell> sampleCellCache;
    private MainUICanvas mainUICanvas = null;
    private Dataset dataset = null;
    private LineEdit datasetNameField = null;
    private SpinBox maxShownRowsSpinbox = null;
    private SpinBox colWidthSpinbox = null;
    private GridContainer headersTable = null;
    private GridContainer samplesTable = null;
    private Label totalRowCountLabel = null;
    private Button doneBtn = null;
    private PackedScene datasetSampleCellScene = null;
    private PackedScene datasetHeaderCellScene = null;
    private int maxShownRows = 20;

    public override void _Ready()
    {
        if (headerCellCache == null)
        {
            headerCellCache = new Stack<EditDatasetHeaderCell>();
        }

        if (sampleCellCache == null)
        {
            sampleCellCache = new Stack<EditDatasetSampleCell>();
        }

        mainUICanvas = GetTree().Root.GetNode<MainUICanvas>("MainUICanvas");
        datasetNameField = GetNode<LineEdit>("MainVbox/TopButtonsHbox/DatasetNameField");
        maxShownRowsSpinbox = GetNode<SpinBox>("MainVbox/TopButtonsHbox/MaxShownRowsSpinbox");
        maxShownRowsSpinbox.Value = maxShownRows;
        colWidthSpinbox = GetNode<SpinBox>("MainVbox/TopButtonsHbox/ColWidthSpinbox");
        headersTable = GetNode<GridContainer>("MainVbox/ScrollContainer/TableVbox/HeadersTable");
        samplesTable = GetNode<GridContainer>("MainVbox/ScrollContainer/TableVbox/SamplesTable");
        totalRowCountLabel = GetNode<Label>("MainVbox/BottomButtonsHbox/SampleCountLabel");
        doneBtn = GetNode<Button>("MainVbox/BottomButtonsHbox/DoneBtn");
        doneBtn.Connect("pressed", this, nameof(onDoneBtnPressed));

        maxShownRowsSpinbox.Connect("value_changed", this, nameof(onMaxShownRowsValueChanged));
        colWidthSpinbox.Connect("value_changed", this, nameof(onHeaderWidthValueChanged));
        datasetNameField.Connect("text_entered", this, nameof(onDatasetNameEntered));
        datasetNameField.Connect("text_changed", this, nameof(onDatasetNameChanged));
        datasetNameField.Connect("focus_exited", this, nameof(onDatasetNameFocusExited));

        this.Connect("popup_hide", this, nameof(onPopupHide));
        this.GetCloseButton().Visible = false;
    }

    public Dataset Dataset
    {
        get { return dataset; }
        set
        {
            dataset = value;
            datasetNameField.Text = dataset.Name;
            totalRowCountLabel.Text = "Total number of rows: " + dataset.Count.ToString();
            RefreshSamplesTable();
        }
    }

    public void RefreshSamplesTable()
    {
        clearTable();

        if (dataset == null)
        {
            return;
        }

        DatasetSchema schema = dataset.ParentCollection.Schema;
        samplesTable.Columns = schema.Count;
        headersTable.Columns = schema.Count;
        maxShownRows = (int)maxShownRowsSpinbox.Value;

        if (datasetSampleCellScene == null)
        {
            datasetSampleCellScene = GD.Load<PackedScene>("res://scene/ui/EditDatasetSampleCell.tscn");
        }

        if (datasetHeaderCellScene == null)
        {
            datasetHeaderCellScene = GD.Load<PackedScene>("res://scene/ui/EditDatasetHeaderCell.tscn");
        }

        for (int i = 0; i < schema.Count; i++)
        {
            EditDatasetHeaderCell newHeaderCell = constructHeaderCell();
            newHeaderCell.SetParentDatasetEditDialog(this);
            newHeaderCell.SetCollectionAndColumn(dataset.ParentCollection, i);
            headersTable.AddChild(newHeaderCell);
        }

        string[] targetClassNames = schema.GetAllTargetClassNames();

        for (int i = 0; i < dataset.Count && i < maxShownRows - 1; i++)
        {
            Sample sample = dataset[i];

            for (int j = 0; j < sample.Count; j++)
            {
                EditDatasetSampleCell newCell = constructSampleCell();
                newCell.SetFeature(sample, j);
                samplesTable.AddChild(newCell);
            }
        }
    }

    private void clearTable()
    {
        foreach (Node child in headersTable.GetChildren())
        {
            EditDatasetHeaderCell castChild = (EditDatasetHeaderCell)child;
            castChild.SetParentDatasetEditDialog(null);
            castChild.SetCollectionAndColumn(null, -1);
            headersTable.RemoveChild(castChild);
            headerCellCache.Push(castChild);
        }

        foreach (Node child in samplesTable.GetChildren())
        {
            EditDatasetSampleCell castChild = (EditDatasetSampleCell)child;
            samplesTable.RemoveChild(castChild);
            castChild.SetFeature(null, -1);
            sampleCellCache.Push(castChild);
        }
    }

    private EditDatasetHeaderCell constructHeaderCell()
    {
        if (headerCellCache.Count > 0)
        {
            return headerCellCache.Pop();
        }
        else
        {
            return datasetHeaderCellScene.Instance<EditDatasetHeaderCell>();
        }
    }

    private EditDatasetSampleCell constructSampleCell()
    {
        if (sampleCellCache.Count > 0)
        {
            return sampleCellCache.Pop();
        }
        else
        {
            return datasetSampleCellScene.Instance<EditDatasetSampleCell>();
        }
    }

    private void resizeColumns(float newVal)
    {
        foreach (Control c in headersTable.GetChildren())
        {
            Vector2 newSize = new Vector2(c.RectMinSize);
            newSize.x = newVal;
            c.RectMinSize = newSize;
        }

        foreach (Control c in samplesTable.GetChildren())
        {
            Vector2 newSize = new Vector2(c.RectMinSize);
            newSize.x = newVal;
            c.RectMinSize = newSize;
        }
    }

    private void onDatasetNameEntered(string newName)
    {
        try
        {
            dataset.Name = newName;
            datasetNameField.Text = dataset.Name;
            if (datasetNameField.Text == dataset.Name)
            {
                datasetNameField.RemoveColorOverride("font_color");
            }
        }
        catch (Exception e)
        {
            GD.PrintErr(e.GetType(), e.StackTrace, e.Message);
        }
    }

    private void onDatasetNameChanged(string newName)
    {
        if (datasetNameField.Text != dataset.Name)
        {
            datasetNameField.AddColorOverride("font_color", Color.Color8(0, 255, 0, 255));
        }
        else
        {
            datasetNameField.RemoveColorOverride("font_color");
        }
    }

    private void onDatasetNameFocusExited()
    {
        datasetNameField.Text = dataset.Name;
        datasetNameField.RemoveColorOverride("font_color");
    }

    private void onMaxShownRowsValueChanged(float val)
    {
        RefreshSamplesTable();
    }

    private void onHeaderWidthValueChanged(float newVal)
    {
        resizeColumns(newVal);
    }

    private void onDoneBtnPressed()
    {
        if (!dataset.Schema.HasTargetClassColumn())
        {
            mainUICanvas.ShowMessageBox("Error", "You must specify which column is the target class before continuing.\n" +
                "To do this, click the desired column header, then choose \"Change Data Type\".");
        }
        else
        {
            dataset.AutoColorSamples();
            this.Visible = false;
        }
    }

    private void onPopupHide()
    {
        this.GetParent().CallDeferred("remove_child", this);
        this.CallDeferred("queue_free");
    }
}
