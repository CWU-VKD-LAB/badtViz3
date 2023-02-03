using System;
using System.Collections.Generic;
using Godot;

public struct ConfusionMatrix
{
    public Dictionary<string, Dictionary<string, uint>> Matrix;
    public string[] TargetClasses;
    public uint ClassifiedSamples;
    public uint SkippedSamples;

    public override string ToString()
    {
        string result = "====== Confusion Matrix ======";
        result += "\n\nClassified: " + ClassifiedSamples;
        result += "\nSkipped: " + SkippedSamples + "\n\n";

        List<string> tableRows = new List<string>();

        // Minimum 6 characters for col width
        int maxClassNameLen = 6;

        foreach (string targetClass in TargetClasses)
        {
            if (targetClass.Length > maxClassNameLen)
            {
                maxClassNameLen = targetClass.Length;
            }
        }

        result += padString("", maxClassNameLen + 3);

        foreach (string targetClass in TargetClasses)
        {
            result += padString(targetClass, maxClassNameLen, true) + "   ";
        }
        result += "\n";
        int maxRowLen = 0;

        foreach (string targetKey in TargetClasses)
        {
            if (!Matrix.ContainsKey(targetKey))
            {
                Matrix[targetKey] = new Dictionary<string, uint>();
            }

            Dictionary<string, uint> targetDict = Matrix[targetKey];
            string row = padString(targetKey, maxClassNameLen) + " | ";

            foreach (string predKey in TargetClasses)
            {
                if (!targetDict.ContainsKey(predKey))
                {
                    targetDict[predKey] = 0;
                }
                row += padString(targetDict[predKey].ToString(), maxClassNameLen, true) + " | ";
            }
            tableRows.Add(row);
            if (row.Length > maxRowLen)
            {
                maxRowLen = row.Length;
            }
        }

        for (int i = 0; i < maxRowLen; i++)
        {
            result += "-";
        }
        result += "\n";

        foreach (string row in tableRows)
        {
            result += row + "\n";
        }

        for (int i = 0; i < maxRowLen; i++)
        {
            result += "-";
        }
        result += "\n";

        return result;
    }

    private string padString(string inputStr, int targetLen, bool leftSide = false)
    {
        while (inputStr.Length < targetLen)
        {
            if (leftSide)
            {
                inputStr = " " + inputStr;
            }
            else
            {
                inputStr += " ";
            }
        }
        return inputStr;
    }
}

public struct PredictionMetrics
{
    public double Accuracy;
    public double Precision;
    public double Recall;
    public double F1Score;

    public override string ToString()
    {
        string result = "====== Classification Metrics ======";
        result += "\n\nAccuracy: " + Math.Round(Accuracy * 100.0, 4);

        if (Precision >= 0)
        {
            result += "\nPrecision: " + Math.Round(Precision, 6);
        }

        if (Recall >= 0)
        {
            result += "\nRecall: " + Math.Round(Recall, 6);
        }

        if (F1Score >= 0)
        {
            result += "\nF1 Score: " + Math.Round(F1Score, 6);
        }

        return result;
    }
}

public class StatsHbox : HBoxContainer
{
    private static PackedScene statsTextBoxScene = null;

    public override void _Ready()
    {
        if (statsTextBoxScene == null)
        {
            statsTextBoxScene = GD.Load<PackedScene>("res://scene/ui/StatsTextEdit.tscn");
        }
    }

    public void UpdateStats(Godot.Collections.Array<PredictedResult> predictedResults)
    {
        foreach (Node child in GetChildren())
        {
            RemoveChild(child);
            child.QueueFree();
        }

        foreach (PredictedResult result in predictedResults)
        {
            generateStats(result);
        }
    }

    private void generateStats(PredictedResult result)
    {
        Dataset resultDataset = result.SourceDataset;
        List<Tuple<Sample, string>> predictions = result.SamplePredictions;
        TextEdit statsTextEdit = statsTextBoxScene.Instance<TextEdit>();
        ConfusionMatrix confusionMatrix = generateConfusionMatrix(result);
        PredictionMetrics metrics = generateMetrics(confusionMatrix);

        string statsOutput = "Dataset: " + resultDataset.Name + "\n\n";
        statsOutput += metrics.ToString() + "\n\n";
        statsOutput += confusionMatrix.ToString();
        statsTextEdit.Text = statsOutput;
        AddChild(statsTextEdit);
    }

