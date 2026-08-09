using System;
using System.Collections.Generic;
using Rewired;
using UnityEngine;

/// <summary>
/// Handles camera movement/zoom
/// </summary>
public class CameraManager : MonoBehaviour {
    private const float MaxGameWindowAspectRatio = 1.77778f; // 16/9
    private const float CameraZoomEpsilon = .01f;
    
    public enum CameraDirection {
        Left,
        Right,
        Up,
        Down
    }
    
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private Camera _innerCamera;
    [SerializeField] private float _cameraMoveSpeed;
    [SerializeField] private float _cameraPanSpeed;
    [SerializeField] private float _boundaryBufferHorizontal, _boundaryBufferVertical;
    [Tooltip("To account for the bottom-screen UI that would otherwise get in the way")]
    [SerializeField] private float _additionalBoundaryBufferDown;
    [SerializeField] private int _edgeScrollNormalThresholdPixels;
    [SerializeField] private RectTransform _gameWindow;
    [SerializeField] private RectTransform _cameraWindow;
    [SerializeField] private float _cameraZoomInLimit = 2f;
    [SerializeField] private float _cameraZoomOutLimit = 10f;
    [Tooltip("Controls acceleration/deceleration dampening and max zoom speed")]
    [SerializeField] private AnimationCurve _cameraZoomCurve;
    [SerializeField] private AnimationCurve _cameraZoomIncrementsCurve;
    [SerializeField] private int _zoomIncrements = 4;
    private int verticalEdgeScrollThresholdPixels => _edgeScrollNormalThresholdPixels;
    private int horizontalEdgeScrollThresholdPixels => _edgeScrollNormalThresholdPixels;
    private CameraDirection? _currentEdgeScrollDirection_horizontal;
    private CameraDirection? _currentEdgeScrollDirection_vertical;
    private bool _edgeScrollEnabled;
    private float _edgeScrollSpeedMultiplier;
    
    private float EdgeScrollSpeed => _cameraMoveSpeed * _edgeScrollSpeedMultiplier;
    
    private bool InputAllowed => GameManager.Instance.GameSetupManager.InputAllowed;
    
    private Vector2? _middleMouseDragLastPosition;

    private float _mapMinXBase, _mapMaxXBase, _mapMinYBase, _mapMaxYBase;
    private float MapMinX => _mapMinXBase - _boundaryBufferHorizontal;
    private float MapMaxX => _mapMaxXBase + _boundaryBufferHorizontal;
    private float MapMinY => _mapMinYBase - _boundaryBufferVertical - _additionalBoundaryBufferDown;
    private float MapMaxY => _mapMaxYBase + _boundaryBufferVertical;

    private float _targetOrthographicSize;
    private float _latestOrthographicSize;
    private float _orthographicSizeAtLastZoomOrigin;

    private Player _playerInput;

    public void Initialize(Vector3 startPosition, float boundaryLeft, float boundaryRight, float boundaryUp, float boundaryDown) {
        SetBoundaries(boundaryLeft, boundaryRight, boundaryUp, boundaryDown);
        SetCameraStartPosition(startPosition);
        _edgeScrollEnabled = PlayerPrefs.GetInt(PlayerPrefsKeys.EdgeScrollKey, 1) == 1;
        SetEdgeScrollSpeed(PlayerPrefs.GetInt(PlayerPrefsKeys.EdgeScrollSpeed, PlayerPrefsKeys.DefaultEdgeScrollSpeed));
        
        _playerInput = ReInput.players.GetPlayer(0);
        
        _targetOrthographicSize = _latestOrthographicSize = _orthographicSizeAtLastZoomOrigin = _cameraZoomOutLimit;
    }
    
    private void SetBoundaries(float boundaryLeft, float boundaryRight, float boundaryUp, float boundaryDown) {
        _mapMinXBase = boundaryLeft;
        _mapMaxXBase = boundaryRight;
        _mapMinYBase = boundaryDown;
        _mapMaxYBase = boundaryUp;
    }

