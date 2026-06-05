using System.Collections.Generic;

namespace Astrion.Game
{
    /// Client-side mirror of AchievementManager.ALL on the server. Must
    /// stay in lockstep — bump the id suffix (e.g. _v2) if the threshold
    /// changes so a player who hit the old target gets to re-claim.
    /// Used by AchievementUI to render the locked/unlocked list + progress.
    public static class AchievementDatabase
    {
        public enum Kind { Level, Kills, Gold, Friends, Party, Trade, Whisper, Cities }

        public class Def
        {
            public string id;
            public string displayName;
            public string description;
            public Kind kind;
            public long target;
            public string rewardItemId;
            public int rewardQty;
        }

        public static readonly List<Def> All = new()
        {
            new Def { id="LV_5",   displayName="첫 발걸음",       description="캐릭터 Lv 5 도달",        kind=Kind.Level,   target=5,     rewardItemId="stardust", rewardQty=50 },
            new Def { id="LV_10",  displayName="모험가",         description="캐릭터 Lv 10 도달",       kind=Kind.Level,   target=10,    rewardItemId="stardust", rewardQty=100 },
            new Def { id="LV_30",  displayName="베테랑",         description="캐릭터 Lv 30 도달",       kind=Kind.Level,   target=30,    rewardItemId="stardust", rewardQty=500 },
            new Def { id="LV_50",  displayName="챔피언",         description="캐릭터 Lv 50 도달",       kind=Kind.Level,   target=50,    rewardItemId="stardust", rewardQty=1000 },

            new Def { id="KILL_100",   displayName="사냥꾼",       description="몬스터 100마리 처치",     kind=Kind.Kills,   target=100,   rewardItemId="stardust", rewardQty=50 },
            new Def { id="KILL_1000",  displayName="숙련 사냥꾼",   description="몬스터 1,000마리 처치",   kind=Kind.Kills,   target=1000,  rewardItemId="stardust", rewardQty=200 },
            new Def { id="KILL_10000", displayName="전설의 사냥꾼", description="몬스터 10,000마리 처치",   kind=Kind.Kills,   target=10000, rewardItemId="stardust", rewardQty=1000 },

            new Def { id="GOLD_10K",  displayName="부자",  description="10,000 골드 보유",   kind=Kind.Gold, target=10_000,  rewardItemId="stardust", rewardQty=50 },
            new Def { id="GOLD_100K", displayName="부호",  description="100,000 골드 보유",  kind=Kind.Gold, target=100_000, rewardItemId="stardust", rewardQty=200 },

            new Def { id="FRIEND_1",   displayName="사교가",    description="친구 1명 만들기",        kind=Kind.Friends, target=1,  rewardItemId="stardust", rewardQty=50 },
            new Def { id="FRIEND_10",  displayName="인기인",    description="친구 10명 만들기",       kind=Kind.Friends, target=10, rewardItemId="stardust", rewardQty=200 },

            new Def { id="PARTY_FIRST",  displayName="동행",     description="첫 파티 합류",      kind=Kind.Party,   target=1, rewardItemId="stardust", rewardQty=50 },
            new Def { id="TRADE_FIRST",  displayName="첫 거래",  description="거래 1회 성사",     kind=Kind.Trade,   target=1, rewardItemId="stardust", rewardQty=50 },
            new Def { id="WHISPER_FIRST",displayName="속삭임",   description="귓속말 1회 보내기", kind=Kind.Whisper, target=1, rewardItemId="stardust", rewardQty=20 },

            new Def { id="CITIES_ALL", displayName="세계여행자", description="5개 도시 모두 방문", kind=Kind.Cities, target=5, rewardItemId="stardust", rewardQty=500 },
        };

        public static long CurrentValue(Def d, AchievementSystem.Progress p)
        {
            if (p == null) return 0;
            return d.kind switch
            {
                Kind.Level   => p.level,
                Kind.Kills   => p.kills,
                Kind.Gold    => p.gold,
                Kind.Friends => p.friends,
                Kind.Cities  => p.cities,
                _ => 0,
            };
        }
    }
}
