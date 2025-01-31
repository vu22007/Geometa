using Fusion;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
    public Vector2 moveDirection;
    public Vector2 aimDirection;
    public bool shoot;
    public bool reload;
}
