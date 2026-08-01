using System;
using PrettyKnights.Data;
using UnityEngine;

namespace PrettyKnights.Save
{
    /// <summary>
    /// JSON 직렬화 단위. 필드를 지울 때는 <see cref="Version"/> 을 올리고
    /// <see cref="SaveService"/> 에 마이그레이션을 추가한다.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        /// <summary>현재 세이브 포맷 버전.</summary>
        public const int CurrentVersion = 1;

        [SerializeField] private int version = CurrentVersion;
        [SerializeField] private long savedAtUtcTicks;
        [SerializeField] private PlayerRuntimeState player = new PlayerRuntimeState();

        public int Version => version;
        public PlayerRuntimeState Player => player;
        public DateTime SavedAtUtc => new DateTime(savedAtUtcTicks, DateTimeKind.Utc);

        public static SaveData CreateNew() => From(new PlayerRuntimeState());

        /// <summary>
        /// 살아 있는 상태를 그대로 감싼다. 복사하지 않고 참조를 들기 때문에
        /// 저장 직전 값이 그대로 직렬화된다.
        /// </summary>
        public static SaveData From(PlayerRuntimeState state) => new SaveData
        {
            version = CurrentVersion,
            savedAtUtcTicks = DateTime.UtcNow.Ticks,
            player = state ?? new PlayerRuntimeState()
        };

        public void StampSaveTime() => savedAtUtcTicks = DateTime.UtcNow.Ticks;
    }
}
