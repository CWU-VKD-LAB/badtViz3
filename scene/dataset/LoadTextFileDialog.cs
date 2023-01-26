using System;
using Godot;

public class LoadTextFileDialog : WindowDialog
{
    private MainUICanvas mainUICanvas = null;
    private FileDialog datasetFileDialog = null;
    private LineEdit filePathLineEdit = null;
    private Button browseFilesBtn = null;
    private LineEdit datasetNameEdit = null;
    private CheckBox hasIndexColCheckbox = null;
    private SpinBox indexColSpinbox = null;
    private CheckBox hasFileHeaderCheckbox = null;
    private ItemList columnDelimiterItemList = null;
    private HBoxContainer customDelimiterHbox = null;
    private LineEdit customDelimiterLineEdit = null;
    private ItemList nullFeaturesItemList = null;
    private CheckBox ignoreDuplicatesCheckbox = null;

    public override void _Ready()
    {
        mainUICanvas = GetTree().Root.GetNode<MainUICanvas>("MainUICanvas");
        datasetFileDialog = GetNode<FileDialog>("DatasetFileDialog");
        datasetFileDialog.Connect("file_selected", this, nameof(onBrowseFileDialogAccepted));
        filePathLineEdit = GetNode<LineEdit>("MainVbox/FilePathHbox/FilePathEdit");
        browseFilesBtn = GetNode<Button>("MainVbox/FilePathHbox/FileBrowseBtn");
        browseFilesBtn.Connect("pressed", this, nameof(onBrowseFilesBtnPressed));

        datasetNameEdit = GetNode<LineEdit>("MainVbox/NameHbox/NameEdit");
        hasIndexColCheckbox = GetNode<CheckBox>("MainVbox/IndexColHbox/HasIndexCheckbox");
        hasIndexColCheckbox.Connect("toggled", this, nameof(onHasIndexColCheckboxToggled));
        indexColSpinbox = GetNode<SpinBox>("MainVbox/IndexColHbox/IndexColSpinbox");

        hasFileHeaderCheckbox = GetNode<CheckBox>("MainVbox/HasHeaderHbox/HasHeaderCheckbox");

        columnDelimiterItemList = GetNode<ItemList>("MainVbox/DelimiterSelectHbox/ColumnDelimiterList");
        columnDelimiterItemList.Select(0);
        columnDelimiterItemList.Connect("item_selected", this, nameof(onFileDelimiterItemListSelection));
        customDelimiterHbox = GetNode<HBoxContainer>("MainVbox/DelimiterCustomHbox");
        customDelimiterLineEdit = GetNode<LineEdit>("MainVbox/DelimiterCustomHbox/CustomDelimeterEdit");

        nullFeaturesItemList = GetNode<ItemList>("MainVbox/NullFeatureHbox/NullFeatureHandlingList");
        nullFeaturesItemList.Select(0);

        ignoreDuplicatesCheckbox = GetNode<CheckBox>("MainVbox/IgnoreDuplicatesHbox/IgnoreDuplicatesCheckbox");
    }

    public Button CancelButton
    {
        get
        {
            return GetNode<Button>("MainVbox/BtnsHbox/CancelBtn");
        }
    }

    public Button PreviousButton
    {
        get
        {
            return GetNode<Button>("MainVbox/BtnsHbox/PreviousBtn");
        }
    }

    public Button NextButton
    {
        get
        {
            return GetNode<Button>("MainVbox/BtnsHbox/NextBtn");
        }
    }

    public string SelectedFilePath
    {
        get
        {
            return filePathLineEdit.Text.Trim();
        }
    }

    public TextFileDatasetLoaderOptions SelectedOptions
    {
        get
        {
            string datasetName = datasetNameEdit.Text.Trim();
            int indexCol = hasIndexColCheckbox.Pressed ? (int)indexColSpinbox.Value : -1;
            bool hasFileHeader = hasFileHeaderCheckbox.Pressed;

            int[] columnDelimiterListSelectedItems = columnDelimiterItemList.GetSelectedItems();
            int selectedDelimIndex = 0;
            char colDelimiter = ',';

            if (columnDelimiterListSelectedItems.Length > 0)
            {
                selectedDelimIndex = columnDelimiterListSelectedItems[0];
            }

            if (selectedDelimIndex == 0)
            {
                colDelimiter = '\0';
            }
            else if (selectedDelimIndex == 1)
            {
                colDelimiter = ',';
            }
            else if (selectedDelimIndex == 2)
            {
                colDelimiter = ';';
            }
            else if (selectedDelimIndex == 3)
            {
                colDelimiter = '\t';
            }
            else
            {
                if (customDelimiterLineEdit.Text.Length == 0)
                {
                    mainUICanvas.ShowMessageBox("Error", "Custom selected column delimiter cannot be empty.");
                    return null;
                }
                colDelimiter = customDelimiterLineEdit.Text[0];
            }

            NullFeaturePolicy nullFeaturePolicy = NullFeaturePolicy.Skip;
            int[] nullFeaturesListSelectedItems = nullFeaturesItemList.GetSelectedItems();
            if (nullFeaturesListSelectedItems.Length > 0 && nullFeaturesListSelectedItems[0] == 1)
            {
                nullFeaturePolicy = NullFeaturePolicy.InsertZeros;
            }

            bool ignoreDuplicateSamples = ignoreDuplicatesCheckbox.Pressed;

            return new TextFileDatasetLoaderOptions(datasetName, colDelimiter, hasFileHeader, nullFeaturePolicy, ignoreDuplicateSamples, indexCol);
        }
    }

    public void ResetForm()
    {
        string userHomePath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
        datasetFileDialog.CurrentPath = userHomePath;

        filePathLineEdit.Text = "";
        datasetNameEdit.Text = "";
        hasIndexColCheckbox.Pressed = false;
        indexColSpinbox.Editable = false;
        indexColSpinbox.Value = 0;
        hasFileHeaderCheckbox.Pressed = true;
        columnDelimiterItemList.Select(0);
        customDelimiterHbox.Visible = false;
        customDelimiterLineEdit.Text = ",";
        nullFeaturesItemList.Select(0);
        ignoreDuplicatesCheckbox.Pressed = true;
    }

    private void onBrowseFilesBtnPressed()
    {
        datasetFileDialog.PopupCentered();
    }

    private void onBrowseFileDialogAccepted(string filePath)
    {
        filePathLineEdit.Text = filePath.Trim();

        if (datasetNameEdit.Text.Empty())
        {
            datasetNameEdit.Text = System.IO.Path.GetFileNameWithoutExtension(filePathLineEdit.Text);
        }
    }

    private void onHasIndexColCheckboxToggled(bool isChecked)
    {
        indexColSpinbox.Editable = isChecked;
    }

    private void onFileDelimiterItemListSelection(int selectedIndex)
    {
        // Custom selected
        if (selectedIndex == 3)
        {
            customDelimiterHbox.Visible = true;
        }
        else
        {
            customDelimiterHbox.Visible = false;
        }
    }
}
