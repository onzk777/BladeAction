using UnityEngine;

namespace BladeAction.BT
{
    /// <summary>
    /// 검술 선택 액션 노드
    /// 이번 턴에 사용할 검술을 지정합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "CommandSelection", menuName = "BT/Actions/Command Selection", order = 2)]
    public class BTAction_CommandSelection : BTActionNode
    {
        [Header("검술 선택 설정")]
        [Tooltip("선택 방식")]
        public SelectionType selectionType = SelectionType.ByIndex;
        
        [Tooltip("ByIndex일 때 사용할 검술 인덱스")]
        [Min(0)]
        public int commandIndex = 0;
        
        [Tooltip("ByTag일 때 사용할 태그")]
        public string requiredTag = "";
        
        public enum SelectionType
        {
            [Tooltip("인덱스로 직접 선택")]
            ByIndex,
            [Tooltip("태그로 랜덤 선택")]
            ByTag
        }
        
        public override void Execute(BehaviorTreeContext context)
        {
            if (context == null)
            {
                BTLogger.LogWarning("CommandSelection: Context null");
                return;
            }
            
            switch (selectionType)
            {
                case SelectionType.ByIndex:
                    context.selectedCommandIndex = commandIndex;
                    context.selectedCommandTag = null;
                    break;
                    
                case SelectionType.ByTag:
                    if (!string.IsNullOrEmpty(requiredTag))
                    {
                        context.selectedCommandTag = requiredTag;
                        context.selectedCommandIndex = null;
                    }
                    else
                    {
                        BTLogger.LogWarning("CommandSelection: Tag 비어있음");
                    }
                    break;
            }
        }
        
        /// <summary>
        /// 노드 설명 자동 생성
        /// </summary>
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(description))
            {
                if (selectionType == SelectionType.ByIndex)
                {
                    description = $"검술 인덱스 {commandIndex} 선택";
                }
                else
                {
                    string tagText = string.IsNullOrEmpty(requiredTag) ? "태그 미설정" : requiredTag;
                    description = $"태그 '{tagText}' 검술 랜덤 선택";
                }
            }
        }
    }
}

