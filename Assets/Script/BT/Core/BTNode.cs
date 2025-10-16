using UnityEngine;

namespace BladeAction.BT
{
    /// <summary>
    /// Behavior Tree 노드의 기본 클래스
    /// 모든 BT 노드는 이 클래스를 상속받습니다.
    /// </summary>
    public abstract class BTNode : ScriptableObject
    {
        [Header("노드 정보")]
        [Tooltip("노드 설명 (디버깅용)")]
        [TextArea(2, 4)]
        public string description = "";
        
        // ⚠️ Deprecated: 노드는 여러 BT에서 공유되므로 노드 자체에 isEnabled를 두면 안 됨
        // BT Entry의 ActionWrapper.isEnabled를 사용하세요.
        [System.Obsolete("노드 자체의 isEnabled는 사용하지 마세요. BT Entry에서 액션별 활성화를 관리합니다.", false)]
        [HideInInspector]
        public bool isEnabled = true;
        
        /// <summary>
        /// 노드가 유효한지 확인
        /// </summary>
        public virtual bool IsValid()
        {
            // isEnabled 체크 제거 (Entry에서 관리)
            return true;
        }
        
        /// <summary>
        /// 노드 초기화 (필요시 오버라이드)
        /// </summary>
        public virtual void Initialize()
        {
            // 기본 구현은 비어있음
        }
        
        /// <summary>
        /// 노드 정리 (필요시 오버라이드)
        /// </summary>
        public virtual void Cleanup()
        {
            // 기본 구현은 비어있음
        }
    }
}

