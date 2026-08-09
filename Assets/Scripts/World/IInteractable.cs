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

        /// <summary>
        /// 버튼에 표시할 짧은 말. "2F로 이동", "상자 열기".
        /// <b>화면에는 안 나올 수 있다</b> — 사용 버튼이 아이콘만 쓰기 때문이다.
        /// 로그와 나중의 툴팁이 이걸 읽는다.
        /// </summary>
        string PromptLabel { get; }

        /// <summary>
        /// 사용 버튼에 띄울 그림. <b>대상이 자기 아이콘을 정한다</b> (2026-08-09 확정).
        ///
        /// 버튼 하나를 포탈·아이템·상자가 나눠 쓰는데 글자를 없애기로 했으므로,
        /// 무엇을 하게 되는지를 이 그림이 대신 말한다.
        /// 포탈 위에 아이템이 겹쳐 있을 때 <b>이동인지 줍기인지 가리는 유일한 단서</b>다.
        ///
        /// <c>null</c> 이면 버튼이 기본 그림을 쓴다.
        /// </summary>
        Sprite PromptIcon { get; }

        /// <summary>여러 개가 겹쳤을 때 가장 가까운 것을 고르는 기준점.</summary>
        Transform Anchor { get; }

        void Interact();
    }
}
