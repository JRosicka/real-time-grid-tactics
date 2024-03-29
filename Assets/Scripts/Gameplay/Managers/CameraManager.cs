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
    [SerializeField] private float _mapMinX, _mapMaxX, _mapMinY, _mapMaxY;
    
    [SerializeField] [Range(0, .1f)] private float _edgeScrollNormalThreshold;
    private float EdgeScrollThreshold => Screen.height * _edgeScrollNormalThreshold;
    private CameraDirection? _currentEdgeScrollDirection_horizontal;
    private CameraDirection? _currentEdgeScrollDirection_vertical;

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
    
    private void Update() {
        // Note - rather than updating here, we could check for edge scroll via GridInputController when the mouse moves. 
        // This would allow us to avoid triggering scroll when the mouse is over UI elements. Not sure if we want that. 
        Vector2 mousePosition = Input.mousePosition;
        CheckForEdgeScroll(mousePosition);

        if (_currentEdgeScrollDirection_horizontal != null) {
            MoveCameraOrthogonally(_currentEdgeScrollDirection_horizontal.Value);
        }
        if (_currentEdgeScrollDirection_vertical != null) {
            MoveCameraOrthogonally(_currentEdgeScrollDirection_vertical.Value);
        }
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
    
    private Vector3 ClampCamera(Vector3 targetPosition) {
        float cameraHeight = _camera.orthographicSize;
        float cameraWidth = cameraHeight * _camera.aspect;
        
        float minX = _mapMinX + cameraWidth;
        float maxX = _mapMaxX - cameraWidth;
        float minY = _mapMinY + cameraHeight;
        float maxY = _mapMaxY - cameraHeight;

        float newX = Mathf.Clamp(targetPosition.x, minX, maxX);
        float newY = Mathf.Clamp(targetPosition.y, minY, maxY);

        return new Vector3(newX, newY, targetPosition.z);
    }
}