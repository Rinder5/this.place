﻿using System;
using System.Collections;
using UnityEngine;

public class CameraController : MonoBehaviour
{

    public GameObject PlayerObject;
    public const float StartingXOffset = 0;
    public const float StartingXOffset2 = -6;
    public const float StartingXOffset3 = 0;
    public const float StartingXOffset4 = 6;
    public const float StartingYOffset = 3;
    public const float StartingZOffset = -6;
    public const float StartingZOffset2 = 0;
    public const float StartingZOffset3 = 6;
    public const float StartingZOffset4 = 0;
    public const float StartingXRotation = 30;
    public const float StartingYRotation = 0;
    public const float StartingYRotation2 = 90;
    public const float StartingYRotation3 = 180;
    public const float StartingYRotation4 = 270;
    public const float StartingZRotation = 0;

    //used for translation
    private Vector3 _offset;

    //used for rotation
    private bool _isKeyboardRotating;
    private const float KeyboardSpeed = 380f;
    private const float MouseXSpeed = 3f;
    private const float MouseYSpeed = 1f;
    private float _currentYDisplacement = -20f;
    private const float MaxYDisplacement = 60f;
    private PlayerController _playerController;
    private ArrayList _fadeBlocks = new ArrayList();
    private bool _idle = false;

    //used for auto-rotate
    private const float IdleRotateSpeed = 0.3f;
    private const float repositionMultiplier = 10f;
    private float currentAutoRotate = 0f;

    //used for zoom
    private const float MaxZoom = 12f;
    private const float MinZoom = 0.8f;
    private const float zoomSpeed = 6;
    private const float joystickZoomSpeedFactor = 0.02f;
    private const float joystickMaxSpeed = 1;
    private float zoomSpeedDistance = 6.5f;
    private BoxCollider _playerCollider;

    //used for autozoom
    private const float autoZoomSpeed = 0.04f;
    private float currentAutoZoomValue = 0;
    private float _zoomLeeway = 5;
    private float _currentZoomDelay = 0;
    public float ZoomDelay = 0.2f;
    private bool _isZooming = false;

    //used for camera shake
    private Rigidbody _playerRb;
    private Vector3 _playerVelocity = Vector3.zero;
    private float shakeThreshold = 4;
    private float maxShakeOffset = 0.05f;
    private float shakeOffsetSpeed = 0.01f;
    private float currentShakeOffset = 0;
    private bool shakingUp = true;
    private bool shaking = false;

    private void Awake()
    {
        _offset = new Vector3(StartingXOffset, StartingYOffset, StartingZOffset);
        transform.eulerAngles = new Vector3(StartingXRotation, StartingYRotation, StartingZRotation);
    }

    private void Start()
    {
        _playerController = PlayerObject.GetComponent<PlayerController>();
        _playerCollider = PlayerObject.GetComponent<BoxCollider>();
        _playerRb = PlayerObject.GetComponent<Rigidbody>();
        _playerController.UpdateCamera();
    }

    private void Update()
    {
        Vector3 newPosition = PlayerObject.transform.position + _offset;
        transform.position = newPosition;
        if (!_idle)
        {
            RotateMouseCamera();
            if (Input.GetAxis("CameraZoom") > 0 || Input.GetAxis("CameraZoom") < 0)
            {
                ZoomCamera(-Input.GetAxis("CameraZoom"));
            }
            else if (Input.GetAxis("CameraZoomJoystick") > 0 || Input.GetAxis("CameraZoomJoystick") < 0)
            {
                if (Math.Abs(Input.GetAxis("CameraZoomJoystick")) < joystickMaxSpeed)
                    ZoomCamera(-Input.GetAxis("CameraZoomJoystick") * joystickZoomSpeedFactor);
                else if (Input.GetAxis("CameraZoomJoystick") > 0)
                    ZoomCamera(-joystickMaxSpeed * joystickZoomSpeedFactor);
                else
                    ZoomCamera(joystickMaxSpeed * joystickZoomSpeedFactor);
            }
            else
            {
                AutoZoom();
            }

            if (currentAutoRotate > 0)
            {
                float rotateAmount = IdleRotateSpeed * repositionMultiplier;
                if (currentAutoRotate > 180)
                    rotateAmount *= -1;
                currentAutoRotate -= rotateAmount;
                if (currentAutoRotate < 0 || currentAutoRotate > 360)
                {
                    rotateAmount += currentAutoRotate;
                    currentAutoRotate = 0;
                }
                RotateHorizontal(rotateAmount);
            }
        }
        else
        {
            RotateHorizontal(-IdleRotateSpeed);
            currentAutoRotate += IdleRotateSpeed;
            if (currentAutoRotate > 360)
            {
                currentAutoRotate -= 360;
            }
        }

        UpdateFadingBlocks();

        UpdateScreenShake();
    }

