using System;
using System.IO;
using UnityEngine;

namespace PrettyKnights.Save
{
    /// <summary>
    /// 세이브 파일 입출력.
    ///
    /// 모바일은 앱이 예고 없이 종료되므로 쓰기 도중 죽으면 파일이 깨진다.
    /// 그래서 항상 임시 파일에 먼저 쓰고 원본과 교체한다.
    /// 교체 실패 시에는 직전 백업으로 복구를 시도한다.
    /// </summary>
    public sealed class SaveService
    {
        private const string FileName = "save.json";
        private const string TempSuffix = ".tmp";
        private const string BackupSuffix = ".bak";

        private readonly string savePath;
        private readonly string tempPath;
        private readonly string backupPath;

        public SaveService(string directory = null)
        {
            string root = string.IsNullOrEmpty(directory) ? Application.persistentDataPath : directory;
            savePath = Path.Combine(root, FileName);
            tempPath = savePath + TempSuffix;
            backupPath = savePath + BackupSuffix;
        }

        public string SavePath => savePath;
        public bool Exists => File.Exists(savePath);

        public bool TrySave(SaveData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            try
            {
                data.StampSaveTime();
                string json = JsonUtility.ToJson(data, prettyPrint: true);

                // 1) 임시 파일에 완전히 기록한다.
                File.WriteAllText(tempPath, json);

                // 2) 기존 파일을 백업으로 밀어낸다.
                if (File.Exists(savePath))
                {
                    if (File.Exists(backupPath)) File.Delete(backupPath);
                    File.Move(savePath, backupPath);
                }

                // 3) 임시 파일을 본 파일 자리로 옮긴다.
                File.Move(tempPath, savePath);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveService] 저장 실패: {e.Message}");
                CleanupTemp();
                return false;
            }
        }

        /// <summary>
        /// 세이브를 읽는다. 파일이 없으면 새 데이터를 만들어 <c>false</c> 를 돌려준다
        /// (호출부가 "신규 플레이"인지 구분할 수 있게).
        /// </summary>
        public bool TryLoad(out SaveData data)
        {
            if (TryReadFrom(savePath, out data)) return true;

            if (File.Exists(backupPath))
            {
                Debug.LogWarning("[SaveService] 본 세이브를 읽지 못해 백업으로 복구를 시도합니다.");
                if (TryReadFrom(backupPath, out data)) return true;
            }

            data = SaveData.CreateNew();
            return false;
        }

        public void Delete()
        {
            TryDelete(savePath);
            TryDelete(backupPath);
            TryDelete(tempPath);
        }

        private static bool TryReadFrom(string path, out SaveData data)
        {
            data = null;
            if (!File.Exists(path)) return false;

            try
            {
                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json)) return false;

                SaveData parsed = JsonUtility.FromJson<SaveData>(json);
                if (parsed?.Player == null) return false;

                if (parsed.Version != SaveData.CurrentVersion)
                {
                    Debug.LogWarning(
                        $"[SaveService] 세이브 버전 불일치 (파일 {parsed.Version} / 현재 {SaveData.CurrentVersion}). " +
                        "마이그레이션이 아직 없어 그대로 사용합니다.");
                }

                data = parsed;
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveService] '{path}' 를 읽지 못했습니다: {e.Message}");
                return false;
            }
        }

        private void CleanupTemp() => TryDelete(tempPath);

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveService] '{path}' 삭제 실패: {e.Message}");
            }
        }
    }
}
