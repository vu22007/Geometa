using Fusion;
using UnityEngine;

enum InputButtons
{
    Shoot = 0,
    Reload = 1,
    TakeDamage = 2,
}

public struct NetworkInputData : INetworkInput
{
    public NetworkButtons buttons;
    public Vector2 moveDirection;
    public Vector2 aimDirection;
}
