using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public enum GameState { Playing, Win, Lose }
public enum BonusType { Attack, Defend }

public class BonusItem
{
    public GameObject visual;
    public BonusType type;
}

[RequireComponent(typeof(GridManager), typeof(UIManager))]
public class GameManager : MonoBehaviour
{
    [Header("Levels (leave empty to use the 3 built-in levels)")]
    public LevelData[] levels;

    [Header("Units")]
    public UnitStats miraStats = new UnitStats("Mira", 24, 4, 4, 2, 8);
    public UnitStats seedStats = new UnitStats("The Seed", 10, 0, 0, 0, 0);
    public UnitStats capcapStats = new UnitStats("Capcap", 8, 3, 3, 1, 0);
    public UnitStats sporelingStats = new UnitStats("Sporeling", 5, 3, 2, 3, 0);

    [Header("Real-Time Speed & Settings")]
    public float miraMoveDelay = 0.15f;
    public float enemyActionInterval = 3.5f;
    public float seedGrowthInterval = 4.5f;
    public int mendCost = 2;
    public int mendAmount = 4;

    [Header("Bonus Pickups (Level 2+)")]
    public int bonusPickupsCount = 4;   // 2 për Attack, 2 për Defend
    public int bonusAttackGain = 2;     // Sa +Attack jep bonusi i artë
    public float shieldDuration = 6.0f; // Sa sekonda e mbron mburoja farën

    GridManager grid;
    UIManager ui;

    GameState state;
    bool miraBusy;
    int levelIndex;
    int bonusAttack;
    int shieldCharges;      // Sa mburoja (Defend) ka mbledhur Mira
    float activeShieldTime; // Koha e mbetur kur aktivizohet mburoja rreth farës
    int lastSeedHP;
    float growthTimer;
    LevelData level;

    Transform unitRoot;
    PlayerUnit mira;
    SeedUnit seed;
    GameObject seedShieldVisual;
    readonly List<EnemyUnit> enemies = new List<EnemyUnit>();
    readonly Dictionary<Tile, BonusItem> bonusPickups = new Dictionary<Tile, BonusItem>();

    void Awake()
    {
        grid = GetComponent<GridManager>();
        ui = GetComponent<UIManager>();
        if (levels == null || levels.Length == 0) levels = LevelData.CreateBuiltIn();
    }

    void Start()
    {
        ui.Build();
        ui.SetPortrait(miraStats.sprite != null ? miraStats.sprite : IsoArt.Mira());
        ui.AttackPressed += OnAttack;
        ui.SkillPressed += OnSkill;
        grid.TileClicked += OnTileClicked;
        LoadLevel(0);
    }

    void OnDestroy()
    {
        if (grid != null) grid.TileClicked -= OnTileClicked;
    }

    void LoadLevel(int index)
    {
        StopAllCoroutines();
        miraBusy = false;
        growthTimer = 0f;
        bonusAttack = 0;
        shieldCharges = 0;
        activeShieldTime = 0f;
        levelIndex = index;
        level = levels[index];
        state = GameState.Playing;

        if (unitRoot != null) Destroy(unitRoot.gameObject);
        enemies.Clear();
        bonusPickups.Clear();

        ui.HideResult();
        grid.Build(level);
        grid.SetSelected(null);
        ui.ShowTileInfo(null);

        unitRoot = new GameObject("Units").transform;
        mira = Spawn<PlayerUnit>(miraStats, level.miraStart, null);
        seed = Spawn<SeedUnit>(seedStats, level.seedPos, null);
        seed.SetMaxStage(level.seedStagesNeeded);
        lastSeedHP = seed.HP;

        CreateSeedShieldVisual();

        mira.Changed += RefreshUI;
        seed.Changed += OnSeedChanged;

        foreach (var e in level.enemies)
        {
            var enemy = SpawnEnemy(e.kind, e.pos);
            StartCoroutine(EnemyRealTimeLoop(enemy));
        }

        // Nga Niveli 2 e lart (index >= 1), shfaq bonuse Attack dhe Defend
        if (levelIndex >= 1)
        {
            SpawnBonusPickups(bonusPickupsCount);
        }

        ui.SetObjective(level.objective);
        RefreshUI();

        if (levelIndex >= 1)
            ui.ShowMessage($"Niveli {levelIndex + 1}: Merr Arin (+Attack) & Blunë (+Defend, shtyp Q)!");
        else
            ui.ShowMessage("WASD: Lëviz | SPACE: Sulmo | E: Shëro Farën");
    }

