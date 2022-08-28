using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class GamePlayerController : NetworkBehaviour {
    [SyncVar] public int Index;
    [SyncVar] public Color Color;
    public override void OnStartLocalPlayer() { }
    public override void OnStopLocalPlayer() { }

}
