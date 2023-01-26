using System;
using Godot;

public class SimpleStringInputDialog : WindowDialog
{
    [Signal] public delegate void StringInputAcceptedEvent(string enteredString);
    private Label mainLabel = null;
    private LineEdit mainLineEdit = null;
    private Button cancelButton = null;
    private Button okayButton = null;
    private string labelText = "";
    private string mainLineEditPlaceholder = "";

    public override void _Ready()
    {
        mainLabel = GetNode<Label>("MainVbox/MainLabel");
        mainLabel.Text = labelText;
        mainLineEdit = GetNode<LineEdit>("MainVbox/InputLineEdit");
        mainLineEdit.Connect("text_changed", this, nameof(onInputTextChanged));

        cancelButton = GetNode<Button>("MainVbox/HBoxContainer/CancelBtn");
        cancelButton.Connect("pressed", this, nameof(onCancelBtnPressed));

        okayButton = GetNode<Button>("MainVbox/HBoxContainer/OkayBtn");
        okayButton.Disabled = true;
        okayButton.Connect("pressed", this, nameof(onOkayBtnPressed));

        this.Connect("popup_hide", this, nameof(onPopupHide));
        this.SetAsMinsize();
    }

    public string LabelText
    {
        get
        {
            return labelText;
        }
        set
        {
            labelText = value;
            if (mainLabel != null)
            {
                mainLabel.Text = value;
            }
        }
    }

    public string InputPlaceholder
    {
        get
        {
            return mainLineEditPlaceholder;
        }
        set
        {
            mainLineEditPlaceholder = value;
            if (mainLineEdit != null)
            {
                mainLineEdit.Text = value;
            }
        }
    }

    public string InputValue
    {
        get
        {
            return mainLineEdit.Text.Trim();
        }
    }

    private void onInputTextChanged(string newText)
    {
        okayButton.Disabled = InputValue.Empty();
    }

    private void onCancelBtnPressed()
    {
        Visible = false;
    }

    private void onOkayBtnPressed()
    {
        EmitSignal("StringInputAcceptedEvent", InputValue);
        Visible = false;
    }

    private void onPopupHide()
    {
        this.GetParent().CallDeferred("remove_child", this);
        this.CallDeferred("queue_free");
    }
}
