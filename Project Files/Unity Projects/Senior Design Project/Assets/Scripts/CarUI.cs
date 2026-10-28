using UnityEngine;
using TMPro;

public class CarUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_Text episodeText;
    public TMP_Text lapText;
    public TMP_Text collisionText;
    public TMP_Text speedText;
    public TMP_Text rewardText;
    public TMP_Text bestRewardText;

    private CarAgent carAgent;
    private int episodeCount = 1;
    private int lapCount = 0;
    private int collisionCount = 0;
    private float bestReward = 0f;

    void Start()
    {
        carAgent = FindFirstObjectByType<CarAgent>();
    }

    void Update()
    {
        if (carAgent == null) return;

        float speed = carAgent.GetCurrentSpeed();
        float currentReward = carAgent.GetCumulativeReward();

        episodeText.text = "Episode: " + carAgent.CompletedEpisodes;
        lapText.text = "Laps: " + lapCount;
        collisionText.text = "Collisions: " + collisionCount;
        speedText.text = "Speed: " + Mathf.Abs(speed).ToString("F1") + " m/s";
        rewardText.text = "Reward: " + currentReward.ToString("F2");
        bestRewardText.text = "Best Reward: " + bestReward.ToString("F2");
    }

    public void UpdateBestReward(float episodeReward)
    {
        if (episodeReward > bestReward)
            bestReward = episodeReward;
    }

    public void AddCollision()
    {
        collisionCount++;
    }

    public void AddLap()
    {
        lapCount++;
    }

    public void AddEpisode()
    {
        episodeCount++;
    }
}