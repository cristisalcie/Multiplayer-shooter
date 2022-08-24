using UnityEngine;
using UnityEngine.UI;

public class HitVisualFogEffect : MonoBehaviour
{
    private RawImage fogImage;
    private const float fadeSpeed = 0.5f;

    private void Awake()
    {
        fogImage = GetComponent<RawImage>();
    }

    private void Update()
    {
        if (fogImage.color.a > 0)
        {
            Color _new = fogImage.color;
            _new.a = Mathf.Clamp(_new.a - fadeSpeed * Time.deltaTime, 0f, 1f);
            fogImage.color = _new;
        }
    }

    // TODO: [possible optimization]
    // TODO: Affect could start a coroutine (only if a boolean says it is not already started) looping inside of it while color has alfa.
    public void Affect(float _value)
    {
        Color _new = fogImage.color;
        _new.a = Mathf.Clamp(_new.a + _value, 0f, 1f);
        fogImage.color = _new;
    }
}
