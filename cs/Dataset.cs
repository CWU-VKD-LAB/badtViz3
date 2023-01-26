using System;
using System.Collections.Generic;
using Godot;

public class Dataset : Reference
{
    public static readonly float AUTO_HUE_SCALE_FEW_CAT = 0.654f;
    public static readonly float AUTO_HUE_SCALE_MANY_CAT = 0.835f;
    private DatasetCollection parentCollection = null;
    private string name = "Dataset";
    private List<IDatasetColumn> datasetColumns;
    private List<Sample> samples;
    private Dictionary<string, Sample> samplesLookupTable;
    private bool display = true;

    public Dataset()
    {
        datasetColumns = new List<IDatasetColumn>();
        samples = new List<Sample>();
        samplesLookupTable = new Dictionary<string, Sample>();
    }

    public Dataset(string name, DatasetCollection parentCollection)
    {
        this.name = name;
        this.parentCollection = parentCollection;
        datasetColumns = DatasetColumnFactory.CreateFromSchema(parentCollection.Schema, this);
        samples = new List<Sample>();
        samplesLookupTable = new Dictionary<string, Sample>();
    }

    public string Name
    {
        get { return this.name; }
        set { parentCollection.Rename(name, value); }
    }

    public bool Display
    {
        get { return this.display; }
        set { display = value; }
    }

    public Sample this[string sampleName]
    {
        get { return samplesLookupTable[sampleName]; }
    }

    public Sample this[int sampleIndex]
    {
        get { return samples[sampleIndex]; }
    }

    public IDatasetColumn GetColumn(int colIndex)
    {
        return datasetColumns[colIndex];
    }

    public IDatasetColumn GetColumn(string colName)
    {
        return datasetColumns[parentCollection.Schema.GetFeatureIndex(colName)];
    }

    public List<IDatasetColumn> GetAllColumns()
    {
        return new List<IDatasetColumn>(datasetColumns);
    }

    public int Count
    {
        get { return samples.Count; }
    }

    public int ColumnCount
    {
        get { return datasetColumns.Count; }
    }

    public List<Sample> GetAll()
    {
        return new List<Sample>(samples);
    }

    public int LookupFeatureIndex(string featureName)
    {
        return ParentCollection.Schema.GetFeatureIndex(featureName);
    }

    public DatasetCollection ParentCollection
    {
        get { return parentCollection; }
    }

    public DatasetSchema Schema
    {
        get { return parentCollection.Schema; }
    }

    public Sample Add(List<string> features)
    {
        if (features.Count != datasetColumns.Count)
        {
            throw new FeatureCountMismatchException("Error adding new sample: " +
                "Sample has " + features.Count + " features but dataset expects " + datasetColumns.Count);
        }

        for (int i = 0; i < datasetColumns.Count; i++)
        {
            if (!datasetColumns[i].CanConvertValue(features[i]))
            {
                throw new FeatureTypeMismatchException("Error adding new sample: " +
                    "Feature " + (i + 1) + " is type " + features[i].GetType() + " but column expects " + datasetColumns[i].GetType());
            }
        }

        int indexCol = parentCollection.Schema.IndexColumn;
        if (parentCollection.Schema.HasIndexColumn() &&
            datasetColumns[indexCol].Has(features[indexCol]))
        {
            throw new DuplicateFeatureIndexException("Error adding new sample: " +
                "Sample index " + features[indexCol] + " already exists in dataset.");
        }

        for (int i = 0; i < datasetColumns.Count; i++)
        {
            datasetColumns[i].Add(features[i]);
        }

        Sample newSample = new Sample(this, datasetColumns[0].GetCount() - 1);
        samples.Add(newSample);

        if (parentCollection.Schema.HasIndexColumn())
        {
            samplesLookupTable[features[indexCol].ToString()] = newSample;
        }

        if (parentCollection.Schema.HasTargetClassColumn())
        {
            Schema.AddTargetClassName(newSample.TargetClass);
        }

        return newSample;
    }

    public bool Contains(Sample sample)
    {
        foreach (Sample testSample in samples)
        {
            if (testSample.Equals(sample))
            {
                return true;
            }
        }

        return false;
    }

    public void AutoColorSamples()
    {
        string[] targetClassNames = Schema.GetAllTargetClassNames();
        Array.Sort(targetClassNames);

        float startHue = 0.0f;
        float endHue = 1.0f;

        if (targetClassNames.Length <= 3)
        {
            startHue *= AUTO_HUE_SCALE_FEW_CAT;
            endHue *= AUTO_HUE_SCALE_FEW_CAT;
        }
        else
        {
            startHue *= AUTO_HUE_SCALE_MANY_CAT;
            endHue *= AUTO_HUE_SCALE_MANY_CAT;
        }

        Dictionary<String, float> baseHues = new Dictionary<string, float>();

        for (int i = 0; i < targetClassNames.Length; i++)
        {
            float hueFactor = 0.0f;
            if (targetClassNames.Length > 1)
            {
                hueFactor = (float)i / (float)(targetClassNames.Length - 1);
            }

            baseHues[targetClassNames[i]] = (hueFactor * (endHue - startHue)) + startHue;
        }

        for (int i = 0; i < Count; i++)
        {
            Sample s = this[i];
            float baseHue = baseHues[s.TargetClass];
            float sat = 0.7f + (GD.Randf() * 0.3f);
            Color sampleColor = Color.FromHsv(baseHue, sat, sat, 0.75f);
            s.Color = sampleColor;
        }
    }

    // Warning! This should only be called from DatasetCollection
    public void _setName(string name)
    {
        this.name = name;
    }

    // Warning! This should only be called from DatasetCollection
    public void _setColumn(int index, IDatasetColumn col)
    {
        datasetColumns[index] = col;
    }
}
