using UnityEngine;

/// <summary>
/// Objeto que cae: un meteorito (quita vida) o una estrella (da puntos).
/// </summary>
public class FallingObject : MonoBehaviour
{
    public enum Kind { Meteor, Star }

    public Kind kind;
    public float fallSpeed;
    public float spinSpeed;
    public float radius;

    void Update()
    {
        var gm = GameManager.Instance;
        if (!gm.IsPlaying) return;

        transform.position += Vector3.down * fallSpeed * Time.deltaTime;
        transform.Rotate(0, 0, spinSpeed * Time.deltaTime);

        if (transform.position.y < gm.WorldBounds.yMin - 1.5f)
        {
            Destroy(gameObject);
            return;
        }

        var player = gm.Player;
        float hitDistance = radius + player.radius;
        if ((player.transform.position - transform.position).sqrMagnitude < hitDistance * hitDistance)
        {
            gm.OnTouched(this);
        }
    }
}

/// <summary>
/// Partícula sencilla para explosiones: sale disparada y se desvanece.
/// </summary>
public class BurstParticle : MonoBehaviour
{
    public Vector3 velocity;
    public float life = 0.6f;

    SpriteRenderer sr;
    float age;

    void Awake() => sr = GetComponent<SpriteRenderer>();

    void Update()
    {
        age += Time.deltaTime;
        transform.position += velocity * Time.deltaTime;
        velocity *= 1f - 2f * Time.deltaTime;
        var c = sr.color;
        c.a = 1f - age / life;
        sr.color = c;
        if (age >= life) Destroy(gameObject);
    }
}
