using System;
using Godot;

public class LoadDatasetWizard : Node2D
{
    private MainUICanvas mainUICanvas = null;
    private WindowDialog sourceSelectionDialog = null;
    private ItemList sourceSelectionItemList = null;

    private LoadTextFileDialog loadTextFileDialog = null;

    private int wizardStep = 0;

    public override void _Ready()
    {
        mainUICanvas = GetTree().Root.GetNode<MainUICanvas>("MainUICanvas");
        sourceSelectionDialog = GetNode<WindowDialog>("SourceSelectionDialog");
        sourceSelectionDialog.GetCloseButton().Visible = false;
        sourceSelectionItemList = GetNode<ItemList>("SourceSelectionDialog/MainVbox/SourceTypeList");
        GetNode<Button>("SourceSelectionDialog/MainVbox/BtnHbox/CancelBtn").Connect("pressed", this, nameof(onStepCancelButtonPressed));
        GetNode<Button>("SourceSelectionDialog/MainVbox/BtnHbox/NextBtn").Connect("pressed", this, nameof(onStepNextButtonPressed));

        loadTextFileDialog = GetNode<LoadTextFileDialog>("LoadTextFileDialog");
        loadTextFileDialog.GetCloseButton().Visible = false;
        loadTextFileDialog.CancelButton.Connect("pressed", this, nameof(onStepCancelButtonPressed));
        loadTextFileDialog.PreviousButton.Connect("pressed", this, nameof(onStepPreviousButtonPressed));
        loadTextFileDialog.NextButton.Connect("pressed", this, nameof(onStepNextButtonPressed));

    }

    public void StartWizardFromBeginning()
    {
        sourceSelectionItemList.Select(0);
        loadTextFileDialog.ResetForm();

        wizardStep = 0;
        popupCurrentStep();
    }

    private void popupCurrentStep()
    {
        sourceSelectionDialog.Visible = false;
        loadTextFileDialog.Visible = false;

        if (wizardStep == 0)
        {
            sourceSelectionDialog.PopupCentered();
        }
        else if (wizardStep == 1)
        {
            loadTextFileDialog.PopupCentered();
        }
    }

    private void onStepCancelButtonPressed()
    {
        sourceSelectionDialog.Visible = false;
        this.GetParent().CallDeferred("remove_child", this);
        this.CallDeferred("queue_free");
    }

    private void onStepPreviousButtonPressed()
    {
        if (wizardStep == 1)
        {
            wizardStep = 0;
            popupCurrentStep();
        }
    }

    private void onStepNextButtonPressed()
    {
        if (wizardStep == 0)
        {
            int sourceSelectionIndex = 0;
            int[] selectedItems = sourceSelectionItemList.GetSelectedItems();
            if (selectedItems.Length > 0)
            {
                sourceSelectionIndex = selectedItems[0];
            }

            // Text file import
            if (sourceSelectionIndex == 0)
            {
                wizardStep = 1;
            }
        }
        else if (wizardStep == 1)
        {
            string selectedFilePath = loadTextFileDialog.SelectedFilePath;
            TextFileDatasetLoaderOptions options = loadTextFileDialog.SelectedOptions;
            if (validateTextFileOptions(selectedFilePath, options))
            {
                loadTextFileDialog.Visible = false;
                mainUICanvas.ImportDatasetForCurrentProject(selectedFilePath, options);
                this.GetParent().RemoveChild(this);
                this.QueueFree();
                return;
            }
        }

        popupCurrentStep();
    }

    private bool validateTextFileOptions(string filePath, TextFileDatasetLoaderOptions options)
    {
        if (options == null)
        {
            return false;
        }
        else if (mainUICanvas.ActiveProject == null)
        {
            mainUICanvas.ShowMessageBox("Error", "No project is currently selected.");
            return false;
        }
        else if (filePath.Empty() || !System.IO.File.Exists(filePath))
        {
            mainUICanvas.ShowMessageBox("Error", "Selected file path does not exist or is invalid.");
            return false;
        }
        else if (options.DatasetName.Empty())
        {
            mainUICanvas.ShowMessageBox("Error", "Dataset name cannot be empty.");
            return false;
        }
        else if (mainUICanvas.ActiveProject.HasDataset(options.DatasetName))
        {
            mainUICanvas.ShowMessageBox("Error", "A dataset with the name '" + options.DatasetName + "' already exists in the current project");
            return false;
        }

        return true;
    }
}
