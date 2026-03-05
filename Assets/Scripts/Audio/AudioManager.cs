using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AudioSystem
{

    public class AudioManager : MonoBehaviour
    {
        #region Singleton
        private static AudioManager _instance;
        public static AudioManager Instance { get { return _instance; } }
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                if (DontDestroy)
                {
                    DontDestroyOnLoad(gameObject);
                }
            }
            else
            {
                Destroy(gameObject);
                return;
            }
            SetObjects();
            CreatePool(true);
        }
        #endregion

        [SerializeField] private AudioScriptableObject AudioListObject;
        private List<Audio> AudioList;
        public int PoolSize = 5;
        public bool DontDestroy;
        [SerializeField] private GameObject AudioPlayer;


        private void SetObjects()
        {
            if (AudioListObject != null)
            {
                AudioList = AudioListObject.Audios;
            }
            else
            {
                Debug.LogError("AudioListObject not assigned in Inspector!");
            }
        }


        private void CreatePool(bool isInitial = false)
        {
            if (isInitial)
            {
                for (int i = 0; i < PoolSize; i++)
                {
                    Instantiate(AudioPlayer, transform);
                }
            }
            else
            {
                Instantiate(AudioPlayer, transform);
            }
        }


        //helper
        private AudioSource UseFromPool()
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                if (!transform.GetChild(i).GetComponent<AudioSource>().isPlaying)
                {
                    return transform.GetChild(i).GetComponent<AudioSource>();
                }
            }
            CreatePool();
            return transform.GetChild(transform.childCount - 1).GetComponent<AudioSource>();
        }


        #region Play Audio
        private Audio GetAudio(string audioName)
        {
            if (PoolSize == 0)
            {
                CreatePool();
            }
            for (int i = 0; i < AudioList.Count; i++)
            {
                if (audioName == AudioList[i].name)
                {
                    return AudioList[i];
                }
            }
            return null;
        }

        public void PlayAudio(string audioName, int volume = 1, int pitch = 1, bool loop = false)
        {
            Audio audio = GetAudio(audioName);
            if (audio == null)
            {
                Debug.LogError($"Audio '{audioName}' not found!");
                return;
            }

            AudioSource audioSource = UseFromPool();
            audioSource.clip = audio.audioClip;
            audioSource.volume = audio.volume;
            audioSource.pitch = audio.pitch;
            audioSource.loop = audio.isLoop;

            audioSource.Play();
        }
        #endregion


        private void Update()
        {

        }



    }

}


