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

        float _attackTime;
        float _releaseTime;

        const float kDeltaTime = 1.0f / 60;
        const float kEpsilon = 0.001f;

        #endregion

        #region Public methods

        public MidiClip(float bpm)
        {
            _bpm = bpm;
        }

        public void EnableEasing(float attack, float release)
        {
            _attackTime = attack;
            _releaseTime = release;
        }

        public void DisableEasing()
        {
            _attackTime = _releaseTime = 0;
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
                var status = e.status & 0xf0;
                var index = e.data1;
                var value = (float)e.data2 / 127;

                if (status == 0x90) // Note on
                    SetNoteKey(index, time, value);
                else if (status == 0x80) // Note off
                    SetNoteKey(index, time - kDeltaTime, 0);
                else if (status == 0xb0) // CC
                    SetCCKey(index, time, value);
            }
        }

        public AnimationClip ConvertToAnimationClip()
        {
            var dest = new AnimationClip();

            // Beat count/clock
            FlattenTangents(_beatCount);
            SawtoothTangents(_beatClock);
            dest.SetCurve("", typeof(MidiState), "BeatCount", _beatCount);
            dest.SetCurve("", typeof(MidiState), "BeatClock", _beatClock);

            // Bar count/clock
            FlattenTangents(_barCount);
            SawtoothTangents(_barClock);
            dest.SetCurve("", typeof(MidiState), "BarCount", _barCount);
            dest.SetCurve("", typeof(MidiState), "BarClock", _barClock);

            // Note curves
            for (var i = 0; i < _noteCurves.Length; i++)
            {
                var curve = _noteCurves[i];
                if (curve == null) continue;
                if (_attackTime > 0)
                    curve = CreateEasedCurve(curve);
                else
                    FlattenTangents(curve);
                dest.SetCurve("", typeof(MidiState), "Note[" + i + "]", curve);
            }

            // CC curves
            for (var i = 0; i < _ccCurves.Length; i++)
            {
                var curve = _ccCurves[i];
                if (curve == null) continue;
                LinearizeTangents(curve);
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

        static void FlattenTangents(AnimationCurve curve)
        {
            var tan = AnimationUtility.TangentMode.Constant;
            for (var i = 0; i < curve.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, tan);
                AnimationUtility.SetKeyRightTangentMode(curve, i, tan);
            }
        }

        static void LinearizeTangents(AnimationCurve curve)
        {
            var tan = AnimationUtility.TangentMode.Linear;
            for (var i = 0; i < curve.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, tan);
                AnimationUtility.SetKeyRightTangentMode(curve, i, tan);
            }
        }

        static void SawtoothTangents(AnimationCurve curve)
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

        AnimationCurve CreateEasedCurve(AnimationCurve source)
        {
            var curve = new AnimationCurve();

            // Copy the first key.
            var key0 = source[0];
            curve.AddKey(new Keyframe(key0.time, key0.value, 0, 0));

            var lastValue = source[0].value;

            for (var i = 1; i < source.length - 1; i++)
            {
                var key = source[i];
                var time = key.time;
                var value = key.value;

                // Transition time from the settings (attack/release)
                var transTime = lastValue < value ? _attackTime : _releaseTime;

                // Delay time until it reaches the current value.
                var delay = transTime * Mathf.Abs(value - lastValue);

                // Clip the delay time with the time of the next key.
                var nextKeyTime = source[i + 1].time;
                delay = Mathf.Min(delay, nextKeyTime - time - kEpsilon);

                // Actual difference of values between keys.
                var delta = Mathf.Sign(value - lastValue) * delay / transTime;

                curve.AddKey(new Keyframe(time, lastValue, 0, 0));
                curve.AddKey(new Keyframe(time + delay, lastValue + delta, 0, 0));

                lastValue += delta;
            }

            // Copy the last key.
            var end = source[source.length - 1].time;
            curve.AddKey(new Keyframe(end, lastValue, 0, 0));
            curve.AddKey(new Keyframe(end + lastValue * _releaseTime, 0, 0, 0));

            return curve;
        }

        #endregion
    }
}
