using System;
using TMPro;
using UnityEngine;

namespace Gameplay.UI {
    /// <summary>
    /// Tips menu that displays gameplay tips
    /// </summary>
    public class TipsMenu : MonoBehaviour {
        [SerializeField] private GameObject[] _pages;
        [SerializeField] private GameObject _previousButton;
        [SerializeField] private GameObject _nextButton;
        [SerializeField] private TMP_Text _pageNumberText;
        [SerializeField] private string _pageFormat = "Page {0} / {1}";
        
        private int _currentPageIndex;
        private int TotalPages => _pages.Length;

        private Action _onDismiss;
        
        public void Open(Action onDismiss) {
            _onDismiss = onDismiss;
            
            gameObject.SetActive(true);
            _currentPageIndex = 0;
            _previousButton.SetActive(false);
            _nextButton.SetActive(true);
            _pages[_currentPageIndex].SetActive(true);
            
            SetPageNumberText();
        }

        public void Close() {
            gameObject.SetActive(false);
            _pages[_currentPageIndex].SetActive(false);
            _onDismiss?.Invoke();
            _onDismiss = null;
        }
        
        public void PreviousPage() {
            _pages[_currentPageIndex].SetActive(false);
            _currentPageIndex--;
            if (_currentPageIndex == 0) {
                _previousButton.SetActive(false);
            }
            
            _pages[_currentPageIndex].SetActive(true);
            _nextButton.SetActive(true);
            
            SetPageNumberText();
        }

        public void NextPage() {
            _pages[_currentPageIndex].SetActive(false);
            _currentPageIndex++;
            if (_currentPageIndex == TotalPages - 1) {
                _nextButton.SetActive(false);
            }
            
            _pages[_currentPageIndex].SetActive(true);
            _previousButton.SetActive(true);
            
            SetPageNumberText();
        }

        private void SetPageNumberText() {
            _pageNumberText.text = string.Format(_pageFormat, _currentPageIndex + 1, TotalPages);
        }
    }
}