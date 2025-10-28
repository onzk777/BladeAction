using UnityEngine;
using BladeAction.Item;

namespace BladeAction.Combat.Test
{
    /// <summary>
    /// 스탯 계산 시스템 테스트 스크립트
    /// Inspector 우클릭 또는 ContextMenu로 각 테스트 실행 가능
    /// </summary>
    public class StatsTest : MonoBehaviour
    {
        [Header("테스트 설정")]
        [Tooltip("자동으로 모든 테스트 실행")]
        public bool runAllTestsOnStart = false;
        
        [Header("테스트 아이템 ID")]
        [Tooltip("테스트용 무기 아이템 ID")]
        public string testWeaponId = "itm_weapon_test";
        
        [Tooltip("테스트용 방어구 아이템 ID")]
        public string testArmorId = "itm_armor_test";
        
        private void Start()
        {
            if (runAllTestsOnStart)
            {
                Debug.Log("========== 자동 테스트 시작 ==========");
                Test1_GetFinalStats_NoEquipment();
                Test2_EquipWeapon_ATKIncrease();
                Test3_UnequipWeapon_ATKRestore();
                Test4_EquipArmor_HPRatioPreserve();
                Debug.Log("========== 자동 테스트 완료 ==========");
            }
        }
        
        [ContextMenu("1. GetFinalStats 기본 테스트 (장비 없음)")]
        public void Test1_GetFinalStats_NoEquipment()
        {
            Debug.Log("========== Test 1: GetFinalStats 기본 테스트 ==========");
            
            var player = CharacterManager.Instance.PlayerCharacter;
            if (player == null)
            {
                Debug.LogError("[StatsTest] PlayerCharacter가 null입니다!");
                return;
            }
            
            if (player.Inventory == null)
            {
                Debug.LogError("[StatsTest] PlayerCharacter.Inventory가 null입니다!");
                return;
            }
            
            // 장비 모두 해제
            foreach (var slot in player.Inventory.equipmentSlots)
            {
                if (!slot.IsEmpty())
                {
                    player.Inventory.UnequipItem(slot.slotType);
                }
            }
            
            // 최종 스탯 조회
            var stats = StatsCalculationManager.Instance.GetFinalStats(player);
            
            Debug.Log($"[StatsTest] Player Final Stats (장비 없음):");
            Debug.Log($"  ATK: {stats.attack}");
            Debug.Log($"  MaxHP: {stats.maxHP}");
            Debug.Log($"  CurrentHP: {stats.currentHP}");
            Debug.Log($"  MaxPoise: {stats.maxPoise}");
            Debug.Log($"  CurrentPoise: {stats.currentPoise}");
            Debug.Log($"  DR: {stats.defenseDR}");
            Debug.Log($"  CritChance: {stats.critChance}");
            Debug.Log($"  CritMultiplier: {stats.critMultiplier}");
            
            Debug.Log("✅ Test 1 완료");
        }
        
        [ContextMenu("2. 무기 장착 시 ATK 증가 테스트")]
        public void Test2_EquipWeapon_ATKIncrease()
        {
            Debug.Log("========== Test 2: 무기 장착 시 ATK 증가 ==========");
            
            var player = CharacterManager.Instance.PlayerCharacter;
            if (player == null || player.Inventory == null)
            {
                Debug.LogError("[StatsTest] PlayerCharacter 또는 Inventory가 null입니다!");
                return;
            }
            
            // 장착 전 ATK
            int beforeATK = StatsCalculationManager.Instance.GetFinalATK(player);
            Debug.Log($"[StatsTest] 장착 전 ATK: {beforeATK}");
            
            // 무기 아이템 추가
            bool added = player.Inventory.AddItem(testWeaponId, 1);
            if (!added)
            {
                Debug.LogWarning($"[StatsTest] 아이템 추가 실패: {testWeaponId} (ItemDatabase에 등록되어 있는지 확인)");
                return;
            }
            
            // 무기 장착
            bool equipped = player.Inventory.EquipItem(testWeaponId, EquipmentSlotType.Weapon);
            if (!equipped)
            {
                Debug.LogError($"[StatsTest] 장착 실패: {testWeaponId}");
                return;
            }
            
            // 장착 후 ATK
            int afterATK = StatsCalculationManager.Instance.GetFinalATK(player);
            Debug.Log($"[StatsTest] 장착 후 ATK: {afterATK} (증가: +{afterATK - beforeATK})");
            
            if (afterATK > beforeATK)
            {
                Debug.Log("✅ Test 2 통과: ATK 증가 확인됨");
            }
            else
            {
                Debug.LogError("❌ Test 2 실패: ATK가 증가하지 않았습니다!");
            }
        }
        
