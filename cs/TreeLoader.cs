using System;
using Godot;

public abstract class TreeLoader : Reference
{
    public abstract ITreeClassifier LoadTreeFromFile(string path);
}
