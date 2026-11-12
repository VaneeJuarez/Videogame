using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// LLUVIA ESTELAR
/// Esquiva los meteoritos y recoge estrellas. La dificultad sube con el tiempo.
///
/// Todo el juego se construye por código: basta con abrir cualquier escena
/// (por ejemplo SampleScene) y presionar Play.
/// </summary>
public class GameManager : MonoBehaviour
{
    enum State { Menu, Playing, Paused, GameOver }

    public static GameManager Instance { get; private set; }

    const int StartingLives = 3;
    const string HighScoreKey = "LluviaEstelar_HighScore";

    public PlayerController Player { get; private set; }
    public bool IsPlaying => state == State.Playing;
    public Rect WorldBounds { get; private set; }

    State state = State.Menu;
    Camera cam;
    Transform objectsRoot;
    Sprite circleSprite, starSprite;
    Sprite[] meteorSprites;

    float score;
    int lives;
    int highScore;
    float elapsed;
    float spawnTimer;
    float starTimer;
    bool newRecord;

    // ---------- Arranque automático ----------
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Instance == null)
            new GameObject("GameManager").AddComponent<GameManager>();
    }

    void Awake()
    {
        Instance = this;
        highScore = PlayerPrefs.GetInt(HighScoreKey, 0);

        SetupCamera();
        circleSprite = SpriteFactory.Circle();
        starSprite = SpriteFactory.Star();
        meteorSprites = new Sprite[4];
        for (int i = 0; i < meteorSprites.Length; i++)
            meteorSprites[i] = SpriteFactory.Meteor(64, i * 31 + 7);

        CreateBackground();
        CreatePlayer();
        objectsRoot = new GameObject("Objetos").transform;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void SetupCamera()
    {
        cam = Camera.main;
        if (cam == null)
        {
            cam = new GameObject("Main Camera").AddComponent<Camera>();
            cam.tag = "MainCamera";
        }
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.transform.position = new Vector3(0, 0, -10);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.04f, 0.04f, 0.12f);
        UpdateBounds();
    }

    void UpdateBounds()
    {
        float h = cam.orthographicSize;
        float w = h * cam.aspect;
        WorldBounds = new Rect(-w, -h, w * 2f, h * 2f);
    }

    SpriteRenderer CreateSprite(string name, Sprite sprite, Color color, Vector3 pos, float scale, int order, Transform parent = null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = order;
        return sr;
    }

    void CreateBackground()
    {
        // Estrellas de fondo que se desplazan lentamente (efecto parallax)
        var bg = new GameObject("Fondo").transform;
        for (int i = 0; i < 80; i++)
        {
            float depth = Random.Range(0.2f, 1f);
            var pos = new Vector3(Random.Range(-12f, 12f), Random.Range(-6f, 6f), 0);
            var sr = CreateSprite("EstrellaFondo", circleSprite, new Color(1, 1, 1, depth * 0.8f), pos, 0.03f + depth * 0.05f, -10, bg);
            sr.gameObject.AddComponent<BackgroundStar>().speed = depth * 1.5f;
        }
    }

    void CreatePlayer()
    {
        var sr = CreateSprite("Jugador", SpriteFactory.Ship(), Color.white, new Vector3(0, -3.5f, 0), 0.9f, 5);
        Player = sr.gameObject.AddComponent<PlayerController>();
    }

    // ---------- Ciclo de juego ----------
    void StartGame()
    {
        foreach (Transform child in objectsRoot) Destroy(child.gameObject);
        score = 0;
        elapsed = 0;
        lives = StartingLives;
        spawnTimer = 0.5f;
        starTimer = 2f;
        newRecord = false;
        Player.transform.position = new Vector3(0, -3.5f, 0);
        Player.gameObject.SetActive(true);
        Player.MakeInvulnerable(1f);
        state = State.Playing;
    }

    void EndGame()
    {
        state = State.GameOver;
        Explode(Player.transform.position, new Color(1f, 0.5f, 0.3f), 30);
        Player.gameObject.SetActive(false);
        if ((int)score > highScore)
        {
            highScore = (int)score;
            newRecord = true;
            PlayerPrefs.SetInt(HighScoreKey, highScore);
            PlayerPrefs.Save();
        }
    }

    void Update()
    {
        UpdateBounds();
        var kb = Keyboard.current;
        var pad = Gamepad.current;
        bool confirm = (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
                       || (pad != null && pad.startButton.wasPressedThisFrame)
                       || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && state != State.Playing);
        bool pause = (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame))
                     || (pad != null && pad.selectButton.wasPressedThisFrame);

        switch (state)
        {
            case State.Menu:
            case State.GameOver:
                if (confirm) StartGame();
                break;
            case State.Paused:
                if (pause || confirm) state = State.Playing;
                break;
            case State.Playing:
                if (pause) { state = State.Paused; break; }
                UpdatePlaying();
                break;
        }
    }

    void UpdatePlaying()
    {
        elapsed += Time.deltaTime;
        score += Time.deltaTime * 10f; // 10 puntos por segundo sobrevivido

        // Dificultad: cada vez aparecen más rápido y caen más rápido
        float difficulty = 1f + elapsed / 20f;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0)
        {
            SpawnMeteor(difficulty);
            spawnTimer = Mathf.Max(0.18f, 0.9f / difficulty) * Random.Range(0.7f, 1.3f);
        }

        starTimer -= Time.deltaTime;
        if (starTimer <= 0)
        {
            SpawnStar();
            starTimer = Random.Range(1.5f, 3.5f);
        }
    }

    void SpawnMeteor(float difficulty)
    {
        float size = Random.Range(0.5f, 1.4f);
        var sr = CreateSprite("Meteorito", meteorSprites[Random.Range(0, meteorSprites.Length)], Color.white,
            RandomTopPosition(size), size, 1, objectsRoot);
        var fo = sr.gameObject.AddComponent<FallingObject>();
        fo.kind = FallingObject.Kind.Meteor;
        fo.fallSpeed = Random.Range(2.5f, 4.5f) * Mathf.Min(difficulty, 3f) / Mathf.Sqrt(size);
        fo.spinSpeed = Random.Range(-180f, 180f);
        fo.radius = size * 0.4f; // un poco menor que el dibujo para ser justo con el jugador
    }

    void SpawnStar()
    {
        var sr = CreateSprite("Estrella", starSprite, Color.white, RandomTopPosition(0.6f), 0.6f, 2, objectsRoot);
        var fo = sr.gameObject.AddComponent<FallingObject>();
        fo.kind = FallingObject.Kind.Star;
        fo.fallSpeed = Random.Range(2f, 3f);
        fo.spinSpeed = 90f;
        fo.radius = 0.35f;
    }

    Vector3 RandomTopPosition(float size)
    {
        var b = WorldBounds;
        return new Vector3(Random.Range(b.xMin + size * 0.5f, b.xMax - size * 0.5f), b.yMax + size, 0);
    }

    public void OnTouched(FallingObject obj)
    {
        if (obj.kind == FallingObject.Kind.Star)
        {
            score += 50;
            Explode(obj.transform.position, new Color(1f, 0.9f, 0.3f), 12);
            Destroy(obj.gameObject);
            return;
        }

        if (Player.IsInvulnerable) return;

        Explode(obj.transform.position, new Color(0.7f, 0.55f, 0.4f), 16);
        Destroy(obj.gameObject);
        lives--;
        if (lives <= 0) EndGame();
        else Player.MakeInvulnerable(1.5f);
    }

    void Explode(Vector3 pos, Color color, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var sr = CreateSprite("Particula", circleSprite, color, pos, Random.Range(0.08f, 0.18f), 10);
            var p = sr.gameObject.AddComponent<BurstParticle>();
            p.velocity = (Vector3)(Random.insideUnitCircle.normalized * Random.Range(2f, 6f));
            p.life = Random.Range(0.4f, 0.8f);
        }
    }

    // ---------- Interfaz (IMGUI, no requiere Canvas) ----------
    GUIStyle hudStyle, titleStyle, textStyle;

    void BuildStyles()
    {
        float k = Screen.height / 720f;
        hudStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(28 * k), fontStyle = FontStyle.Bold };
        hudStyle.normal.textColor = Color.white;
        titleStyle = new GUIStyle(hudStyle) { fontSize = Mathf.RoundToInt(72 * k), alignment = TextAnchor.MiddleCenter };
        titleStyle.normal.textColor = new Color(1f, 0.85f, 0.3f);
        textStyle = new GUIStyle(hudStyle) { fontSize = Mathf.RoundToInt(30 * k), alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Normal };
        textStyle.normal.textColor = Color.white;
    }

    void OnGUI()
    {
        BuildStyles();
        float w = Screen.width, h = Screen.height, pad = h * 0.02f;

        if (state == State.Playing || state == State.Paused)
        {
            GUI.Label(new Rect(pad, pad, w, h * 0.06f), $"Puntos: {(int)score}", hudStyle);
            GUI.Label(new Rect(pad, pad + h * 0.06f, w, h * 0.06f), $"Vidas: {Mathf.Max(lives, 0)}", hudStyle);
            var right = new GUIStyle(hudStyle) { alignment = TextAnchor.UpperRight };
            GUI.Label(new Rect(0, pad, w - pad, h * 0.06f), $"Récord: {highScore}", right);
        }

        Rect title = new Rect(0, h * 0.22f, w, h * 0.15f);
        Rect line1 = new Rect(0, h * 0.45f, w, h * 0.07f);
        Rect line2 = new Rect(0, h * 0.53f, w, h * 0.07f);
        Rect line3 = new Rect(0, h * 0.65f, w, h * 0.07f);

        switch (state)
        {
            case State.Menu:
                GUI.Label(title, "LLUVIA ESTELAR", titleStyle);
                GUI.Label(line1, "Esquiva los meteoritos y recoge estrellas (+50)", textStyle);
                GUI.Label(line2, "Muévete con Flechas / WASD   ·   Pausa: Esc o P", textStyle);
                GUI.Label(line3, Blink("Presiona ESPACIO para empezar"), textStyle);
                if (highScore > 0)
                    GUI.Label(new Rect(0, h * 0.75f, w, h * 0.07f), $"Récord: {highScore}", textStyle);
                break;
            case State.Paused:
                GUI.Label(title, "PAUSA", titleStyle);
                GUI.Label(line3, "Presiona Esc o ESPACIO para continuar", textStyle);
                break;
            case State.GameOver:
                GUI.Label(title, "FIN DEL JUEGO", titleStyle);
                GUI.Label(line1, $"Puntuación: {(int)score}", textStyle);
                GUI.Label(line2, newRecord ? "¡Nuevo récord!" : $"Récord: {highScore}", textStyle);
                GUI.Label(line3, Blink("Presiona ESPACIO para jugar otra vez"), textStyle);
                break;
        }
    }

    static string Blink(string text) => Mathf.Repeat(Time.unscaledTime, 1f) < 0.7f ? text : "";
}

/// <summary>Estrella decorativa del fondo que baja y reaparece arriba.</summary>
public class BackgroundStar : MonoBehaviour
{
    public float speed;

    void Update()
    {
        var p = transform.position;
        p.y -= speed * Time.deltaTime;
        if (p.y < -6f) { p.y = 6f; p.x = Random.Range(-12f, 12f); }
        transform.position = p;
    }
}
