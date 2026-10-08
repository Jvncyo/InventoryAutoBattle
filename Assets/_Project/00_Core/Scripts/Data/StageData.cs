using System.Collections.Generic;
using UnityEngine;

namespace InventoryAutoBattle.Core
{
    [CreateAssetMenu(fileName = "NewStageData", menuName = "InventoryAutoBattle/Stage Data")]
    public class StageData : ScriptableObject
    {
        [Header("스테이지 정보")]
        public int stageNumber = 1;
        public string stageName = "Stage 1";

        [Header("적 스탯")]
        public string enemyName = "Slime";
        public float enemyMaxHp = 100f;
        public float enemyAttack = 10f;
        public float enemyDefense = 0f;
        public float enemyAttackCooldown = 2.0f;
        public Sprite enemySprite;

        [Header("보상")]
        public List<ItemData> rewardItemPool;
        public int rewardExpansionFragments = 1; // 3개 수집 시 인벤토리 확장
    }
}
