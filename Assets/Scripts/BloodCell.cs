using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BloodCell : Enemy
{
    public int addValue;
    public AudioClip audioClip;

    protected override void OnCollisionEnter(Collision collision)
    {
        base.OnCollisionEnter(collision);
        if (collision.transform.tag=="Player")
        {
            AudioSourceManager.Instance.PlaySound(audioClip);
            playerTrans.GetComponent<DoctorController>().TakeDamage(-addValue);
            gameObject.SetActive(false);
        }
    }

    public override void TakeDamage(float damageValue)
    {
        base.TakeDamage(damageValue);
        if (currentHealth<=0)
        {
            AudioSourceManager.Instance.PlaySound(audioClip);
            playerTrans.GetComponent<DoctorController>().TakeDamage(-addValue);
        }
    }
}
