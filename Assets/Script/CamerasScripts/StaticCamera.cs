using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StaticCamera : MonoBehaviour
{
  
    [SerializeField]
    private Transform m_PlayertoFollow;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (m_PlayertoFollow != null)
        {
            transform.LookAt(m_PlayertoFollow.position);
        }
    }
}