    private void SetCameraStartPosition(Vector3 startPosition) {
        SnapToPosition(startPosition);
    }

    public void ToggleEdgeScroll(bool enable) {
        _edgeScrollEnabled = enable;
    }

    public void SetEdgeScrollSpeed(int newSpeed) {
        _edgeScrollSpeedMultiplier = newSpeed / 75f + .25f; // .25 to 1.75
    }
    
    public void StartMiddleMousePan(Vector2 startMousePosition) {
        _middleMouseDragLastPosition = _mainCamera.ScreenToWorldPoint(startMousePosition);
    }

    public void StopMiddleMousePan() {
        _middleMouseDragLastPosition = null;
    }

    public void MoveCameraOrthogonally(CameraDirection direction) {
        Vector2 moveVector = direction switch {
            CameraDirection.Left => Vector2.left,
            CameraDirection.Right => Vector2.right,
            CameraDirection.Up => Vector2.up,
            CameraDirection.Down => Vector2.down,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        };
        
        Vector3 difference = moveVector * Time.deltaTime * EdgeScrollSpeed;
        _mainCamera.transform.position = ClampCamera(_mainCamera.transform.position + difference);
    }

    public void SnapToPosition(Vector2 location) {
        Vector3 cameraStartPosition = _mainCamera.transform.position;
        Vector3 newPosition = ClampCamera(location);
        newPosition.z = cameraStartPosition.z;
        _mainCamera.transform.position = newPosition;
    }

    public void Zoom(bool zoomIn) {
        bool currentlyAdjustingZoom = Mathf.Abs(_latestOrthographicSize - _targetOrthographicSize) > 0;
        
        int zoomDirection = zoomIn ? 1 : -1;
        float zoomRange = _cameraZoomOutLimit - _cameraZoomInLimit;
        float normalizedCurrentTargetOrthographicSize = (_targetOrthographicSize - _cameraZoomInLimit) / zoomRange;
        float normalizedNewTargetOrthographicSize = normalizedCurrentTargetOrthographicSize - zoomDirection / (float)_zoomIncrements;
        float newTargetOrthographicSize = normalizedNewTargetOrthographicSize * zoomRange + _cameraZoomInLimit;
        _targetOrthographicSize = Mathf.Clamp(newTargetOrthographicSize, _cameraZoomInLimit, _cameraZoomOutLimit);
        
        // Only set origin zoom if we are not already moving so that we don't reset the zoom acceleration
        if (!currentlyAdjustingZoom) {
            _orthographicSizeAtLastZoomOrigin = _latestOrthographicSize;
        }
    }

    private void ApplyCameraZoom() {
        if (_mainCamera.orthographicSize - _targetOrthographicSize == 0) return;

        // Determine how fast the zoom should be this frame
        float distanceFromZoomStart = Mathf.Abs(_latestOrthographicSize - _orthographicSizeAtLastZoomOrigin);
        float distanceFromZoomEnd = Mathf.Abs(_latestOrthographicSize - _targetOrthographicSize);
        float delta = _cameraZoomCurve.Evaluate(Mathf.Min(distanceFromZoomStart, distanceFromZoomEnd));
        
        // Apply zoom
        float zoomDirection = Mathf.Sign(_targetOrthographicSize - _latestOrthographicSize);
        _latestOrthographicSize += zoomDirection * delta * Time.deltaTime;
        _latestOrthographicSize = Mathf.Clamp(_latestOrthographicSize, _cameraZoomInLimit, _cameraZoomOutLimit);
        
        // If we are very close to the target size, just snap to that
        if (Mathf.Abs(_latestOrthographicSize - _targetOrthographicSize) < CameraZoomEpsilon) {
            _latestOrthographicSize = _targetOrthographicSize;
        }
        
        foreach(Camera cam in new List<Camera> {_mainCamera, _innerCamera}) {
            cam.orthographicSize = _latestOrthographicSize;
        }

        ClampAndUpdateCamera();
    }
    
