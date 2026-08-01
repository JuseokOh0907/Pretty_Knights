using System;
using System.Collections.Generic;
using UnityEngine;

namespace PrettyKnights.Core
{
    /// <summary>
    /// 씬 비종속 서비스 조회 지점.
    ///
    /// 매니저마다 싱글턴을 만드는 대신 여기 한 곳에만 등록한다.
    /// Additive 로 올라온 게임플레이 씬의 컴포넌트는 <see cref="Get{T}"/> 으로
    /// <c>GameRoot</c> 가 들고 있는 서비스에 닿는다.
    /// </summary>
    public static class ServiceRegistry
    {
        private static readonly Dictionary<Type, object> Services = new Dictionary<Type, object>();

        /// <summary>
        /// 도메인 리로드를 끈 상태(Enter Play Mode Options)에서도 플레이 시작 시
        /// 이전 세션의 등록이 남지 않도록 비운다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => Services.Clear();

        public static void Register<T>(T service) where T : class
        {
            if (service == null) throw new ArgumentNullException(nameof(service));

            Type key = typeof(T);
            if (Services.ContainsKey(key))
                Debug.LogWarning($"[ServiceRegistry] {key.Name} 가 이미 등록되어 있어 덮어씁니다.");

            Services[key] = service;
        }

        public static void Unregister<T>() where T : class => Services.Remove(typeof(T));

        public static bool TryGet<T>(out T service) where T : class
        {
            if (Services.TryGetValue(typeof(T), out object found))
            {
                service = (T)found;
                return true;
            }

            service = null;
            return false;
        }

        /// <summary>등록되지 않았으면 <c>null</c> 과 함께 에러를 남긴다.</summary>
        public static T Get<T>() where T : class
        {
            if (TryGet(out T service)) return service;

            Debug.LogError(
                $"[ServiceRegistry] {typeof(T).Name} 가 등록되어 있지 않습니다. " +
                "Boot 씬을 거치지 않고 게임플레이 씬을 단독 실행하지 않았는지 확인하세요.");
            return null;
        }

        public static void Clear() => Services.Clear();
    }
}
