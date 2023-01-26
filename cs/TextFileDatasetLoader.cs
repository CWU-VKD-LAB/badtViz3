using System;
using System.Collections.Generic;
using Godot;

public class TextFileDatasetLoaderOptions : DatasetLoaderOptions
{
    public static char[] SupportedDelimiters = new char[] { ',', ';', '\t' };
    public char Delimiter = ',';
    public bool HasHeader = true;

    public TextFileDatasetLoaderOptions(String DatasetName, char Delimiter, bool HasHeader, NullFeaturePolicy NullFeaturePolicy, bool IgnoreDuplicates, int indexColumn)
        : base(DatasetName, NullFeaturePolicy, IgnoreDuplicates, indexColumn)
    {
        this.Delimiter = Delimiter;
        this.HasHeader = HasHeader;
    }
}

public class InvalidDatasetCollectionException : Exception
{
    public InvalidDatasetCollectionException(string message) : base(message)
    {
    }
}

public class InvalidLoaderOptionsTypeException : Exception
{
    public InvalidLoaderOptionsTypeException(string message) : base(message)
    {
    }
}

public class InvalidFileFormatException : Exception
{
    public InvalidFileFormatException(string message) : base(message)
    {
    }
}

public class InvalidIndexColumnException : Exception
{
    public InvalidIndexColumnException(string message) : base(message)
    {
    }
}

public class UnknownFileDelimiterException : Exception
{
    public UnknownFileDelimiterException(string message) : base(message)
    {
    }
}

public class TextFileDatasetLoader : DatasetLoader
{
    public static readonly string NULL_STRING = "NULL";

    public override Tuple<DatasetCollection, Dataset> LoadFromFile(string path)
    {
        return this.LoadFromFile(path, null, null);
    }

    public override Tuple<DatasetCollection, Dataset> LoadFromFile(string path, DatasetLoaderOptions options)
    {
        return this.LoadFromFile(path, null, options);
    }

    public override Tuple<DatasetCollection, Dataset> LoadFromFile(string path, DatasetCollection parentCollection, DatasetLoaderOptions options)
    {
        TextFileDatasetLoaderOptions textfileOptions = null;

        if (options == null)
        {
            textfileOptions = new TextFileDatasetLoaderOptions("Dataset", ',', true, NullFeaturePolicy.Skip, true, -1);
        }
        else if (!typeof(TextFileDatasetLoaderOptions).Equals(options.GetType()))
        {
            throw new InvalidLoaderOptionsTypeException("Expected " + typeof(TextFileDatasetLoaderOptions).ToString() +
                " but recieved " + options.GetType().ToString());
        }
        else
        {
            textfileOptions = (TextFileDatasetLoaderOptions)options;
        }

        string[] fileLines = readFileLines(path);

        if (fileLines == null || fileLines.Length == 0 ||
            (parentCollection == null && textfileOptions.HasHeader && fileLines.Length <= 1))
        {
            throw new InvalidFileFormatException("Error loading text dataset file: File does not exist or contains no data.");
        }

        if (textfileOptions.Delimiter == '\0')
        {
            textfileOptions.Delimiter = tryGuessDelimiter(fileLines[0]);
        }

        if (parentCollection == null)
        {
            List<String> headerNames = parseHeaderNames(fileLines, textfileOptions);
            parentCollection = generateCollection(headerNames, splitLine(fileLines[textfileOptions.HasHeader ? 1 : 0], textfileOptions.Delimiter));

            if (textfileOptions.indexColumn > 0)
            {
                if (textfileOptions.indexColumn >= parentCollection.Schema.Count)
                {
                    throw new InvalidIndexColumnException("The specified index column (" + textfileOptions.indexColumn + ") is out of bounds.");
                }

                parentCollection.Schema.IndexColumn = textfileOptions.indexColumn;
            }
        }

        Dataset newDataset = parseFileLines(parentCollection, fileLines, textfileOptions);
        parentCollection.Add(newDataset);
        return new Tuple<DatasetCollection, Dataset>(parentCollection, newDataset);
    }

    public override void SaveToFile(Dataset dataset, string path, DatasetLoaderOptions options = null)
    {

    }

    private string[] readFileLines(string path)
    {
        return System.IO.File.ReadAllLines(path);
    }

