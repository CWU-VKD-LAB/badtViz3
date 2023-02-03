using System;
using System.Collections.Generic;
using Godot;

public class MainUICanvas : CanvasLayer
{
    [Signal] public delegate void ProjectAdded(Project newProject);
    [Signal] public delegate void ProjectAboutToBeRemoved(Project newProject);
    [Signal] public delegate void ProjectTreeListChanged(Project project);
    [Signal] public delegate void ProjectTreeNodeChanged(Project project, ITreeClassifier tree);
    [Signal] public delegate void ProjectDatasetCollectionChanged(Project project);

    private List<Project> projects = null;
    private List<ProjectContainer> projectContainers = null;
    private PackedScene messageBoxScene = null;
    private PackedScene stringInputDialogScene = null;
    private PackedScene projectContainerScene = null;
    private PackedScene loadDatasetWizardScene = null;
    private PackedScene editDatasetDialogScene = null;
    private PackedScene editBinaryTreeNodeDialogScene = null;
    private PackedScene loadTreeFileDialogScene = null;

    private MenuButton projectMenuBtn = null;
    private MenuButton treeMenuBtn = null;
    private MenuButton treeNodesMenuBtn = null;
    private MenuButton datasetMenuBtn = null;
    private HSplitContainer treeSplitter = null;
    private HSplitContainer datasetSplitter = null;
    private TabContainer projectTabsContainer = null;
    private TreeContainer treeContainer = null;
    private DatasetContainer datasetContainer = null;
    private StatsHbox statsInfoHbox = null;
    private FileDialog loadTreeFileDialog = null;

    private JSONTreeLoader jsonTreeLoader;

    public MainUICanvas()
    {
        projects = new List<Project>();
        projectContainers = new List<ProjectContainer>();
        jsonTreeLoader = new JSONTreeLoader();
    }

    public override void _Ready()
    {
        messageBoxScene = GD.Load<PackedScene>("res://scene/ui/SimpleMessageBox.tscn");
        stringInputDialogScene = GD.Load<PackedScene>("res://scene/ui/SimpleStringInputDialog.tscn");
        projectContainerScene = GD.Load<PackedScene>("res://scene/project/ProjectContainer.tscn");
        loadDatasetWizardScene = GD.Load<PackedScene>("res://scene/dataset/LoadDatasetWizard.tscn");
        editDatasetDialogScene = GD.Load<PackedScene>("res://scene/ui/EditDatasetDialog.tscn");
        editBinaryTreeNodeDialogScene = GD.Load<PackedScene>("res://scene/tree/EditBinaryTreeNodeDialog.tscn");
        loadTreeFileDialogScene = GD.Load<PackedScene>("res://scene/ui/LoadTreeFileDialog.tscn");

        projectMenuBtn = GetNode<MenuButton>("MainPanel/MainScrollContainer/MainPanelVbox/MainMenuHbox/ProjectMenuBtn");
        projectMenuBtn.GetPopup().Connect("id_pressed", this, nameof(onProjectMenuItemSelected));

        treeMenuBtn = GetNode<MenuButton>("MainPanel/MainScrollContainer/MainPanelVbox/MainMenuHbox/TreeMenuBtn");
        treeMenuBtn.GetPopup().Connect("id_pressed", this, nameof(onTreeMenuItemSelected));

        treeNodesMenuBtn = GetNode<MenuButton>("MainPanel/MainScrollContainer/MainPanelVbox/MainMenuHbox/TreeNodesMenuBtn");
        treeNodesMenuBtn.GetPopup().Connect("id_pressed", this, nameof(onTreeNodesMenuItemSelected));

        datasetMenuBtn = GetNode<MenuButton>("MainPanel/MainScrollContainer/MainPanelVbox/MainMenuHbox/DatasetMenuBtn");
        datasetMenuBtn.GetPopup().Connect("id_pressed", this, nameof(onDatasetMenuItemSelected));

        treeSplitter = GetNode<HSplitContainer>("MainPanel/MainScrollContainer/MainPanelVbox/InfoSplitter/TreeSplitter");
        datasetSplitter = GetNode<HSplitContainer>("MainPanel/MainScrollContainer/MainPanelVbox/InfoSplitter/TreeSplitter/DatasetSplitter");

        projectTabsContainer = GetNode<TabContainer>("MainPanel/MainScrollContainer/MainPanelVbox/InfoSplitter/TreeSplitter/DatasetSplitter/ProjectTabsContainer");
        treeContainer = GetNode<TreeContainer>("MainPanel/MainScrollContainer/MainPanelVbox/InfoSplitter/TreeSplitter/TreeContainer");
        treeContainer.Connect("TreeSelectionChanged", this, nameof(OnTreeContainerSelectionChanged));
        datasetContainer = GetNode<DatasetContainer>("MainPanel/MainScrollContainer/MainPanelVbox/InfoSplitter/TreeSplitter/DatasetSplitter/DatasetContainer");

        statsInfoHbox = GetNode<StatsHbox>("MainPanel/MainScrollContainer/MainPanelVbox/InfoSplitter/InfoVbox/InfoScrollContainer/StatsHbox");

        treeSplitter.SplitOffset = -200;
        datasetSplitter.SplitOffset = 200;
        createDefaultProject();
    }