    private void Update() {
        if (!InputAllowed) {
            StopMiddleMousePan();
            return;
        }

        ApplyCameraZoom();

        if (_playerInput.GetButtonUp("MiddleMouse")) {
            StopMiddleMousePan();
            return;
        }
        
        // Note - rather than updating here, we could check for edge scroll via GridInputController when the mouse moves. 
        // This would allow us to avoid triggering scroll when the mouse is over UI elements. Not sure if we want that. 
        Vector2 mousePosition = Input.mousePosition;
        Vector2 mousePositionInWorldSpace = _mainCamera.ScreenToWorldPoint(mousePosition);
        
        // Middle mouse drag
        if (_middleMouseDragLastPosition != null) {
            Vector2 difference = _middleMouseDragLastPosition.Value - mousePositionInWorldSpace;
            Vector3 moveVector = new Vector3(difference.x, difference.y, 0);
            _mainCamera.transform.position = ClampCamera(_mainCamera.transform.position + moveVector);
            
            // Now that the camera has moved, update the position
            _middleMouseDragLastPosition = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        }
        
        // Edge scroll
        CheckForEdgeScroll(mousePosition);
        if (_currentEdgeScrollDirection_horizontal != null) {
            MoveCameraOrthogonally(_currentEdgeScrollDirection_horizontal.Value);
        }
        if (_currentEdgeScrollDirection_vertical != null) {
            MoveCameraOrthogonally(_currentEdgeScrollDirection_vertical.Value);
        }
    }

    private void CheckForEdgeScroll(Vector2 mouseScreenPosition) {
        if (!_edgeScrollEnabled || !Application.isFocused) {
            _currentEdgeScrollDirection_horizontal = null;
            _currentEdgeScrollDirection_vertical = null;
            return;
        }
        
        Vector2 mouseGameWindowPosition = mouseScreenPosition * _cameraWindow.rect.width / Screen.width;
        
        // Horizontal
        float gameWindowOffset = (_cameraWindow.rect.width - _gameWindow.rect.width) / 2f;
        float cameraWindowMouseX = mouseGameWindowPosition.x - gameWindowOffset;
        if (cameraWindowMouseX < horizontalEdgeScrollThresholdPixels) {
            _currentEdgeScrollDirection_horizontal = CameraDirection.Left;
        } else if (cameraWindowMouseX > _gameWindow.rect.width - horizontalEdgeScrollThresholdPixels) {
            _currentEdgeScrollDirection_horizontal = CameraDirection.Right;
        } else {
            _currentEdgeScrollDirection_horizontal = null;
        }
        
        // Vertical
        if (mouseGameWindowPosition.y < verticalEdgeScrollThresholdPixels) {
            _currentEdgeScrollDirection_vertical = CameraDirection.Down;
        } else if (mouseGameWindowPosition.y > _gameWindow.rect.height - verticalEdgeScrollThresholdPixels) {
            _currentEdgeScrollDirection_vertical = CameraDirection.Up;
        } else {
            _currentEdgeScrollDirection_vertical = null;
        }
    }

    private void ClampAndUpdateCamera() {
        _mainCamera.transform.position = ClampCamera(_mainCamera.transform.position);
    }
    
    private Vector3 ClampCamera(Vector3 targetPosition) {
        float cameraHeight = _mainCamera.orthographicSize;
        // Make the camera as wide as needed, but don't go over the standard 16/9
        float cameraWidth = cameraHeight * Mathf.Min(_mainCamera.aspect, MaxGameWindowAspectRatio);
        
        float minX = MapMinX + cameraWidth;
        float maxX = MapMaxX - cameraWidth;
        float minY = MapMinY + cameraHeight;
        float maxY = MapMaxY - cameraHeight;

        float newX;
        if (minX > maxX) {
            // The aspect ratio is too high. Just keep the x position in the middle of the min and max
            newX = (MapMinX + MapMaxX) / 2;
        } else {
            newX = Mathf.Clamp(targetPosition.x, minX, maxX);
        }
        float newY = Mathf.Clamp(targetPosition.y, minY, maxY);

        return new Vector3(newX, newY, targetPosition.z);
    }
}