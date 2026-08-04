using Unity.Netcode;
using UnityEngine;

public class NetworkDebugManager : MonoBehaviour
{
    private NetworkManager m_NetworkManager;

    private void Awake()
    {
        m_NetworkManager = GetComponent<NetworkManager>();
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 300));
        if (m_NetworkManager.IsClient || m_NetworkManager.IsServer)
        {
            StatusLabels();
        }

        GUILayout.EndArea();
    }

    private void StatusLabels()
    {
        string mode =
            m_NetworkManager.IsHost ? "Host"
            : m_NetworkManager.IsServer ? "Server"
            : "Client";

        ulong localId = m_NetworkManager.LocalClientId;

        GUILayout.Label(
            "Transport: " + m_NetworkManager.NetworkConfig.NetworkTransport.GetType().Name
        );
        GUILayout.Label("Mode: " + mode);
        GUILayout.Label("Client ID: " + localId);
    }
}
