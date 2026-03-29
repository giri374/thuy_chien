using System;
using System.Collections.Generic;
using Assets.OnlineMode.ConnectionMenu;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
public class MatchSpawner : MonoBehaviour
{
    [SerializeField] private GameObject eGameMatchPrefab;
    private NetworkObject spawned;
    private void Start ()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnServerStarted += OnServerStarted;
    }

    private void OnServerStarted ()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        if (spawned != null) return;

        var go = Instantiate(eGameMatchPrefab);
        DontDestroyOnLoad(go);
        spawned = go.GetComponent<NetworkObject>();
        spawned.Spawn();
    }
}