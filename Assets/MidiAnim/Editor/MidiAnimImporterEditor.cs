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
        SerializedProperty _gateEasing;
        SerializedProperty _attackTime;
        SerializedProperty _releaseTime;

        static class Styles
        {
            public static readonly GUIContent BPM = new GUIContent("BPM");
        }

        public override bool showImportedObject { get { return false; } }
        protected override bool useAssetDrawPreview { get { return false; } }

        public override void OnEnable()
        {
            base.OnEnable();

            _bpm = serializedObject.FindProperty("_bpm");
            _gateEasing = serializedObject.FindProperty("_gateEasing");
            _attackTime = serializedObject.FindProperty("_attackTime");
            _releaseTime = serializedObject.FindProperty("_releaseTime");
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(_bpm, Styles.BPM);
            EditorGUILayout.PropertyField(_gateEasing);

            EditorGUI.BeginDisabledGroup(
                !_gateEasing.boolValue && !_gateEasing.hasMultipleDifferentValues
            );

            EditorGUI.indentLevel++;
            EditorGUILayout.Slider(_attackTime, 0.01f, 1);
            EditorGUILayout.Slider(_releaseTime, 0.01f, 1);
            EditorGUI.indentLevel--;

            EditorGUI.EndDisabledGroup();

            base.ApplyRevertGUI();
        }
    }
}