        [ContextMenu("3. 무기 해제 시 ATK 복귀 테스트")]
        public void Test3_UnequipWeapon_ATKRestore()
        {
            Debug.Log("========== Test 3: 무기 해제 시 ATK 복귀 ==========");
            
            var player = CharacterManager.Instance.PlayerCharacter;
            if (player == null || player.Inventory == null)
            {
                Debug.LogError("[StatsTest] PlayerCharacter 또는 Inventory가 null입니다!");
                return;
            }
            
            // 기본 ATK 저장 (CharacterData의 baseStats)
            int baseATK = player.CharacterData.ATK;
            Debug.Log($"[StatsTest] 기본 ATK (CharacterData): {baseATK}");
            
            // 현재 ATK (장착 상태)
            int equippedATK = StatsCalculationManager.Instance.GetFinalATK(player);
            Debug.Log($"[StatsTest] 현재 ATK (장착 상태): {equippedATK}");
            
            // 무기 해제
            bool unequipped = player.Inventory.UnequipItem(EquipmentSlotType.Weapon);
            if (!unequipped)
            {
                Debug.LogWarning("[StatsTest] 해제할 무기가 없습니다.");
            }
            
            // 해제 후 ATK
            int afterUnequipATK = StatsCalculationManager.Instance.GetFinalATK(player);
            Debug.Log($"[StatsTest] 해제 후 ATK: {afterUnequipATK}");
            
            if (afterUnequipATK == baseATK)
            {
                Debug.Log("✅ Test 3 통과: ATK가 기본값으로 복귀됨");
            }
            else
            {
                Debug.LogError($"❌ Test 3 실패: ATK가 기본값과 다릅니다! (기대: {baseATK}, 실제: {afterUnequipATK})");
            }
        }
        
        [ContextMenu("4. HP 비율 보존 테스트")]
        public void Test4_EquipArmor_HPRatioPreserve()
        {
            Debug.Log("========== Test 4: HP 비율 보존 테스트 ==========");
            
            var player = CharacterManager.Instance.PlayerCharacter;
            if (player == null || player.Inventory == null)
            {
                Debug.LogError("[StatsTest] PlayerCharacter 또는 Inventory가 null입니다!");
                return;
            }
            
            // 현재 HP를 50%로 설정
            int maxHPBefore = (int)player.MaxHP;
            player.currentHP = Mathf.RoundToInt(maxHPBefore * 0.5f);
            float hpRatioBefore = (float)player.currentHP / maxHPBefore;
            
            Debug.Log($"[StatsTest] 장착 전 HP: {player.currentHP}/{maxHPBefore} ({hpRatioBefore:P0})");
            
            // 방어구 아이템 추가 및 장착
            player.Inventory.AddItem(testArmorId, 1);
            bool equipped = player.Inventory.EquipItem(testArmorId, EquipmentSlotType.Armor);
            
            if (!equipped)
            {
                Debug.LogWarning($"[StatsTest] 방어구 장착 실패: {testArmorId} (ItemDatabase에 등록되어 있는지 확인)");
                return;
            }
            
            // 장착 후 HP 비율 확인 (RecalculateAndCommit가 자동 호출됨)
            int maxHPAfter = (int)player.MaxHP;
            float hpRatioAfter = (float)player.currentHP / maxHPAfter;
            Debug.Log($"[StatsTest] 장착 후 HP: {player.currentHP}/{maxHPAfter} ({hpRatioAfter:P0})");
            
            // 비율 차이 계산 (오차 범위 1% 허용)
            float ratioDiff = Mathf.Abs(hpRatioBefore - hpRatioAfter);
            
            if (ratioDiff < 0.01f)
            {
                Debug.Log($"✅ Test 4 통과: HP 비율 보존됨 (오차: {ratioDiff:P2})");
            }
            else
            {
                Debug.LogError($"❌ Test 4 실패: HP 비율이 변경됨! (이전: {hpRatioBefore:P0}, 이후: {hpRatioAfter:P0}, 오차: {ratioDiff:P2})");
            }
        }
        
