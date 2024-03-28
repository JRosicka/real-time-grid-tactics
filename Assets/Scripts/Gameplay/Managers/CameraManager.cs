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
    
    public void CheckForEdgeScroll(Vector2 mouseScreenPosition) {
        
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