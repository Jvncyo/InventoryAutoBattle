using System.Collections.Generic;
using UnityEngine;
using InventoryAutoBattle.Core;

namespace InventoryAutoBattle.GameLoop
{
    /// <summary>
    /// 스테이지 진행 및 난이도 관리 시스템.
    /// 스테이지 순차 진행, 적 스탯 생성 및 스케일링, 보상 아이템 결정을 담당합니다.
    /// </summary>
    public class StageManager : MonoBehaviour
    {
        [Header("스테이지 목록 (설정되지 않으면 자동 생성)")]
        [SerializeField] private List<StageData> predefinedStages = new List<StageData>();

        [Header("현재 진행 상황")]
        [SerializeField] private int currentStageIndex = 0;
        [SerializeField] private StageData currentStageData;

        public int CurrentStageNumber => currentStageIndex + 1;
        public StageData CurrentStage => currentStageData;

        public void Initialize()
        {
            currentStageIndex = 0;
            LoadStage(currentStageIndex);
        }

        public StageData LoadStage(int index)
        {
            currentStageIndex = index;

            if (predefinedStages != null && index < predefinedStages.Count && predefinedStages[index] != null)
            {
                currentStageData = predefinedStages[index];
            }
            else
            {
                // 프리셋이 없으면 난이도 공식에 의해 동적 StageData 생성
                currentStageData = CreateDynamicStage(index + 1);
            }

            Debug.Log($"[StageManager] 🗺️ 스테이지 로드 완료: {currentStageData.stageName} (적 HP: {currentStageData.enemyMaxHp}, ATK: {currentStageData.enemyAttack})");
            return currentStageData;
        }

        public StageData NextStage()
        {
            currentStageIndex++;
            return LoadStage(currentStageIndex);
        }

        public void RestartStage()
        {
            LoadStage(currentStageIndex);
        }

        private StageData CreateDynamicStage(int stageNumber)
        {
            StageData stage = ScriptableObject.CreateInstance<StageData>();
            stage.stageNumber = stageNumber;
            stage.stageName = $"Stage {stageNumber}";
            stage.enemyName = stageNumber % 3 == 0 ? $"오크 대장 (Lv.{stageNumber})" : $"정예 오크 (Lv.{stageNumber})";

            // 난이도 스케일링 공식
            stage.enemyMaxHp = 80f + (stageNumber - 1) * 35f;
            stage.enemyAttack = 8f + (stageNumber - 1) * 3.5f;
            stage.enemyDefense = (stageNumber - 1) * 1.5f;
            stage.enemyAttackCooldown = Mathf.Max(1.2f, 2.0f - (stageNumber - 1) * 0.1f);
            stage.rewardExpansionFragments = 1;

            return stage;
        }
    }
}
