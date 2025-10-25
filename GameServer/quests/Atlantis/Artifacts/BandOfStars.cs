using System;
using System.Collections.Generic;
using System.Text;
using DOL.Events;
using DOL.GS.Quests;
using DOL.Database;
using DOL.GS.PacketHandler;
using System.Collections;
using log4net;
using System.Reflection;

namespace DOL.GS.Quests.Atlantis.Artifacts
{
    /// <summary>
    /// Quest for the Band of Stars artifact.
    /// </summary>
    /// <author>Aredhel</author>
    class BandOfStars : ArtifactQuest
    {
        /// <summary>
        /// Defines a logger for this class.
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// The name of the quest (not necessarily the same as
        /// the name of the reward).
        /// </summary>
        public override string Name
        {
            get { return "Band of Stars"; }
        }

        /// <summary>
        /// The reward for this quest.
        /// </summary>
        private static String m_artifactID = "Band of Stars";
        public override String ArtifactID
        {
            get { return m_artifactID; }
        }

        /// <summary>
        /// Description for the current step.
        /// </summary>
        public override string Description
        {
            get
            {
                switch (Step)
                {
                    case 1:
                        return "Defeat Chisisi.";
                    case 2:
                        return "Turn in the King's Vase in the Stygia Haven or in the Hall of Heroes.";
                    default:
                        return base.Description;
                }
            }
        }

        public BandOfStars()
            : base() { }

        public BandOfStars(GamePlayer questingPlayer)
            : base(questingPlayer) { }

        /// <summary>
        /// This constructor is needed to load quests from the DB.
        /// </summary>
        /// <param name="questingPlayer"></param>
        /// <param name="dbQuest"></param>
        public BandOfStars(GamePlayer questingPlayer, DbQuest dbQuest)
            : base(questingPlayer, dbQuest) { }

        /// <summary>
        /// Quest initialisation.
        /// </summary>
        public static void Init()
        {
            ArtifactQuest.Init(m_artifactID, typeof(BandOfStars));
        }

        /// <summary>
        /// Check if player is eligible for this quest.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public override bool CheckQuestQualification(GamePlayer player)
        {
            if (!base.CheckQuestQualification(player))
                return false;

            // TODO: Check if this is the correct level for the quest.
            return (player.Level >= 45);
        }

        /// <summary>
        /// Handle an item given to the scholar.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="item"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public override bool ReceiveItem(GameLiving source, GameLiving target, DbInventoryItem item)
        {
            if (base.ReceiveItem(source, target, item))
                return true;

            GamePlayer player = source as GamePlayer;
            Scholar scholar = target as Scholar;

            // DEBUGGING: Log alle relevanten Informationen
            if (log.IsInfoEnabled)
                log.Info($"BandOfStars.ReceiveItem called - Player: {player?.Name}, Scholar: {scholar?.Name}, Item: {item?.Name}, Step: {Step}");

            if (player == null || scholar == null)
            {
                if (log.IsWarnEnabled)
                    log.Warn($"BandOfStars.ReceiveItem - Player or Scholar is null! Player: {player?.Name}, Scholar: {scholar?.Name}");
                return false;
            }

            // Pr�fen ob wir auf Step 2 sind und das richtige Buch haben
            if (Step == 2)
            {
                if (log.IsInfoEnabled)
                    log.Info($"BandOfStars.ReceiveItem - Quest is on Step 2, checking book...");

                // Hole die ArtifactID aus dem Item
                string itemArtifactID = null;
                var pageNumbers = ArtifactMgr.GetPageNumbers(item, ref itemArtifactID);

                if (log.IsInfoEnabled)
                    log.Info($"BandOfStars.ReceiveItem - Item ArtifactID: {itemArtifactID}, PageNumbers: {pageNumbers}, Expected ArtifactID: {ArtifactID}");

                // Pr�fe ob es ein vollst�ndiges Buch f�r dieses Artefakt ist
                if (pageNumbers == ArtifactMgr.Book.AllPages && itemArtifactID == ArtifactID)
                {
                    if (log.IsInfoEnabled)
                        log.Info($"BandOfStars.ReceiveItem - Book matches! Getting artifact versions...");

                    Dictionary<String, DbItemTemplate> versions = ArtifactMgr.GetArtifactVersions(ArtifactID,
                        (eCharacterClass)player.CharacterClass.ID, (eRealm)player.Realm);

                    if (log.IsInfoEnabled)
                        log.Info($"BandOfStars.ReceiveItem - Found {versions.Count} artifact versions");

                    if (versions.Count > 0 && RemoveItem(player, item))
                    {
                        if (log.IsInfoEnabled)
                            log.Info($"BandOfStars.ReceiveItem - Item removed successfully, giving artifact to player");

                        GiveItem(scholar, player, ArtifactID, versions[";;"]);
                        String reply = String.Format("Do you see how the stars in the {0} {1}? {2} {3} {4} {5} {6} {7}.",
                            "band have begun to glow, ",
                            player.Name,
                            "That is because the magic of the bracelet is active again. It shall flow through",
                            "the Band and into you, then back into the Band. Because of this flow, you may not",
                            "give the bracelet to anyone else. Destroying the Bracelet will end the flow of",
                            "magic and once stopped, it cannot be restarted again. May the Band of Stars guide",
                            "you to success in the trials, ",
                            player.Name);
                        scholar.TurnTo(player);
                        scholar.SayTo(player, eChatLoc.CL_PopupWindow, reply);
                        FinishQuest();
                        return true;
                    }
                    else
                    {
                        if (log.IsWarnEnabled)
                            log.Warn($"BandOfStars.ReceiveItem - Failed to remove item or no versions found!");
                    }
                }
                else
                {
                    if (log.IsWarnEnabled)
                        log.Warn($"BandOfStars.ReceiveItem - Book doesn't match criteria! PageNumbers: {pageNumbers}, Expected: {ArtifactMgr.Book.AllPages}, ItemArtifactID: {itemArtifactID}, Expected: {ArtifactID}");
                }
            }
            else
            {
                if (log.IsInfoEnabled)
                    log.Info($"BandOfStars.ReceiveItem - Quest is NOT on Step 2 (currently on Step {Step})");
            }

            if (log.IsInfoEnabled)
                log.Info($"BandOfStars.ReceiveItem - Returning false (item not accepted)");

            return false;
        }

        /// <summary>
        /// Handle whispers to the scholar.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public override bool WhisperReceive(GameLiving source, GameLiving target, string text)
        {
            if (base.WhisperReceive(source, target, text))
                return true;

            GamePlayer player = source as GamePlayer;
            Scholar scholar = target as Scholar;
            if (player == null || scholar == null)
                return false;

            if (Step == 1 && text.ToLower() == ArtifactID.ToLower())
            {
                String reply = String.Format("This bracelet was quite beautiful and powerful. {0} {1} {2}",
                    "Let's get that power flowing again! Give me the King's Vase and I shall examine it.",
                    "Once I have uncovered the secrets in the Vase, I shall make the Band of Stars",
                    "available to you.");
                scholar.TurnTo(player);
                scholar.SayTo(player, eChatLoc.CL_PopupWindow, reply);
                Step = 2;
                return true;
            }

            return false;
        }
    }
}