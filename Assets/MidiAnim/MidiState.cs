// MidiAnimImporter - MIDI animation importer
// https://github.com/keijiro/MidiAnimImporter

using UnityEngine;

namespace MidiAnim
{
    // Placeholder for MIDI parameters
    public class MidiState : MonoBehaviour
    {
        public float BeatCount;
        public float BeatClock;

        public float BarCount;
        public float BarClock;

        public float [] CC = new float [128];
        public float [] Note = new float [128];
    }
}
