using UnityEngine;

// Stands in for the old UNet NetworkManager, which used to spawn the player
// prefab when hosting. The field name matches NetworkManager's so the scene's
// existing player prefab reference carries over.
public class LocalPlayerSpawner : MonoBehaviour {
    [SerializeField] GameObject m_PlayerPrefab;

    void Start () {
        // NetworkManager spawned at the origin when the scene had no NetworkStartPosition.
        Instantiate(m_PlayerPrefab, Vector3.zero, Quaternion.identity);
    }
}