        [ContextMenu("5. 여러 장비 장착 시 스탯 누적 테스트")]
        public void Test5_MultipleEquipment_StatStack()
        {
            Debug.Log("========== Test 5: 여러 장비 장착 시 스탯 누적 ==========");
            
            var player = CharacterManager.Instance.PlayerCharacter;
            if (player == null || player.Inventory == null)
            {
                Debug.LogError("[StatsTest] PlayerCharacter 또는 Inventory가 null입니다!");
                return;
            }
            
            // 모든 장비 해제
            foreach (var slot in player.Inventory.equipmentSlots)
            {
                if (!slot.IsEmpty())
                {
                    player.Inventory.UnequipItem(slot.slotType);
                }
            }
            
            // 기본 스탯 확인
            var baseStats = StatsCalculationManager.Instance.GetFinalStats(player);
            Debug.Log($"[StatsTest] 기본 스탯: ATK={baseStats.attack}, MaxHP={baseStats.maxHP}, DR={baseStats.defenseDR}");
            
            // 무기 장착
            player.Inventory.AddItem(testWeaponId, 1);
            player.Inventory.EquipItem(testWeaponId, EquipmentSlotType.Weapon);
            
            var statsWithWeapon = StatsCalculationManager.Instance.GetFinalStats(player);
            Debug.Log($"[StatsTest] 무기 장착: ATK={statsWithWeapon.attack}, MaxHP={statsWithWeapon.maxHP}, DR={statsWithWeapon.defenseDR}");
            
            // 방어구 추가 장착
            player.Inventory.AddItem(testArmorId, 1);
            player.Inventory.EquipItem(testArmorId, EquipmentSlotType.Armor);
            
            var statsWithBoth = StatsCalculationManager.Instance.GetFinalStats(player);
            Debug.Log($"[StatsTest] 무기+방어구: ATK={statsWithBoth.attack}, MaxHP={statsWithBoth.maxHP}, DR={statsWithBoth.defenseDR}");
            
            // 검증
            bool atkIncreased = statsWithWeapon.attack > baseStats.attack;
            bool hpOrDRIncreased = statsWithBoth.maxHP > statsWithWeapon.maxHP || statsWithBoth.defenseDR > statsWithWeapon.defenseDR;
            
            if (atkIncreased && hpOrDRIncreased)
            {
                Debug.Log("✅ Test 5 통과: 여러 장비 장착 시 스탯이 누적됨");
            }
            else
            {
                Debug.LogError("❌ Test 5 실패: 스탯 누적이 정상적으로 작동하지 않습니다!");
            }
        }
        
