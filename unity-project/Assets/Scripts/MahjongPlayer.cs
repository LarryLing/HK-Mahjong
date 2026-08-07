using Unity.Netcode;
using UnityEngine;

public enum Wind
{
    East,
    South,
    West,
    North,
}

public class MahjongPlayer : NetworkBehaviour
{
    private Camera mainCamera;

    public NetworkVariable<int> assignedFlowerNumber;
    public NetworkVariable<Wind> assignedWind;

    public override void OnNetworkSpawn()
    {
        mainCamera = Camera.main;
    }
}
