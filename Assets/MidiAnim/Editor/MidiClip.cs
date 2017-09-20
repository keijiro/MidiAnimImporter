// MidiAnimImporter - MIDI animation importer
// https://github.com/keijiro/MidiAnimImporter

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace MidiAnim
{
    // MIDI animation clip
    internal sealed class MidiClip
    {
        #region Parameter curves

        AnimationCurve _beatCount = new AnimationCurve();
        AnimationCurve _beatClock = new AnimationCurve();

        AnimationCurve _barCount = new AnimationCurve();
        AnimationCurve _barClock = new AnimationCurve();

        AnimationCurve [] _noteCurves = new AnimationCurve [128];
        AnimationCurve [] _ccCurves = new AnimationCurve [128];

        #endregion

        #region Private members

        float _bpm;
        int _beat = -1;

        const float kDeltaTime = 1.0f / 60;

        #endregion

        #region Public methods

        public MidiClip(float bpm)
        {
            _bpm = bpm;
        }

        public void WriteBeat(float time)
        {
            var beatAtTime = (int)(_bpm * time / 60);

            // Do nothing if it's still in the same beat.
            if (beatAtTime == _beat) return;

            // Update the beat number.
            _beat = beatAtTime;

            // Beat count
            _beatCount.AddKey(time, _beat);

            // Beat clock curve
            if (_beat > 0) _beatClock.AddKey(time - kDeltaTime, 1);
            _beatClock.AddKey(time, 0);

            if (_beat % 4 == 0)
            {
                // Bar count
                _barCount.AddKey(time, _beat / 4);

                // Bar clock curve
                if (_beat > 0) _barClock.AddKey(time - kDeltaTime, 1);
                _barClock.AddKey(time, 0);
            }
        }

        public void WriteEvents(float time, List<MidiEvent> events)
        {
            if (events == null) return;

            foreach (var e in events)
            {
                var stat = e.status & 0xf0;
                var index = e.data1;
                var value = (float)e.data2 / 127;

                if (stat == 0x90) // Note on
                    SetNoteKey(index, time, value);
                else if (stat == 0x80) // Note off
                    SetNoteKey(index, time - kDeltaTime, 0);
                else if (stat == 0xb0) // CC
                    SetCCKey(index, time, value);
            }
        }

        public AnimationClip ConvertToAnimationClip()
        {
            var dest = new AnimationClip();

            ModifyTangentsForCount(_beatCount);
            ModifyTangentsForClock(_beatClock);

            dest.SetCurve("", typeof(MidiState), "BeatCount", _beatCount);
            dest.SetCurve("", typeof(MidiState), "BeatClock", _beatClock);

            ModifyTangentsForCount(_barCount);
            ModifyTangentsForClock(_barClock);

            dest.SetCurve("", typeof(MidiState), "BarCount", _barCount);
            dest.SetCurve("", typeof(MidiState), "BarClock", _barClock);

            for (var i = 0; i < _noteCurves.Length; i++)
            {
                var curve = _noteCurves[i];
                if (curve == null) continue;
                ModifyTangentsForNotes(curve);
                dest.SetCurve("", typeof(MidiState), "Note[" + i + "]", curve);
            }

            for (var i = 0; i < _ccCurves.Length; i++)
            {
                var curve = _ccCurves[i];
                if (curve == null) continue;
                ModifyTangentsForCC(curve);
                dest.SetCurve("", typeof(MidiState), "CC[" + i + "]", curve);
            }

            return dest;
        }

        #endregion

        #region Keyframe utilities

        void SetNoteKey(int index, float time, float value)
        {
            if (_noteCurves[index] == null)
            {
                _noteCurves[index] = new AnimationCurve();
                _noteCurves[index].AddKey(0, 0); // zeroing key
            }

            // Avoid adding a key at the first frame.
            // It's reserved for the zeroing key.
            if (time == 0) time += kDeltaTime;

            _noteCurves[index].AddKey(time, value);
        }

        void SetCCKey(int index, float time, float value)
        {
            var curve = _ccCurves[index];

            if (curve == null)
            {
                // No curve for this CC. Create a new one and add the first key.
                _ccCurves[index] = new AnimationCurve();
                _ccCurves[index].AddKey(time, value);
            }
            else
            {
                // Avoid adding a key in the same frame.
                var availFrom = curve[curve.length - 1].time + kDeltaTime;
                curve.AddKey(Mathf.Max(availFrom, time), value);
            }
        }

        #endregion

        #region Tangent modifiers

        static void ModifyTangentsForCount(AnimationCurve curve)
        {
            var tan = AnimationUtility.TangentMode.Constant;

            for (var i = 0; i < curve.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, tan);
                AnimationUtility.SetKeyRightTangentMode(curve, i, tan);
            }
        }

        static void ModifyTangentsForClock(AnimationCurve curve)
        {
            var ctan = AnimationUtility.TangentMode.Constant;
            var ltan = AnimationUtility.TangentMode.Linear;

            for (var i = 0; i < curve.length; i++)
            {
                if (curve[i].value < 0.5f)
                {
                    AnimationUtility.SetKeyLeftTangentMode(curve, i, ctan);
                    AnimationUtility.SetKeyRightTangentMode(curve, i, ltan);
                }
                else
                {
                    AnimationUtility.SetKeyLeftTangentMode(curve, i, ltan);
                    AnimationUtility.SetKeyRightTangentMode(curve, i, ctan);
                }
            }
        }

        static void ModifyTangentsForNotes(AnimationCurve curve)
        {
            var ctan = AnimationUtility.TangentMode.Constant;
            var ltan = AnimationUtility.TangentMode.Linear;

            for (var i = 0; i < curve.length; i++)
            {
                if (curve[i].value > 0.5f)
                {
                    AnimationUtility.SetKeyLeftTangentMode(curve, i, ctan);
                    AnimationUtility.SetKeyRightTangentMode(curve, i, ltan);
                }
                else
                {
                    AnimationUtility.SetKeyLeftTangentMode(curve, i, ltan);
                    AnimationUtility.SetKeyRightTangentMode(curve, i, ctan);
                }
            }
        }

        static void ModifyTangentsForCC(AnimationCurve curve)
        {
            var tan = AnimationUtility.TangentMode.Linear;

            for (var i = 0; i < curve.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, tan);
                AnimationUtility.SetKeyRightTangentMode(curve, i, tan);
            }
        }

        #endregion
    }
}