        [ContextMenu("6. GetFinalStat (특정 스탯 조회) 테스트")]
        public void Test6_GetFinalStat_SingleStat()
        {
            Debug.Log("========== Test 6: GetFinalStat 특정 스탯 조회 ==========");
            
            var player = CharacterManager.Instance.PlayerCharacter;
            if (player == null)
            {
                Debug.LogError("[StatsTest] PlayerCharacter가 null입니다!");
                return;
            }
            
            // 개별 스탯 조회 테스트
            float atk = StatsCalculationManager.Instance.GetFinalStat(player, "attack");
            float maxHP = StatsCalculationManager.Instance.GetFinalStat(player, "maxHP");
            float critChance = StatsCalculationManager.Instance.GetFinalStat(player, "critChance");
            
            Debug.Log($"[StatsTest] GetFinalStat 결과:");
            Debug.Log($"  attack: {atk}");
            Debug.Log($"  maxHP: {maxHP}");
            Debug.Log($"  critChance: {critChance}");
            
            // GetFinalStats()와 비교
            var fullStats = StatsCalculationManager.Instance.GetFinalStats(player);
            
            bool match = (atk == fullStats.attack && maxHP == fullStats.maxHP && critChance == fullStats.critChance);
            
            if (match)
            {
                Debug.Log("✅ Test 6 통과: GetFinalStat과 GetFinalStats 결과 일치");
            }
            else
            {
                Debug.LogError("❌ Test 6 실패: GetFinalStat과 GetFinalStats 결과 불일치!");
            }
        }
        
        [ContextMenu("7. RecalculateAndCommit 테스트")]
        public void Test7_RecalculateAndCommit()
        {
            Debug.Log("========== Test 7: RecalculateAndCommit 테스트 ==========");
            
            var player = CharacterManager.Instance.PlayerCharacter;
            if (player == null || player.Inventory == null)
            {
                Debug.LogError("[StatsTest] PlayerCharacter 또는 Inventory가 null입니다!");
                return;
            }
            
            // 무기 해제 (기본 상태로)
            player.Inventory.UnequipItem(EquipmentSlotType.Weapon);
            int atkBefore = StatsCalculationManager.Instance.GetFinalATK(player);
            Debug.Log($"[StatsTest] Commit 전 ATK: {atkBefore}");
            
            // 무기 장착
            player.Inventory.AddItem(testWeaponId, 1);
            player.Inventory.EquipItem(testWeaponId, EquipmentSlotType.Weapon);
            
            // 장착 후 ATK (자동 Commit됨)
            int atkAfter = StatsCalculationManager.Instance.GetFinalATK(player);
            Debug.Log($"[StatsTest] 장착 후 (자동 Commit) ATK: {atkAfter}");
            
            // 검증
            if (atkAfter > atkBefore)
            {
                Debug.Log($"✅ Test 7 통과: RecalculateAndCommit이 자동으로 호출되어 스탯이 갱신됨 (ATK: {atkBefore} → {atkAfter})");
            }
            else
            {
                Debug.LogError($"❌ Test 7 실패: ATK가 증가하지 않았습니다! (이전: {atkBefore}, 이후: {atkAfter})");
            }
        }
        
        [ContextMenu("8. 모든 스탯 상세 출력")]
        public void Test8_PrintAllStats()
        {
            Debug.Log("========== Test 8: 모든 스탯 상세 출력 ==========");
            
            var player = CharacterManager.Instance.PlayerCharacter;
            if (player == null)
            {
                Debug.LogError("[StatsTest] PlayerCharacter가 null입니다!");
                return;
            }
            
            var stats = StatsCalculationManager.Instance.GetFinalStats(player);
            
            Debug.Log("[StatsTest] === Player 최종 스탯 (Full) ===");
            Debug.Log($"  [체력] MaxHP: {stats.maxHP}, CurrentHP: {stats.currentHP}");
            Debug.Log($"  [포이즈] MaxPoise: {stats.maxPoise}, CurrentPoise: {stats.currentPoise}");
            Debug.Log($"  [공격] ATK: {stats.attack}");
            Debug.Log($"  [방어] DR: {stats.defenseDR}, TempDRBonus: {stats.tempDRBonus}");
            Debug.Log($"  [치명타] Chance: {stats.critChance:P0}, Multiplier: {stats.critMultiplier}x");
            Debug.Log($"  [막기] DamageReduction: {stats.guardDamageReduction:P0}, DRBonus: {stats.guardDRBonus}");
            Debug.Log($"  [막기 효율] BlockEfficiency: {stats.blockEfficiency:P0}, PoiseConsumption: {stats.blockPoiseConsumption}");
            Debug.Log($"  [패링 효율] ParryEfficiency: {stats.parryEfficiency:P0}, PoiseConsumption: {stats.parryPoiseConsumption}");
            Debug.Log($"  [패링 공격] AttackPower: {stats.parryPoiseAttackPower}, Damage: {stats.parryPoiseDamage}");
            Debug.Log($"  [포이즈 획득] PoiseGain: {stats.poiseGain}");
            
            Debug.Log("✅ Test 8 완료");
        }
        
