using UnityEngine;

public class GameSession
{
    public Board player1Board { get; private set; }
    public Board player2Board { get; private set; }
    public int currentPlayer { get; private set; }
    public bool isGameOver { get; private set; }
    public int winnerPlayer { get; private set; }

    public GameSession()
    {
        player1Board = new Board();
        player2Board = new Board();
        currentPlayer = 1;
        isGameOver = false;
        winnerPlayer = 0;
    }

    public bool IsSetupComplete(int player)
    {
        var board = GetBoard(player);
        return board.GetShipCount() >= 5;
    }

    public AttackResult Attack(Vector2Int position)
    {
        var enemyBoard = currentPlayer == 1 ? player2Board : player1Board;
        var wasHit = enemyBoard.Attack(position);
        var cell = enemyBoard.GetCell(position);

        return new AttackResult
        {
            position = position,
            isHit = wasHit,
            isSunk = cell.HasShip ? enemyBoard.GetShip(cell.shipInstanceId)?.IsSunk ?? false : false,
            isGameOver = enemyBoard.AllShipsSunk()
        };
    }

    public void EndTurn()
    {
        currentPlayer = currentPlayer == 1 ? 2 : 1;
    }

    public bool CheckGameOver()
    {
        if (player1Board.AllShipsSunk())
        {
            isGameOver = true;
            winnerPlayer = 2;
            return true;
        }

        if (player2Board.AllShipsSunk())
        {
            isGameOver = true;
            winnerPlayer = 1;
            return true;
        }

        return false;
    }

    public Board GetBoard(int player)
    {
        return player == 1 ? player1Board : player == 2 ? player2Board : null;
    }

    public Board GetEnemyBoard()
    {
        return currentPlayer == 1 ? player2Board : player1Board;
    }

    public void Reset()
    {
        player1Board = new Board();
        player2Board = new Board();
        currentPlayer = 1;
        isGameOver = false;
        winnerPlayer = 0;
    }
}

[System.Serializable]
public struct AttackResult
{
    public Vector2Int position;
    public bool isHit;
    public bool isSunk;
    public bool isGameOver;

    public override string ToString()
    {
        return $"Attack({position}) Hit:{isHit} Sunk:{isSunk} GameOver:{isGameOver}";
    }
}
