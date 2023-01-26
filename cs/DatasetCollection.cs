using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class FeatureCountMismatchException : Exception
{
    public FeatureCountMismatchException(string message) : base(message)
    {
    }
}

public class FeatureTypeMismatchException : Exception
{
    public FeatureTypeMismatchException(string message) : base(message)
    {
    }
}

public class DuplicateFeatureIndexException : Exception
{
    public DuplicateFeatureIndexException(string message) : base(message)
    {
    }
}

public class DuplicateDatasetNameException : Exception
{
    public DuplicateDatasetNameException(string message) : base(message)
    {
    }
}

public class InvalidTargetClassException : Exception
{
    public InvalidTargetClassException(string message) : base(message)
    {
    }
}

public class DatasetSchemaFactory : Reference
{
    public static DatasetSchema CreateFromFeatureData(List<string> featureNames, List<string> featureData)
    {
        DatasetSchema newSchema = new DatasetSchema();

        for (int i = 0; i < featureNames.Count; i++)
        {
            if (isContinuousFeature(featureData[i]))
            {
                newSchema.Add(featureNames[i], typeof(double));
            }
            else
            {
                newSchema.Add(featureNames[i], typeof(string));
            }
        }

        return newSchema;
    }

    public static bool isContinuousFeature(string featureData)
    {
        try
        {
            double val = (double)System.Convert.ChangeType(featureData, typeof(double));
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}

public class DatasetSchema : Reference
{
    private List<string> featureNames;
    private Dictionary<string, int> featureNamesLookupTable;
    private HashSet<string> targetClassNames;
    private List<Type> featureTypes;
    private int indexColumn = -1;
    private int targetClassColumn = -1;

    public DatasetSchema()
    {
        featureNames = new List<string>();
        featureNamesLookupTable = new Dictionary<string, int>();
        targetClassNames = new HashSet<string>();
        featureTypes = new List<Type>();
    }

    public Type this[int index]
    {
        get { return featureTypes[index]; }
        set
        {
            featureTypes[index] = value;
        }
    }

    public Type this[string name]
    {
        get { return featureTypes[featureNamesLookupTable[name.Trim()]]; }
        set
        {
            featureTypes[featureNamesLookupTable[name.Trim()]] = value;
        }
    }

    public bool IsContinuousType(int featureIndex)
    {
        return IsContinuousType(featureTypes[featureIndex]);
    }

    public bool IsContinuousType(string name)
    {
        return IsContinuousType(featureTypes[featureNamesLookupTable[name.Trim()]]);
    }

    public static bool IsContinuousType(Type featureType)
    {
        if (featureType == typeof(double))
        {
            return true;
        }
        return false;
    }

    public int Count
    {
        get { return featureTypes.Count; }
    }

    public int IndexColumn
    {
        get { return indexColumn; }
        set
        {
            if (value < -1 || value > Count)
            {
                throw new IndexOutOfRangeException(value + " is out of range for schema columnn index.");
            }
            indexColumn = value;
        }
    }

    public bool HasIndexColumn()
    {
        return indexColumn >= 0 && indexColumn < featureTypes.Count;
    }

    public int TargetClassColumn
    {
        get { return targetClassColumn; }
        set
        {
            if (value < -1 || value > Count)
            {
                throw new IndexOutOfRangeException(value + " is out of range for schema columnn target class.");
            }
            targetClassColumn = value;
        }
    }

    public bool HasTargetClassColumn()
    {
        return targetClassColumn >= 0 && targetClassColumn < featureTypes.Count;
    }

    public int GetFeatureIndex(string featureName)
    {
        return featureNamesLookupTable[featureName.Trim()];
    }

    public string GetFeatureName(int index)
    {
        return featureNames[index];
    }

    public bool HasFeature(string name)
    {
        return featureNamesLookupTable.ContainsKey(name.Trim());
    }

    public void SetFeatureName(int index, string newName)
    {
        string oldName = featureNames[index];
        newName = newName.Trim();
        if (featureNames[index].Equals(newName))
        {
            return;
        }
        else if (featureNamesLookupTable.ContainsKey(newName))
        {
            throw new DuplicateFeatureIndexException("Error setting feature name: A feature with name '" + newName + "' already exists.");
        }
        featureNames[index] = newName;
        featureNamesLookupTable.Remove(oldName);
        featureNamesLookupTable[newName] = index;
    }

    public void Validate(Dataset dataset)
    {
        if (dataset.ColumnCount != featureTypes.Count)
        {
            throw new FeatureCountMismatchException("Schema expects " + featureTypes.Count + " features, " +
                "but dataset has " + dataset.ColumnCount + " columns.");
        }

        for (int i = 0; i < dataset.ColumnCount; i++)
        {
            if (!featureTypes[i].Equals(dataset.GetColumn(i).GetFeatureType()))
            {
                throw new FeatureTypeMismatchException("Schema expects feature type " + featureTypes[i] +
                    " but database column us type " + dataset.GetColumn(i).GetFeatureType());
            }
        }
    }

    public void Validate(ITreeClassifier tree)
    {
        if (tree == null)
        {
            throw new ArgumentNullException("Tree cannot be null");
        }

        ITreeNode rootNode = tree.GetTreeRoot();

        if (rootNode == null)
        {
            throw new ArgumentNullException("Tree root cannot be null");
        }

        validateTreeNodeRecursive(rootNode);
    }

    private void validateTreeNodeRecursive(ITreeNode node)
    {
        if (node == null)
        {
            return;
        }

        if (node.GetIsLeafClass() && !targetClassNames.Contains(node.GetLeafClass()))
        {
            throw new InvalidTargetClassException("Tree references unknown target class: " + node.GetLeafClass());
        }

        foreach (ITreeNode child in node.GetChildren())
        {
            validateTreeNodeRecursive(child);
        }
    }

    public void Add(string featureName, Type featureType)
    {
        featureName = featureName.Trim();
        featureTypes.Add(featureType);
        featureNames.Add(featureName);
        featureNamesLookupTable[featureName] = featureNames.Count - 1;
    }

    public void RebuildTargetClassNames(DatasetCollection collection)
    {
        targetClassNames.Clear();

        if (!HasTargetClassColumn())
        {
            return;
        }

        foreach (Dataset dataset in collection.GetAll())
        {
            IDatasetColumn col = dataset.GetColumn(targetClassColumn);
            for (int i = 0; i < col.GetCount(); i++)
            {
                targetClassNames.Add(col.Get(i).ToString().Trim());
            }
        }
    }

    public void AddTargetClassName(string className)
    {
        targetClassNames.Add(className);
    }

    public string[] GetAllTargetClassNames()
    {
        return targetClassNames.ToArray();
    }

    public bool HasTargetClassName(string className)
    {
        return targetClassNames.Contains(className.Trim());
    }
}

public class DatasetCollection : Reference
{
    private DatasetSchema schema;
    private Dictionary<string, Dataset> datasets;

    public DatasetCollection()
    {
        this.schema = new DatasetSchema();
        this.datasets = new Dictionary<string, Dataset>();
    }

    public DatasetCollection(DatasetSchema schema)
    {
        this.schema = schema;
        this.datasets = new Dictionary<string, Dataset>();
    }

    public int Count
    {
        get
        {
            return datasets.Count;
        }
    }

    public bool IsSchemaCreated
    {
        get
        {
            return schema.Count > 0;
        }
    }

    public bool HasDataset(string uid)
    {
        return datasets.ContainsKey(uid.Trim());
    }

    public Dataset this[string uid]
    {
        get { return datasets[uid.Trim()]; }
    }

    public List<Dataset> GetAll()
    {
        return new List<Dataset>(datasets.Values);
    }

    public List<String> GetAllDatasetNames()
    {
        return GetAll().Select(dataset => dataset.Name).ToList();
    }

    public DatasetSchema Schema
    {
        get { return schema; }
    }

    public void Add(Dataset newDataset)
    {
        if (newDataset == null)
        {
            return;
        }

        schema.Validate(newDataset);

        string datasetNameTrimmed = newDataset.Name.Trim();
        if (datasetNameTrimmed.Empty())
        {
            datasetNameTrimmed = "Database";
        }

        string uniqueDatasetName = datasetNameTrimmed;
        int index = 1;
        while (datasets.ContainsKey(uniqueDatasetName))
        {
            uniqueDatasetName = datasetNameTrimmed + " " + index.ToString();
            index++;
        }
        newDataset._setName(uniqueDatasetName);
        datasets[uniqueDatasetName] = newDataset;

        schema.RebuildTargetClassNames(this);
    }

    public void Remove(string name)
    {
        datasets.Remove(name);
        schema.RebuildTargetClassNames(this);
    }

    public void Rename(string oldName, string newName)
    {
        oldName = oldName.Trim();
        newName = newName.Trim();

        if (newName.Empty() || oldName.Equals(newName) || !datasets.ContainsKey(oldName))
        {
            return;
        }

        if (datasets.ContainsKey(newName))
        {
            throw new DuplicateDatasetNameException("A dataset with the name '" + newName + "' already existrs.");
        }

        Dataset renamedDataset = datasets[oldName];
        datasets.Remove(oldName);
        datasets[newName] = renamedDataset;
        renamedDataset._setName(newName);
    }

    public void ConvertColumnType<T>(int colIndex)
    {
        Dictionary<string, IDatasetColumn> convertedColumns = new Dictionary<string, IDatasetColumn>();

        foreach (var datasetDictPair in datasets)
        {
            Dataset d = datasetDictPair.Value;
            IDatasetColumn col = d.GetColumn(colIndex);
            convertedColumns[datasetDictPair.Key] = col.ConvertTo<T>(datasetDictPair.Value);
        }

        // If no exceptions occur by this point, we should be good to switch over
        foreach (var datasetDictPair in datasets)
        {
            Dataset d = datasetDictPair.Value;
            d._setColumn(colIndex, convertedColumns[datasetDictPair.Key]);
        }

        schema[colIndex] = typeof(T);
        if (schema.HasTargetClassColumn() && schema.TargetClassColumn == colIndex)
        {
            schema.RebuildTargetClassNames(this);
        }
    }
}
