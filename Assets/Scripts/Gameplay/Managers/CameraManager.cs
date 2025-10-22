using System;
using Rewired;
using UnityEngine;

/// <summary>
/// Handles camera movement/zoom
/// </summary>
public class CameraManager : MonoBehaviour {
    private const float MaxGameWindowAspectRatio = 1.77778f; // 16/9
    
    public enum CameraDirection {
        Left,
        Right,
        Up,
        Down
    }
    
    [SerializeField] private Camera _camera;
    [SerializeField] private float _cameraMoveSpeed;
    [SerializeField] private float _cameraPanSpeed;
    [SerializeField] private float _boundaryBufferHorizontal, _boundaryBufferVertical;
    [Tooltip("To account for the bottom-screen UI that would otherwise get in the way")]
    [SerializeField] private float _additionalBoundaryBufferDown;
    [SerializeField] [Range(0, .1f)] private float _edgeScrollNormalThreshold;
    [SerializeField] private RectTransform _gameWindow;
    [SerializeField] private RectTransform _cameraWindow;
    private float VerticalEdgeScrollThreshold => _gameWindow.rect.height * _edgeScrollNormalThreshold * _edgeScrollSensitivityMultiplier;
    private float HorizontalEdgeScrollThreshold => _gameWindow.rect.width * _edgeScrollNormalThreshold * _edgeScrollSensitivityMultiplier;
    private CameraDirection? _currentEdgeScrollDirection_horizontal;
    private CameraDirection? _currentEdgeScrollDirection_vertical;
    private bool _edgeScrollEnabled;
    private float _edgeScrollSpeedMultiplier;
    private float _edgeScrollSensitivityMultiplier;
    
    private float EdgeScrollSpeed => _cameraMoveSpeed * _edgeScrollSpeedMultiplier;
    
    private bool InputAllowed => GameManager.Instance.GameSetupManager.InputAllowed;
    
    private Vector2? _middleMouseDragLastPosition;

    private float _mapMinXBase, _mapMaxXBase, _mapMinYBase, _mapMaxYBase;
    private float MapMinX => _mapMinXBase - _boundaryBufferHorizontal;
    private float MapMaxX => _mapMaxXBase + _boundaryBufferHorizontal;
    private float MapMinY => _mapMinYBase - _boundaryBufferVertical - _additionalBoundaryBufferDown;
    private float MapMaxY => _mapMaxYBase + _boundaryBufferVertical;
    
    private Player _playerInput;

    public void Initialize(Vector3 startPosition, float boundaryLeft, float boundaryRight, float boundaryUp, float boundaryDown) {
        SetBoundaries(boundaryLeft, boundaryRight, boundaryUp, boundaryDown);
        SetCameraStartPosition(startPosition);
        _edgeScrollEnabled = PlayerPrefs.GetInt(PlayerPrefsKeys.EdgeScrollKey, 1) == 1;
        SetEdgeScrollSpeed(PlayerPrefs.GetInt(PlayerPrefsKeys.EdgeScrollSpeed, PlayerPrefsKeys.DefaultEdgeScrollSpeed));
        SetEdgeScrollSensitivity(PlayerPrefs.GetInt(PlayerPrefsKeys.EdgeScrollSensitivity, PlayerPrefsKeys.DefaultEdgeScrollSensitivity));
        
        _playerInput = ReInput.players.GetPlayer(0);
    }
    
    private void SetBoundaries(float boundaryLeft, float boundaryRight, float boundaryUp, float boundaryDown) {
        _mapMinXBase = boundaryLeft;
        _mapMaxXBase = boundaryRight;
        _mapMinYBase = boundaryDown;
        _mapMaxYBase = boundaryUp;
    }

    private void SetCameraStartPosition(Vector3 startPosition) {
        Vector3 cameraStartPosition = _camera.transform.position;
        Vector3 newPosition = ClampCamera(startPosition);
        newPosition.z = cameraStartPosition.z;
        _camera.transform.position = newPosition;
    }

