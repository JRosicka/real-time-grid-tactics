using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class MPGamePlayer : NetworkBehaviour, IGamePlayer {
    [SyncVar] public int Index;
    [SyncVar] public Color Color;
    public override void OnStartLocalPlayer() { }
    public override void OnStopLocalPlayer() { }
    
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