    private void UpdateScreenShake()
    {
        Vector3 currentVelocity = _playerRb.velocity;
        if (currentVelocity.y - _playerVelocity.y > shakeThreshold && _playerVelocity.y < 0)
        {
            shakingUp = true;
            shaking = true;
        }
        _playerVelocity = currentVelocity;
        ScreenShake();
    }

    private void ScreenShake()
    {
        if (shaking)
        {
            if (shakingUp)
            {
                if (currentShakeOffset < maxShakeOffset)
                {
                    transform.position = new Vector3(transform.position.x, transform.position.y + shakeOffsetSpeed, transform.position.z);
                    currentShakeOffset += shakeOffsetSpeed;
                }
                else
                {
                    shakingUp = false;
                }
            }
            else
            {
                if (currentShakeOffset > 0)
                {
                    transform.position = new Vector3(transform.position.x, transform.position.y - shakeOffsetSpeed, transform.position.z);
                    currentShakeOffset -= shakeOffsetSpeed;
                }
                else
                {
                    shaking = false;
                }
            }

            _offset = transform.position - PlayerObject.transform.position;
        }
    }

    private Vector2 CalculateScreenSizeInWorldCoords() {
        Camera cam = Camera.main;
        Vector3 p1 = cam.ViewportToWorldPoint(new Vector3(0, 0, cam.nearClipPlane));
        Vector3 p2 = cam.ViewportToWorldPoint(new Vector3(1, 0, cam.nearClipPlane));
        Vector3 p3 = cam.ViewportToWorldPoint(new Vector3(1, 1, cam.nearClipPlane));

        float width = (p2 - p1).magnitude;
        float height = (p3 - p2).magnitude;

        Vector2 dimensions = new Vector2(width, height);

        return dimensions;
    }

    private bool IsPlayerObstructed(float distance)
    {
        Camera cameraObject = Camera.main;
        Vector2 dimensions = CalculateScreenSizeInWorldCoords();
        Vector3 playerTarget = PlayerObject.transform.position + Vector3.up * _playerCollider.bounds.extents.y;
        Vector3 centrePositionDifference = transform.position - playerTarget;

        if (Physics.Raycast(playerTarget, (centrePositionDifference + Vector3.left * (dimensions.x / 2) + Vector3.up * (dimensions.y / 2)), distance))
            return true;
        if (Physics.Raycast(playerTarget, (centrePositionDifference + Vector3.left * (dimensions.x / 2) - Vector3.up * (dimensions.y / 2)), distance))
            return true;
        if (Physics.Raycast(playerTarget, (centrePositionDifference - Vector3.left * (dimensions.x / 2) - Vector3.up * (dimensions.y / 2)), distance))
            return true;
        if (Physics.Raycast(playerTarget, (centrePositionDifference - Vector3.left * (dimensions.x / 2) + Vector3.up * (dimensions.y / 2)), distance))
            return true;
        if (Physics.Raycast(playerTarget, centrePositionDifference, distance))
            return true;

        return false;
    }

    private void AutoZoom()
    {
        float zoomAmount = autoZoomSpeed * (_offset.magnitude / zoomSpeedDistance);
        bool isActionTried = false;
        if (currentAutoZoomValue < 0 &&
            !IsPlayerObstructed(_offset.magnitude + zoomAmount * _zoomLeeway))
        {
            isActionTried = true;
            if (_isZooming || _currentZoomDelay > ZoomDelay)
            {
                _isZooming = true;
                _currentZoomDelay = 0;
                if (ZoomCamera(zoomAmount))
                {
                    currentAutoZoomValue += zoomAmount;
                }
                else
                {
                    _isZooming = false;
                }
            }
            else
            {
                _currentZoomDelay += Time.deltaTime;
            }
        }
        if(IsPlayerObstructed(_offset.magnitude))
        {
            isActionTried = true;
            if (_isZooming || _currentZoomDelay > ZoomDelay)
            {
                _isZooming = true;
                _currentZoomDelay = 0;
                if (ZoomCamera(-zoomAmount))
                {
                    currentAutoZoomValue -= zoomAmount;
                }
                else
                {
                    _isZooming = false;
                }
            }
            else
            {
                _currentZoomDelay += Time.deltaTime;
            }
        } 
        if(!isActionTried)
        {
            _currentZoomDelay = 0;
            _isZooming = false;
        }
    }

    //returns true if zoom happened, false if not
    private bool ZoomCamera(float zoomAmount)
    {
        float zoomDifference = zoomSpeed * zoomAmount;
        _offset = transform.position - PlayerObject.transform.position + Vector3.Normalize(transform.position - PlayerObject.transform.position) * zoomDifference;
        if (_offset.magnitude > MaxZoom || _offset.magnitude < MinZoom)
        {
            _offset = transform.position - PlayerObject.transform.position;
            return false;
        }
        return true;
    }

