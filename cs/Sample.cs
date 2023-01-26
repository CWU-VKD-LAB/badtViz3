using System;
using System.Collections.Generic;
using Godot;

public class Sample : Reference
{
    [Signal] public delegate void VisiblityChangedEvent(bool newVisibility);
    [Signal] public delegate void ColorChangedEvent(Color newColor);
    private Dataset parentDataset = null;
    private int rowArrIndex = -1;
    private bool visible = true;
    private Color color = Color.Color8(0, 0, 0, 255);

    public Sample()
    {
    }

    public Sample(Dataset parentDataset, int rowArrIndex)
    {
        this.parentDataset = parentDataset;
        this.rowArrIndex = rowArrIndex;
    }

    public Dataset ParentDataset
    {
        get { return parentDataset; }
    }

    public int Count
    {
        get
        {
            return parentDataset.ColumnCount;
        }
    }

    public int DatasetRow
    {
        get { return rowArrIndex; }
    }

    public string TargetClass
    {
        get
        {
            return this[parentDataset.Schema.TargetClassColumn].ToString();
        }
    }

    public string Index
    {
        get
        {
            if (parentDataset.Schema.HasIndexColumn())
            {
                return parentDataset.Name + "." + this[parentDataset.Schema.IndexColumn].ToString();
            }
            else
            {
                return parentDataset.Name + "." + rowArrIndex;
            }
        }
    }

    public DatasetSchema Schema
    {
        get
        {
            return parentDataset.Schema;
        }
    }

    public bool Visible
    {
        get
        {
            return this.visible;
        }
        set
        {
            if (this.visible == value)
            {
                return;
            }
            this.visible = value;
            EmitSignal("VisiblityChangedEvent", this.visible);
        }
    }

    public object this[int columnIndex]
    {
        get
        {
            return parentDataset.GetColumn(columnIndex).Get(rowArrIndex);
        }
        set
        {
            parentDataset.GetColumn(columnIndex).Set(rowArrIndex, value);
        }
    }

    public object this[string featureName]
    {
        get
        {
            return parentDataset.GetColumn(featureName).Get(rowArrIndex);
        }
        set
        {
            parentDataset.GetColumn(featureName).Set(rowArrIndex, value);
        }
    }

    public object GetMinValue(string featureName)
    {
        return parentDataset.GetColumn(featureName).GetMinValue();
    }

    public object GetMaxValue(string featureName)
    {
        return parentDataset.GetColumn(featureName).GetMaxValue();
    }

    public Color Color
    {
        get
        {
            return this.color;
        }
        set
        {
            if (this.color == value)
            {
                return;
            }
            this.color = value;
            EmitSignal("ColorChangedEvent", this.color);
        }
    }

    public override bool Equals(object obj)
    {
        if (obj == null || GetType() != obj.GetType())
        {
            return false;
        }

        Sample otherSample = (Sample)obj;

        return otherSample.parentDataset == parentDataset && otherSample.DatasetRow == DatasetRow;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + parentDataset.GetHashCode();
            hash = hash * 31 + DatasetRow.GetHashCode();
            return hash;
        }
    }
}
