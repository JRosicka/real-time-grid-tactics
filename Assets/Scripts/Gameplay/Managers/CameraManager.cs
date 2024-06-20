using System;
using UnityEngine;

/// <summary>
/// Handles camera movement/zoom
/// </summary>
public class CameraManager : MonoBehaviour {
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
    private float EdgeScrollThreshold => Screen.height * _edgeScrollNormalThreshold;
    private CameraDirection? _currentEdgeScrollDirection_horizontal;
    private CameraDirection? _currentEdgeScrollDirection_vertical;
    
    private bool InputAllowed => GameManager.Instance.GameSetupManager.InputAllowed;
    
    private Vector2? _middleMousePanStartPosition;

    private float _mapMinXBase, _mapMaxXBase, _mapMinYBase, _mapMaxYBase;
    private float MapMinX => _mapMinXBase - _boundaryBufferHorizontal;
    private float MapMaxX => _mapMaxXBase + _boundaryBufferHorizontal;
    private float MapMinY => _mapMinYBase - _boundaryBufferVertical - _additionalBoundaryBufferDown;
    private float MapMaxY => _mapMaxYBase + _boundaryBufferVertical;

    public void SetBoundaries(float boundaryLeft, float boundaryRight, float boundaryUp, float boundaryDown) {
        _mapMinXBase = boundaryLeft;
        _mapMaxXBase = boundaryRight;
        _mapMinYBase = boundaryDown;
        _mapMaxYBase = boundaryUp;
    }

    public void SetCameraStartPosition(Vector3 startPosition) {
        Vector3 cameraStartPosition = _camera.transform.position;
        Vector3 newPosition = ClampCamera(startPosition);
        newPosition.z = cameraStartPosition.z;
        _camera.transform.position = newPosition;
    }
    
    public void StartMiddleMousePan(Vector2 startMousePosition) {
        _middleMousePanStartPosition = startMousePosition;
    }

    public void StopMiddleMousePan() {
        _middleMousePanStartPosition = null;
    }

    public void MoveCameraOrthogonally(CameraDirection direction) {
        Vector2 moveVector = direction switch {
            CameraDirection.Left => Vector2.left,
            CameraDirection.Right => Vector2.right,
            CameraDirection.Up => Vector2.up,
            CameraDirection.Down => Vector2.down,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        };
        
        Vector3 difference = moveVector * Time.deltaTime * _cameraMoveSpeed;
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
        
        // Note - rather than updating here, we could check for edge scroll via GridInputController when the mouse moves. 
        // This would allow us to avoid triggering scroll when the mouse is over UI elements. Not sure if we want that. 
        Vector2 mousePosition = Input.mousePosition;
        
        // Middle mouse pan
        if (_middleMousePanStartPosition != null) {
            Vector2 difference = mousePosition - _middleMousePanStartPosition.Value;
            Vector3 moveVector = new Vector3(difference.x, difference.y, 0) * Time.deltaTime * _cameraPanSpeed;
            _camera.transform.position = ClampCamera(_camera.transform.position + moveVector);
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
        // Horizontal
        if (mouseScreenPosition.x < EdgeScrollThreshold) {
            _currentEdgeScrollDirection_horizontal = CameraDirection.Left;
        } else if (mouseScreenPosition.x > Screen.width - EdgeScrollThreshold) {
            _currentEdgeScrollDirection_horizontal = CameraDirection.Right;
        } else {
            _currentEdgeScrollDirection_horizontal = null;
        }
        
        // Vertical
        if (mouseScreenPosition.y < EdgeScrollThreshold) {
            _currentEdgeScrollDirection_vertical = CameraDirection.Down;
        } else if (mouseScreenPosition.y > Screen.height - EdgeScrollThreshold) {
            _currentEdgeScrollDirection_vertical = CameraDirection.Up;
        } else {
            _currentEdgeScrollDirection_vertical = null;
        }
    }
    
    private Vector3 ClampCamera(Vector3 targetPosition) {
        float cameraHeight = _camera.orthographicSize;
        float cameraWidth = cameraHeight * _camera.aspect;
        
        float minX = MapMinX + cameraWidth;
        float maxX = MapMaxX - cameraWidth;
        float minY = MapMinY + cameraHeight;
        float maxY = MapMaxY - cameraHeight;

        float newX = Mathf.Clamp(targetPosition.x, minX, maxX);
        float newY = Mathf.Clamp(targetPosition.y, minY, maxY);

        return new Vector3(newX, newY, targetPosition.z);
    }
}