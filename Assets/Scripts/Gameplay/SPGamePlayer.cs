using UnityEngine;

public class SPGamePlayer : MonoBehaviour, IGamePlayer {
    public int Index;
    public Color Color;

    public void SetIndex(int index) {
        Index = index;
    }

    public int GetIndex() {
        return Index;
    }

    public void SetColor(Color color) {
        Color = color;
    }
    
    public Color GetColor() {
        return Color;
    }
}