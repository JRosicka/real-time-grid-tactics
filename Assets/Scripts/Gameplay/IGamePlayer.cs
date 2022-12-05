using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IGamePlayer {
    PlayerData Data { get; set; }
    string DisplayName { get; set; }
}
