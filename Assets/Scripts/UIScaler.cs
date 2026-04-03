using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles applying user-defined scale setting to UI components
/// </summary>
public class UIScaler : MonoBehaviour {
    [SerializeField] private List<Transform> _scalableTransforms;
    [SerializeField] private bool _inGame;
    [SerializeField] private float _scaleFactor = .005f;
    [SerializeField] private float _scaleAddition = .5f;

    public void Initialize() {
        SetScale(PlayerPrefs.GetInt(_inGame 
            ? PlayerPrefsKeys.UIScaleInGame 
            : PlayerPrefsKeys.UIScaleMenu, PlayerPrefsKeys.DefaultUIScale));
    }
    
    public void SetScale(int scale) {
        float trueScale = scale * _scaleFactor + _scaleAddition;
        _scalableTransforms.ForEach(t => t.localScale = new Vector3(trueScale, trueScale, trueScale));
    }
}