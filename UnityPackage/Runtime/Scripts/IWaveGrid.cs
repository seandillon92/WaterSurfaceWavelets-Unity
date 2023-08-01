using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WaveGrid
{
    internal interface IWaveGrid : IDisposable
    {
        internal void Update(UpdateSettings settings);
        internal Mesh Mesh { get; }
    }

    [Serializable]
    internal class UpdateSettings
    {
        internal void OnValidate()
        {
            amplitudeTracks.OnValidate();
        }

        [Serializable]
        internal class AmplitudeTrackList
        {
            internal bool[] IncludeValues { get; private set; }

            internal int m_numTracks => 16;
            internal void OnValidate()
            {
                Populate();
                Rename();
                CheckForSolo();
                SetIncludeValues();
            }
            internal AmplitudeTrackList(int numTracks) {
                data = new List<AmplitudeTrack>();
                for (int i = 0; i < numTracks; i++) {
                    data.Add(new AmplitudeTrack());
                }
                Rename();
            }

            [HideInInspector]
            public List<AmplitudeTrack> data;

            //For display in the editor
            public List<AmplitudeTrack> tracks;

            void CheckForSolo()
            {
                if (tracks == null)
                {
                    tracks = new List<AmplitudeTrack>();
                }

                var solo = data.FindIndex(x => x.mode == AmplitudeTrack.Mode.Solo);
                if (solo == -1)
                {
                    tracks.Clear();
                    tracks.AddRange(data);
                    return;
                }
                tracks.Clear();
                tracks.Add(data[solo]);
            }

            void Rename()
            {
                for(int i = 0; i < data.Count; i++)
                {
                    data[i].name = $"Track {i + 1}";
                }
            }

            void SetIncludeValues()
            {
                IncludeValues = Enumerable.Repeat(false, m_numTracks).ToArray();
                var solo = data.FindIndex(x => x.mode == AmplitudeTrack.Mode.Solo);
                if (solo == -1)
                {
                    for (int i = 0; i < m_numTracks; i++)
                    {
                        IncludeValues[i] = data[i].mode == AmplitudeTrack.Mode.Include;
                    }
                }
                else
                {

                    IncludeValues[solo] = true;
                }
            }

            void Populate()
            {
                if (data.Count > m_numTracks)
                {
                    data.RemoveRange(m_numTracks - 1, data.Count - m_numTracks);
                    return;
                }
                if (data.Count < m_numTracks)
                {
                    for(int i = data.Count; i < m_numTracks; i++)
                    {
                        data.Add(new AmplitudeTrack());
                    }
                }
            }
        }
        [Serializable]
        internal class AmplitudeTrack
        {
            internal enum Mode
            {
                Include,
                Exclude,
                Solo,
            }
            [HideInInspector]
            public string name;
            public Mode mode;
        }
        [HideInInspector]
        public float dt;

        public bool updateSimulation = true;
        public float amplitudeMultiplier = 4f;

        public AmplitudeTrackList amplitudeTracks = new AmplitudeTrackList(16);

        public bool renderOutsideBorders = true;
    }
}