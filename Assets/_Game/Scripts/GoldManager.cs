using UnityEngine;

namespace TinySwordsBattle
{
    public class GoldManager : MonoBehaviour
    {
        public static GoldManager Instance { get; private set; }

        public float baseIncome = 1.5f;
        public float incomePerWorker = 2f;
        public int maxWorkers = 5;

        public float Gold { get; private set; }
        public int Workers { get; private set; }
        public float Income => baseIncome + Workers * incomePerWorker;

        void Awake() { Instance = this; }

        public void Init(StageData stage)
        {
            Gold = stage.startGold;
            baseIncome = stage.baseIncome;
            incomePerWorker = stage.incomePerWorker;
            maxWorkers = stage.maxWorkers;
            Workers = 0;
        }

        void Update()
        {
            var bm = BattleManager.Instance;
            if (bm != null && bm.IsRunning) Gold += Income * Time.deltaTime;
        }

        public bool CanAfford(int cost) => Gold >= cost;

        public bool TrySpend(int cost)
        {
            if (Gold < cost) return false;
            Gold -= cost;
            return true;
        }

        public void AddGold(float amount) { Gold += amount; }
        public void AddWorker() { Workers++; }
        public void RemoveWorker() { Workers = Mathf.Max(0, Workers - 1); }
    }
}
