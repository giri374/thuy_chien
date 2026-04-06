using Core.Models;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(SpriteRenderer))]
public class Cell : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Cell Info")]
    public Vector2Int gridPosition;
    public CellState cellState = CellState.Unknown;

    [Header("Visual")]
    public SpriteRenderer spriteRenderer;
    public Color normalColor = Color.white;
    public Color hoverColor = Color.yellow;
    public Color emptyColor = Color.gray;
    public Color hitColor = Color.red;

    [Header("State Sprites")]
    public Sprite emptySprite;
    public Sprite hitSprite;
    public Sprite previewSprite;
    public Sprite hintSprite;              // Shown by Radar weapon
    public Sprite antiAircraftSprite;      // Shown by Anti-Aircraft weapon

    [Header("Effects")]
    public GameObject hitAnimationPrefab;
    public GameObject missAnimationPrefab;

    [Header("Ship Reference")]
    public Ship occupyingShip;

    private GridManager gridManager;

    public void Initialize (Vector2Int position, GridManager manager)
    {
        gridPosition = position;
        gridManager = manager;
        spriteRenderer = GetComponent<SpriteRenderer>();
        ResetCell();
    }

    public void ResetCell ()
    {
        cellState = CellState.Unknown;
        occupyingShip = null;
        UpdateVisual();
    }

    /// <summary>
    /// Sets the cell state and updates visuals.
    /// Used by weapon systems to change cell state.
    /// </summary>
    public void SetCellState (CellState newState)
    {
        cellState = newState;
        UpdateVisual();
    }

    public void OnPointerClick (PointerEventData eventData)
    {
        // Nếu có tàu ở đây, cho tàu xử lý click trước
        if (occupyingShip != null && SceneManager.GetActiveScene().name == SceneNames.Setup)
        {
            // Forward event to ship's ShipPlacement component
            var shipPlacement = occupyingShip.GetComponent<ShipPlacement>();
            if (shipPlacement != null)
            {
                shipPlacement.OnPointerClick(eventData);
            }
            return;
        }

        // Nếu không có tàu, Grid xử lý bình thường
        gridManager.OnCellClicked(this);
    }

    public void OnBeginDrag (PointerEventData eventData)
    {
        if (SceneManager.GetActiveScene().name != SceneNames.Setup)
        {
            return; // Chỉ cho phép kéo thả trong SetupScene
        }
        // Forward drag events to ship's ShipPlacement component
        if (occupyingShip != null)
        {
            var shipPlacement = occupyingShip.GetComponent<ShipPlacement>();
            if (shipPlacement != null)
            {
                shipPlacement.OnBeginDrag(eventData);
            }
        }
    }

    public void OnDrag (PointerEventData eventData)
    {
        if (SceneManager.GetActiveScene().name != SceneNames.Setup)
        {
            return; // Chỉ cho phép kéo thả trong SetupScene
        }
        // Forward drag events to ship's ShipPlacement component
        if (occupyingShip != null)
        {
            var shipPlacement = occupyingShip.GetComponent<ShipPlacement>();
            if (shipPlacement != null)
            {
                shipPlacement.OnDrag(eventData);
            }
        }
    }

    public void OnEndDrag (PointerEventData eventData)
    {
        if (SceneManager.GetActiveScene().name != SceneNames.Setup)
        {
            return; // Chỉ cho phép kéo thả trong SetupScene
        }
        // Forward drag events to ship's ShipPlacement component
        if (occupyingShip != null)
        {
            var shipPlacement = occupyingShip.GetComponent<ShipPlacement>();
            if (shipPlacement != null)
            {
                shipPlacement.OnEndDrag(eventData);
            }
        }
    }

    public void OnPointerEnter (PointerEventData eventData)
    {
        // Check if we should show weapon preview instead of default preview
        if (BattleWeaponManager.Instance != null && gridManager != null)
        {
            var currentWeapon = BattleWeaponManager.Instance.GetCurrentWeapon();

            // Show preview for special weapons on opponent's grid only
            if (currentWeapon != WeaponType.NormalShot)
            {
                // Determine which grid is the opponent (the one being attacked)
                GridManager targetGrid = null;
                if (BattleSceneLogic.Instance != null)
                {
                    var currentTurn = BattleSceneLogic.Instance.currentTurn;
                    var isOwnGrid = (gridManager.isPlayer1Grid && currentTurn == Turn.Player1) ||
                                     (!gridManager.isPlayer1Grid && currentTurn == Turn.Player2);

                    // Only show preview on opponent's grid
                    if (!isOwnGrid)
                    {
                        targetGrid = gridManager;
                    }
                }

                if (targetGrid != null)
                {
                    BattleWeaponManager.Instance.ShowWeaponPreview(gridPosition, targetGrid, currentWeapon);
                    return;
                }
            }
        }

        // Default preview behavior for NormalShot or own grid
        spriteRenderer.sprite = previewSprite;
    }

    public void OnPointerExit (PointerEventData eventData)
    {
        // Check if weapon preview was shown
        if (BattleWeaponManager.Instance != null && gridManager != null)
        {
            var currentWeapon = BattleWeaponManager.Instance.GetCurrentWeapon();

            if (currentWeapon != WeaponType.NormalShot)
            {
                // Determine which grid is the opponent
                GridManager targetGrid = null;
                if (BattleSceneLogic.Instance != null)
                {
                    var currentTurn = BattleSceneLogic.Instance.currentTurn;
                    var isOwnGrid = (gridManager.isPlayer1Grid && currentTurn == Turn.Player1) ||
                                     (!gridManager.isPlayer1Grid && currentTurn == Turn.Player2);

                    if (!isOwnGrid)
                    {
                        targetGrid = gridManager;
                    }
                }

                if (targetGrid != null)
                {
                    BattleWeaponManager.Instance.HideWeaponPreview(gridManager);
                    return;
                }
            }
        }

        // Default behavior
        UpdateVisual();
    }

    public void UpdateVisual ()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        switch (cellState)
        {
            case CellState.Unknown:
                spriteRenderer.sprite = null;
                spriteRenderer.color = normalColor;
                break;

            case CellState.Empty:
                spriteRenderer.sprite = emptySprite;
                spriteRenderer.color = emptyColor;
                break;

            case CellState.Hit:
                spriteRenderer.sprite = hitSprite;
                spriteRenderer.color = hitColor;
                break;

            case CellState.Sunk:
                spriteRenderer.sprite = hitSprite;
                spriteRenderer.color = hitColor;
                break;

            case CellState.RadarHinted:
                spriteRenderer.sprite = hintSprite;
                spriteRenderer.color = normalColor;
                break;

            case CellState.AntiAircraftMarked:
                spriteRenderer.sprite = antiAircraftSprite;
                spriteRenderer.color = normalColor;
                break;
        }
    }

    public void SetOccupyingShip (Ship ship)
    {
        occupyingShip = ship;
    }

    public bool IsAvailable ()
    {
        return occupyingShip == null;
    }
}