using UnityEngine;

namespace Game.Core
{
    [CreateAssetMenu(fileName = "GameData", menuName = "Game/Core/Game Data")]
    public class GameData : ScriptableObject
    {
        [Header("Game Settings")]
        public int startingLives = 3;
        public int maxScore = 999999;
        
        [Header("Player Settings")]
        public float playerSpeed = 5f;
        public float screenBoundOffset = 0.56f;
        
        [Header("Weapon Settings")]
        public int maxMissiles = 10;
        public float missileSpeed = 8f;
        public float fireRate = 0.12f;
        
        [Header("Meteor Settings")]
        public int maxMeteors = 12;
        public float meteorSpawnDelay = 4f;
        public int meteorDefaultHealth = 10;
        public float meteorJumpForce = 10f;
        public float meteorBounceForce = 8f;
        
        [Header("Scoring")]
        public int meteorDestroyScore = 100;
        public int perfectShotBonus = 50;
        
        [Header("Physics")]
        public float wheelMotorSpeed = 150f;
        public float gravityScale = 1f;
        
        [Header("UI Settings")]
        public float transitionDuration = 0.5f;
        
        [Header("Audio Settings")]
        public float masterVolume = 1f;
        public float sfxVolume = 0.8f;
        public float musicVolume = 0.6f;
    }
}