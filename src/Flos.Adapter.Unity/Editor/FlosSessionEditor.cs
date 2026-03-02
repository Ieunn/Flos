using Flos.Core.Sessions;
using UnityEditor;
using UnityEngine;

namespace Flos.Adapter.Unity.Editor
{
    /// <summary>
    /// Custom inspector for <see cref="FlosSession"/> showing runtime state.
    /// </summary>
    [CustomEditor(typeof(FlosSession), true)]
    public class FlosSessionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var flosSession = (FlosSession)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Runtime State", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to see runtime state.", MessageType.Info);
                return;
            }

            var session = flosSession.Session;
            if (session == null)
            {
                EditorGUILayout.HelpBox("Session not initialized.", MessageType.Warning);
                return;
            }

            GUI.enabled = false;
            EditorGUILayout.EnumPopup("Session State", session.State);
            EditorGUILayout.LongField("Current Tick", session.Scheduler.CurrentTick);
            EditorGUILayout.FloatField("Elapsed Time", session.Scheduler.ElapsedTime);

            var types = session.World.RegisteredTypes;
            EditorGUILayout.LabelField("State Slices", types.Count.ToString());
            EditorGUI.indentLevel++;
            foreach (var type in types)
            {
                EditorGUILayout.LabelField(type.Name);
            }
            EditorGUI.indentLevel--;

            GUI.enabled = true;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Controls", EditorStyles.boldLabel);

            if (session.State == SessionState.Running)
            {
                if (GUILayout.Button("Pause"))
                    session.Pause();
            }
            else if (session.State == SessionState.Paused)
            {
                if (GUILayout.Button("Resume"))
                    session.Resume();
            }

            Repaint();
        }
    }
}