    public Project ActiveProject
    {
        get
        {
            if (projectTabsContainer.CurrentTab >= 0)
            {
                return (projectTabsContainer.GetCurrentTabControl() as ProjectContainer).GetProject();
            }
            else
            {
                return null;
            }
        }
    }

    public ProjectContainer ActiveProjectContainer
    {
        get
        {
            if (projectTabsContainer.CurrentTab >= 0)
            {
                return projectTabsContainer.GetCurrentTabControl() as ProjectContainer;
            }
            else
            {
                return null;
            }
        }
    }

    public ITreeClassifier ActiveTree
    {
        get
        {
            if (ActiveProject == null)
            {
                return null;
            }

            string selectedTree = treeContainer.SelectedTreeName;
            if (selectedTree == null || !ActiveProject.HasTree(selectedTree))
            {
                return null;
            }

            return ActiveProject.GetTree(selectedTree);
        }
    }

    public void RefreshViewport(bool ignoreErrors = false)
    {
        ActiveProjectContainer.RefreshViewport(ignoreErrors);
    }

    public void RefreshDatasetSamples()
    {
        datasetContainer.RefreshSamplesList();
    }

    public void UpdateStats(Godot.Collections.Array<PredictedResult> predictedResults)
    {
        statsInfoHbox.UpdateStats(predictedResults);
    }

    public void OnTreeContainerSelectionChanged(string treeName)
    {
        RefreshViewport();
    }

    public void ImportDatasetForCurrentProject(string path, DatasetLoaderOptions options)
    {
        if (ActiveProject == null)
        {
            ShowMessageBox("Error importing dataset", "No project is currently selected.");
            return;
        }

        try
        {
            Dataset importedDataset = ActiveProject.ImportDataset(path, options);

            EditDatasetDialog editDatasetDialog = editDatasetDialogScene.Instance<EditDatasetDialog>();
            AddChild(editDatasetDialog);
            editDatasetDialog.Dataset = importedDataset;
            editDatasetDialog.Connect("popup_hide", this, nameof(onEditDatasetDialogHidden));
            editDatasetDialog.PopupCenteredRatio();
        }
        catch (Exception ex)
        {
            ShowMessageBox("Error", ex.GetType().ToString() + "\n\n" + ex.Message + "\n\n" + ex.StackTrace);
        }

    }

    public SimpleMessageBox ShowMessageBox(string windowTitle, string message, bool popupExclusive = true)
    {
        SimpleMessageBox msgBox = messageBoxScene.Instance<SimpleMessageBox>();
        msgBox.WindowTitle = windowTitle;
        msgBox.Message = message;
        this.AddChild(msgBox);
        msgBox.PopupExclusive = popupExclusive;
        msgBox.PopupCentered();

        return msgBox;
    }

    public SimpleStringInputDialog ShowSimpleStringInputDialog(string windowTitle, string label, string inputPlaceholder, bool popupExclusive = true)
    {
        SimpleStringInputDialog inputDialog = stringInputDialogScene.Instance<SimpleStringInputDialog>();
        inputDialog.WindowTitle = windowTitle;
        inputDialog.LabelText = label;
        inputDialog.InputPlaceholder = inputPlaceholder;
        this.AddChild(inputDialog);
        inputDialog.PopupExclusive = popupExclusive;
        inputDialog.PopupCentered();

        return inputDialog;
    }

