using UnityEngine;

public class NailProjectileTrail : MonoBehaviour
{
    private TrailRenderer trail;
    private const float fadeSpeed = 1.6f;

    private void Awake()
    {
        trail = GetComponent<TrailRenderer>();
        trail.time = 10;  // seconds
    }

    private void Update()
    {
        Color _color = trail.material.color;
        _color.a = Mathf.Clamp(_color.a - Time.deltaTime * fadeSpeed, 0, 1);
        trail.material.color = _color;
        if (_color.a == 0)
        {
            trail.time = 0;
        }
    }
}
