using System;
using System.Collections.Generic;
using Godot;

public class DuplicateTreeNameException : Exception
{
    public DuplicateTreeNameException(string message) : base(message)
    {
    }
}

public class Project : Reference
{
    private TextFileDatasetLoader textFileDatasetLoader = null;
    private string name = null;
    private DatasetCollection datasetCollection = null;
    private Dictionary<string, ITreeClassifier> trees = null;


    public Project(string name, DatasetCollection datasetCollection, ITreeClassifier[] trees)
    {
        textFileDatasetLoader = new TextFileDatasetLoader();
        this.name = name.Trim();
        this.datasetCollection = datasetCollection;
        this.trees = new Dictionary<string, ITreeClassifier>();

        if (trees != null)
        {
            foreach (ITreeClassifier tree in trees)
            {
                this.trees[tree.GetName()] = tree;
            }
        }
    }

    public bool IsDatasetSchemaCreated
    {
        get
        {
            if (datasetCollection == null)
            {
                return false;
            }
            return datasetCollection.IsSchemaCreated;
        }
    }

    public ITreeClassifier GetTree(string name)
    {
        return this.trees[name];
    }

    public List<string> GetAllTreeNames()
    {
        return new List<string>(this.trees.Keys);
    }

    public List<ITreeClassifier> GetAllTrees()
    {
        return new List<ITreeClassifier>(this.trees.Values);
    }

    public List<string> GetAllDatasetNames()
    {
        if (datasetCollection == null)
        {
            return new List<string>();
        }
        return datasetCollection.GetAllDatasetNames();
    }

    public Dataset GetDataset(string datasetName)
    {
        if (datasetCollection == null)
        {
            return null;
        }
        return datasetCollection[datasetName];
    }

    public List<Dataset> GetAllDatasets()
    {
        if (datasetCollection == null)
        {
            return null;
        }
        return datasetCollection.GetAll();
    }

    public string Name
    {
        get { return name; }
    }

    public DatasetSchema DatasetSchema
    {
        get
        {
            if (datasetCollection == null)
            {
                return null;
            }
            return datasetCollection.Schema;
        }
    }

    public void AddTree(ITreeClassifier tree, bool skipValidation = false)
    {
        if (datasetCollection != null && !skipValidation)
        {
            datasetCollection.Schema.Validate(tree);
        }

        if (trees.ContainsKey(tree.GetName()))
        {
            throw new DuplicateTreeNameException("Error adding tree: A tree with the name '" + tree.GetName() + "' already exists in this project.");
        }

        trees[tree.GetName()] = tree;
    }

    public bool HasDataset(string datasetName)
    {
        if (datasetCollection == null)
        {
            return false;
        }
        else
        {
            return datasetCollection.HasDataset(datasetName);
        }
    }

    public bool HasTree(string treeName)
    {
        if (trees == null)
        {
            return false;
        }
        else
        {
            return trees.ContainsKey(treeName);
        }
    }

    public bool DeleteTree(string treeName)
    {
        if (trees == null)
        {
            return false;
        }
        else
        {
            return trees.Remove(treeName);
        }
    }

    public Dataset ImportDataset(string path, DatasetLoaderOptions options)
    {
        if (options.GetType() == typeof(TextFileDatasetLoaderOptions))
        {
            return ImportDatasetFromTextFile(path, (TextFileDatasetLoaderOptions)options);
        }
        return null;
    }

    public Dataset ImportDatasetFromTextFile(string filePath, TextFileDatasetLoaderOptions options)
    {
        Tuple<DatasetCollection, Dataset> loadRetVal = textFileDatasetLoader.LoadFromFile(filePath, datasetCollection, options);
        datasetCollection = loadRetVal.Item1;
        return loadRetVal.Item2;
    }
}
