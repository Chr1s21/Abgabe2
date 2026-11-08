using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class NetworkPlayerSetup : NetworkBehaviour
{
    [SerializeField] private PlayerInput _playerInput;
    [SerializeField] private Camera _playerCamera;
    [SerializeField] private AudioListener _audioListener;
    [SerializeField] private FP_CharacterController _characterController;
    public override void OnNetworkSpawn()
    {
        _playerInput.enabled = IsOwner;
        _playerCamera.enabled = IsOwner;
        _audioListener.enabled = IsOwner;
        _characterController.enabled = IsOwner;

        if (IsOwner)
        {
            CharacterController cc = GetComponent<CharacterController>();
            cc.enabled = false;
            transform.position = SpawnPoint.GetRandomPosition((int)(OwnerClientId % 2));
            cc.enabled = true;
        }
    }
}
