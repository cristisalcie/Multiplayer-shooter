using UnityEngine;
using UnityEngine.UI;

public class CrosshairUI : MonoBehaviour
{
    [SerializeField]
    private GameObject crosshair;
    private Transform dotTransform;
    private RawImage dotImage;
    private GameObject xFrame;
    private RectTransform frameRectTransform;

    private Color dotInitialColor;
    private RectTransform canvasRectTransform;
    private Vector2 middleOfScreen;

    public Weapon activeWeapon;  // Needs to be public to have it set by PlayerShoot script

    public Vector3 targetPosition;

    public GameObject hitboxParent;  // Needs to be public to have it set by PlayerShoot script

    private void Awake()
    {
        canvasRectTransform = GetComponent<RectTransform>();
        middleOfScreen = canvasRectTransform.sizeDelta * canvasRectTransform.localScale / 2;
        dotTransform = crosshair.transform.Find("Dot");
        dotImage = dotTransform.GetComponent<RawImage>();
        xFrame = crosshair.transform.Find("X").gameObject;
        xFrame.SetActive(false);
        frameRectTransform = crosshair.transform.Find("Frame").GetComponent<RectTransform>();
        dotInitialColor = dotImage.color;
        activeWeapon = null;
    }

    private void FixedUpdate()
    {
        if (activeWeapon == null) { return; }

        Vector2 _newPosition = CalculateNewCanvasPosition();

        if (dotTransform.position.x != _newPosition.x || dotTransform.position.y != _newPosition.y)  // Position changed
        {
            UpdateUI(_newPosition);
        }
    }

    private void OnRectTransformDimensionsChange()
    {
        if (canvasRectTransform != null)
        {
            middleOfScreen = canvasRectTransform.sizeDelta * canvasRectTransform.localScale / 2;
        }
    }

    private Vector2 CalculateNewCanvasPosition()
    {
        Vector2 _newPosition = middleOfScreen;
        targetPosition = Vector3.zero;

        // Raycast from camera straight in the middle of the screen
        hitboxParent.SetActive(false);
        bool _cameraHasHit = Physics.Raycast(
                Camera.main.transform.position,
                Camera.main.transform.forward,
                out RaycastHit _cameraHitInfo,
                activeWeapon.range);
        hitboxParent.SetActive(true);
        {
            //Debug.DrawRay(
            //    Camera.main.transform.position,
            //    Camera.main.transform.forward * activeWeapon.weaponRange,
            //    Color.yellow,
            //    3f);
        }
        if (_cameraHasHit)
        {
            targetPosition = _cameraHitInfo.point;  // Wanted position to convert to UI space

            // Following linecast is for the case when camera hits the ground but the weapon hits an object
            bool _weaponHasHit = Physics.Linecast(
                activeWeapon.fireLocationTransform.position,
                _cameraHitInfo.point,
                out RaycastHit _weaponHitInfo);
            {
                //Debug.DrawLine(
                //    activeWeapon.weaponFireTransform.position,
                //    _cameraHitInfo.point,
                //    Color.red,
                //    3f);
            }

            if (_weaponHasHit)
            {
                targetPosition = _weaponHitInfo.point;
            }
            _newPosition = WorldToCanvasPosition(targetPosition);
        }
        else
        {
            // Raycast from weapon fire transform towards middle of the screen (camera)
            bool _weaponHasHit = Physics.Raycast(
                activeWeapon.fireLocationTransform.position,
                Camera.main.transform.forward,
                out RaycastHit _weaponHitInfo,
                activeWeapon.range);
            {
                //Debug.DrawRay(
                //    activeWeapon.weaponFireTransform.position,
                //    Camera.main.transform.forward,
                //    Color.red,
                //    3f);
            }

            if (_weaponHasHit)
            {
                targetPosition = _weaponHitInfo.point;
                _newPosition = WorldToCanvasPosition(targetPosition);
            }
        }

        if (Mathf.Abs(middleOfScreen.x - _newPosition.x) <= frameRectTransform.sizeDelta.x
            && Mathf.Abs(middleOfScreen.y - _newPosition.y) <= frameRectTransform.sizeDelta.y)  // Hasn't exceeded minimum position difference threshold
        {
            _newPosition = middleOfScreen;
        }

        return _newPosition;
    }
    
    private Vector2 WorldToCanvasPosition(Vector3 _targetPosition)
    {
        /* Vector position (percentage from 0 to 1) considering camera size.
         * For example (0,0) is lower left, middle is (0.5,0.5), upper right is (1.0, 1.0) */
        var _newPosition = Camera.main.WorldToViewportPoint(_targetPosition);

        // Calculate new position considering our percentage, using our canvasRectTransform size and scale
        _newPosition.x *= canvasRectTransform.sizeDelta.x * canvasRectTransform.localScale.x;
        _newPosition.y *= canvasRectTransform.sizeDelta.y * canvasRectTransform.localScale.x;

        return _newPosition;
    }

    public void UpdateUI(Vector2 _position)
    {
        dotImage.color = (_position == middleOfScreen) ? dotInitialColor : Color.red;
        dotTransform.position = new Vector3(_position.x, _position.y, 0.0f);
    }

    public void DisplayX(bool _allowShooting)
    {
        xFrame.SetActive(!_allowShooting);
    }
}
