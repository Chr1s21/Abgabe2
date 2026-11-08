using System.Collections.Generic;
using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    private static readonly List<SpawnPoint> all = new List<SpawnPoint>();

    [SerializeField] private int side = 0;                 // 0 = eine Seite, 1 = andere Seite
    [SerializeField] private float blockedRadius = 1.5f;   // so nah darf kein anderer Spieler stehen

    private void OnEnable() => all.Add(this);
    private void OnDisable() => all.Remove(this);

    public static Vector3 GetRandomPosition(int side)
    {
        // Nur Spawnpunkte der gewünschten Seite.
        List<SpawnPoint> candidates = new List<SpawnPoint>();
        foreach (SpawnPoint point in all)
        {
            if (point.side == side) candidates.Add(point);
        }

        if (candidates.Count == 0)
        {
            Debug.LogWarning($"Keine SpawnPoints für Seite {side} in der Szene!");
            return new Vector3(0f, 1.1f, 0f);
        }

        // Mischen und den ersten freien nehmen.
        for (int i = 0; i < candidates.Count; i++)
        {
            int j = Random.Range(i, candidates.Count);
            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
        }

        foreach (SpawnPoint point in candidates)
        {
            if (!point.IsBlocked()) return point.transform.position;
        }

        // Alle besetzt: dann eben einen zufälligen.
        return candidates[0].transform.position;
    }

    private bool IsBlocked()
    {
        foreach (Collider col in Physics.OverlapSphere(transform.position, blockedRadius))
        {
            if (col.GetComponentInParent<PlayerHealth>() != null) return true;
        }
        return false;
    }

    // Im Scene-Fenster sichtbar machen: Seite 0 blau, Seite 1 rot.
    private void OnDrawGizmos()
    {
        Gizmos.color = side == 0 ? Color.blue : Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward);
    }
}