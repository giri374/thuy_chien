namespace AudioSystem
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Pool;

    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance;

        [Header("Data & Pool Settings")]
        [SerializeField] private AudioScriptableObject audioData;
        [SerializeField] private int defaultCapacity = 10;
        [SerializeField] private int maxPoolSize = 20;

        [Header("Audio Settings")]
        public bool isMusicOn = true;
        public bool isSfxOn = true;

        private IObjectPool<AudioSource> sfxPool;
        private AudioSource musicSource;
        private Audio currentMusicData;
        private readonly List<AudioSource> activeSfxSources = new List<AudioSource>(); // Theo dõi các SFX đang chạy

        private void Awake ()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            sfxPool = new ObjectPool<AudioSource>(
                CreateAudioSource, OnGetFromPool, OnReleaseToPool, OnDestroyPooledObject,
                true, defaultCapacity, maxPoolSize
            );

            musicSource = gameObject.AddComponent<AudioSource>();
        }

        #region Pool Handlers
        private AudioSource CreateAudioSource ()
        {
            var go = new GameObject("Pooled_SFX");
            // QUAN TRỌNG: Gắn nó vào Manager để nó không bị xóa khi đổi Scene
            go.transform.SetParent(this.transform);
            return go.AddComponent<AudioSource>();
        }
        private void OnGetFromPool (AudioSource source) => source.gameObject.SetActive(true);

        private void OnReleaseToPool (AudioSource source) => source.gameObject.SetActive(false);
        private void OnDestroyPooledObject (AudioSource source) => Destroy(source.gameObject);
        #endregion

        #region Settings Methods

        public void SetSfx (bool isOn)
        {
            isSfxOn = isOn;
            if (!isSfxOn)
            {
                // Ngắt toàn bộ SFX đang phát khi tắt
                foreach (var source in activeSfxSources.ToArray())
                {
                    if (source.gameObject.activeInHierarchy)
                    {
                        source.Stop();
                        sfxPool.Release(source);
                    }
                }
                activeSfxSources.Clear();
            }
        }

        public void SetMusic (bool isOn)
        {
            isMusicOn = isOn;
            if (isMusicOn)
            {
                // Nếu có bài nhạc đã được load trước đó và đang im lặng -> Phát ngay
                if (currentMusicData != null && !musicSource.isPlaying)
                {
                    musicSource.Play();
                }
            }
            else
            {
                musicSource.Stop();
            }
        }

        #endregion

        public void PlayAudio (string audioName)
        {
            var s = audioData.Audios.Find(a => a.name == audioName);
            if (s == null)
            {
                return;
            }

            if (s.isMusic)
            {
                currentMusicData = s; // Ghi nhớ bài nhạc muốn phát
                SetupSource(musicSource, s); // Luôn load clip vào source

                if (isMusicOn)
                {
                    musicSource.Play();
                }
                else
                {
                    musicSource.Stop();
                }
            }
            else
            {
                if (!isSfxOn)
                {
                    return;
                }

                var source = sfxPool.Get();
                SetupSource(source, s);
                source.Play();

                activeSfxSources.Add(source);
                StartCoroutine(ReturnToPoolAfterFinished(source));
            }
        }

        private void SetupSource (AudioSource source, Audio data)
        {
            source.clip = data.audioClip;
            source.volume = data.volume;
            source.pitch = data.pitch;
            source.loop = data.isLoop;
        }

        private IEnumerator ReturnToPoolAfterFinished (AudioSource source)
        {
            // Tính toán thời gian dựa trên clip và pitch
            var duration = (source.clip != null) ? source.clip.length / Mathf.Abs(source.pitch) : 0f;
            yield return new WaitForSeconds(duration);

            // Kiểm tra source có bị Destroy bởi Unity khi chuyển Scene hay không
            if (source != null && source.gameObject != null)
            {
                if (activeSfxSources.Contains(source))
                {
                    activeSfxSources.Remove(source);
                }

                // Chỉ trả về pool nếu object vẫn còn đang hoạt động
                if (source.gameObject.activeInHierarchy)
                {
                    sfxPool.Release(source);
                }
            }
        }


        private void OnDisable ()
        {
            // Ngắt toàn bộ các hàm đang đợi trả về Pool để tránh MissingReference
            StopAllCoroutines();
            activeSfxSources.Clear();
        }
    }
}


