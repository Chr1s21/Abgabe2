using System.Net;
using System.Net.Sockets;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class NetworkUI : MonoBehaviour
{
    [SerializeField] private ushort port = 7777;

    private string ipAddress = "127.0.0.1";
    private string localIp;
    private string status = "";

    private void Start()
    {
        localIp = GetLocalIPAddress();

        // Zuletzt benutzte IP merken, damit man sie nicht jedes Mal neu tippen muss.
        ipAddress = PlayerPrefs.GetString("LastIP", "127.0.0.1");

        NetworkManager.Singleton.OnClientDisconnectCallback += OnDisconnected;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnDisconnected;
        }
    }

    private void OnGUI()
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        GUILayout.BeginArea(new Rect(10, 10, 260, 220));

        if (!networkManager.IsClient && !networkManager.IsServer)
        {
            if (GUILayout.Button("Host")) StartHost();

            GUILayout.Space(10);
            GUILayout.Label("IP des Hosts:");
            ipAddress = GUILayout.TextField(ipAddress);
            if (GUILayout.Button("Client")) StartClient();

            if (!string.IsNullOrEmpty(status)) GUILayout.Label(status);
        }
        else
        {
            GUILayout.Label(networkManager.IsHost ? "Modus : Host" : "Modus : Client");
            if (networkManager.IsHost) GUILayout.Label($"Deine IP: {localIp}");
        }

        GUILayout.EndArea();
    }

    private void StartHost()
    {
        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        // 0.0.0.0 = Verbindungen von allen Geräten im Netzwerk annehmen,
        // nicht nur vom eigenen Rechner.
        transport.SetConnectionData("127.0.0.1", port, "0.0.0.0");

        status = "";
        NetworkManager.Singleton.StartHost();
    }

    private void StartClient()
    {
        string ip = ipAddress.Trim();

        if (!IPAddress.TryParse(ip, out _))
        {
            status = "Ungültige IP-Adresse";
            return;
        }

        PlayerPrefs.SetString("LastIP", ip);

        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetConnectionData(ip, port);

        status = $"Verbinde mit {ip} ...";
        NetworkManager.Singleton.StartClient();
    }

    private void OnDisconnected(ulong clientId)
    {
        // Nur beim Client: Verbindung fehlgeschlagen oder Host weg.
        if (!NetworkManager.Singleton.IsServer)
        {
            status = "Keine Verbindung zum Host";
        }
    }

    // Eigene IP im lokalen Netzwerk herausfinden.
    private static string GetLocalIPAddress()
    {
        try
        {
            // Es wird nichts gesendet. Windows sucht nur aus, über welche
            // Netzwerkkarte es ins Netz gehen würde, und wir lesen deren IP aus.
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                socket.Connect("8.8.8.8", 65530);
                return ((IPEndPoint)socket.LocalEndPoint).Address.ToString();
            }
        }
        catch
        {
            // Kein Internet: erste IPv4-Adresse des Rechners nehmen.
            foreach (IPAddress address in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (address.AddressFamily == AddressFamily.InterNetwork) return address.ToString();
            }
            return "unbekannt";
        }
    }
}