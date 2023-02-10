using System;
using System.Collections.Generic;
using Godot;

public class EditDatasetSampleCell : MarginContainer
{
    private LineEdit cellField = null;
    private MenuButton cellSelect = null;
    private int featureIndex = -1;
    private Sample sample = null;

    public override void _Ready()
    {
        cellField = GetNodeOrNull<LineEdit>("CellDataLineEdit");
        cellField.Connect("text_entered", this, nameof(onCellDataEntered));
        cellField.Connect("text_changed", this, nameof(onCellDataChanged));
        cellField.Connect("focus_exited", this, nameof(onFocusExited));

        cellSelect = GetNodeOrNull<MenuButton>("CellDataMenuBtn");
        cellSelect.GetPopup().Connect("id_pressed", this, nameof(onDiscreteItemPressed));
        refresh();
    }

    public override void _EnterTree()
    {
        refresh();
    }

    public void SetFeature(Sample sample, int featureIndex)
    {
        this.sample = sample;
        this.featureIndex = featureIndex;
        refresh();
    }

    public object Value
    {
        get { return sample[featureIndex]; }
        set
        {
            sample[featureIndex] = value;
            cellField.Text = value.ToString();
            cellSelect.Text = cellField.Text;
        }
    }

    private void refresh()
    {
        if (cellField != null && sample != null)
        {
            cellField.Text = sample[featureIndex].ToString();
            cellSelect.Text = cellField.Text;

            if (sample.Schema.HasTargetClassColumn() && sample.Schema.TargetClassColumn == featureIndex)
            {
                cellField.Visible = false;
                cellSelect.Visible = true;
                updateTargetClassValues();
            }
            else
            {
                cellField.Visible = true;
                cellSelect.Visible = false;
            }
        }
    }

    private void onCellDataEntered(string newValue)
    {
        try
        {
            sample[featureIndex] = newValue;
        }
        catch (Exception e)
        {
            cellField.Text = Value.ToString();
            GD.PrintErr(e.GetType(), e.StackTrace, e.Message);
        }
        cellField.RemoveColorOverride("font_color");
    }

    private void onCellDataChanged(string newValue)
    {
        if (!Value.ToString().Equals(newValue))
        {
            cellField.AddColorOverride("font_color", Color.Color8(0, 255, 0, 255));
        }
        else
        {
            cellField.RemoveColorOverride("font_color");
        }
    }

    private void onFocusExited()
    {
        if (sample != null)
        {
            cellField.Text = sample[featureIndex].ToString();
            cellField.RemoveColorOverride("font_color");
        }
    }

    private void onDiscreteItemPressed(int id)
    {
        try
        {
            sample[featureIndex] = cellSelect.GetPopup().GetItemText(id);
            cellField.Text = sample[featureIndex].ToString();
            cellSelect.Text = cellField.Text;
        }
        catch (Exception e)
        {
            cellField.Text = Value.ToString();
            cellSelect.Text = cellField.Text;
            GD.PrintErr(e.GetType(), e.StackTrace, e.Message);
        }
    }

    private void updateTargetClassValues()
    {
        if (sample.Schema.HasTargetClassColumn())
        {
            PopupMenu popup = cellSelect.GetPopup();
            popup.Clear();

            List<String> sortedDiscreteValues = new List<string>(sample.Schema.GetAllTargetClassNames());
            sortedDiscreteValues.Sort();

            foreach (string val in sortedDiscreteValues)
            {
                popup.AddItem(val);
            }

            if (sample.Schema.HasTargetClassColumn() && sample.Schema.TargetClassColumn == featureIndex)
            {
                cellSelect.AddColorOverride("font_color", new Color(135, 206, 235, 255));
            }
            else
            {
                cellSelect.RemoveColorOverride("font_color");
            }
        }
    }
}
