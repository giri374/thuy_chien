using UnityEngine;

namespace Core.Models
{
    [System.Serializable]
    public struct CellData
    {
        public Vector2Int position;
        public CellState state;
        public int shipInstanceId;

        public CellData(Vector2Int pos, CellState cellState = CellState.Unknown, int shipId = -1)
        {
            position = pos;
            state = cellState;
            shipInstanceId = shipId;
        }

        public override string ToString()
        {
            return $"Cell({position}) State:{state} ShipId:{shipInstanceId}";
        }

        public bool HasShip => shipInstanceId != -1;
    }
}