    public void ToggleEdgeScroll(bool enable) {
        _edgeScrollEnabled = enable;
    }

    public void SetEdgeScrollSpeed(int newSpeed) {
        _edgeScrollSpeedMultiplier = newSpeed / 75f + .25f; // .25 to 1.75
    }

    public void SetEdgeScrollSensitivity(int newSensitivity) {
        _edgeScrollSensitivityMultiplier = newSensitivity / 75f + .25f; // .25 to 1.75
    }

    public void StartMiddleMousePan(Vector2 startMousePosition) {
        _middleMouseDragLastPosition = _camera.ScreenToWorldPoint(startMousePosition);
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
        _camera.transform.position = ClampCamera(_camera.transform.position + difference);
        
        // float scroll = Input.GetAxis("Mouse ScrollWheel");
        // if (Math.Abs(scroll) > 0.01f) {
        //     _camera.orthographicSize = Mathf.Clamp(_camera.orthographicSize - scroll * 2, 2, 10);
        // }
    }
    
    private void Update() {
        if (!InputAllowed) {
            StopMiddleMousePan();
            return;
        }

        if (_playerInput.GetButtonUp("MiddleMouse")) {
            StopMiddleMousePan();
            return;
        }
        
        // Note - rather than updating here, we could check for edge scroll via GridInputController when the mouse moves. 
        // This would allow us to avoid triggering scroll when the mouse is over UI elements. Not sure if we want that. 
        Vector2 mousePosition = Input.mousePosition;
        Vector2 mousePositionInWorldSpace = _camera.ScreenToWorldPoint(mousePosition);
        
        // Middle mouse drag
        if (_middleMouseDragLastPosition != null) {
            Vector2 difference = _middleMouseDragLastPosition.Value - mousePositionInWorldSpace;
            Vector3 moveVector = new Vector3(difference.x, difference.y, 0);
            _camera.transform.position = ClampCamera(_camera.transform.position + moveVector);
            
            // Now that the camera has moved, update the position
            _middleMouseDragLastPosition = _camera.ScreenToWorldPoint(Input.mousePosition);
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
        if (!_edgeScrollEnabled) {
            _currentEdgeScrollDirection_horizontal = null;
            _currentEdgeScrollDirection_vertical = null;
            return;
        }
        
        Vector2 mouseGameWindowPosition = mouseScreenPosition * _cameraWindow.rect.width / Screen.width;
        
        // Horizontal
        float gameWindowOffset = (_cameraWindow.rect.width - _gameWindow.rect.width) / 2f;
        float cameraWindowMouseX = mouseGameWindowPosition.x - gameWindowOffset;
        if (cameraWindowMouseX < HorizontalEdgeScrollThreshold) {
            _currentEdgeScrollDirection_horizontal = CameraDirection.Left;
        } else if (cameraWindowMouseX > _gameWindow.rect.width - HorizontalEdgeScrollThreshold) {
            _currentEdgeScrollDirection_horizontal = CameraDirection.Right;
        } else {
            _currentEdgeScrollDirection_horizontal = null;
        }
        
        // Vertical
        if (mouseGameWindowPosition.y < VerticalEdgeScrollThreshold) {
            _currentEdgeScrollDirection_vertical = CameraDirection.Down;
        } else if (mouseGameWindowPosition.y > _gameWindow.rect.height - VerticalEdgeScrollThreshold) {
            _currentEdgeScrollDirection_vertical = CameraDirection.Up;
        } else {
            _currentEdgeScrollDirection_vertical = null;
        }
    }
    
    private Vector3 ClampCamera(Vector3 targetPosition) {
        float cameraHeight = _camera.orthographicSize;
        // Make the camera as wide as needed, but don't go over the standard 16/9
        float cameraWidth = cameraHeight * Mathf.Min(_camera.aspect, MaxGameWindowAspectRatio);
        
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