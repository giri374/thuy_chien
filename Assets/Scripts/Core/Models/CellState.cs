namespace Core.Models
{
    public enum CellState
    {
        Unknown,
        Empty,
        Hit,
        Sunk,
        RadarHinted,           // Shown by Radar weapon - reveals ship location without attacking
        AntiAircraftMarked     // Marked by Anti-Aircraft weapon - blocks incoming attacks
    }
}