    private List<string> parseHeaderNames(string[] fileLines, TextFileDatasetLoaderOptions textfileOptions)
    {
        List<String> headerNames = new List<string>();
        HashSet<String> headerNamesSet = new HashSet<string>();

        if (textfileOptions.HasHeader)
        {
            List<string> headerSplit = splitLine(fileLines[0], textfileOptions.Delimiter);
            foreach (string col in headerSplit)
            {
                string colName = col.Trim();
                if (colName.Empty())
                {
                    const string newColNameBase = "Column ";
                    int counter = 1;
                    colName = newColNameBase + counter.ToString();
                    while (headerNamesSet.Contains(colName))
                    {
                        colName = newColNameBase + counter.ToString();
                        counter++;
                    }
                }
                headerNames.Add(colName);
                headerNamesSet.Add(headerNames[headerNames.Count - 1]);
            }
        }
        else
        {
            int firstRowNumCol = splitLine(fileLines[0], textfileOptions.Delimiter).Count;
            for (int i = 0; i < firstRowNumCol; i++)
            {
                headerNames.Add("Column " + (i + 1).ToString());
                headerNamesSet.Add(headerNames[headerNames.Count - 1]);
            }
        }

        if (headerNames.Count != headerNamesSet.Count)
        {
            throw new FeatureCountMismatchException("Error reading dataset header: Header contains one or more duplicate column names. Unique names for each column is required.");
        }

        return headerNames;
    }

    private Dataset parseFileLines(DatasetCollection parentCollection, string[] fileData, TextFileDatasetLoaderOptions options)
    {
        int firstSampleIndex = options.HasHeader ? 1 : 0;

        Dataset newDataset = new Dataset(options.DatasetName, parentCollection);

        for (int i = firstSampleIndex; i < fileData.Length; i++)
        {
            List<string> lineSplit = splitLine(fileData[i], options.Delimiter);

            if (!applyNullPolicy(lineSplit, options.NullFeaturePolicy))
            {
                continue;
            }

            newDataset.Add(lineSplit);

        }

        return newDataset;
    }

    private bool applyNullPolicy(List<string> features, NullFeaturePolicy nullPolicy)
    {
        for (int i = 0; i < features.Count; i++)
        {
            string current = features[i];
            if (current == null || current.Empty())
            {
                if (nullPolicy == NullFeaturePolicy.InsertZeros)
                {
                    features[i] = "0";
                }
                else
                {
                    return false;
                }
            }
        }

        return true;
    }

    private List<string> splitLine(string line, char delimiter)
    {
        List<string> retList = new List<string>();

        int startColIndex = 0;
        int endColIndex = 0;

        while (startColIndex < line.Length && endColIndex < line.Length)
        {
            bool doubleQuoteWrapped = false;

            while (startColIndex < line.Length && line[startColIndex] == ' ')
            {
                startColIndex++;
            }

            if (startColIndex >= line.Length)
            {
                break;
            }
            else if (line[startColIndex] == '"')
            {
                doubleQuoteWrapped = true;
                startColIndex++;
            }

            if (startColIndex >= line.Length)
            {
                break;
            }

            endColIndex = startColIndex;
            bool endQuoteFound = false;

            while (endColIndex < line.Length)
            {
                char currentChar = line[endColIndex];
                if (doubleQuoteWrapped && currentChar == '"')
                {
                    if (endColIndex == line.Length - 1)
                    {
                        // Last char of line
                        break;
                    }
                    else if (line[endColIndex + 1] == '"')
                    {
                        // Two seq. double quotes means skip
                        endColIndex++;
                    }
                    else
                    {
                        endQuoteFound = true;
                    }
                }
                else if (currentChar == delimiter && (!doubleQuoteWrapped || endQuoteFound))
                {
                    break;
                }
                endColIndex++;
            }

            string substring = line.Substr(startColIndex, endColIndex - startColIndex);
            while (endQuoteFound && substring.Length > 0)
            {
                bool found = substring[substring.Length - 1] == '"';
                substring = substring.Substr(0, substring.Length - 1);
                if (found) { break; }
            }

            retList.Add(substring.Replace("\"\"", "\"").Trim());
            startColIndex = endColIndex + 1;
        }

        return retList;
    }

    private DatasetCollection generateCollection(List<string> headerNames, List<string> featureData)
    {
        DatasetSchema newSchema = DatasetSchemaFactory.CreateFromFeatureData(headerNames, featureData);
        return new DatasetCollection(newSchema);
    }

    private char tryGuessDelimiter(string exampleLine)
    {
        List<string> strSplit;
        int delimIndex = 0;

        do
        {
            char delim = TextFileDatasetLoaderOptions.SupportedDelimiters[delimIndex];
            strSplit = splitLine(exampleLine, delim);
            if (strSplit.Count > 1)
            {
                return delim;
            }
            delimIndex++;
        } while (delimIndex < TextFileDatasetLoaderOptions.SupportedDelimiters.Length);

        throw new UnknownFileDelimiterException("Failed to automatically determine text file column delimeter");
    }
}
