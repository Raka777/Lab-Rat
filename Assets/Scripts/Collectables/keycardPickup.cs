using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class keycardPickup : MonoBehaviour, ICollectable
{
    public void Collect()
    {
        Debug.Log("Keycard Acquired");
        // Play Pickup Sound
        gameObject.SetActive(false);
        gameManager.instance.keycardAcquired = true;
    }
}