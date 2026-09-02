using GooglePlayGames;
using GooglePlayGames.BasicApi;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using UnityEngine.UI;
using TMPro;

public class LeaderBoardManager : MonoBehaviour
{
    [SerializeField] private Button leaderBoardButton;
    [SerializeField] private TextMeshProUGUI leaderBoardDebugText;

    private void Start()
    {
        if (leaderBoardButton != null)
        {
            leaderBoardButton.onClick.AddListener(ShowLeaderBoardUI);
        }

        // 자동 로그인 시도
        AuthenticateUser();
    }

    // 사용자 인증
    private void AuthenticateUser()
    {
        PlayGamesPlatform.Activate();
        Social.localUser.Authenticate((bool success) => {
            UpdateDebugText(success ? "로그인 성공" : "로그인 실패");
        });
    }

    // 로그인 상태 확인
    private bool IsAuthenticated() => Social.localUser.authenticated;

    // 리더보드 UI 표시
    public void ShowLeaderBoardUI()
    {
        if (IsAuthenticated())
        {
            LoadTopScores(); // 상위 점수 불러오기
            UpdateAndRecordScore(); // 점수 갱신
            PlayGamesPlatform.Instance.ShowLeaderboardUI(GPGSIds.leaderboard_one); // 리더보드 UI 표시
        }
        else
        {
            UpdateDebugText("사용자가 인증되지 않았습니다.");
        }
    }

    // 리더보드 상위 점수 로드
    public void LoadTopScores()
    {
        if (IsAuthenticated())
        {
            PlayGamesPlatform.Instance.LoadScores(
                GPGSIds.leaderboard_one, // 리더보드 ID
                LeaderboardStart.TopScores, // 상위 점수부터 시작
                10, // 불러올 점수의 개수
                LeaderboardCollection.Public, // 공개 리더보드
                LeaderboardTimeSpan.AllTime, // 전체 기간
                (LeaderboardScoreData data) => {
                    if (data.Valid)
                    {
                        foreach (var score in data.Scores)
                        {
                            UpdateDebugText($"플레이어 ID: {score.userID}, 점수: {score.value}");
                        }
                    }
                    else
                    {
                        UpdateDebugText("리더보드 데이터가 유효하지 않습니다.");
                    }
                });
        }
        else
        {
            UpdateDebugText("사용자가 인증되지 않았습니다.");
        }
    }

    // 나의 랭킹과 점수 보기
    public void MyRank()
    {
        if (IsAuthenticated())
        {
            PlayGamesPlatform.Instance.LoadScores(
                GPGSIds.leaderboard_one, // 리더보드 ID
                LeaderboardStart.PlayerCentered, // 플레이어 중심 모드
                1, // 1개의 점수만 불러오기
                LeaderboardCollection.Public, // 공개 리더보드
                LeaderboardTimeSpan.AllTime, // 전체 기간
                (LeaderboardScoreData data) => {
                    if (data.Valid && data.Scores.Length > 0)
                    {
                        long playerScore = data.PlayerScore.value;

                        // 점수 갱신 확인
                        if (playerScore != DataManager.Instance.baseCost.PlayerGold)
                        {
                            DataManager.Instance.baseCost.PlayerGold = playerScore;
                            RecordScore(true); // 점수 저장 후 UI 갱신
                        }
                    }
                });
        }
        else
        {
            UpdateDebugText("사용자가 인증되지 않았습니다.");
        }
    }

    // 점수 등록
    private void RecordScore(bool UI = false)
    {
        int MaxScore = (int)DataManager.Instance.baseCost.PlayerGold;

        if (MaxScore > 0)
        {
            PlayGamesPlatform.Instance.ReportScore(MaxScore, GPGSIds.leaderboard_one, (bool success) => {
                if (success && UI)
                {
                    MyRank(); // 리더보드 갱신
                }
                else
                {
                    UpdateDebugText("점수 등록에 실패했습니다.");
                }
            });
        }
        else
        {
            UpdateDebugText("Gold 값이 설정되지 않았거나 유효하지 않습니다.");
        }
    }

    // 점수 갱신 후 등록
    private void UpdateAndRecordScore()
    {
        PlayGamesPlatform.Instance.LoadScores(
            GPGSIds.leaderboard_one,
            LeaderboardStart.PlayerCentered,
            1,
            LeaderboardCollection.Public,
            LeaderboardTimeSpan.AllTime,
            (LeaderboardScoreData data) =>
            {
                if (data.Valid && data.PlayerScore != null)
                {
                    long leaderboardScore = data.PlayerScore.value;
                    int playerScore = (int)DataManager.Instance.baseCost.PlayerGold;

                    if (playerScore > leaderboardScore)
                    {
                        RecordScore(false); // 점수만 등록하고 UI는 갱신하지 않음
                    }
                    else
                    {
                        UpdateDebugText("플레이어 점수가 리더보드 점수보다 높지 않습니다.");
                    }
                }
                else
                {
                    UpdateDebugText("리더보드 점수 데이터를 불러오는 데 실패했습니다.");
                }
            });
    }

    // 디버그 메시지를 UI에 업데이트
    private void UpdateDebugText(string message)
    {
        if (leaderBoardDebugText != null)
        {
            leaderBoardDebugText.text = message;
        }
    }
}
