namespace InventoryAutoBattle.Core
{
    public enum ItemType
    {
        Weapon,      // 검 (기본 공격)
        Shield,      // 방패 (방어)
        Potion,      // 하트/포션 (회복)
        Bow,         // 활 (원거리)
        Greatsword,  // 대검 (고위력)
        Bomb         // 폭탄 (범위 피해)
    }

    public enum TokenType
    {
        Attack,      // ⚔️ 공격 토큰
        Defense,     // 🛡️ 방어 토큰
        Heal         // ❤️ 회복 토큰
    }

    public enum GameState
    {
        Ready,       // 전투 준비 및 인벤토리 정비 단계
        Battle,      // 자동 전투 및 토큰 드롭 진행 단계
        Reward,      // 승리 후 보상 획득 및 인벤토리 확장 단계
        GameOver     // 패배 및 재도전 단계
    }

    public enum CharacterType
    {
        Player,
        Enemy
    }
}
