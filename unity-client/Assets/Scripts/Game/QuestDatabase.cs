using System.Collections.Generic;

namespace Astrion.Game
{
    /// Single source of truth for player-facing quest metadata.
    /// NPC2D defines the dialogue + flow; this just maps the questId to a
    /// title / summary so QuestLogUI can show completed quests too.
    public static class QuestDatabase
    {
        public class QuestDef
        {
            public string id;
            public string title;
            public string summary;
            public string giver; // optional NPC name
        }

        private static readonly Dictionary<string, QuestDef> _quests = new Dictionary<string, QuestDef>
        {
            ["star_fragments"] = new QuestDef
            {
                id = "star_fragments",
                title = "흩어진 별의 조각",
                summary = "바람의 등대섬에 흩어진 별의 조각 5개를 모아 폴라리스에게 가져간다.",
                giver = "폴라리스",
            },
            ["awaken_power"] = new QuestDef
            {
                id = "awaken_power",
                title = "별의 힘 깨우기",
                summary = "Q 키로 별빛 투사체를 익히고, 옛 기사단의 훈련용 표적 3개를 부순다.",
                giver = "폴라리스",
            },
        };

        public static QuestDef Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return _quests.TryGetValue(id, out var d) ? d : null;
        }

        public static string TitleOf(string id)
        {
            var d = Get(id);
            return d != null ? d.title : id;
        }
    }
}
