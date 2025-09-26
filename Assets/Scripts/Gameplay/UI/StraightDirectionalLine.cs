using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI {
    /// <summary>
    /// A visual straight line between two cells, with customizable end point masks. Conceptually similar to
    /// <see cref="AbstractDirectionalLine"/>, but different implementation. 
    /// </summary>
    public class StraightDirectionalLine : MonoBehaviour {
        [SerializeField] private Transform _destination;
        [SerializeField] private Image _originMask;
        [SerializeField] private Image _destinationMask;
        [SerializeField] private RectTransform _lineRect;
        [SerializeField] private Image _lineImage;
        
        [SerializeField] private Image _targetAttackIcon;
        [SerializeField] private Image _attackMoveIcon;
        [SerializeField] private Image _moveIcon;
        
        [SerializeField] private Color _attackMoveColor;
        [SerializeField] private Color _targetAttackColor;
        [SerializeField] private Color _moveColor;
        
        [SerializeField] private GameObject _arrowTemplate;
        [SerializeField] private int _arrowPoolSize = 300;
        [SerializeField] private float _arrowSpacing;

        [SerializeField] private float _arrowSpeed;
        
        private readonly List<GameObject> _arrowPool = new List<GameObject>();
        private readonly List<GameObject> _activeArrows = new List<GameObject>();

        private Vector2 _origin;
        private Vector2 _target;

        public void Initialize() {
            // Populate arrow pool
            for (int i = 0; i < _arrowPoolSize; i++) {
                GameObject arrow = Instantiate(_arrowTemplate, _lineRect);
                arrow.SetActive(false);
                _arrowPool.Add(arrow);
            }
        }

        public void SetLine(Vector2 startLocation, Vector2 endLocation, PathVisualizer.PathType pathType) {
            transform.position = startLocation;
            _origin = startLocation;
            _target = endLocation;
            
            float angle = Vector2.SignedAngle(Vector2.right, endLocation - startLocation);
            transform.Rotate(0, 0, angle);
            _destination.position = endLocation;
            _destinationMask.transform.position = endLocation;
            
            // We want the masks to face the same direction as before since they have hex shapes
            _originMask.transform.Rotate(0, 0, -angle);
            _destinationMask.transform.Rotate(0, 0, -angle);
            _destination.transform.Rotate(0, 0, -angle);

            // Set the icon
            _targetAttackIcon.gameObject.SetActive(pathType == PathVisualizer.PathType.TargetAttack);
            _attackMoveIcon.gameObject.SetActive(pathType == PathVisualizer.PathType.AttackMove);
            _moveIcon.gameObject.SetActive(pathType == PathVisualizer.PathType.Move);
            
            // Stretch line out
            float distance = Vector2.Distance(startLocation, endLocation);
            _lineRect.sizeDelta = new Vector2(distance / transform.localScale.x, _lineRect.sizeDelta.y);
            
            // Space out arrows
            int arrowCount = Mathf.FloorToInt(100 * distance / _arrowSpacing);
            arrowCount = Mathf.Clamp(arrowCount, 0, _arrowPoolSize);
            float spacingPerArrow = 100 * distance / arrowCount;
            for (int i = 0; i < arrowCount; i++) {
                GameObject arrow = _arrowPool[i];
                arrow.SetActive(true);
                _activeArrows.Add(arrow);
                arrow.transform.localPosition = new Vector2(i * spacingPerArrow, arrow.transform.localPosition.y);
            }
            
            // Set colors
            Color color = pathType switch {
                PathVisualizer.PathType.Move => _moveColor,
                PathVisualizer.PathType.AttackMove => _attackMoveColor,
                PathVisualizer.PathType.TargetAttack => _targetAttackColor,
                _ => throw new NotImplementedException(),
            };
            _lineImage.color = color;
            
            gameObject.SetActive(true);
        }

        public void MaskOrigin() {
            _originMask.gameObject.SetActive(true);
        }

        public void MaskEnd() {
            _destinationMask.gameObject.SetActive(true);
            _targetAttackIcon.gameObject.SetActive(false);
            _attackMoveIcon.gameObject.SetActive(false);
            _moveIcon.gameObject.SetActive(false);
        }

        public void ClearLine() {
            transform.localRotation = Quaternion.identity;
            _originMask.gameObject.SetActive(false);
            _originMask.transform.localRotation = Quaternion.identity;
            _destinationMask.gameObject.SetActive(false);
            _destinationMask.transform.localRotation = Quaternion.identity;
            _destination.transform.localRotation = Quaternion.identity;
            
            _targetAttackIcon.gameObject.SetActive(false);
            _attackMoveIcon.gameObject.SetActive(false);
            _moveIcon.gameObject.SetActive(false);
            
            _activeArrows.ForEach(a => a.SetActive(false));
            _activeArrows.Clear();
            
            gameObject.SetActive(false);
        }

        private void Update() {
            if (_activeArrows.Count == 0) return;
            
            Vector3 distance = new Vector3(Time.deltaTime * _arrowSpeed, 0, 0);
            float rectWidth = _lineRect.sizeDelta.x;
            foreach (GameObject arrow in _activeArrows) {
                arrow.transform.localPosition += distance;
                if (arrow.transform.localPosition.x > rectWidth) {
                    // The arrow went past the end, so reset it to the start
                    arrow.transform.localPosition -= new Vector3(rectWidth, 0, 0);
                }
            }
        }
    }
}