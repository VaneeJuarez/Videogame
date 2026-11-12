using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Nave del jugador: se mueve con flechas / WASD / gamepad y queda dentro de la pantalla.
/// </summary>
public class PlayerController : MonoBehaviour
{
    public float speed = 9f;
    public float radius = 0.35f;

    SpriteRenderer sr;
    float invulnerableUntil;

    public bool IsInvulnerable => Time.time < invulnerableUntil;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public void MakeInvulnerable(float seconds)
    {
        invulnerableUntil = Time.time + seconds;
    }

    void Update()
    {
        // Parpadeo mientras es invulnerable
        sr.enabled = !IsInvulnerable || Mathf.Repeat(Time.time * 10f, 1f) > 0.5f;

        if (!GameManager.Instance.IsPlaying) return;

        Vector2 input = ReadInput();
        Vector3 pos = transform.position + (Vector3)(input * speed * Time.deltaTime);

        Rect b = GameManager.Instance.WorldBounds;
        pos.x = Mathf.Clamp(pos.x, b.xMin + 0.5f, b.xMax - 0.5f);
        pos.y = Mathf.Clamp(pos.y, b.yMin + 0.5f, b.yMin + b.height * 0.4f);
        transform.position = pos;

        // Ligera inclinación al moverse de lado
        transform.rotation = Quaternion.Euler(0, 0, -input.x * 15f);
    }

    static Vector2 ReadInput()
    {
        Vector2 dir = Vector2.zero;
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.leftArrowKey.isPressed || kb.aKey.isPressed) dir.x -= 1;
            if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) dir.x += 1;
            if (kb.downArrowKey.isPressed || kb.sKey.isPressed) dir.y -= 1;
            if (kb.upArrowKey.isPressed || kb.wKey.isPressed) dir.y += 1;
        }
        var pad = Gamepad.current;
        if (pad != null) dir += pad.leftStick.ReadValue();
        return Vector2.ClampMagnitude(dir, 1f);
    }
}
