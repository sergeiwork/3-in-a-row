using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThreeInARow.Presentation
{
    [CreateAssetMenu(menuName = "Three in a Row/Presentation Catalog")]
    public sealed class PresentationCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class SpriteEntry
        {
            public string Key;
            public Sprite Sprite;
        }

        [Serializable]
        public sealed class AudioEntry
        {
            public string Key;
            public AudioClip Clip;
        }

        [SerializeField] private List<SpriteEntry> sprites = new List<SpriteEntry>();
        [SerializeField] private List<AudioEntry> audioClips = new List<AudioEntry>();

        private Dictionary<string, Sprite> _spriteLookup;
        private Dictionary<string, AudioClip> _audioLookup;

        public IReadOnlyList<SpriteEntry> Sprites => sprites;
        public IReadOnlyList<AudioEntry> AudioClips => audioClips;

        public Sprite GetSprite(string key)
        {
            EnsureLookups();
            Sprite sprite;
            return !string.IsNullOrEmpty(key) && _spriteLookup.TryGetValue(key, out sprite) ? sprite : null;
        }

        public AudioClip GetAudio(string key)
        {
            EnsureLookups();
            AudioClip clip;
            return !string.IsNullOrEmpty(key) && _audioLookup.TryGetValue(key, out clip) ? clip : null;
        }

#if UNITY_EDITOR
        public void ReplaceEntries(IEnumerable<SpriteEntry> spriteEntries, IEnumerable<AudioEntry> audioEntries)
        {
            sprites = spriteEntries == null ? new List<SpriteEntry>() : new List<SpriteEntry>(spriteEntries);
            audioClips = audioEntries == null ? new List<AudioEntry>() : new List<AudioEntry>(audioEntries);
            _spriteLookup = null;
            _audioLookup = null;
        }
#endif

        private void EnsureLookups()
        {
            if (_spriteLookup != null && _audioLookup != null) return;
            _spriteLookup = new Dictionary<string, Sprite>(StringComparer.Ordinal);
            _audioLookup = new Dictionary<string, AudioClip>(StringComparer.Ordinal);
            foreach (var entry in sprites)
                if (entry != null && !string.IsNullOrEmpty(entry.Key) && entry.Sprite != null)
                    _spriteLookup[entry.Key] = entry.Sprite;
            foreach (var entry in audioClips)
                if (entry != null && !string.IsNullOrEmpty(entry.Key) && entry.Clip != null)
                    _audioLookup[entry.Key] = entry.Clip;
        }
    }
}
