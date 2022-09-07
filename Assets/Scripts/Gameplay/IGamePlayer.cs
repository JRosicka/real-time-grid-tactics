using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IGamePlayer {
    void SetIndex(int index);
    int GetIndex();
    
    void SetColor(Color color);
    Color GetColor();
}
