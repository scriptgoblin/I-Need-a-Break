// ────────────────────────────────────────────────────────────────────────────
//  I Need A Break — Script Goblin
//  Free Unity editor extension. Rename this file to anything you like.
//
//
//  Hidden under Help/ — because nobody ever checks the Help menu.
//  Tip: add this file to your .gitignore — your teammates don't need to know.
//
//  Psst: feeling better already? Press Cmd+Alt+B (Mac) / Ctrl+Alt+B (Win)
//  to sneak back to work early. Don't tell your brain.
//
//
//  Like this? Script Goblin makes more Unity tools — check them out:
//  https://assetstore.unity.com/publishers/141132
//  https://github.com/scriptgoblin
//  nregoblin@gmail.com
// ────────────────────────────────────────────────────────────────────────────

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class BreakOverlay : EditorWindow
{
    private static BreakOverlay _instance;

    public static new void Show()
    {
        if (_instance != null)
        {
            return;
        }

        _instance = CreateInstance<BreakOverlay>();
        _instance.ShowPopup();
        _instance.position = EditorGUIUtility.GetMainWindowPosition();
    }

    public static void Hide()
    {
        if (_instance == null)
        {
            return;
        }

        _instance.Close();
        _instance = null;
    }

    private void OnGUI()
    {
        var fullRect = new Rect(0, 0, position.width, position.height);

        EditorGUI.DrawRect(fullRect, new Color(0f, 0f, 0f, 0.04f));
        EditorGUIUtility.AddCursorRect(fullRect, MouseCursor.NotAllowed);

        if (Event.current.type != EventType.Repaint && Event.current.type != EventType.Layout)
        {
            Event.current.Use();
        }
    }
}

[InitializeOnLoad]
public static class BreakRunner
{
    private static bool _running;
    private static bool _lockEditor;
    private static double _startTime;
    private static double _totalSeconds;
    private static string _progressTitle;
    private static int _fileCount;
    private static List<string> _files = new();

    static BreakRunner() => EditorApplication.update += Tick;

    public static void Begin(List<string> files, double totalSeconds, bool lockEditor, string progressTitle)
    {
        _files = files;
        _fileCount = Mathf.Max(files.Count, 1);
        _totalSeconds = totalSeconds;
        _lockEditor = lockEditor;
        _progressTitle = string.IsNullOrWhiteSpace(progressTitle) ? "Processing files…" : progressTitle;
        _startTime = EditorApplication.timeSinceStartup;
        _running = true;

        if (_lockEditor)
        {
            BreakOverlay.Show();
        }
    }

    public static void Cancel() => Finish(cancelled: true);

    public static bool IsRunning => _running;

    private static void Tick()
    {
        if (!_running)
        {
            return;
        }

        var elapsed = EditorApplication.timeSinceStartup - _startTime;
        var progress = (float)Math.Min(elapsed / _totalSeconds, 1.0);
        var fileIdx = Mathf.Min((int)(progress * _fileCount), _fileCount - 1);
        var fileName = Path.GetFileName(_files[fileIdx]);

        EditorUtility.DisplayProgressBar(_progressTitle, fileName, progress);

        if (elapsed >= _totalSeconds)
        {
            Finish(cancelled: false);
        }
    }

    private static void Finish(bool cancelled)
    {
        _running = false;
        EditorUtility.ClearProgressBar();
        BreakOverlay.Hide();

        if (!cancelled)
        {
            EditorUtility.DisplayDialog(
                "All done!",
                "Processing complete. Everything looks great.\n\nWelcome back.",
                "Let's go");
        }
    }
}

public class INeedABreak : EditorWindow
{
    private string _folder = Application.dataPath;
    private int _hours = 0;
    private int _minutes = 30;
    private int _seconds = 0;
    private bool _lockEditor = true;
    private string _progressTitle = "";
    private string _error = "";

    private const string AssetStoreUrl = "https://assetstore.unity.com/publishers/141132";

    private GUIStyle _styleTitle;
    private GUIStyle _styleSubtitle;
    private GUIStyle _styleMoreTools;
    private GUIStyle _styleError;

