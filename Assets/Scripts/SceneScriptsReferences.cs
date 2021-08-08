using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneScriptsReferences : MonoBehaviour
{
    // Networked objects will not be always found with "Find" which is why we use a sceneReference that is a monoBehaviour
    public SceneScript sceneScript;
    public MatchScript matchScript;
}
