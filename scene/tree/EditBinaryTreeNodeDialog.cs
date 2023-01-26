using System;
using Godot;

public class EditBinaryTreeNodeOptions : Reference
{
    public string Name = "";
    public string FeatureName = "";
    public double Threshold = 0.0;
    public ThresholdOperator ThresholdOperator = ThresholdOperator.LessThan;
    public string LeafClass = "";
    public BinaryTreeNode editedNode = null;

    public EditBinaryTreeNodeOptions(string Name, string FeatureName, double Threshold, ThresholdOperator ThresholdOperator, string LeafClass)
    {
        this.Name = Name;
        this.FeatureName = FeatureName;
        this.Threshold = Threshold;
        this.ThresholdOperator = ThresholdOperator;
        this.LeafClass = LeafClass;
    }
}

public class EditBinaryTreeNodeDialog : WindowDialog
{
    [Signal] public delegate void EditTreeNodeAcceptedEvent(EditBinaryTreeNodeOptions newNodeOptions);
    private static readonly string[] nodeTypeLabels = new string[] { "Decision Node", "Leaf Class Node" };
    private MainUICanvas mainUICanvas = null;
    private DatasetSchema datasetSchema = null;
    private LineEdit nameLineEdit = null;
    private MenuButton nodeTypeMenuBtn = null;
    private VBoxContainer decisionNodeVbox = null;
    private HBoxContainer leafClassHbox = null;
    private MenuButton featureMenuBtn = null;
    private ItemList operatorItemList = null;
    private LineEdit thresholdEdit = null;
    private MenuButton leafClassMenuBtn = null;
    private Button cancelBtn = null;
    private Button okayBtn = null;
    private bool isLeafClass = false;
    private BinaryTreeNode editingNode = null;

    public override void _Ready()
    {
        mainUICanvas = GetTree().Root.GetNode<MainUICanvas>("MainUICanvas");
        nameLineEdit = GetNode<LineEdit>("MainVbox/NameHbox/NameLineEdit");
        nodeTypeMenuBtn = GetNode<MenuButton>("MainVbox/NodeTypeHbox/NodeTypeMenu");
        nodeTypeMenuBtn.GetPopup().Connect("id_pressed", this, nameof(onNodeTypeMenuItemSelected));
        decisionNodeVbox = GetNode<VBoxContainer>("MainVbox/DecisionNodeVbox");
        leafClassHbox = GetNode<HBoxContainer>("MainVbox/LeafClassHbox");
        featureMenuBtn = GetNode<MenuButton>("MainVbox/DecisionNodeVbox/FeatureHbox/FeatureMenuBtn");
        featureMenuBtn.GetPopup().Connect("id_pressed", this, nameof(onFeatureMenuItemSelected));
        operatorItemList = GetNode<ItemList>("MainVbox/DecisionNodeVbox/OperatorHbox/OperatorItemList");
        operatorItemList.Select(0);
        thresholdEdit = GetNode<LineEdit>("MainVbox/DecisionNodeVbox/ThresholdHbox/ThresholdEdit");
        leafClassMenuBtn = GetNode<MenuButton>("MainVbox/LeafClassHbox/LeafClassMenuBtn");
        leafClassMenuBtn.GetPopup().Connect("id_pressed", this, nameof(onLeafClassMenuItemSelected));
        cancelBtn = GetNode<Button>("MainVbox/BtnHbox/CancelBtn");
        cancelBtn.Connect("pressed", this, nameof(onCancelBtnPressed));
        okayBtn = GetNode<Button>("MainVbox/BtnHbox/OkayBtn");
        okayBtn.Connect("pressed", this, nameof(onOkayBtnPressed));

        this.Connect("popup_hide", this, nameof(onPopupHide));
        refreshFeatureNames();
        this.RectSize = new Vector2(this.RectSize.x, 280);
    }

    public DatasetSchema DatasetSchema
    {
        get
        {
            return datasetSchema;
        }
        set
        {
            datasetSchema = value;
            refreshFeatureNames();
        }
    }

    public bool IsEditMode
    {
        get
        {
            return editingNode != null;
        }
    }

    public void SetEditMode(BinaryTreeNode editNode)
    {
        editingNode = editNode;
        nameLineEdit.Text = editNode.GetName();
        if (editNode.GetIsLeafClass())
        {
            onNodeTypeMenuItemSelected(1);
            refreshFeatureNames(null, editNode.GetLeafClass());
        }
        else
        {
            onNodeTypeMenuItemSelected(0);
            thresholdEdit.Text = editNode.FeatureThreshold.ToString();

            if (editNode.ThresholdOperator == ThresholdOperator.LessThan)
            {
                operatorItemList.Select(0);
            }
            else if (editNode.ThresholdOperator == ThresholdOperator.LessThanOrEqual)
            {
                operatorItemList.Select(1);
            }
            else if (editNode.ThresholdOperator == ThresholdOperator.GreaterThan)
            {
                operatorItemList.Select(2);
            }
            else
            {
                operatorItemList.Select(3);
            }

            refreshFeatureNames(editNode.FeatureName, null);
        }
    }

