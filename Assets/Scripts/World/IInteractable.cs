using UnityEngine;

namespace PrettyKnights.World
{
    /// <summary>
    /// 사용 키로 작동시킬 수 있는 것. 포탈이 첫 사례이고
    /// 히든 상자·아이템 줍기가 같은 흐름을 그대로 쓴다.
    ///
    /// 콜라이더 안에 들어와 있는 것만으로는 아무 일도 일어나지 않는다.
    /// 겹친 상태 + 사용 키를 눌러야 <see cref="Interact"/> 가 불린다.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>지금 사용할 수 있는지. 잠긴 문·이미 연 상자는 false 를 돌려준다.</summary>
        bool CanInteract { get; }

        /// <summary>버튼에 표시할 짧은 말. "2F로 이동", "상자 열기".</summary>
        string PromptLabel { get; }

        /// <summary>여러 개가 겹쳤을 때 가장 가까운 것을 고르는 기준점.</summary>
        Transform Anchor { get; }

        void Interact();
    }
}