    // =============== MBUROJA RRETH FARËS (DEFEND) ===============
    void CreateSeedShieldVisual()
    {
        seedShieldVisual = new GameObject("SeedShieldBubble");
        seedShieldVisual.transform.SetParent(seed.transform, false);
        seedShieldVisual.transform.localPosition = new Vector3(0f, 0.25f, -0.2f);
        seedShieldVisual.transform.localScale = Vector3.one * 1.45f;

        var sr = seedShieldVisual.AddComponent<SpriteRenderer>();
        sr.sprite = CreateCircleSprite(48, 20f);
        sr.color = new Color(0.2f, 0.75f, 1f, 0.45f); // Blu e tejdukshme
        sr.sortingOrder = 25;
        seedShieldVisual.SetActive(false);
    }

    void OnSeedChanged()
    {
        // Nëse mburoja është aktive rreth farës, ajo bllokon dëmin e kërpudhave!
        if (activeShieldTime > 0f && seed != null && seed.HP < lastSeedHP)
        {
            int blockedDamage = lastSeedHP - seed.HP;
            seed.Heal(blockedDamage); // Kthen menjëherë HP-në e humbur
        }
        if (seed != null) lastSeedHP = seed.HP;
        RefreshUI();
    }

    void ActivateDefendShield()
    {
        if (shieldCharges <= 0)
        {
            ui.ShowMessage("Nuk ke mburojë! Merr orbin blu në hartë.");
            return;
        }
        if (activeShieldTime > 0f)
        {
            ui.ShowMessage("Mburoja e farës është tashmë aktive!");
            return;
        }

        shieldCharges--;
        activeShieldTime = shieldDuration;
        if (seedShieldVisual != null) seedShieldVisual.SetActive(true);
        ui.ShowMessage($"Mburoja e Farës u aktivizua për {shieldDuration} sekonda! (Mbeten: {shieldCharges})");
    }

    // =============== BONUS PICKUPS (ATTACK & DEFEND) ===============
    void SpawnBonusPickups(int count)
    {
        List<Tile> candidates = new List<Tile>();
        for (int y = 0; y < grid.Height; y++)
        {
            for (int x = 0; x < grid.Width; x++)
            {
                var t = grid.GetTile(x, y);
                if (t != null && t.Walkable && t.Occupant == null && !bonusPickups.ContainsKey(t))
                {
                    candidates.Add(t);
                }
            }
        }

        for (int i = 0; i < count && candidates.Count > 0; i++)
        {
            int rnd = Random.Range(0, candidates.Count);
            Tile chosen = candidates[rnd];
            candidates.RemoveAt(rnd);

            // Çift = Attack (E artë), Tek = Defend (Blu)
            BonusType type = (i % 2 == 0) ? BonusType.Attack : BonusType.Defend;

            GameObject orb = new GameObject(type == BonusType.Attack ? "BonusAttack" : "BonusDefend");
            orb.transform.SetParent(unitRoot, false);
            orb.transform.position = chosen.transform.position + new Vector3(0f, 0.25f, -0.1f);
            orb.transform.localScale = Vector3.one * 0.65f;

            var sr = orb.AddComponent<SpriteRenderer>();
            sr.sprite = CreateCircleSprite(32, 11f);
            sr.color = (type == BonusType.Attack)
                ? new Color(1f, 0.85f, 0.1f, 1f)   // E artë për Attack
                : new Color(0.2f, 0.7f, 1f, 1f);   // Blu për Defend
            sr.sortingOrder = chosen.GetComponent<SpriteRenderer>() != null
                ? chosen.GetComponent<SpriteRenderer>().sortingOrder + 2
                : 10;

            bonusPickups[chosen] = new BonusItem { visual = orb, type = type };
        }
    }