    private ConfusionMatrix generateConfusionMatrix(PredictedResult result)
    {
        List<Tuple<Sample, string>> predictions = result.SamplePredictions;
        ConfusionMatrix confusionMatrix = new ConfusionMatrix();
        confusionMatrix.TargetClasses = result.SourceDataset.Schema.GetAllTargetClassNames();
        Array.Sort(confusionMatrix.TargetClasses);
        confusionMatrix.Matrix = new Dictionary<string, Dictionary<string, uint>>();

        // Initialize matrix to zeros
        foreach (string targetClass in confusionMatrix.TargetClasses)
        {
            confusionMatrix.Matrix[targetClass] = new Dictionary<string, uint>();
            foreach (string targetClass2 in confusionMatrix.TargetClasses)
            {
                confusionMatrix.Matrix[targetClass][targetClass2] = 0;
            }
        }

        confusionMatrix.ClassifiedSamples = 0;
        confusionMatrix.SkippedSamples = 0;

        foreach (Tuple<Sample, string> pred in predictions)
        {
            Sample s = pred.Item1;
            string predClass = pred.Item2;
            string targetClass = s.TargetClass;
            if (predClass == null || targetClass == null)
            {
                confusionMatrix.SkippedSamples += 1;
                continue;
            }
            if (!confusionMatrix.Matrix.ContainsKey(predClass))
            {
                confusionMatrix.Matrix[targetClass] = new Dictionary<string, uint>();
            }

            Dictionary<string, uint> targetDict = confusionMatrix.Matrix[targetClass];
            if (!targetDict.ContainsKey(targetClass))
            {
                targetDict[predClass] = 1;
            }
            else
            {
                targetDict[predClass] += 1;
            }
            confusionMatrix.ClassifiedSamples += 1;
        }

        return confusionMatrix;
    }

    private PredictionMetrics generateMetrics(ConfusionMatrix confusionMatrix)
    {
        PredictionMetrics metrics = new PredictionMetrics();
        metrics.Accuracy = (double)sumCorrectlyClassified(confusionMatrix) / confusionMatrix.ClassifiedSamples;

        if (confusionMatrix.TargetClasses.Length == 2)
        {
            // Precision = (TP)/(TP+FP)
            metrics.Precision = getTP(confusionMatrix) / (getTP(confusionMatrix) + getFP(confusionMatrix));

            // Recall = (TP)/(TP+FN)
            metrics.Recall = getTP(confusionMatrix) / (getTP(confusionMatrix) + getFN(confusionMatrix));

            metrics.F1Score = 2.0 * ((metrics.Precision * metrics.Recall) / (metrics.Precision + metrics.Recall));
        }

        return metrics;
    }

    private uint sumCorrectlyClassified(ConfusionMatrix confusionMatrix)
    {
        uint trueCount = 0;

        foreach (string targetClass in confusionMatrix.TargetClasses)
        {
            trueCount += confusionMatrix.Matrix[targetClass][targetClass];
        }

        return trueCount;
    }

    private double getTP(ConfusionMatrix confusionMatrix)
    {
        string class1 = confusionMatrix.TargetClasses[0];
        return confusionMatrix.Matrix[class1][class1];
    }

    private double getTN(ConfusionMatrix confusionMatrix)
    {
        string class2 = confusionMatrix.TargetClasses[1];
        return confusionMatrix.Matrix[class2][class2];
    }

    private double getFP(ConfusionMatrix confusionMatrix)
    {
        string class1 = confusionMatrix.TargetClasses[0];
        string class2 = confusionMatrix.TargetClasses[1];
        return confusionMatrix.Matrix[class1][class2];
    }
    private double getFN(ConfusionMatrix confusionMatrix)
    {
        string class1 = confusionMatrix.TargetClasses[0];
        string class2 = confusionMatrix.TargetClasses[1];
        return confusionMatrix.Matrix[class2][class1];
    }
}
