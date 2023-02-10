using System;
using System.Collections.Generic;
using Godot;

public class InvalidColumnTypeException : Exception
{
    public InvalidColumnTypeException(string msg) : base(msg)
    {
    }
}

public class InvalidDiscreteTypeCodeException : Exception
{
    public InvalidDiscreteTypeCodeException(string msg) : base(msg)
    {
    }
}

public interface IDatasetColumn
{
    object Get(int index);
    void Set(int index, object value);
    void Add(object value);
    bool Has(object value);
    object GetMinValue();
    object GetMaxValue();
    int FindFirst(object value);
    int GetCount();
    void Clear();
    Type GetFeatureType();
    bool IsContinuous();
    bool CanConvertValue(object value);
    DatasetColumn<E> ConvertTo<E>(Dataset parentDataset);
}

public class DatasetColumnFactory : Reference
{
    public static List<IDatasetColumn> CreateFromSchema(DatasetSchema schema, Dataset parentDataset)
    {
        List<IDatasetColumn> cols = new List<IDatasetColumn>();

        for (int i = 0; i < schema.Count; i++)
        {
            if (schema[i] == typeof(double))
            {
                cols.Add(new DatasetColumn<double>(parentDataset));
            }
            else if (schema[i] == typeof(string))
            {
                cols.Add(new DatasetColumn<string>(parentDataset));
            }
            else
            {
                throw new InvalidColumnTypeException("Unable to create dataset column for schema type " + schema[i]);
            }
        }

        return cols;
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

public class DatasetColumn<T> : Reference, IDatasetColumn
{
    public static readonly Type[] ValidTypes = new Type[] {
        typeof(double),
        typeof(string),
    };
    protected Dataset parentDataset = null;
    protected List<T> values;

    protected T minValue;
    protected T maxValue;

    public DatasetColumn()
    {
        validateType();
        values = new List<T>();
    }

    public DatasetColumn(Dataset parentDataset)
    {
        validateType();
        values = new List<T>();
        this.parentDataset = parentDataset;
    }

    public DatasetColumn(Dataset parentDataset, int capacity)
    {
        validateType();
        values = new List<T>(capacity);
        this.parentDataset = parentDataset;
    }

    public int Count
    {
        get
        {
            return values.Count;
        }
    }

    public T this[int index]
    {
        get
        {
            return values[index];
        }
        set
        {
            values[index] = value;
            updateMinAndMaxValues(value);
        }
    }

    public void AddValue(T value)
    {
        values.Add(value);
        updateMinAndMaxValues(value);
    }

    public bool HasValue(T value)
    {
        return values.Contains(value);
    }

    public int FindFirstValue(T value)
    {
        return values.FindIndex(v => v.Equals(value));
    }

    public T RemoveAt(int index)
    {
        T value = values[index];
        values.RemoveAt(index);
        return value;
    }

    public void Clear()
    {
        values.Clear();
    }

    public DatasetColumn<E> ConvertTo<E>(Dataset parentDataset)
    {
        DatasetColumn<E> newCol = new DatasetColumn<E>(parentDataset, Count);

        foreach (T value in values)
        {
            E convertedValue = (E)System.Convert.ChangeType(value, typeof(E));
            newCol.AddValue(convertedValue);
        }

        return newCol;
    }

    public bool CanConvertValue(object value)
    {
        try
        {
            T convertedValue = (T)System.Convert.ChangeType(value, typeof(T));
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public object Get(int index)
    {
        return this[index];
    }

    public void Set(int index, object value)
    {
        if (value.GetType().Equals(typeof(T)))
        {
            this[index] = (T)value;
            updateMinAndMaxValues((T)value);
        }
        else
        {
            T convertedValue = (T)System.Convert.ChangeType(value, typeof(T));
            this[index] = convertedValue;
            updateMinAndMaxValues(convertedValue);
        }
    }

    public void Add(object value)
    {
        if (value.GetType().Equals(typeof(T)))
        {
            AddValue((T)value);
        }
        else
        {
            T convertedValue = (T)System.Convert.ChangeType(value, typeof(T));
            AddValue(convertedValue);
            updateMinAndMaxValues(convertedValue);
        }
    }

    public bool Has(object value)
    {
        if (value.GetType().Equals(typeof(T)))
        {
            return HasValue((T)value);
        }
        else
        {
            return HasValue((T)System.Convert.ChangeType(value, typeof(T)));
        }
    }

    public object GetMinValue()
    {
        return minValue;
    }
    public object GetMaxValue()
    {
        return maxValue;
    }

    public int FindFirst(object value)
    {
        if (value.GetType().Equals(typeof(T)))
        {
            return FindFirstValue((T)value);
        }
        else
        {
            return FindFirstValue((T)System.Convert.ChangeType(value, typeof(T)));
        }
    }

    public Type GetFeatureType()
    {
        return typeof(T);
    }

    public int GetCount()
    {
        return Count;
    }

    public bool IsContinuous()
    {
        Type type = typeof(T);

        if (type == typeof(double))
        {
            return true;
        }

        return false;
    }

    private void validateType()
    {
        foreach (Type type in ValidTypes)
        {
            if (typeof(T) == type)
            {
                return;
            }
        }

        throw new InvalidColumnTypeException(typeof(T).ToString() + " is not a supported dataset column type.");
    }

    private void updateMinAndMaxValues(T val)
    {
        if (IsContinuous())
        {
            updateMinValue(val);
            updateMaxValue(val);
        }
    }

    private void updateMinValue(T val)
    {
        IComparable oldVal = (IComparable)minValue;
        IComparable newVal = (IComparable)val;

        if (values.Count <= 1 || oldVal == null || oldVal.CompareTo(newVal) > 0)
        {
            minValue = val;
        }
    }

    private void updateMaxValue(T val)
    {
        IComparable oldVal = (IComparable)maxValue;
        IComparable newVal = (IComparable)val;

        if (values.Count <= 1 || oldVal == null || oldVal.CompareTo(newVal) < 0)
        {
            maxValue = val;
        }
    }
}
