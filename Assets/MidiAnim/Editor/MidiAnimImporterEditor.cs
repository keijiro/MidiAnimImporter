// MidiAnimImporter - MIDI animation importer
// https://github.com/keijiro/MidiAnimImporter

using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;

namespace MidiAnim
{
    // Custom inspector UI for MidiAnimImporter
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MidiAnimImporter))]
    class MidiAnimImporterEditor : ScriptedImporterEditor
    {
        SerializedProperty _bpm;

        static class Styles
        {
            public static readonly GUIContent BPM = new GUIContent("BPM");
        }

        public override bool showImportedObject { get { return false; } }

        public override void OnEnable()
        {
            base.OnEnable();

            _bpm = serializedObject.FindProperty("_bpm");
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(_bpm, Styles.BPM);

            base.ApplyRevertGUI();
        }
    }
}
