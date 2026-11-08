using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerHUD : NetworkBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;

    private GUIStyle textStyle;
    private GUIStyle bigStyle;

    private readonly List<PlayerHealth> players = new List<PlayerHealth>();
    private float nextRefresh;

    private void Awake()
    {
        if (playerHealth == null) playerHealth = GetComponent<PlayerHealth>();
    }

    public static string PlayerName(ulong clientId) => $"Spieler {clientId + 1}";


    private void OnGUI()
    {
        if (!IsSpawned || !IsOwner) return;

        InitStyles();
        DrawScoreboard();

        if (GameManager.IsGameOver)
        {
            DrawGameOver();
            return;
        }

        if (playerHealth.isDead.Value)
        {
            DrawDeathScreen();
            return;
        }

        DrawCrosshair();
        DrawHealth();
    }

    private void InitStyles()
    {
        if (textStyle != null) return;   // GUI.skin gibt es nur in OnGUI, deshalb hier

        textStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };

        bigStyle = new GUIStyle(textStyle)
        {
            fontSize = 42,
            alignment = TextAnchor.MiddleCenter
        };
    }

    private void DrawCrosshair()
    {
        float cx = Screen.width / 2f;
        float cy = Screen.height / 2f;
        float length = 8f, thickness = 2f, gap = 4f;
        Color color = Color.white;

        DrawRect(new Rect(cx - thickness / 2, cy - gap - length, thickness, length), color); // oben
        DrawRect(new Rect(cx - thickness / 2, cy + gap, thickness, length), color);          // unten
        DrawRect(new Rect(cx - gap - length, cy - thickness / 2, length, thickness), color); // links
        DrawRect(new Rect(cx + gap, cy - thickness / 2, length, thickness), color);          // rechts
    }

    private void DrawHealth()
    {
        float percent = (float)playerHealth.currentHealth.Value / playerHealth.maxHealth;
        Rect background = new Rect(20, Screen.height - 50, 260, 28);

        DrawRect(background, new Color(0f, 0f, 0f, 0.6f));

        Rect fill = new Rect(background.x + 3, background.y + 3, (background.width - 6) * percent, background.height - 6);
        DrawRect(fill, Color.Lerp(Color.red, Color.green, percent));

        ShadowLabel(new Rect(background.x + 10, background.y, background.width, background.height),
            $"{playerHealth.currentHealth.Value} / {playerHealth.maxHealth}", textStyle);
    }

    private void DrawScoreboard()
    {
        RefreshPlayers();

        int killsToWin = GameManager.Instance != null ? GameManager.Instance.KillsToWin : 0;
        float width = 240f;
        float rowHeight = 26f;
        Rect box = new Rect(Screen.width - width - 20, 20, width, 36 + players.Count * rowHeight);

        DrawRect(box, new Color(0f, 0f, 0f, 0.5f));
        ShadowLabel(new Rect(box.x + 10, box.y + 4, width, rowHeight), $"Ziel: {killsToWin} Kills", textStyle);

        for (int i = 0; i < players.Count; i++)
        {
            PlayerHealth p = players[i];
            string name = PlayerName(p.OwnerClientId) + (p.IsOwner ? " (Du)" : "");
            Rect row = new Rect(box.x + 10, box.y + 34 + i * rowHeight, width - 20, rowHeight);

            ShadowLabel(row, name, textStyle);
            ShadowLabel(new Rect(row.x + row.width - 40, row.y, 40, rowHeight), p.kills.Value.ToString(), textStyle);
        }
    }

    private void DrawDeathScreen()
    {
        DrawRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0.3f, 0f, 0f, 0.5f));

        float left = Mathf.Max(0f, playerHealth.RespawnDelay - (Time.time - playerHealth.DeathTime));

        ShadowLabel(new Rect(0, Screen.height / 2f - 60, Screen.width, 60), "Ausgeschieden!", bigStyle);
        ShadowLabel(new Rect(0, Screen.height / 2f, Screen.width, 60), $"Respawn in {left:0.0} s", bigStyle);
    }

    private void DrawGameOver()
    {
        GameManager gm = GameManager.Instance;
        DrawRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.6f));

        ulong winner = gm.winnerId.Value;
        string title = winner == OwnerClientId ? "Du hast gewonnen!" : $"{PlayerName(winner)} hat gewonnen!";

        float left = Mathf.Max(0f, (float)(gm.restartAt.Value - NetworkManager.ServerTime.Time));

        ShadowLabel(new Rect(0, Screen.height / 2f - 60, Screen.width, 60), title, bigStyle);
        ShadowLabel(new Rect(0, Screen.height / 2f, Screen.width, 60), $"Neue Runde in {left:0} s", bigStyle);
    }


    private void RefreshPlayers()
    {
        if (Time.time >= nextRefresh)
        {
            nextRefresh = Time.time + 0.5f;
            players.Clear();
            players.AddRange(FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None));
        }

        players.RemoveAll(p => p == null);   // falls jemand das Spiel verlassen hat
        players.Sort((a, b) => b.kills.Value.CompareTo(a.kills.Value));   // meiste Kills oben
    }

    private static void DrawRect(Rect rect, Color color)
    {
        Color old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = old;
    }

    private static void ShadowLabel(Rect rect, string text, GUIStyle style)
    {
        Color old = style.normal.textColor;
        style.normal.textColor = Color.black;
        GUI.Label(new Rect(rect.x + 2, rect.y + 2, rect.width, rect.height), text, style);
        style.normal.textColor = old;
        GUI.Label(rect, text, style);
    }
}