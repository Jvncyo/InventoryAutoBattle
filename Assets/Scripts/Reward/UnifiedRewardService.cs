using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 통합 보상 옵션(장비 획득 + 장비 강화) 생성 서비스.
/// 인스펙터에서 버프 풀(availableBuffPool)을 설정한다.
/// </summary>
public class UnifiedRewardService : MonoBehaviour
{
    [Header("강화 버프 풀")]
    [Tooltip("강화 옵션 생성 시 랜덤하게 선택할 StatBuffEffectSO 목록")]
    [SerializeField] private List<StatBuffEffectSO> availableBuffPool = new();

    public List<UnifiedRewardOption> GenerateOptions(
        ItemContainer playerInventory,
        List<ItemDataSO> equipmentDropPool,
        int totalOptions = 3)
    {
        var options = new List<UnifiedRewardOption>();
        if (playerInventory == null) return options;

        // 1. 장비 획득 옵션 수 결정 (1~2개)
        int maxEquipmentOptions = Mathf.Min(2, totalOptions - 1);
        int equipmentCount = Random.Range(1, maxEquipmentOptions + 1);
        int enhancementCount = totalOptions - equipmentCount;

        // 2. 장비 획득 옵션 생성 (equipmentDropPool에서 중복 없이 샘플링)
        if (equipmentDropPool != null && equipmentDropPool.Count > 0)
        {
            var shuffledEquip = new List<ItemDataSO>(equipmentDropPool);
            Shuffle(shuffledEquip);
            for (int i = 0; i < equipmentCount && i < shuffledEquip.Count; i++)
            {
                var opt = UnifiedRewardOption.CreateEquipment(shuffledEquip[i]);
                if (opt != null) options.Add(opt);
            }
        }

        // 3. 강화 옵션 생성 (인벤토리 내 장비 + 랜덤 버프)
        var equipments = new List<ItemInstance>(playerInventory.Items)
            .FindAll(i => i != null && i.Data != null && i.Data.ItemType == ItemType.Equipment);
        if (equipments.Count > 0 && availableBuffPool != null && availableBuffPool.Count > 0)
        {
            for (int i = 0; i < enhancementCount; i++)
            {
                var target = equipments[Random.Range(0, equipments.Count)];
                var buff = availableBuffPool[Random.Range(0, availableBuffPool.Count)];
                var opt = UnifiedRewardOption.CreateEnhancement(target, buff);
                if (opt != null) options.Add(opt);
            }
        }

        // 4. 옵션이 부족하면 장비 옵션으로 채움
        while (options.Count < totalOptions && equipmentDropPool != null && equipmentDropPool.Count > 0)
        {
            var remaining = equipmentDropPool.FindAll(e => !options.Exists(o => o.Type == RewardOptionType.Equipment && o.EquipmentData == e));
            if (remaining.Count == 0) break;
            var pick = remaining[Random.Range(0, remaining.Count)];
            var opt = UnifiedRewardOption.CreateEquipment(pick);
            if (opt != null) options.Add(opt);
        }

        // 5. 순서 랜덤화
        Shuffle(options);
        return options;
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    // 테스트/에디터용: 버프 풀 설정
    public void SetBuffPool(List<StatBuffEffectSO> pool) => availableBuffPool = pool ?? new List<StatBuffEffectSO>();
    public List<StatBuffEffectSO> GetBuffPool() => availableBuffPool;
}