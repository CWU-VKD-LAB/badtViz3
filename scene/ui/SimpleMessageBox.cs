using System;
using Godot;

public class SimpleMessageBox : WindowDialog
{
    private TextEdit messageTextEdit = null;
    private Button okayBtn = null;
    private string messageText = "";

    public override void _Ready()
    {
        messageTextEdit = GetNode<TextEdit>("MainVbox/MessageText");
        messageTextEdit.Text = messageText;

        okayBtn = GetNode<Button>("MainVbox/ButtonContainer/OkayBtn");
        okayBtn.Connect("pressed", this, nameof(onOkayBtnPressed));

        this.Connect("popup_hide", this, nameof(onPopupHide));
    }

    public string Message
    {
        get
        {
            return messageText;
        }
        set
        {
            messageText = value;
            if (messageTextEdit != null)
            {
                messageTextEdit.Text = messageText;
            }
        }
    }

    private void onOkayBtnPressed()
    {
        this.Visible = false;
    }

    private void onPopupHide()
    {
        this.GetParent().CallDeferred("remove_child", this);
        this.CallDeferred("queue_free");
    }
}
