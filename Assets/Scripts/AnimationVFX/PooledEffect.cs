using UnityEngine;

public class PooledEffect : MonoBehaviour
{
    public EffectType type; // Sẽ được Manager gán khi Get
    [SerializeField] private float duration = 1.0f;

    void OnEnable() => Invoke(nameof(AutoReturn), duration);

    void AutoReturn()
    {
        EffectPoolManager.Instance.ReturnEffect(type, gameObject);
    }

    void OnDisable() => CancelInvoke();
}