    private void UpdateFadingBlocks()
    {
        Vector3 cameraPosition = transform.position;
        Vector3 cameraToPlayerPosition = cameraPosition - PlayerObject.transform.position;
        cameraToPlayerPosition.y = 0;
        foreach (FadeoutPlugin fadeoutPlugin in _fadeBlocks)
        {
            if (fadeoutPlugin.GetBlock() == null) continue;
            Vector3 cameraToBlockPosition =
                cameraPosition - fadeoutPlugin.GetBlock().transform.position;
            cameraToBlockPosition.y = 0;
            fadeoutPlugin.SetIsFading(cameraToBlockPosition.magnitude < cameraToPlayerPosition.magnitude);
        }
    }

    public void AddFadeoutTarget(FadeoutPlugin fadeoutPlugin)
    {
        _fadeBlocks.Add(fadeoutPlugin);
    }

    public void RemoveFadeoutTarget(FadeoutPlugin fadeoutPlugin)
    {
        _fadeBlocks.Remove(fadeoutPlugin);
    }

    private void RotateMouseCamera()
    {
        float rotateXAmount = (Input.GetAxis("Mouse X")) * MouseXSpeed;
        float rotateYAmount = (Input.GetAxis("Mouse Y")) * MouseYSpeed;
        _currentYDisplacement += rotateYAmount;
        if (_currentYDisplacement > MaxYDisplacement)
        {
            rotateYAmount -= _currentYDisplacement - MaxYDisplacement;
            _currentYDisplacement = MaxYDisplacement;
        }
        else if (_currentYDisplacement < -MaxYDisplacement)
        {
            rotateYAmount -= _currentYDisplacement + MaxYDisplacement;
            _currentYDisplacement = -MaxYDisplacement;
        }
        transform.RotateAround(PlayerObject.transform.position, Vector3.up, rotateXAmount);
        Vector3 normalVector = GetNormalVector();
        transform.RotateAround(PlayerObject.transform.position, normalVector, rotateYAmount);
        
        _playerController.UpdateCamera();
        _offset = transform.position - PlayerObject.transform.position;
    }

    public void RotateHorizontal(float rotateAmount)
    {
        transform.RotateAround(PlayerObject.transform.position, Vector3.up, rotateAmount);

        _playerController.UpdateCamera();
        _offset = transform.position - PlayerObject.transform.position;
    }

    private void RotateVertical(float rotateAmount)
    {
        Vector3 normalVector = GetNormalVector();
        transform.RotateAround(PlayerObject.transform.position, normalVector, rotateAmount);

        _playerController.UpdateCamera();
        _offset = transform.position - PlayerObject.transform.position;
    }

    private Vector3 GetNormalVector()
    {
        Vector3 playerFacing = transform.position - PlayerObject.transform.position;
        Vector3 planeCompletion = playerFacing - Vector3.up;
        return Vector3.Cross(playerFacing, planeCompletion);
    }

    public void SetIdle(bool idle)
    {
        _idle = idle;
    }

    public void ResetCameraAngle()
    {
        currentAutoRotate = 0;
        _currentYDisplacement = 0;
        _offset = new Vector3(StartingXOffset, StartingYOffset, StartingZOffset);
        transform.eulerAngles = new Vector3(StartingXRotation, StartingYRotation, StartingZRotation);
        currentAutoZoomValue = 0;
    }

    public void ResetCameraAngle(float offsetAngle)
    {
        if (offsetAngle == 0)
            ResetCameraAngle();
        else if (offsetAngle == 90)
        {
            currentAutoRotate = 0;
            _currentYDisplacement = 0;
            _offset = new Vector3(StartingXOffset2, StartingYOffset, StartingZOffset2);
            transform.eulerAngles = new Vector3(StartingXRotation, StartingYRotation2, StartingZRotation);
            currentAutoZoomValue = 0;
        }
        else if (offsetAngle == 180)
        {
            currentAutoRotate = 0;
            _currentYDisplacement = 0;
            _offset = new Vector3(StartingXOffset3, StartingYOffset, StartingZOffset3);
            transform.eulerAngles = new Vector3(StartingXRotation, StartingYRotation3, StartingZRotation);
            currentAutoZoomValue = 0;
        }
        else if (offsetAngle == 270)
        {
            currentAutoRotate = 0;
            _currentYDisplacement = 0;
            _offset = new Vector3(StartingXOffset4, StartingYOffset, StartingZOffset4);
            transform.eulerAngles = new Vector3(StartingXRotation, StartingYRotation4, StartingZRotation);
            currentAutoZoomValue = 0;
        }
    }
}
