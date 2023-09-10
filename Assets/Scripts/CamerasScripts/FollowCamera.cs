using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowCamera : MonoBehaviour
{

    [SerializeField]
    private Transform m_PlayertoFollow;

    // Update is called once per frame
    void LateUpdate()
    {
        this.transform.position = m_PlayertoFollow.position + new Vector3(0, 8, -4);
    }
}
