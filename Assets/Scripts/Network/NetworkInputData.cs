using Fusion;
using UnityEngine;

enum InputButtons
{
    Shoot = 0,
    Reload = 1,
    PlaceShape = 2,
    Triangle = 3,
    Square = 4,
    Pentagon = 5,
}

public struct NetworkInputData : INetworkInput
{
    public NetworkButtons buttons;
    public Vector2 moveDirection;
    public Vector2 aimDirection;
    public Vector2 cursorWorldPoint;
}
