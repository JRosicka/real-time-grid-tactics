using Gameplay.Entities;
using UnityEngine;

public class PathfinderService {

    private GridController GridController => GameManager.Instance.GridController;

    public int RequiredMoves(GridEntity entity, Vector2Int origin, Vector2Int destination) {
        Vector2Int pathVector = destination - origin;
        return Mathf.Abs(pathVector.x) + Mathf.Abs(pathVector.y);
    }

    public float AngleBetweenCells(Vector2Int cell1, Vector2Int cell2) {
        Vector2 cell1WorldPos = GridController.GetWorldPosition(cell1);
        Vector2 cell2WorldPos = GridController.GetWorldPosition(cell2);
        return Vector2.SignedAngle(Vector2.right, new Vector2(cell2WorldPos.x - cell1WorldPos.x, 
                                                        cell2WorldPos.y - cell1WorldPos.y));
    }
}