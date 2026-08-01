using UnityEngine;

namespace PrettyKnights.Characters
{
    /// <summary>
    /// 8방향. 순번은 에셋 명명 규칙 01~08 과 <b>정확히 같은 순서</b>다
    /// (CLAUDE.md §4). <see cref="EightDirectionUtil.AssetIndex"/> 로 파일 순번을 얻는다.
    /// </summary>
    public enum EightDirection
    {
        /// <summary>01_front — 아래</summary>
        Front = 0,
        /// <summary>02_front_faces-screen-right — 아래-오른쪽</summary>
        FrontRight = 1,
        /// <summary>03_faces-screen-right — 오른쪽</summary>
        Right = 2,
        /// <summary>04_back_faces-screen-right — 위-오른쪽</summary>
        BackRight = 3,
        /// <summary>05_back — 위</summary>
        Back = 4,
        /// <summary>06_back_faces-screen-left — 위-왼쪽</summary>
        BackLeft = 5,
        /// <summary>07_faces-screen-left — 왼쪽</summary>
        Left = 6,
        /// <summary>08_front_faces-screen-left — 아래-왼쪽</summary>
        FrontLeft = 7
    }

    public static class EightDirectionUtil
    {
        public const int Count = 8;
        private const float DegreesPerStep = 360f / Count;

        private static readonly Vector2[] Vectors = BuildVectors();

        /// <summary>
        /// 이동 벡터를 8방향으로 스냅한다.
        /// 입력이 0에 가까우면 <paramref name="fallback"/> 을 그대로 돌려준다
        /// (정지 시 마지막 방향 유지용).
        /// </summary>
        public static EightDirection FromVector(Vector2 direction, EightDirection fallback = EightDirection.Front)
        {
            if (direction.sqrMagnitude < 0.0001f) return fallback;

            // Front(0,-1) 가 0번이 되도록 +90도 회전시킨 뒤 45도 단위로 반올림한다.
            float degrees = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 90f;
            int index = Mathf.RoundToInt(degrees / DegreesPerStep);

            // C# 의 % 는 음수를 그대로 두므로 한 번 더 보정한다.
            index = ((index % Count) + Count) % Count;
            return (EightDirection)index;
        }

        /// <summary>해당 방향의 단위 벡터.</summary>
        public static Vector2 ToVector(this EightDirection direction) => Vectors[(int)direction];

        /// <summary>에셋 파일 순번 (01~08). 스프라이트·클립 이름과 맞추는 데 쓴다.</summary>
        public static int AssetIndex(this EightDirection direction) => (int)direction + 1;

        /// <summary>
        /// 블렌드 트리에 넣을 방향 좌표.
        /// 대각선도 길이 1로 정규화되어 있어 2D Simple Directional 에서 균등하게 배치된다.
        /// </summary>
        public static Vector2 ToBlendVector(this EightDirection direction) => Vectors[(int)direction];

        private static Vector2[] BuildVectors()
        {
            var result = new Vector2[Count];
            for (int i = 0; i < Count; i++)
            {
                float radians = (i * DegreesPerStep - 90f) * Mathf.Deg2Rad;
                result[i] = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
            }
            return result;
        }
    }
}
