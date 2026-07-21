using Godot;
using System.Collections.Generic;

public partial class BattleLogPanel : Control
{
    private RichTextLabel _logLabel;
    private List<string> _logs = new List<string>();
    private const int MaxLogLines = 50;

    public override void _Ready()
    {
        _logLabel = GetNode<RichTextLabel>("LogLabel");
        Clear();
    }

    public void AddLog(string message)
    {
        string timestamp = $"{Time.GetTicksMsec() % 10000:0000} ";
        string logLine = timestamp + message;

        _logs.Add(logLine);

        if (_logs.Count > MaxLogLines)
        {
            _logs.RemoveAt(0);
        }

        UpdateDisplay();
    }

    public void AddLog(string format, params object[] args)
    {
        AddLog(string.Format(format, args));
    }

    public void Clear()
    {
        _logs.Clear();
        _logLabel.Text = "";
    }

    private void UpdateDisplay()
    {
        _logLabel.Text = string.Join("\n", _logs);
    }
}
