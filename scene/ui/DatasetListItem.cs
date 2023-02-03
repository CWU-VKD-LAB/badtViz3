using System;
using Godot;

public class DatasetListItem : MarginContainer
{
    private MainUICanvas mainUICanvas = null;
    private PackedScene editDatasetDialogScene = null;
    private CheckBox displayCheckbox = null;
    private Label nameLabel = null;
    private Button editButton = null;
    private Dataset dataset = null;

    public override void _Ready()
    {
        mainUICanvas = GetTree().Root.GetNode<MainUICanvas>("MainUICanvas");
        editDatasetDialogScene = GD.Load<PackedScene>("res://scene/ui/EditDatasetDialog.tscn");
        displayCheckbox = GetNodeOrNull<CheckBox>("VBoxContainer/HBoxContainer/DisplayCheckbox");
        displayCheckbox.Connect("toggled", this, nameof(onDisplayCheckboxToggled));
        nameLabel = GetNodeOrNull<Label>("VBoxContainer/HBoxContainer/NameLabel");
        editButton = GetNodeOrNull<Button>("VBoxContainer/HBoxContainer/EditBtn");
        editButton.Connect("pressed", this, nameof(onEditButtonPressed));

        if (displayCheckbox != null)
        {
            displayCheckbox.Pressed = dataset.Display;
        }

        if (nameLabel != null)
        {
            nameLabel.Text = dataset.Name;
        }
    }

    public Dataset Dataset
    {
        get
        {
            return dataset;
        }
        set
        {
            dataset = value;
            if (displayCheckbox != null)
            {
                displayCheckbox.Pressed = dataset.Display;
            }

            if (nameLabel != null)
            {
                nameLabel.Text = dataset.Name;
            }
        }
    }

    public bool Displayed
    {
        get
        {
            return displayCheckbox.Pressed;
        }
    }

    public string DatasetName
    {
        get
        {
            return dataset.Name;
        }
    }

    private void onEditButtonPressed()
    {
        EditDatasetDialog editDatasetDialog = editDatasetDialogScene.Instance<EditDatasetDialog>();
        AddChild(editDatasetDialog);
        editDatasetDialog.Dataset = dataset;
        editDatasetDialog.Connect("popup_hide", this, nameof(onEditDatasetDialogHidden));
        editDatasetDialog.PopupCenteredRatio();
    }

    private void onEditDatasetDialogHidden()
    {
        displayCheckbox.Pressed = dataset.Display;
        nameLabel.Text = dataset.Name;
    }

    private void onDisplayCheckboxToggled(bool enabled)
    {
        dataset.Display = enabled;
        mainUICanvas.RefreshDatasetSamples();
        mainUICanvas.RefreshViewport(true);
    }
}