    private void refreshFeatureNames(string targetFeature = null, string targetClass = null)
    {
        if (featureMenuBtn == null)
        {
            return;
        }

        PopupMenu popupMenu = featureMenuBtn.GetPopup();
        popupMenu.Clear();
        featureMenuBtn.Text = "";

        if (datasetSchema == null)
        {
            return;
        }

        for (int i = 0; i < datasetSchema.Count; i++)
        {
            if (datasetSchema.IsContinuousType(i))
            {
                string featureName = datasetSchema.GetFeatureName(i);
                popupMenu.AddItem(featureName);
                if (targetFeature == null && featureMenuBtn.Text.Empty())
                {
                    featureMenuBtn.Text = featureName;
                }
                else if (featureName.Equals(targetFeature))

                {
                    featureMenuBtn.Text = featureName;
                }
            }
        }

        // ======================================

        popupMenu = leafClassMenuBtn.GetPopup();
        popupMenu.Clear();

        leafClassMenuBtn.Text = "";

        string[] targetClassNames = datasetSchema.GetAllTargetClassNames();

        foreach (string className in targetClassNames)
        {
            popupMenu.AddItem(className);
            if (targetClass == null && leafClassMenuBtn.Text.Empty())
            {
                leafClassMenuBtn.Text = className;
            }
            else if (className.Equals(targetClass))
            {
                leafClassMenuBtn.Text = className;
            }
        }
    }

    private void onNodeTypeMenuItemSelected(int index)
    {
        if (!nodeTypeMenuBtn.Text.Equals(nodeTypeLabels[index]))
        {
            nodeTypeMenuBtn.Text = nodeTypeLabels[index];
            if (index == 0)
            {
                isLeafClass = false;
                decisionNodeVbox.Visible = true;
                leafClassHbox.Visible = false;
                this.RectSize = new Vector2(this.RectSize.x, 280);
            }
            else
            {
                isLeafClass = true;
                decisionNodeVbox.Visible = false;
                leafClassHbox.Visible = true;
                this.RectSize = new Vector2(this.RectSize.x, 150);
            }
        }
    }

    private void onFeatureMenuItemSelected(int index)
    {
        featureMenuBtn.Text = featureMenuBtn.GetPopup().GetItemText(index);
    }

    private void onLeafClassMenuItemSelected(int index)
    {
        leafClassMenuBtn.Text = leafClassMenuBtn.GetPopup().GetItemText(index);
    }

    private void onCancelBtnPressed()
    {
        Visible = false;
    }

    private void onOkayBtnPressed()
    {
        EditBinaryTreeNodeOptions newOptions = null;

        string name = nameLineEdit.Text.Trim();
        if (name.Empty())
        {
            mainUICanvas.ShowMessageBox("Invalid node name", "Node name cannot be empty.");
            return;
        }

        if (isLeafClass)
        {
            string selectedClassName = leafClassMenuBtn.Text.Trim();
            if (!datasetSchema.HasTargetClassName(selectedClassName))
            {
                mainUICanvas.ShowMessageBox("Invalid class name", "Selected class '" + selectedClassName + "' is not a valid class name.");
                return;
            }
            newOptions = new EditBinaryTreeNodeOptions(name, null, 0.0, ThresholdOperator.LessThan, leafClassMenuBtn.Text);
        }
        else
        {
            string featureName = featureMenuBtn.Text;
            if (featureName.Empty() || !datasetSchema.HasFeature(featureName))
            {
                mainUICanvas.ShowMessageBox("Invalid feature name", "Database does not have a feature with name '" + featureName + "'");
                return;
            }

            double threshold = 0.0;
            if (!double.TryParse(thresholdEdit.Text, out threshold))
            {
                mainUICanvas.ShowMessageBox("Invalid threshold value", "Please enter a valid real number for the threshold.");
                return;
            }

            ThresholdOperator thresholdOperator = ThresholdOperator.LessThan;

            int[] selectedOperators = operatorItemList.GetSelectedItems();
            if (selectedOperators.Length == 0)
            {
                mainUICanvas.ShowMessageBox("Invalid threshold operator", "Please select an operator from the list.");
                return;
            }

            switch (selectedOperators[0])
            {
                case 1:
                    thresholdOperator = ThresholdOperator.LessThanOrEqual;
                    break;
                case 2:
                    thresholdOperator = ThresholdOperator.GreaterThan;
                    break;
                case 3:
                    thresholdOperator = ThresholdOperator.GreaterThanOrEqual;
                    break;
            }

            newOptions = new EditBinaryTreeNodeOptions(name, featureName, threshold, thresholdOperator, null);
        }

        if (IsEditMode)
        {
            newOptions.editedNode = editingNode;
        }

        EmitSignal("EditTreeNodeAcceptedEvent", newOptions);
        Visible = false;
    }

    private void onPopupHide()
    {
        this.GetParent().CallDeferred("remove_child", this);
        this.CallDeferred("queue_free");
    }
}
