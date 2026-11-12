using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BGM(정규/전투 전환)과 전투/보상 SFX를 담당합니다.
/// 기존 EventBus 이벤트를 구독하여 동작하므로 다른 스크립트 수정이 최소화됩니다.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("BGM")]
    [SerializeField] private AudioClip bgmNormal;
    [SerializeField] private AudioClip bgmBattle;
    [SerializeField] private float bgmVolume = 0.5f;

    [Header("SFX (배열에서 무작위 선택)")]
    [SerializeField] private AudioClip[] playerAttack;
    [SerializeField] private AudioClip[] enemyAttack;
    [SerializeField] private AudioClip[] playerHit;
    [SerializeField] private AudioClip[] enemyHit;
    [SerializeField] private AudioClip[] playerDeath;
    [SerializeField] private AudioClip[] enemyDeath;
    [SerializeField] private AudioClip[] chestOpen;

    [Header("SFX Pool")]
    [SerializeField] private int sfxPoolSize = 8;

    private AudioSource _bgmSource;
    private readonly List<AudioSource> _sfxPool = new();
    private int _sfxIndex;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Load audio clips from Resources if not assigned in Inspector
        LoadClipsFromResources();

        _bgmSource = GetComponent<AudioSource>();
        if (_bgmSource == null) _bgmSource = gameObject.AddComponent<AudioSource>();
        _bgmSource.loop = true;
        _bgmSource.volume = bgmVolume;
        _bgmSource.playOnAwake = false;

        // SFX 풀 생성 (중복 재생을 위한 별도 AudioSource)
        for (int i = 0; i < sfxPoolSize; i++)
        {
            var go = new GameObject("SFX_" + i);
            go.transform.SetParent(transform, false);
            _sfxPool.Add(go.AddComponent<AudioSource>());
        }

        EventBus.Subscribe<RunStateChangedEvent>(OnRunStateChanged);
        EventBus.Subscribe<CombatStartedEvent>(OnCombatStarted);
        EventBus.Subscribe<CombatActionEvent>(OnCombatAction);
        EventBus.Subscribe<TokenTrayOpenedEvent>(OnTokenTrayOpened);

        // Always play normal BGM at start (game always begins in Preparation state)
        if (bgmNormal != null)
        {
            PlayBgm(bgmNormal);
        }
    }

    private void LoadClipsFromResources()
    {
        // BGM
        if (bgmNormal == null) bgmNormal = Resources.Load<AudioClip>("Audio/BGM/Goblins_Den_(Regular)");
        if (bgmBattle == null) bgmBattle = Resources.Load<AudioClip>("Audio/BGM/Goblins_Dance_(Battle)");

        // SFX arrays - load multiple clips
        if (playerAttack == null || playerAttack.Length == 0)
        {
            playerAttack = new AudioClip[3];
            for (int i = 1; i <= 3; i++)
                playerAttack[i - 1] = Resources.Load<AudioClip>("Audio/SFX/07_human_atk_sword_" + i);
        }
        if (enemyAttack == null || enemyAttack.Length == 0)
        {
            enemyAttack = new AudioClip[3];
            for (int i = 1; i <= 3; i++)
                enemyAttack[i - 1] = Resources.Load<AudioClip>("Audio/SFX/17_orc_atk_sword_" + i);
        }
        if (playerHit == null || playerHit.Length == 0)
        {
            playerHit = new AudioClip[3];
            for (int i = 1; i <= 3; i++)
                playerHit[i - 1] = Resources.Load<AudioClip>("Audio/SFX/11_human_damage_" + i);
        }
        if (enemyHit == null || enemyHit.Length == 0)
        {
            enemyHit = new AudioClip[3];
            for (int i = 1; i <= 3; i++)
                enemyHit[i - 1] = Resources.Load<AudioClip>("Audio/SFX/21_orc_damage_" + i);
        }
        if (chestOpen == null || chestOpen.Length == 0)
        {
            chestOpen = new AudioClip[4];
            for (int i = 1; i <= 4; i++)
                chestOpen[i - 1] = Resources.Load<AudioClip>("Audio/SFX/01_chest_open_" + i);
        }
        if (playerDeath == null || (playerDeath as AudioClip[])?.Length == 0)
        {
            playerDeath = new AudioClip[] { Resources.Load<AudioClip>("Audio/SFX/14_human_death_spin") };
        }
        if (enemyDeath == null || (enemyDeath as AudioClip[])?.Length == 0)
        {
            enemyDeath = new AudioClip[] { Resources.Load<AudioClip>("Audio/SFX/24_orc_death_spin") };
        }
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<RunStateChangedEvent>(OnRunStateChanged);
        EventBus.Unsubscribe<CombatStartedEvent>(OnCombatStarted);
        EventBus.Unsubscribe<CombatActionEvent>(OnCombatAction);
        EventBus.Unsubscribe<TokenTrayOpenedEvent>(OnTokenTrayOpened);
        if (Instance == this) Instance = null;
    }

    private void OnRunStateChanged(RunStateChangedEvent e)
    {
        if (e.State == RunStateMachine.RunState.Preparation)
            PlayBgm(bgmNormal);
    }

    private void OnCombatStarted(CombatStartedEvent e) => PlayBgm(bgmBattle);

    private void OnCombatAction(CombatActionEvent e)
    {
        if (e.Actor == null) return;
        bool isPlayer = e.Actor.name.Contains("Player");
        switch (e.ActionType)
        {
            case CombatActionType.Attack:
                PlaySfx(isPlayer ? playerAttack : enemyAttack);
                break;
            case CombatActionType.Hit:
                PlaySfx(isPlayer ? playerHit : enemyHit);
                break;
            case CombatActionType.Die:
                PlaySfx(isPlayer ? playerDeath : enemyDeath);
                break;
        }
    }

    private void OnTokenTrayOpened(TokenTrayOpenedEvent e) => PlaySfx(chestOpen);

    private void PlayBgm(AudioClip clip)
    {
        if (clip == null || _bgmSource == null) return;
        if (_bgmSource.clip == clip && _bgmSource.isPlaying) return;
        _bgmSource.Stop();
        _bgmSource.clip = clip;
        _bgmSource.Play();
    }

    private void PlaySfx(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0 || _sfxPool.Count == 0) return;
        var clip = clips[Random.Range(0, clips.Length)];
        if (clip == null) return;
        _sfxIndex = (_sfxIndex + 1) % _sfxPool.Count;
        _sfxPool[_sfxIndex].PlayOneShot(clip);
    }

    // Runtime audio clip assignment (for editor/setup)
    public void SetBgmClips(AudioClip normal, AudioClip battle) { bgmNormal = normal; bgmBattle = battle; }
    public void SetSfxClips(AudioClip[] pAtk, AudioClip[] eAtk, AudioClip[] pHit, AudioClip[] eHit, AudioClip[] pDeath, AudioClip[] eDeath, AudioClip[] cOpen)
    {
        playerAttack = pAtk; enemyAttack = eAtk; playerHit = pHit; enemyHit = eHit;
        playerDeath = pDeath; enemyDeath = eDeath; chestOpen = cOpen;
    }
}
