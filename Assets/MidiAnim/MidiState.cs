using UnityEngine;

namespace MidiAnim
{
    public class MidiState : MonoBehaviour
    {
        public float CountBeat;
        public float CountBar;

        public float ClickBeat;
        public float ClickBar;

        public float [] CC = new float [128];
        public float [] Note = new float [128];
    }
}
