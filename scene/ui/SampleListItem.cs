using System;
using Godot;

public class SampleListItem : MarginContainer
{
    private Label nameLabel = null;
    private CheckBox visibleCheckbox = null;
    private ColorPickerButton colorPickerButton = null;
    private Button editButton = null;
    private Sample sample = null;

    public override void _Ready()
    {
        nameLabel = GetNodeOrNull<Label>("VBoxContainer/HBoxContainer/NameLabel");
        visibleCheckbox = GetNodeOrNull<CheckBox>("VBoxContainer/HBoxContainer/VisibleCheckbox");
        colorPickerButton = GetNodeOrNull<ColorPickerButton>("VBoxContainer/HBoxContainer/ColorPickerButton");
        editButton = GetNodeOrNull<Button>("VBoxContainer/HBoxContainer/EditBtn");
        editButton.Connect("pressed", this, nameof(onEditButtonPressed));

        if (nameLabel != null)
        {
            nameLabel.Text = sample.Index;
        }

        if (visibleCheckbox != null)
        {
            visibleCheckbox.Pressed = sample.Visible;
        }

        if (colorPickerButton != null)
        {
            colorPickerButton.Color = sample.Color;
        }
    }

    public Sample Sample
    {
        get
        {
            return sample;
        }
        set
        {
            sample = value;

            if (nameLabel != null)
            {
                nameLabel.Text = sample.Index;
            }

            if (visibleCheckbox != null)
            {
                visibleCheckbox.Pressed = sample.Visible;
            }

            if (colorPickerButton != null)
            {
                colorPickerButton.Color = sample.Color;
            }
        }
    }

    private void onEditButtonPressed()
    {

    }
}