        [ContextMenu("9. InventoryUI 연결 상태 확인")]
        public void Test9_CheckInventoryUIConnection()
        {
            Debug.Log("========== Test 9: InventoryUI 연결 상태 확인 ==========");
            
            var inventoryUI = FindFirstObjectByType<BladeAction.UI.InventoryUI>();
            if (inventoryUI == null)
            {
                Debug.LogError("[StatsTest] InventoryUI를 찾을 수 없습니다!");
                return;
            }
            
            var connectedInventory = inventoryUI.GetInventory();
            var player = CharacterManager.Instance.PlayerCharacter;
            
            Debug.Log($"[StatsTest] InventoryUI 상태:");
            Debug.Log($"  - InventoryUI 존재: {inventoryUI != null}");
            Debug.Log($"  - 연결된 Inventory: {(connectedInventory != null ? "있음" : "없음")}");
            
            if (connectedInventory != null)
            {
                Debug.Log($"  - Inventory Owner: {connectedInventory.Owner?.Name ?? "Unknown"}");
                Debug.Log($"  - 아이템 수: {connectedInventory.items.Count}");
                Debug.Log($"  - 장비 슬롯 수: {connectedInventory.equipmentSlots.Count}");
                
                bool isPlayerInventory = (player != null && connectedInventory == player.Inventory);
                Debug.Log($"  - PlayerCharacter.Inventory와 동일: {isPlayerInventory}");
                
                if (isPlayerInventory)
                {
                    Debug.Log("✅ Test 9 통과: InventoryUI가 PlayerCharacter.Inventory에 올바르게 연결됨");
                }
                else
                {
                    Debug.LogError("❌ Test 9 실패: InventoryUI가 PlayerCharacter.Inventory와 다른 인벤토리를 참조하고 있습니다!");
                }
            }
            else
            {
                Debug.LogError("❌ Test 9 실패: InventoryUI에 Inventory가 연결되지 않았습니다!");
            }
        }
        
        [ContextMenu("99. 모든 테스트 실행")]
        public void RunAllTests()
        {
            Debug.Log("========================================");
            Debug.Log("========== 전체 테스트 시작 ==========");
            Debug.Log("========================================");
            
            Test1_GetFinalStats_NoEquipment();
            Debug.Log("");
            
            Test2_EquipWeapon_ATKIncrease();
            Debug.Log("");
            
            Test3_UnequipWeapon_ATKRestore();
            Debug.Log("");
            
            Test4_EquipArmor_HPRatioPreserve();
            Debug.Log("");
            
            Test5_MultipleEquipment_StatStack();
            Debug.Log("");
            
            Test6_GetFinalStat_SingleStat();
            Debug.Log("");
            
            Test7_RecalculateAndCommit();
            Debug.Log("");
            
            Test9_CheckInventoryUIConnection();
            Debug.Log("");
            
            Debug.Log("========================================");
            Debug.Log("========== 전체 테스트 완료 ==========");
            Debug.Log("========================================");
        }
    }
}