    private void createDefaultProject()
    {
        try
        {
            Project newProject = new Project("Default project", null, null);
            addProject(newProject);

            ProjectContainer containerInstance = projectContainerScene.Instance<ProjectContainer>();
            containerInstance.SetProject(newProject);
            containerInstance.Name = newProject.Name;

            projectContainers.Add(containerInstance);
            projectTabsContainer.AddChild(containerInstance);
        }
        catch (Exception ex)
        {
            ShowMessageBox("Error", ex.GetType().ToString() + "\n\n" + ex.Message + "\n\n" + ex.StackTrace);
        }
    }

    private void addProject(Project newProject)
    {
        projects.Add(newProject);
        EmitSignal("ProjectAdded", newProject);
    }

    private void removeProject(Project project)
    {
        if (projects.Contains(project))
        {
            EmitSignal("ProjectAboutToBeRemoved", project);
            projects.Remove(project);
        }
    }

    private void onProjectMenuItemSelected(int itemIndex)
    {

    }

    private void onTreeMenuItemSelected(int itemIndex)
    {
        try
        {
            if (ActiveProject == null)
            {
                ShowMessageBox("Error", "You must create a project before you can manage a tree.");
                return;
            }
            else if (itemIndex == 0)
            {
                if (!ActiveProject.IsDatasetSchemaCreated)
                {
                    ShowMessageBox("Error creating new tree", "You cannot create a new tree until a dataset schema is chosen. Import a dataset to automatically create the schema.");
                    return;
                }
                SimpleStringInputDialog inputDialog = ShowSimpleStringInputDialog("Enter name for new tree",
                "Enter a unique name for the tree:", "New Decision Tree");
                inputDialog.Connect("StringInputAcceptedEvent", this, nameof(onNewTreeNameEntered));
            }
            else if (itemIndex == 1)
            {
                if (!ActiveProject.IsDatasetSchemaCreated)
                {
                    ShowMessageBox("Error importing tree", "You cannot import a tree until a dataset schema is chosen. Import a dataset to automatically create the schema.");
                    return;
                }

                if (loadTreeFileDialog == null)
                {
                    loadTreeFileDialog = loadTreeFileDialogScene.Instance<FileDialog>();
                    loadTreeFileDialog.Connect("file_selected", this, nameof(onImportTreeFileSelected));
                    string userHomePath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
                    loadTreeFileDialog.CurrentPath = userHomePath;
                    this.AddChild(loadTreeFileDialog);
                }

                loadTreeFileDialog.PopupCentered();
            }
            else if (itemIndex == 2)
            {
                // Export tree
            }
            else if (itemIndex == 3)
            {
                if (ActiveProject == null)
                {
                    return;
                }

                string selectedTree = treeContainer.SelectedTreeName;
                if (selectedTree == null || !ActiveProject.HasTree(selectedTree))
                {
                    ShowMessageBox("Error", "No tree selected.");
                    return;
                }

                if (!ActiveProject.DeleteTree(selectedTree))
                {
                    ShowMessageBox("Error", "Failed to delete tree:" + selectedTree);
                    return;
                }
                EmitSignal("ProjectTreeListChanged", ActiveProject);
                ActiveProjectContainer.RefreshViewport(true);
            }
        }
        catch (Exception ex)
        {
            ShowMessageBox("Error", ex.GetType().ToString() + "\n\n" + ex.Message + "\n\n" + ex.StackTrace);
        }
    }

    private void onNewTreeNameEntered(string treeName)
    {
        if (ActiveProject == null)
        {
            return;
        }

        try
        {
            ID3Classifier newTree = new ID3Classifier(treeName);
            ActiveProject.AddTree(newTree, true);
            EmitSignal("ProjectTreeListChanged", ActiveProject);
            ActiveProjectContainer.RefreshViewport(true);
        }
        catch (Exception ex)
        {
            ShowMessageBox("Error", ex.GetType().ToString() + "\n\n" + ex.Message + "\n\n" + ex.StackTrace);
        }
    }