    Sprite CreateCircleSprite(int size, float radius)
    {
        Texture2D tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Point;
        Vector2 center = new Vector2(size / 2f, size / 2f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                tex.SetPixel(x, y, dist <= radius ? Color.white : Color.clear);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
    }

    void CheckBonusPickup(Tile tile)
    {
        if (tile != null && bonusPickups.TryGetValue(tile, out BonusItem item))
        {
            bonusPickups.Remove(tile);
            if (item.visual != null) Destroy(item.visual);

            if (item.type == BonusType.Attack)
            {
                bonusAttack += bonusAttackGain;
                int totalAtk = mira.Stats.attack + bonusAttack;
                ui.ShowMessage($"Morët Bonus Sulmi! Attack u rrit në {totalAtk} (+{bonusAttackGain})!");
            }
            else
            {
                shieldCharges++;
                ui.ShowMessage($"Morët Mburojë Mbrojtëse ({shieldCharges})! Shtyp Q për ta mbrojtur farën!");
            }
        }
    }

    T Spawn<T>(UnitStats stats, Vector2Int pos, System.Action<T> beforeInit) where T : Unit
    {
        var go = new GameObject(stats.unitName);
        go.transform.SetParent(unitRoot, false);
        var u = go.AddComponent<T>();
        beforeInit?.Invoke(u);
        u.Init(stats, grid, FreeTileNear(grid.GetTile(pos)));
        return u;
    }

    EnemyUnit SpawnEnemy(EnemyKind kind, Vector2Int pos)
    {
        var stats = kind == EnemyKind.Capcap ? capcapStats : sporelingStats;
        var e = Spawn<EnemyUnit>(stats, pos, u => u.Kind = kind);
        e.Died += OnUnitDied;
        enemies.Add(e);
        return e;
    }

    Tile FreeTileNear(Tile wanted)
    {
        if (wanted != null && wanted.Walkable && wanted.Occupant == null) return wanted;
        Tile best = null; int bestD = int.MaxValue;
        for (int y = 0; y < grid.Height; y++)
            for (int x = 0; x < grid.Width; x++)
            {
                var t = grid.GetTile(x, y);
                if (!t.Walkable || t.Occupant != null) continue;
                int d = wanted == null ? 0 : Mathf.Abs(t.X - wanted.X) + Mathf.Abs(t.Y - wanted.Y);
                if (d < bestD) { bestD = d; best = t; }
            }
        return best;
    }

    void OnUnitDied(Unit u)
    {
        var e = u as EnemyUnit;
        if (e != null) enemies.Remove(e);
        CheckEnd();
    }

    void RefreshUI()
    {
        if (mira == null || seed == null) return;
        ui.SetMira(mira.HP, mira.MaxHP, mira.MP, mira.MaxMP);
        ui.SetTurn(levelIndex + 1);
        ui.SetSeed(seed.Stage, seed.MaxStage);
        bool playing = state == GameState.Playing && mira.IsAlive;
        ui.SetButtons(false, playing && enemies.Count > 0, playing && mira.MP >= mendCost, false, false, 0);
    }

    void OnTileClicked(Tile tile)
    {
        grid.SetSelected(tile);
        ui.ShowTileInfo(tile);
    }

    // =============== KONTROLLET ME TASTIERË (REAL-TIME) ===============
    void Update()
    {
        if (state != GameState.Playing || mira == null || !mira.IsAlive) return;

        // Numërimi mbrapsht i mburojës së farës
        if (activeShieldTime > 0f)
        {
            activeShieldTime -= Time.deltaTime;
            if (activeShieldTime <= 0f)
            {
                activeShieldTime = 0f;
                if (seedShieldVisual != null) seedShieldVisual.SetActive(false);
                ui.ShowMessage("Mburoja e farës përfundoi!");
            }
        }

        UpdateSeedGrowth();

        // Shtyp Q për të aktivizuar Mburojën e Farës (Defend)
        if (IsDefendKeyPressed())
        {
            ActivateDefendShield();
        }

        if (IsAttackKeyPressed() && !miraBusy)
        {
            OnAttack();
            return;
        }

        if (IsSkillKeyPressed() && !miraBusy)
        {
            OnSkill();
            return;
        }

        if (!miraBusy)
        {
            Vector2Int dir = GetContinuousMoveDirection();
            if (dir != Vector2Int.zero && mira.Tile != null)
            {
                Tile nextTile = grid.GetTile(mira.Tile.X + dir.x, mira.Tile.Y + dir.y);
                if (nextTile != null && nextTile.Walkable && nextTile.Occupant == null)
                {
                    StartCoroutine(DoFreeMove(nextTile));
                }
            }
        }
    }

    void UpdateSeedGrowth()
    {
        bool blocked = false;
        foreach (var e in enemies)
            if (e != null && e.IsAlive && e.DistanceTo(seed) == 1) blocked = true;

        // Nëse mburoja e farës është aktive, fara rritet edhe nëse ka kërpudha pranë!
        if (!blocked || activeShieldTime > 0f)
        {
            growthTimer += Time.deltaTime;
            if (growthTimer >= seedGrowthInterval)
            {
                growthTimer = 0f;
                seed.Grow();
                ui.ShowMessage("Fara po rritet!");
                RefreshUI();
                CheckEnd();
            }
        }
    }

    IEnumerator DoFreeMove(Tile dest)
    {
        miraBusy = true;
        var path = new List<Tile> { mira.Tile, dest };
        yield return mira.MoveAlong(path);
        CheckBonusPickup(mira.Tile);
        grid.SetSelected(mira.Tile);
        ui.ShowTileInfo(mira.Tile);
        yield return new WaitForSeconds(miraMoveDelay);
        miraBusy = false;
    }

    void OnAttack()
    {
        if (state != GameState.Playing || miraBusy || !mira.IsAlive) return;

        EnemyUnit closest = null;
        int bestDist = int.MaxValue;
        foreach (var e in enemies)
        {
            if (e != null && e.IsAlive)
            {
                int d = mira.DistanceTo(e);
                if (d < bestDist)
                {
                    bestDist = d;
                    closest = e;
                }
            }
        }

        if (closest != null)
            StartCoroutine(DoAttack(closest));
    }

    IEnumerator DoAttack(EnemyUnit target)
    {
        miraBusy = true;
        yield return mira.Lunge(target.Tile);
        if (target != null && target.IsAlive)
            target.TakeDamage(mira.Stats.attack + bonusAttack);
        yield return new WaitForSeconds(0.25f);
        miraBusy = false;
        RefreshUI();
        CheckEnd();
    }

    void OnSkill()
    {
        if (state != GameState.Playing || miraBusy || !mira.IsAlive) return;
        if (mira.MP < mendCost || seed.HP >= seed.MaxHP) return;
        StartCoroutine(DoMend());
    }

    IEnumerator DoMend()
    {
        miraBusy = true;
        yield return mira.Lunge(seed.Tile);
        mira.SpendMP(mendCost);
        seed.Heal(mendAmount);
        lastSeedHP = seed.HP;
        yield return new WaitForSeconds(0.25f);
        miraBusy = false;
        RefreshUI();
    }

    // =============== AI E KËRPUDHAVE NË KOHË REALE ===============
    IEnumerator EnemyRealTimeLoop(EnemyUnit enemy)
    {
       yield return new WaitForSeconds(Random.Range(1.5f, 2.5f));

        while (state == GameState.Playing && enemy != null && enemy.IsAlive)
        {
            var targets = new List<Unit>();
            if (mira != null && mira.IsAlive) targets.Add(mira);
            if (seed != null && seed.IsAlive) targets.Add(seed);

            if (targets.Count > 0)
            {
                yield return enemy.TakeTurn(grid, targets);
                if (CheckEnd()) yield break;
            }

            yield return new WaitForSeconds(enemyActionInterval);
        }
    }

    // =============== WIN / LOSE ===============
    bool CheckEnd()
    {
        if (state == GameState.Win || state == GameState.Lose) return true;
        if (mira == null || !mira.IsAlive || seed == null || !seed.IsAlive) { Lose(); return true; }
        if (seed.Stage >= seed.MaxStage || enemies.Count == 0) { Win(); return true; }
        return false;
    }

    void Win()
    {
        state = GameState.Win;
        bool last = levelIndex >= levels.Length - 1;
        string sub = seed.Stage >= seed.MaxStage ? "The seed has bloomed!" : "All the mushrooms are gone.";
        if (last) ui.ShowResult("You saved the forest!", sub, "Play again", () => LoadLevel(0));
        else ui.ShowResult("Victory!", sub, "Next Level", () => LoadLevel(levelIndex + 1));
    }

    void Lose()
    {
        state = GameState.Lose;
        string why = (seed == null || !seed.IsAlive) ? "The seed was blighted." : "Mira has fallen.";
        ui.ShowResult("Defeat", why, "Restart", () => LoadLevel(levelIndex));
    }

    // =============== LEXIMI I TASTIERËS ===============
    Vector2Int GetContinuousMoveDirection()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null) return Vector2Int.zero;
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) return Vector2Int.up;
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) return Vector2Int.down;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) return Vector2Int.left;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) return Vector2Int.right;
        return Vector2Int.zero;
#else
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) return Vector2Int.up;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) return Vector2Int.down;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) return Vector2Int.left;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) return Vector2Int.right;
        return Vector2Int.zero;
#endif
    }

    bool IsAttackKeyPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.fKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.F);
#endif
    }

    bool IsSkillKeyPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.E);
#endif
    }

    bool IsDefendKeyPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Q);
#endif
    }
}