using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowCamera : MonoBehaviour
{

    [SerializeField]
    private Transform m_PlayertoFollow;

    private Vector3 difference;

    // Start is called before the first frame update
    void Start()
    {
        difference = this.transform.position - m_PlayertoFollow.position;
    }

    // Update is called once per frame
    void Update()
    {
        this.transform.position = m_PlayertoFollow.position + difference;
    }
}
