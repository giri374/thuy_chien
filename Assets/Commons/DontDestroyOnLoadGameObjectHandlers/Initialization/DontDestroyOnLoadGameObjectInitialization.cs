namespace Assets.Commons.DontDestroyOnLoadGameObjectHandlers.Initialization
{
    using Assets.Commons.SceneHandlers;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    public class DontDestroyOnLoadGameObjectInitialization : MonoBehaviour
    {
        //To make sure all Start() in scene finished
        private void Update ()
        {
            if (InvalidToExecute())
            {
                return;
            }

            InitializeOnlineModeControllers();
            ShowFirstGameViewScene();
            Initialized = true;

            bool InvalidToExecute ()
            {
                return Initialized;
            }
        }

        private bool Initialized { get; set; }

        private void InitializeOnlineModeControllers ()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void ShowFirstGameViewScene ()
        {
            SceneManager.LoadScene(SceneInGame.MenuScene.ToString());
        }
    }
}
