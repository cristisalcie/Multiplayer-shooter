using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// <summary> This class is not used for now. Maybe will be removed later </summary>
public class GameManager : MonoBehaviour
{
    public bool dontDestroyOnLoad = true;
    public static GameManager singleton { get; private set; }

    public virtual void Awake()
    {
        // Don't allow collision-destroyed second instance to continue.
        if (!InitializeSingleton()) return;
    }

    private bool InitializeSingleton()
    {
        if (singleton != null && singleton == this)
            return true;

        if (dontDestroyOnLoad)
        {
            if (singleton != null)
            {
                Debug.LogWarning("Multiple GameManagers detected in the scene. Only one GameManager can exist at a time. The duplicate GameManager will be destroyed.");
                Destroy(gameObject);

                // Return false to not allow collision-destroyed second instance to continue.
                return false;
            }
            Debug.Log("GameManager created singleton (DontDestroyOnLoad)");
            singleton = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
        }
        else
        {
            Debug.Log("GameManager created singleton (ForScene)");
            singleton = this;
        }

        return true;
    }
}
