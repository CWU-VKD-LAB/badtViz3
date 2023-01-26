using System;
using Godot;

public enum HeaderFeatureType
{
    Continuous,
    Discrete,
    TargetClass,
    Index
}

public class EditDatasetColDatatypeDialog : WindowDialog
{
    [Signal] public delegate void DatasetDatatypeAccepted(HeaderFeatureType newType);

    private ItemList datatypeItemList = null;
    private Button okayBtn = null;
    private Button cancelBtn = null;
    private HeaderFeatureType defaultSelected = HeaderFeatureType.Continuous;

    public override void _Ready()
    {
        datatypeItemList = GetNode<ItemList>("MainVbox/DatatypeItemList");
        okayBtn = GetNode<Button>("MainVbox/BtnHbox/OkayBtn");
        okayBtn.Connect("pressed", this, nameof(onOkayBtnPressed));
        cancelBtn = GetNode<Button>("MainVbox/BtnHbox/CancelBtn");
        cancelBtn.Connect("pressed", this, nameof(onCancelBtnPressed));

        SelectedType = defaultSelected;
        this.Connect("popup_hide", this, nameof(onPopupHide));
    }

    public HeaderFeatureType SelectedType
    {
        get
        {
            int selectedIndex = datatypeItemList.GetSelectedItems()[0];
            if (selectedIndex == 0)
            {
                return HeaderFeatureType.Continuous;
            }
            else if (selectedIndex == 1)
            {
                return HeaderFeatureType.Discrete;
            }
            else if (selectedIndex == 2)
            {
                return HeaderFeatureType.TargetClass;
            }
            else
            {
                return HeaderFeatureType.Index;
            }
        }
        set
        {
            defaultSelected = value;
            if (datatypeItemList != null)
            {
                if (defaultSelected == HeaderFeatureType.Continuous)
                {
                    datatypeItemList.Select(0);
                }
                else if (defaultSelected == HeaderFeatureType.Discrete)
                {
                    datatypeItemList.Select(1);
                }
                else if (defaultSelected == HeaderFeatureType.TargetClass)
                {
                    datatypeItemList.Select(2);
                }
                else
                {
                    datatypeItemList.Select(3);
                }
            }
        }
    }

    private void onOkayBtnPressed()
    {
        EmitSignal("DatasetDatatypeAccepted", SelectedType);
        Visible = false;
    }

    private void onCancelBtnPressed()
    {
        Visible = false;
    }

    private void onPopupHide()
    {
        this.GetParent().CallDeferred("remove_child", this);
        this.CallDeferred("queue_free");
    }
}
