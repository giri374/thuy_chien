using UnityEngine;
using System.Collections.Generic;

namespace Core.Models
{
    [System.Serializable]
    public struct ShipInstanceData
    {
        public int shipId;
        public Vector2Int position;
        public bool isHorizontal;
        public int hitCount;
        public List<Vector2Int> occupiedCells;

        public ShipInstanceData(int id, Vector2Int pos, bool horizontal)
        {
            shipId = id;
            position = pos;
            isHorizontal = horizontal;
            hitCount = 0;
            occupiedCells = new List<Vector2Int>();
        }

        public bool IsSunk => hitCount >= occupiedCells.Count && occupiedCells.Count > 0;

        public override string ToString()
        {
            var direction = isHorizontal ? 'H' : 'V';
            return $"Ship(Id:{shipId}) Pos:{position} Dir:{direction} Hits:{hitCount}/{occupiedCells.Count}";
        }
    }
}