    private void onImportTreeFileSelected(string filePath)
    {
        try
        {
            ITreeClassifier importedTree = jsonTreeLoader.LoadTreeFromFile(filePath);
            if (importedTree != null)
            {
                ActiveProject.DatasetSchema.Validate(importedTree);
                ActiveProject.AddTree(importedTree);
                EmitSignal("ProjectTreeListChanged", ActiveProject);
                ActiveProjectContainer.RefreshViewport(true);
            }
        }
        catch (Exception ex)
        {
            ShowMessageBox("Error", ex.GetType().ToString() + "\n\n" + ex.Message + "\n\n" + ex.StackTrace);
        }
    }

    private void onTreeNodesMenuItemSelected(int itemIndex)
    {
        try
        {
            if (ActiveProject == null)
            {
                ShowMessageBox("Error", "You must create a project before you can modify a tree.");
                return;
            }
            else if (itemIndex == 0)
            {
                if (ActiveProject == null)
                {
                    return;
                }

                string selectedTree = treeContainer.SelectedTreeName;
                if (selectedTree == null || !ActiveProject.HasTree(selectedTree))
                {
                    ShowMessageBox("Error", "No tree selected.");
                    return;
                }

                EditBinaryTreeNodeDialog newBtreeNodeDialogInstance = editBinaryTreeNodeDialogScene.Instance<EditBinaryTreeNodeDialog>();
                newBtreeNodeDialogInstance.Connect("EditTreeNodeAcceptedEvent", this, nameof(onEditTreeNodeAccepted));
                newBtreeNodeDialogInstance.DatasetSchema = ActiveProject.DatasetSchema;
                this.AddChild(newBtreeNodeDialogInstance);
                newBtreeNodeDialogInstance.PopupCentered();
            }
            else if (itemIndex == 1)
            {
                if (ActiveProject == null)
                {
                    return;
                }

                string selectedTree = treeContainer.SelectedTreeName;
                if (selectedTree == null || !ActiveProject.HasTree(selectedTree))
                {
                    ShowMessageBox("Error", "No tree selected.");
                    return;
                }

                BinaryTreeNode selectedTreeNode = (BinaryTreeNode)treeContainer.SelectedTreeNode;
                if (selectedTreeNode == null)
                {
                    ShowMessageBox("Error", "No tree node selected.");
                    return;
                }

                EditBinaryTreeNodeDialog newBtreeNodeDialogInstance = editBinaryTreeNodeDialogScene.Instance<EditBinaryTreeNodeDialog>();
                newBtreeNodeDialogInstance.Connect("EditTreeNodeAcceptedEvent", this, nameof(onEditTreeNodeAccepted));
                newBtreeNodeDialogInstance.DatasetSchema = ActiveProject.DatasetSchema;
                this.AddChild(newBtreeNodeDialogInstance);
                newBtreeNodeDialogInstance.SetEditMode(selectedTreeNode);
                newBtreeNodeDialogInstance.PopupCentered();
            }
            else if (itemIndex == 2)
            {
                if (ActiveProject == null)
                {
                    return;
                }

                string selectedTree = treeContainer.SelectedTreeName;
                if (selectedTree == null || !ActiveProject.HasTree(selectedTree))
                {
                    ShowMessageBox("Error", "No tree selected.");
                    return;
                }

                BinaryTreeNode selectedTreeNode = (BinaryTreeNode)treeContainer.SelectedTreeNode;
                if (selectedTreeNode == null)
                {
                    ShowMessageBox("Error", "No tree node selected.");
                    return;
                }

                ITreeClassifier currentTreeBase = ActiveProject.GetTree(selectedTree);
                if (currentTreeBase.GetTreeRoot().Equals(selectedTreeNode))
                {
                    ShowMessageBox("Error", "You cannot delete the root tree node once created.");
                    return;
                }

                ITreeNode parentNode = selectedTreeNode.GetParentNode();
                parentNode.RemoveChild(selectedTreeNode);
                EmitSignal("ProjectTreeNodeChanged", ActiveProject, parentNode);
                ActiveProjectContainer.RefreshViewport(true);
            }
        }
        catch (Exception ex)
        {
            ShowMessageBox("Error", ex.GetType().ToString() + "\n\n" + ex.Message + "\n\n" + ex.StackTrace);
        }
    }

