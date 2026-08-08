using UnityEngine;

namespace PrettyKnights.Combat
{
    /// <summary>
    /// 화면의 스킬 버튼들이 바라보는 창구. <b>아직 구현체가 없다.</b>
    ///
    /// 스킬 3종(전방 베기 · 관통 직선 · 광역 폭발)은 아직 만들지 않았지만
    /// 버튼 자리는 지금 잡아야 한다 — 나중에 넣으면 HUD 배치를 통째로 다시 잰다.
    /// 그렇다고 버튼이 아무 데도 연결되지 않으면 "누르면 무슨 일이 일어나는가" 가
    /// 버튼 안에 눌러앉아, 스킬이 붙을 때 UI 를 고쳐야 한다.
    ///
    /// <b>그래서 창구만 먼저 낸다.</b> 이걸 등록하는 쪽이 생기기 전까지
    /// 버튼은 전부 <b>잠김</b>으로 그려진다 — 그것이 지금의 사실이므로 거짓말이 아니다.
    ///
    /// 슬롯을 <c>int</c> 로 다루고 객체를 돌려주지 않는 이유는 <b>매 프레임 호출</b>이라서다.
    /// 버튼 4개가 각자 상태를 물으므로 여기서 무언가를 새로 만들면 계속 쌓인다.
    ///
    /// 구현체는 <c>Player.prefab</c> 에 붙고 <c>ServiceRegistry.Register&lt;ISkillBar&gt;</c> 로 등록한다.
    /// 몸은 씬마다 새로 생기므로 버튼이 인스펙터로 물고 있을 수 없다.
    /// </summary>
    public interface ISkillBar
    {
        /// <summary>슬롯 개수. 화면 버튼이 이보다 많으면 남는 것은 잠김으로 그린다.</summary>
        int SlotCount { get; }

        /// <summary>배웠는가. 배우지 않은 슬롯은 눌리지 않는다.</summary>
        bool IsUnlocked(int slot);

        /// <summary>지금 쓸 수 있는가. 쿨타임·경직·전환 중이면 <c>false</c>.</summary>
        bool IsReady(int slot);

        /// <summary>쿨타임 진행도 0~1. 1 이면 다 찼다. 버튼이 이걸로 채워진 정도를 그린다.</summary>
        float CooldownProgress(int slot);

        /// <summary>버튼에 그릴 아이콘. 없으면 <c>null</c> — 버튼이 기본 이미지를 유지한다.</summary>
        Sprite IconOf(int slot);

        /// <summary>지금 쓴다. 화면 버튼과 키가 <b>같은 이 경로</b>를 탄다.</summary>
        bool TryCast(int slot);
    }
}
