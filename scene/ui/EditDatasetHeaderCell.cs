using System;
using Godot;

public class EditDatasetHeaderCell : MarginContainer
{
    private static PackedScene editDatasetColDatatypeDialogScene = null;
    private MainUICanvas mainUICanvas = null;
    private EditDatasetDialog parentDialog = null;
    private MenuButton cellSelect = null;
    private DatasetCollection collection = null;
    private int columnIndex = -1;

    public override void _Ready()
    {
        mainUICanvas = GetTree().Root.GetNode<MainUICanvas>("MainUICanvas");
        cellSelect = GetNodeOrNull<MenuButton>("HeaderCellMenuBtn");
        cellSelect.GetPopup().Connect("id_pressed", this, nameof(onHeaderMenuBtnPressed));

        if (editDatasetColDatatypeDialogScene == null)
        {
            editDatasetColDatatypeDialogScene = GD.Load<PackedScene>("res://scene/ui/EditDatasetColDatatypeDialog.tscn");
        }

        refresh();
    }

    public void SetParentDatasetEditDialog(EditDatasetDialog dialog)
    {
        this.parentDialog = dialog;
    }

    public void SetCollectionAndColumn(DatasetCollection collection, int columnIndex)
    {
        this.collection = collection;
        this.columnIndex = columnIndex;
        refresh();
    }

    private void refresh()
    {
        if (cellSelect != null && this.collection != null)
        {
            cellSelect.Text = collection.Schema.GetFeatureName(columnIndex);
            if (collection.Schema.HasIndexColumn() && collection.Schema.IndexColumn == columnIndex)
            {
                cellSelect.GetPopup().SetItemDisabled(1, true);
                cellSelect.RemoveColorOverride("font_color");
            }
            else if (collection.Schema.TargetClassColumn == columnIndex)
            {
                cellSelect.AddColorOverride("font_color", new Color(72, 180, 224, 255));
            }
            else
            {
                cellSelect.RemoveColorOverride("font_color");
            }
        }
    }

    private void onHeaderMenuBtnPressed(int id)
    {
        if (id == 0)
        {
            // Rename col
            SimpleStringInputDialog inputDialog = mainUICanvas.ShowSimpleStringInputDialog("Enter new column name",
                "Enter a new name for column " + (columnIndex + 1).ToString(),
                "Column " + (columnIndex + 1).ToString());

            inputDialog.Connect("StringInputAcceptedEvent", this, nameof(onNewHeaderNameAccepted));
        }
        else if (id == 1)
        {
            // Change col data type
            EditDatasetColDatatypeDialog editColDatatypeInstance = editDatasetColDatatypeDialogScene.Instance<EditDatasetColDatatypeDialog>();

            editColDatatypeInstance.SelectedType = getHeaderFeatureType();
            editColDatatypeInstance.Connect("DatasetDatatypeAccepted", this, nameof(onDatasetTypeChangeAccepted));
            mainUICanvas.AddChild(editColDatatypeInstance);
            editColDatatypeInstance.PopupCentered();
        }
    }

    private HeaderFeatureType getHeaderFeatureType()
    {
        HeaderFeatureType headerFeatureType = HeaderFeatureType.Continuous;

        if (collection.Schema.HasIndexColumn() && collection.Schema.IndexColumn == columnIndex)
        {
            headerFeatureType = HeaderFeatureType.Index;
        }
        else if (collection.Schema.HasTargetClassColumn() && collection.Schema.TargetClassColumn == columnIndex)
        {
            headerFeatureType = HeaderFeatureType.TargetClass;
        }
        else if (!DatasetSchema.IsContinuousType(collection.Schema[columnIndex]))
        {
            headerFeatureType = HeaderFeatureType.Discrete;
        }

        return headerFeatureType;
    }

    private void onNewHeaderNameAccepted(string newName)
    {
        if (newName.Empty() || newName.Equals(collection.Schema.GetFeatureName(columnIndex)))
        {
            return;
        }
        else if (collection.Schema.HasFeature(newName))
        {
            mainUICanvas.ShowMessageBox("Error", "A column with the name '" + newName + "' already exists.");
            return;
        }

        collection.Schema.SetFeatureName(columnIndex, newName);
        cellSelect.Text = collection.Schema.GetFeatureName(columnIndex);
    }

    private void onDatasetTypeChangeAccepted(HeaderFeatureType newType)
    {
        try
        {
            HeaderFeatureType oldType = getHeaderFeatureType();
            if (oldType == newType)
            {
                return;
            }

            if (newType == HeaderFeatureType.Continuous)
            {
                collection.ConvertColumnType<double>(columnIndex);
            }
            else
            {
                collection.ConvertColumnType<string>(columnIndex);
            }

            if (oldType == HeaderFeatureType.Index)
            {
                collection.Schema.IndexColumn = -1;
            }
            else if (oldType == HeaderFeatureType.TargetClass)
            {
                collection.Schema.TargetClassColumn = -1;
                collection.Schema.RebuildTargetClassNames(collection);
            }

            if (newType == HeaderFeatureType.Index)
            {
                collection.Schema.IndexColumn = columnIndex;
            }
            else if (newType == HeaderFeatureType.TargetClass)
            {
                collection.Schema.TargetClassColumn = columnIndex;
                collection.Schema.RebuildTargetClassNames(collection);
            }

            refresh();
            parentDialog.RefreshSamplesTable();
        }
        catch (Exception ex)
        {
            mainUICanvas.ShowMessageBox("Error", ex.GetType().ToString() + "\n\n" + ex.Message + "\n\n" + ex.StackTrace);
        }
    }
}
