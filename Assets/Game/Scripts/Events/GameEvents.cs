using Game.Core;

namespace Game.Events
{
    // Game State Events
    public struct GameStateChangeEvent : IGameEvent
    {
        public GameState PreviousState { get; }
        public GameState NewState { get; }

        public GameStateChangeEvent(GameState previousState, GameState newState)
        {
            PreviousState = previousState;
            NewState = newState;
        }
    }

    public struct GameStartEvent : IGameEvent
    {
        public GameStartEvent(bool isNewGame)
        {
            IsNewGame = isNewGame;
        }

        public bool IsNewGame { get; }
    }

    public struct GameOverEvent : IGameEvent
    {
        public int FinalScore { get; }
        public int HighScore { get; }
        public bool IsNewHighScore { get; }

        public GameOverEvent(int finalScore, int highScore, bool isNewHighScore)
        {
            FinalScore = finalScore;
            HighScore = highScore;
            IsNewHighScore = isNewHighScore;
        }
    }

    // Score Events
    public struct ScoreUpdateEvent : IGameEvent
    {
        public int ScoreChange { get; }
        public int NewTotalScore { get; }
        public ScoreReason Reason { get; }

        public ScoreUpdateEvent(int scoreChange, int newTotalScore, ScoreReason reason)
        {
            ScoreChange = scoreChange;
            NewTotalScore = newTotalScore;
            Reason = reason;
        }
    }

    public enum ScoreReason
    {
        MeteorDestroyed,
        PerfectShot,
        Combo,
        Bonus
    }

    // Player Events
    public struct PlayerDeathEvent : IGameEvent
    {
        public int RemainingLives { get; }
        public DeathCause Cause { get; }

        public PlayerDeathEvent(int remainingLives, DeathCause cause)
        {
            RemainingLives = remainingLives;
            Cause = cause;
        }
    }

    public enum DeathCause
    {
        MeteorHit,
        FallOffScreen,
        Other
    }

    // Level Events
    public struct LevelCompleteEvent : IGameEvent
    {
        public int Level { get; }
        public int Bonus { get; }

        public LevelCompleteEvent(int level, int bonus)
        {
            Level = level;
            Bonus = bonus;
        }
    }

    public struct WaveCompleteEvent : IGameEvent
    {
        public int WaveNumber { get; }
        public int MeteorsDestroyed { get; }

        public WaveCompleteEvent(int waveNumber, int meteorsDestroyed)
        {
            WaveNumber = waveNumber;
            MeteorsDestroyed = meteorsDestroyed;
        }
    }
}