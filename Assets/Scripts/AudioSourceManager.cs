using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioSourceManager : MonoBehaviour
{
    public static AudioSourceManager Instance { get; private set; }
    public SoundPlayer soundPlayer;

    void Start()
    {
        Instance = this;
        PoolManager.Instance.InitPool(soundPlayer,20);
    }

    public void PlaySound(AudioClip audioClip,float pitchMin=1,float pitchMax=1)
    {
        PoolManager.Instance.
            GetInstance<SoundPlayer>(soundPlayer).PlayClip(audioClip,pitchMin,pitchMax);
    }
}
