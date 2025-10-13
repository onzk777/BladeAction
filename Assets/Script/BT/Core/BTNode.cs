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
        
        [Tooltip("노드 활성화 여부")]
        public bool isEnabled = true;
        
        /// <summary>
        /// 노드가 유효한지 확인
        /// </summary>
        public virtual bool IsValid()
        {
            return isEnabled;
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

