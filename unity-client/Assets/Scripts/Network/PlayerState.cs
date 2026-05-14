namespace Astrion.Network
{
    [System.Serializable]
    public class PlayerState
    {
        public string questId = "";
        public string questTitle = "";
        public int questProgress;
        public int questTarget;
        public int questState; // 0=NotStarted, 1=InProgress, 2=Complete
        public string[] collectedFragmentIds = new string[0];
        public string[] brokenTargetIds = new string[0];
        public string[] completedQuestIds = new string[0];
        public string[] inventoryItemIds = new string[0];
        public int[] inventoryQuantities = new int[0];
        public string[] collectedPickupIds = new string[0];
        public int hp;
        public int maxHp;
        public int mp;
        public int maxMp;
        public int level = 1;
        public int exp = 0;
        public int statStr = 5;
        public int statDex = 5;
        public int statInt = 5;
        public int statLuk = 5;
        public int statPoints = 5;
        public string equippedWeaponId = "";
        public string lastScene = ""; // last game scene the player was in
    }
}
