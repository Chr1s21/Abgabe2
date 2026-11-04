using UnityEngine;
using Unity.Netcode;

public class NetworkUI : MonoBehaviour
{
    void OnGUI(){
      GUILayout.BeginArea(new Rect(10, 10, 200, 100));
      NetworkManager networkManager = NetworkManager.Singleton;

      if (!networkManager.IsClient && !networkManager.IsServer)
      {
          if (GUILayout.Button("Host")) networkManager.StartHost();
          if (GUILayout.Button("Client")) networkManager.StartClient();
                }
      else
      {
        GUILayout.Label(networkManager.IsHost ? "Modus : Host" : "Modus : Client");
        GUILayout.EndArea();
      }

    }
}
