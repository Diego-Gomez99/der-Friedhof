using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WalkSteps : MonoBehaviour
{

    private AudioSource myAudio;
    public AudioClip Ground, Concrete;
    public LayerMask GroundLayer, BrickLayer;
    public Transform checkSoundTransform;
    // Start is called before the first frame update
    void Start()
    {
        myAudio = GetComponent<AudioSource>();
    }

    void SoundWalk()
    {
        if (Physics.CheckSphere(checkSoundTransform.position, 1, GroundLayer))
        {
            myAudio.PlayOneShot(Ground, .5f);
            // print("Estoy pisando tierra");
        }
        else if(Physics.CheckSphere (checkSoundTransform.position, 1, BrickLayer))
        {
            myAudio.PlayOneShot(Concrete, .5f);
            // print("Estoy pisando concreto");
        }
    }
}