    private void onEditTreeNodeAccepted(EditBinaryTreeNodeOptions newNodeOptions)
    {
        if (ActiveProject == null)
        {
            return;
        }

        string selectedTree = treeContainer.SelectedTreeName;
        if (selectedTree == null || !ActiveProject.HasTree(selectedTree))
        {
            ShowMessageBox("Error", "No tree selected.");
            return;
        }

        try
        {
            if (newNodeOptions.editedNode != null)
            {
                editTreeNode(newNodeOptions);
                EmitSignal("ProjectTreeNodeChanged", ActiveProject, newNodeOptions.editedNode);
            }
            else
            {
                ITreeClassifier currentTreeBase = ActiveProject.GetTree(selectedTree);
                ITreeNode root = currentTreeBase.GetTreeRoot();
                ITreeNode newNode = createTreeNode(currentTreeBase, newNodeOptions);

                if (root == null)
                {
                    currentTreeBase.CreateTreeRoot(newNode);
                }
                else
                {
                    ITreeNode selectedTreeNode = treeContainer.SelectedTreeNode;
                    if (selectedTreeNode == null)
                    {
                        ShowMessageBox("Error", "No parent tree node selected.");
                        return;
                    }
                    selectedTreeNode.AddChild(newNode, selectedTreeNode.GetChildrenCount());
                }

                EmitSignal("ProjectTreeNodeChanged", ActiveProject, currentTreeBase);
            }
            ActiveProjectContainer.RefreshViewport(true);
        }
        catch (Exception ex)
        {
            ShowMessageBox("Error", ex.GetType().ToString() + "\n\n" + ex.Message + "\n\n" + ex.StackTrace);
        }
    }

    private ITreeNode createTreeNode(ITreeClassifier treeBase, EditBinaryTreeNodeOptions options)
    {
        ITreeNode newNode = null;

        if (treeBase.GetType() == typeof(ID3Classifier))
        {
            if (options.LeafClass == null)
            {
                newNode = new BinaryTreeNode(options.Name, options.FeatureName, options.Threshold, options.ThresholdOperator);
            }
            else
            {
                newNode = new BinaryTreeNode(options.Name, options.LeafClass);
            }
            ActiveProjectContainer.RefreshViewport(true);
        }
        else
        {
            throw new InvalidCastException("Unknown tree node type: " + treeBase.GetType().ToString());
        }

        return newNode;
    }

    private ITreeNode editTreeNode(EditBinaryTreeNodeOptions options)
    {
        if (options.editedNode.GetType() == typeof(BinaryTreeNode))
        {
            BinaryTreeNode btreeNode = (BinaryTreeNode)options.editedNode;
            btreeNode.SetName(options.Name);
            if (options.LeafClass != null)
            {
                btreeNode.SetLeafClass(options.LeafClass);
            }
            else
            {
                btreeNode.SetLeafClass(null);
                btreeNode.ThresholdOperator = options.ThresholdOperator;
                btreeNode.FeatureThreshold = options.Threshold;
                btreeNode.FeatureName = options.FeatureName;
            }
        }
        else
        {
            throw new InvalidCastException("Unknown tree node type: " + options.editedNode.GetType().ToString());
        }
        return options.editedNode;
    }

    private void onDatasetMenuItemSelected(int itemIndex)
    {
        try
        {
            if (ActiveProject == null)
            {
                ShowMessageBox("Error", "You must create a project before you can manage a dataset.");
                return;
            }
            else if (itemIndex == 1)
            {
                LoadDatasetWizard loadDatasetWizard = loadDatasetWizardScene.Instance<LoadDatasetWizard>();
                AddChild(loadDatasetWizard);
                loadDatasetWizard.StartWizardFromBeginning();
            }
        }
        catch (Exception ex)
        {
            ShowMessageBox("Error", ex.GetType().ToString() + "\n\n" + ex.Message + "\n\n" + ex.StackTrace);
        }
    }

    public void onEditDatasetDialogHidden()
    {
        datasetContainer.RefreshDatasetList();
        datasetContainer.RefreshSamplesList();
        ActiveProjectContainer.RefreshViewport(true);
    }
}
