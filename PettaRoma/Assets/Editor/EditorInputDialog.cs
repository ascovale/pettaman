/*  Simple input dialog for Unity Editor  */
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class EditorInputDialog : EditorWindow
{
    private string description;
    private string inputText;
    private string result;
    private bool confirmed;
    private bool firstFrame = true;

    public static string Show(string title, string description, string defaultValue)
    {
        var window = ScriptableObject.CreateInstance<EditorInputDialog>();
        window.titleContent = new GUIContent(title);
        window.description = description;
        window.inputText = defaultValue;
        window.result = null;
        window.confirmed = false;
        window.minSize = new Vector2(400, 300);
        window.maxSize = new Vector2(400, 400);
        window.ShowModal();
        return window.result;
    }

    void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField(description, EditorStyles.wordWrappedLabel);
        EditorGUILayout.Space(10);

        GUI.SetNextControlName("InputField");
        inputText = EditorGUILayout.TextField("Numero:", inputText);

        if (firstFrame)
        {
            EditorGUI.FocusTextInControl("InputField");
            firstFrame = false;
        }

        EditorGUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("OK", GUILayout.Width(100)))
        {
            result = inputText;
            confirmed = true;
            Close();
        }
        if (GUILayout.Button("Annulla", GUILayout.Width(100)))
        {
            result = null;
            Close();
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        // Enter key confirms
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
        {
            result = inputText;
            confirmed = true;
            Close();
        }
    }
}
#endif
