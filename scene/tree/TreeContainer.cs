using System;
using Godot;

public class TreeContainer : ScrollContainer
{
    [Signal] public delegate void TreeSelectionChanged(string selectedTreeName);
    private MainUICanvas mainUICanvas = null;
    private ItemList treesItemList = null;
    private Tree selectedTree = null;

    public override void _Ready()
    {
        mainUICanvas = GetTree().Root.GetNode<MainUICanvas>("MainUICanvas");
        mainUICanvas.Connect("ProjectTreeListChanged", this, nameof(onProjectTreeListChanged));
        mainUICanvas.Connect("ProjectTreeNodeChanged", this, nameof(onProjectTreeNodeChanged));

        treesItemList = GetNode<ItemList>("VSplitter/TreeSelectVbox/TreesItemList");
        treesItemList.Connect("item_selected", this, nameof(onTreeItemSelected));
        selectedTree = GetNode<Tree>("VSplitter/TreeEditVbox/SelectedTree");
    }

    public string SelectedTreeName
    {
        get
        {
            int[] selectedIndices = treesItemList.GetSelectedItems();
            if (selectedIndices.Length == 0)
            {
                return null;
            }

            return treesItemList.GetItemText(selectedIndices[0]);
        }
    }

    public ITreeNode SelectedTreeNode
    {
        get
        {
            string selectedTreeName = SelectedTreeName;
            if (selectedTreeName == null || mainUICanvas.ActiveProject == null)
            {
                return null;
            }

            ITreeClassifier selectedTreeClassifier = mainUICanvas.ActiveProject.GetTree(selectedTreeName);
            if (selectedTreeClassifier == null)
            {
                return null;
            }

            TreeItem selectedTreeItem = selectedTree.GetSelected();
            if (selectedTreeItem == null)
            {
                return null;
            }

            return (ITreeNode)selectedTreeItem.GetMetadata(0);
        }
    }

    private void onProjectTreeListChanged(Project project)
    {
        if (project != mainUICanvas.ActiveProject)
        {
            return;
        }

        refreshTreesList();
    }

    private void onProjectTreeNodeChanged(Project project, ITreeClassifier tree)
    {
        refreshSelectedTree();
    }

    private void onTreeItemSelected(int itemIndex)
    {
        refreshSelectedTree();
        EmitSignal("TreeSelectionChanged", SelectedTreeName);
    }

    private void refreshTreesList()
    {
        string selectedTreeName = null;

        int[] selectedIndices = treesItemList.GetSelectedItems();
        if (selectedIndices.Length > 0)
        {
            selectedTreeName = treesItemList.GetItemText(selectedIndices[0]);
        }

        treesItemList.Clear();

        if (mainUICanvas.ActiveProject == null)
        {
            return;
        }

        foreach (string treeName in mainUICanvas.ActiveProject.GetAllTreeNames())
        {
            treesItemList.AddItem(treeName);
            if (treeName.Equals(selectedTreeName))
            {
                treesItemList.Select(treesItemList.GetItemCount() - 1);
            }
        }

        treesItemList.EnsureCurrentIsVisible();

        refreshSelectedTree();
    }

    private void refreshSelectedTree()
    {
        selectedTree.Clear();

        if (mainUICanvas.ActiveProject == null)
        {
            return;
        }

        string selectedTreeName = null;

        int[] selectedIndices = treesItemList.GetSelectedItems();
        if (selectedIndices.Length > 0)
        {
            selectedTreeName = treesItemList.GetItemText(selectedIndices[0]);
        }
        else
        {
            return;
        }

        ITreeClassifier selectedTreeClassifier = mainUICanvas.ActiveProject.GetTree(selectedTreeName);
        if (selectedTreeClassifier == null)
        {
            mainUICanvas.ShowMessageBox("Error", "Error refreshing current tree: Current project does not have a tree named: " + selectedTreeName);
        }

        ITreeNode rootNode = selectedTreeClassifier.GetTreeRoot();
        if (rootNode == null)
        {
            return;
        }

        TreeItem rootItem = selectedTree.CreateItem();
        rootItem.SetText(0, rootNode.GetName());
        rootItem.SetMetadata(0, rootNode);
        createChildTreeItems(rootItem, rootNode);
    }


    private void createChildTreeItems(TreeItem parentItem, ITreeNode parentNode)
    {
        if (parentItem == null || parentNode == null)
        {
            return;
        }

        ITreeNode[] childrenNodes = parentNode.GetChildren();
        if (childrenNodes == null)
        {
            return;
        }

        foreach (ITreeNode childNode in childrenNodes)
        {
            if (childNode == null) { continue; }
            TreeItem childItem = selectedTree.CreateItem(parentItem);
            childItem.SetText(0, childNode.GetName());
            childItem.SetMetadata(0, childNode);
            createChildTreeItems(childItem, childNode);
        }
    }
}
