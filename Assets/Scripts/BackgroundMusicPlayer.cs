using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundMusicPlayer : MonoBehaviour
{
    [SerializeField] private AudioClip[] tracks;
    [SerializeField, Range(0f, 1f)] private float volume = 0.3f;

    private readonly List<AudioClip> playlist = new List<AudioClip>();
    private AudioSource source;
    private int lastTrack = -1;

    private void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.volume = volume;

        if (tracks != null)
        {
            foreach (AudioClip track in tracks)
            {
                if (track != null && !playlist.Contains(track))
                    playlist.Add(track);
            }
        }
    }

    private void OnEnable()
    {
        if (playlist.Count == 0)
        {
            Debug.LogWarning("BackgroundMusicPlayer has no music tracks.", this);
            return;
        }

        StartCoroutine(PlayMusic());
    }

    private IEnumerator PlayMusic()
    {
        while (true)
        {
            // Pick any track initially, then avoid repeating the previous track.
            int nextTrack = Random.Range(0,
                playlist.Count > 1 && lastTrack >= 0
                    ? playlist.Count - 1 : playlist.Count);
            if (playlist.Count > 1 && lastTrack >= 0 && nextTrack >= lastTrack)
                nextTrack++;

            lastTrack = nextTrack;
            source.clip = playlist[nextTrack];
            source.Play();

            do
            {
                yield return null;
            }
            while (source.isPlaying || AudioListener.pause);
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        if (source != null)
            source.Stop();
    }
}
