using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum DirectionsEnum
{
    NORTH,
    SOUTH,
    EAST,
    WEST,
}

public class CameraController : MonoBehaviour
{
    public DirectionsEnum direction;
}