    [MenuItem("Help/I Need A Break/Stop Break &%b")]
    private static void StopBreak() => BreakRunner.Cancel();

    [MenuItem("Help/I Need A Break/Stop Break &%b", true)]
    private static bool StopBreakValidate() => BreakRunner.IsRunning;

    [MenuItem("Help/I Need A Break/Take A Break")]
    private static void Open()
    {
        if (BreakRunner.IsRunning)
        {
            EditorUtility.DisplayDialog("Already running", "A break is already in progress.", "OK");
            return;
        }

        var w = GetWindow<INeedABreak>(true, "I Need A Break", true);
        w.minSize = w.maxSize = new Vector2(380, 370);
    }

    private void OnGUI()
    {
        BuildStyles();

        GUILayout.Space(24);
        GUILayout.Label("I Need A Break", _styleTitle);
        GUILayout.Label("Script <color=#FFB300>Goblin</color>", _styleSubtitle);
        GUILayout.Space(4);

        if (_styleMoreTools != null)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("More tools ↗", _styleMoreTools))
            {
                Application.OpenURL(AssetStoreUrl);
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(16);

        GUILayout.Label("Folder to process", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();

        _folder = EditorGUILayout.TextField(_folder);

        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            var picked = EditorUtility.OpenFolderPanel("Select folder", _folder, "");
            if (!string.IsNullOrEmpty(picked))
            {
                _folder = picked;
            }
        }

        GUILayout.EndHorizontal();

        GUILayout.Space(16);

        GUILayout.Label("Duration", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();

        _hours = Mathf.Clamp(EditorGUILayout.IntField(_hours, GUILayout.Width(40)), 0, 23);
        GUILayout.Label("Hours", GUILayout.Width(44));

        _minutes = Mathf.Clamp(EditorGUILayout.IntField(_minutes, GUILayout.Width(40)), 0, 59);
        GUILayout.Label("Minutes", GUILayout.Width(52));

        _seconds = Mathf.Clamp(EditorGUILayout.IntField(_seconds, GUILayout.Width(40)), 0, 59);
        GUILayout.Label("Seconds", GUILayout.Width(52));

        GUILayout.EndHorizontal();

        GUILayout.Space(16);

        GUILayout.Label("Progress message", EditorStyles.boldLabel);
        _progressTitle = EditorGUILayout.TextField(_progressTitle);
        EditorGUILayout.LabelField("Leave empty to use \"Processing files…\"", EditorStyles.miniLabel);

        GUILayout.Space(16);

        _lockEditor = EditorGUILayout.ToggleLeft("Lock editor during break", _lockEditor);

        GUILayout.Space(16);

        if (!string.IsNullOrEmpty(_error))
        {
            GUILayout.Label(_error, _styleError);
            GUILayout.Space(8);
        }

        if (GUILayout.Button("Start Break", GUILayout.Height(36)))
        {
            TryStart();
        }
    }

    private void TryStart()
    {
        _error = "";

        var total = _hours * 3600 + _minutes * 60 + _seconds;
        if (total <= 0)
        {
            _error = "Please set a duration greater than zero.";
            return;
        }

        if (string.IsNullOrWhiteSpace(_folder) || !Directory.Exists(_folder))
        {
            _error = "Please select a valid folder.";
            return;
        }

        var files = new List<string>(Directory.GetFiles(_folder, "*", SearchOption.AllDirectories));

        if (files.Count == 0)
        {
            _error = "The selected folder contains no files.";
            return;
        }

        files.Sort(StringComparer.OrdinalIgnoreCase);

        BreakRunner.Begin(files, total, _lockEditor, _progressTitle);
        Close();
    }

    private void BuildStyles()
    {
        _styleTitle ??= new GUIStyle(EditorStyles.largeLabel)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
        };

        _styleSubtitle ??= new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            richText = true,
        };

        if (_styleMoreTools == null && EditorStyles.linkLabel != null)
        {
            _styleMoreTools = new GUIStyle(EditorStyles.linkLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
            };
        }

        _styleError ??= new GUIStyle(EditorStyles.wordWrappedLabel)
        {
            normal = { textColor = new Color(1f, 0.4f, 0.4f) }
        };
    }
}
#endif
