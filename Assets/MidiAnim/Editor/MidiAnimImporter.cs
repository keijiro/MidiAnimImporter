// MidiAnimImporter - MIDI animation importer
// https://github.com/keijiro/MidiAnimImporter

using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using System.IO;

namespace MidiAnim
{
    // Scripted importer for MIDI animation files
    [ScriptedImporter(1, "midianim")]
    class MidiAnimImporter : ScriptedImporter
    {
        [SerializeField] int _trackIndex = 0;
        [SerializeField] float _bpm = 120;
        [SerializeField] bool _gateEasing = false;
        [SerializeField] float _attackTime = 0.1f;
        [SerializeField] float _releaseTime = 0.3f;

        public override void OnImportAsset(AssetImportContext context)
        {
            var song = MidiFileLoader.Load(File.ReadAllBytes(context.assetPath));
            var seq = new MidiTrackSequencer(song.tracks[_trackIndex], song.division, _bpm);

            var clip = new MidiClip(_bpm);
            if (_gateEasing) clip.EnableEasing(_attackTime, _releaseTime);

            clip.WriteBeat(0);
            clip.WriteEvents(0, seq.Start());

            const float dt = 1.0f / 60;
            for (var t = dt; seq.Playing; t += dt)
            {
                clip.WriteBeat(t);
                clip.WriteEvents(t, seq.Advance(dt));
            }

            var anim = clip.ConvertToAnimationClip();
            context.AddObjectToAsset("MIDI", anim);
            context.SetMainObject(anim);
        }
    }
}
