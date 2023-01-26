using System;
using System.Collections.Generic;
using Godot;

public enum NullFeaturePolicy
{
    Skip,
    InsertZeros
}

public abstract class DatasetLoaderOptions : Reference
{
    public string DatasetName = "Dataset";
    public NullFeaturePolicy NullFeaturePolicy = NullFeaturePolicy.Skip;
    public bool IgnoreDuplicates = true;
    public int indexColumn = -1;

    public DatasetLoaderOptions(string DatasetName, NullFeaturePolicy NullFeaturePolicy, bool IgnoreDuplicates, int indexColumn)
    {
        this.DatasetName = DatasetName;
        this.NullFeaturePolicy = NullFeaturePolicy;
        this.IgnoreDuplicates = IgnoreDuplicates;
        this.indexColumn = indexColumn;
    }
}

public abstract class DatasetLoader : Reference
{
    public abstract Tuple<DatasetCollection, Dataset> LoadFromFile(string path);
    public abstract Tuple<DatasetCollection, Dataset> LoadFromFile(string path, DatasetLoaderOptions options = null);
    public abstract Tuple<DatasetCollection, Dataset> LoadFromFile(string path, DatasetCollection parentCollection = null, DatasetLoaderOptions options = null);
    public abstract void SaveToFile(Dataset dataset, string path, DatasetLoaderOptions options = null);
}
