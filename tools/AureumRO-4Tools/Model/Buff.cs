using System;
using System.Collections.Generic;
using System.Drawing;
using _4RTools.Utils;

namespace _4RTools.Model
{
    internal enum BuffOrigin
    {
        Common = 0,
        Rops = 1
    }

    internal class Buff
    {
        public String name { get; set; }
        public EffectStatusIDs effectStatusID { get; set; }
        public Bitmap icon { get; set; }
        public bool IsPreRenewal { get; set; }
        public BuffOrigin Origin { get; set; }

        public Buff(string name, EffectStatusIDs effectStatus, Bitmap icon)
            : this(name, effectStatus, icon, IsPreRenewalSkill(effectStatus), BuffOrigin.Common)
        {
        }

        public Buff(string name, EffectStatusIDs effectStatus, Bitmap icon, bool isPreRenewal, BuffOrigin origin)
        {
            this.name = name;
            this.effectStatusID = effectStatus;
            this.icon = icon;
            this.IsPreRenewal = isPreRenewal;
            this.Origin = origin;
        }

        // Authentic Pre-Renewal (Ep. 13.1 / Transclasses + Ninja/Gunslinger/Taekwon) skill buff IDs.
        private static readonly HashSet<EffectStatusIDs> PreRenewalSkillIds = new HashSet<EffectStatusIDs>
        {
            // Archer / Hunter / Bard / Dancer / Sniper / Clown / Gypsy
            EffectStatusIDs.CONCENTRATION,
            EffectStatusIDs.WINDWALK,
            EffectStatusIDs.TRUESIGHT,
            EffectStatusIDs.POEMBRAGI,
            EffectStatusIDs.EFST_WHISTLE,
            EffectStatusIDs.EFST_ASSASSINCROSS,
            EffectStatusIDs.EFST_APPLEIDUN,
            EffectStatusIDs.EFST_HUMMING,
            EffectStatusIDs.EFST_DONTFORGETME,
            EffectStatusIDs.EFST_FORTUNEKISS,
            EffectStatusIDs.EFST_SERVICEFORYOU,
            EffectStatusIDs.EFST_RICHMANKIM,
            EffectStatusIDs.EFST_ETERNALCHAOS,
            EffectStatusIDs.EFST_DRUMBATTLEFIELD,
            EffectStatusIDs.EFST_RINGNIBELUNGEN,
            EffectStatusIDs.EFST_ROKISWEIL,
            EffectStatusIDs.EFST_INTOABYSS,
            EffectStatusIDs.EFST_SIEGFRIED,
            EffectStatusIDs.EFST_MARIONETTE_MASTER,
            EffectStatusIDs.EFST_MOON,
            EffectStatusIDs.EFST_LONGING,
            EffectStatusIDs.EFST_HERMODE,

            // Swordsman / Knight / Crusader / Lord Knight / Paladin
            EffectStatusIDs.ENDURE,
            EffectStatusIDs.AUTOBERSERK,
            EffectStatusIDs.AUTOGUARD,
            EffectStatusIDs.REFLECTSHIELD,
            EffectStatusIDs.SPEARQUICKEN,
            EffectStatusIDs.DEFENDER,
            EffectStatusIDs.LKCONCENTRATION,
            EffectStatusIDs.BERSERK,
            EffectStatusIDs.TWOHANDQUICKEN,
            EffectStatusIDs.PARRYING,
            EffectStatusIDs.AURABLADE,
            EffectStatusIDs.CR_SHRINK,
            EffectStatusIDs.EFST_GOSPEL,
            EffectStatusIDs.EFST_ONEHANDQUICKEN,

            // Mage / Wizard / Sage / High Wizard / Professor
            EffectStatusIDs.ENERGYCOAT,
            EffectStatusIDs.SIGHTBLASTER,
            EffectStatusIDs.AUTOSPELL,
            EffectStatusIDs.DOUBLECASTING,
            EffectStatusIDs.MEMORIZE,
            EffectStatusIDs.MYST_AMPLIFY,

            // Merchant / Blacksmith / Alchemist / Whitesmith / Creator
            EffectStatusIDs.CRAZY_UPROAR,
            EffectStatusIDs.OVERTHRUST,
            EffectStatusIDs.ADRENALINE,
            EffectStatusIDs.ADRENALINE2,
            EffectStatusIDs.OVERTHRUSTMAX,
            EffectStatusIDs.WEAPONPERFECT,
            EffectStatusIDs.MAXIMIZE,
            EffectStatusIDs.CARTBOOST,
            EffectStatusIDs.MELTDOWN,

            // Thief / Assassin / Rogue / Assassin Cross / Stalker
            EffectStatusIDs.EFST_HIDING,
            EffectStatusIDs.POISONREACT,
            EffectStatusIDs.SWORDREJECT,
            EffectStatusIDs.PRESERVE,
            EffectStatusIDs.EDP,

            // Acolyte / Priest / Monk / High Priest / Champion
            EffectStatusIDs.BLESSING,
            EffectStatusIDs.INC_AGI,
            EffectStatusIDs.ASSUMPTIO,
            EffectStatusIDs.EFST_BASILICA,
            EffectStatusIDs.EFST_KYRIE,
            EffectStatusIDs.EFST_SUFFRAGIUM,
            EffectStatusIDs.GLORIA,
            EffectStatusIDs.MAGNIFICAT,
            EffectStatusIDs.ANGELUS,
            EffectStatusIDs.FURY,
            EffectStatusIDs.IMPOSITIO,
            EffectStatusIDs.ASPERSIO,

            // Ninja
            EffectStatusIDs.PEEL_CHANGE,
            EffectStatusIDs.AURA_NINJA,
            EffectStatusIDs.EFST_NJ_BUNSINJYUTSU,

            // Taekwon / Star Gladiator
            EffectStatusIDs.PROPERTYGROUND,
            EffectStatusIDs.PROPERTYFIRE,
            EffectStatusIDs.PROPERTYWATER,
            EffectStatusIDs.PROPERTYWIND,
            EffectStatusIDs.PROPERTYTELEKINESIS,
            EffectStatusIDs.ASPERSIO,
            EffectStatusIDs.PROPERTYDARK,
            EffectStatusIDs.EFST_SG_SUN_WARM,
            EffectStatusIDs.EFST_SUN_COMFORT,
            EffectStatusIDs.EFST_SG_MOON_WARM,
            EffectStatusIDs.EFST_MOON_COMFORT,
            EffectStatusIDs.EFST_SG_STAR_WARM,
            EffectStatusIDs.EFST_STAR_COMFORT,
            EffectStatusIDs.DODGE_ON,

            // Gunslinger
            EffectStatusIDs.GATLINGFEVER,
            EffectStatusIDs.MADNESSCANCEL,
            EffectStatusIDs.ADJUSTMENT,
            EffectStatusIDs.ACCURACY
        };

        public static bool IsPreRenewalSkill(EffectStatusIDs statusId)
        {
            return PreRenewalSkillIds.Contains(statusId);
        }

        //--------------------- SKILLS ------------------------------

        //Archer Skills
        public static List<Buff> GetArcherSkills()
        {
            List<Buff> skills = new List<Buff>
            {
                new Buff("Concentration", EffectStatusIDs.CONCENTRATION, Resources._4RTools.Icons.ac_concentration),
                new Buff("Wind Walk", EffectStatusIDs.WINDWALK, Resources._4RTools.Icons.sn_windwalk),
                new Buff("True Sight", EffectStatusIDs.TRUESIGHT, Resources._4RTools.Icons.sn_sight),
                new Buff("Ilimitar", EffectStatusIDs.UNLIMIT, Resources._4RTools.Icons.Ilimitar),
                new Buff("A Poem of Bragi", EffectStatusIDs.POEMBRAGI, Resources._4RTools.Icons.poem_of_bragi),
                new Buff("A Whistle", EffectStatusIDs.EFST_WHISTLE, Resources._4RTools.Icons.ba_whistle),
                new Buff("Assassin Cross of Sunset", EffectStatusIDs.EFST_ASSASSINCROSS, Resources._4RTools.Icons.ba_assassincross),
                new Buff("The Apple of Idun", EffectStatusIDs.EFST_APPLEIDUN, Resources._4RTools.Icons.ba_appleidun),
                new Buff("Humming", EffectStatusIDs.EFST_HUMMING, Resources._4RTools.Icons.dc_humming),
                new Buff("Please Don't Forget Me", EffectStatusIDs.EFST_DONTFORGETME, Resources._4RTools.Icons.dc_dontforgetme),
                new Buff("Fortune's Kiss", EffectStatusIDs.EFST_FORTUNEKISS, Resources._4RTools.Icons.dc_fortunekiss),
                new Buff("Service for You", EffectStatusIDs.EFST_SERVICEFORYOU, Resources._4RTools.Icons.dc_serviceforyou),
                new Buff("Mr. Kim A Rich Man", EffectStatusIDs.EFST_RICHMANKIM, Resources._4RTools.Icons.bd_richmankim),
                new Buff("Eternal Chaos", EffectStatusIDs.EFST_ETERNALCHAOS, Resources._4RTools.Icons.bd_eternalchaos),
                new Buff("Drum on the Battlefield", EffectStatusIDs.EFST_DRUMBATTLEFIELD, Resources._4RTools.Icons.bd_drumbattlefield),
                new Buff("Ring of Nibelungen", EffectStatusIDs.EFST_RINGNIBELUNGEN, Resources._4RTools.Icons.bd_ringnibelungen),
                new Buff("Loki's Veil", EffectStatusIDs.EFST_ROKISWEIL, Resources._4RTools.Icons.bd_rokisweil),
                new Buff("Into the Abyss", EffectStatusIDs.EFST_INTOABYSS, Resources._4RTools.Icons.bd_intoabyss),
                new Buff("Invulnerable Siegfried", EffectStatusIDs.EFST_SIEGFRIED, Resources._4RTools.Icons.bd_siegfried),
                new Buff("Marionette Control", EffectStatusIDs.EFST_MARIONETTE_MASTER, Resources._4RTools.Icons.cg_marionette),
                new Buff("Moonlit Water Mill", EffectStatusIDs.EFST_MOON, Resources._4RTools.Icons.cg_moonlit),
                new Buff("Longing for Freedom", EffectStatusIDs.EFST_LONGING, Resources._4RTools.Icons.cg_longingfreedom),
                new Buff("Wand of Hermode", EffectStatusIDs.EFST_HERMODE, Resources._4RTools.Icons.cg_hermode),
                new Buff("Windmill Rush", EffectStatusIDs.RUSH_WINDMILL, Resources._4RTools.Icons.windmill_rush),
                new Buff("Moonlight Serenade", EffectStatusIDs.MOONLIT_SERENADE, Resources._4RTools.Icons.moonlight_serenade),
                new Buff("Frigg's Song", EffectStatusIDs.FRIGG_SONG, Resources._4RTools.Icons.friggs_song),
                new Buff("Mystic Symphony", EffectStatusIDs.EFST_MYSTIC_SYMPHONY, Resources._4RTools.Icons.mystic_symphony),
                new Buff("Jawaii Serenade", EffectStatusIDs.EFST_JAWAII_SERENADE, Resources._4RTools.Icons.jawaii_serenade),
                new Buff("Musical Interlude", EffectStatusIDs.EFST_MUSICAL_INTERLUDE, Resources._4RTools.Icons.musical_interlude),
                new Buff("Prontera March", EffectStatusIDs.EFST_PRON_MARCH, Resources._4RTools.Icons.prontera_march),
                new Buff("Swing Dance", EffectStatusIDs.EFST_SWING, Resources._4RTools.Icons.swing_dance),
                new Buff("Calamity Gale", EffectStatusIDs.EFST_CALAMITYGALE, Resources._4RTools.Icons.calamity_gale),
                new Buff("Fear Breeze", EffectStatusIDs.EFST_FEARBREEZE, Resources._4RTools.Icons.fear_breeze),
            };

            return skills;
        }

        //Swordsman Skills
        public static List<Buff> GetSwordmanSkill()
        {
            List<Buff> skills = new List<Buff>
            {
                new Buff("Endure", EffectStatusIDs.ENDURE, Resources._4RTools.Icons.sm_endure),
                new Buff("Auto Berserk", EffectStatusIDs.AUTOBERSERK, Resources._4RTools.Icons.sm_autoberserk),
                new Buff("Guard", EffectStatusIDs.AUTOGUARD, Resources._4RTools.Icons.cr_autoguard),
                new Buff("Shield Reflection", EffectStatusIDs.REFLECTSHIELD, Resources._4RTools.Icons.cr_reflectshield),
                new Buff("Spear Quicken", EffectStatusIDs.SPEARQUICKEN, Resources._4RTools.Icons.cr_spearquicken),
                new Buff("Defending Aura", EffectStatusIDs.DEFENDER, Resources._4RTools.Icons.cr_defender),
                new Buff("Dedication", EffectStatusIDs.LKCONCENTRATION, Resources._4RTools.Icons.lk_concentration),
                new Buff("Frenzy", EffectStatusIDs.BERSERK, Resources._4RTools.Icons.lk_berserk),
                new Buff("Twohand Quicken", EffectStatusIDs.TWOHANDQUICKEN, Resources._4RTools.Icons.mer_quicken),
                new Buff("Parry", EffectStatusIDs.PARRYING, Resources._4RTools.Icons.ms_parrying),
                new Buff("Aura Blade", EffectStatusIDs.AURABLADE, Resources._4RTools.Icons.lk_aurablade),
                new Buff("Enchant Blade", EffectStatusIDs.ENCHANT_BLADE, Resources._4RTools.Icons.enchant_blade),
                new Buff("Shrink", EffectStatusIDs.CR_SHRINK, Resources._4RTools.Icons.cr_shrink),
                new Buff("Battle Chant", EffectStatusIDs.EFST_GOSPEL, Resources._4RTools.Icons.pa_gospel),
                new Buff("Inspiration", EffectStatusIDs.INSPIRATION, Resources._4RTools.Icons.lg_inspiration),
                new Buff("Prestige", EffectStatusIDs.PRESTIGE, Resources._4RTools.Icons.lg_prestige),
                new Buff("Shield Spell", EffectStatusIDs.SHIELDSPELL, Resources._4RTools.Icons.lg_shieldspell),
                new Buff("Vanguard Force", EffectStatusIDs.FORCEOFVANGUARD, Resources._4RTools.Icons.vanguard_force),
                new Buff("Reflect Damage", EffectStatusIDs.REFLECTDAMAGE, Resources._4RTools.Icons.reflect_damage),
                new Buff("Vigor", EffectStatusIDs.EFST_VIGOR, Resources._4RTools.Icons.vigor),
                new Buff("Servant Weapon", EffectStatusIDs.SERVANTWEAPON, Resources._4RTools.Icons.servant_weapon),
                new Buff("Attack Stance", EffectStatusIDs.EFST_ATTACK_STANCE, Resources._4RTools.Icons.attack_stance),
                new Buff("Guard Stance", EffectStatusIDs.EFST_GUARD_STANCE, Resources._4RTools.Icons.guard_stance),
                new Buff("Rebound Shield", EffectStatusIDs.EFST_REBOUND_S, Resources._4RTools.Icons.rebound_shield),
                new Buff("Guardian Shield", EffectStatusIDs.EFST_GUARDIAN_S, Resources._4RTools.Icons.guardian_shield),
                new Buff("Holy Shield", EffectStatusIDs.EFST_HOLY_S, Resources._4RTools.Icons.holy_shield),
                new Buff("Exceed Break", EffectStatusIDs.EFST_EXEEDBREAK, Resources._4RTools.Icons.exceed_break),
                new Buff("One-Hand Quicken", EffectStatusIDs.EFST_ONEHANDQUICKEN, Resources._4RTools.Icons.one_hand_quicken),
            };

            return skills;
        }

        //Mage Skills
        public static List<Buff> GetMageSkills()
        {
            List<Buff> skills = new List<Buff>
            {
                new Buff("Energy Coat", EffectStatusIDs.ENERGYCOAT, Resources._4RTools.Icons.mg_energycoat),
                new Buff("Sight Blaster", EffectStatusIDs.SIGHTBLASTER, Resources._4RTools.Icons.wz_sightblaster),
                new Buff("Autospell", EffectStatusIDs.AUTOSPELL, Resources._4RTools.Icons.sa_autospell),
                new Buff("Double Casting", EffectStatusIDs.DOUBLECASTING, Resources._4RTools.Icons.pf_doublecasting),
                new Buff("Memorize", EffectStatusIDs.MEMORIZE, Resources._4RTools.Icons.pf_memorize),
                new Buff("Telekinesis Intense", EffectStatusIDs.TELEKINESIS_INTENSE, Resources._4RTools.Icons.telecinese),
                new Buff("Amplification", EffectStatusIDs.MYST_AMPLIFY, Resources._4RTools.Icons.amplify),
                new Buff("Recognized Spell", EffectStatusIDs.RECOGNIZEDSPELL, Resources._4RTools.Icons.recognized_spell),
                new Buff("Climax", EffectStatusIDs.EFST_CLIMAX, Resources._4RTools.Icons.climax),
            };

            return skills;
        }

        //Merchant Skills
        public static List<Buff> GetMerchantSkills()
        {
            List<Buff> skills = new List<Buff>
            {
                new Buff("Crazy Uproar", EffectStatusIDs.CRAZY_UPROAR, Resources._4RTools.Icons.mc_loud),
                new Buff("Power-Thrust", EffectStatusIDs.OVERTHRUST, Resources._4RTools.Icons.bs_overthrust),
                new Buff("Adrenaline Rush", EffectStatusIDs.ADRENALINE, Resources._4RTools.Icons.bs_adrenaline),
                new Buff("Advanced Adrenaline Rush", EffectStatusIDs.ADRENALINE2, Resources._4RTools.Icons.bs_adrenaline2),
                new Buff("Maximum Power-Thrust", EffectStatusIDs.OVERTHRUSTMAX, Resources._4RTools.Icons.ws_overthrustmax),
                new Buff("Weapon Perfection", EffectStatusIDs.WEAPONPERFECT, Resources._4RTools.Icons.bs_weaponperfect),
                new Buff("Power Maximize", EffectStatusIDs.MAXIMIZE, Resources._4RTools.Icons.bs_maximize),
                new Buff("Cart Boost", EffectStatusIDs.CARTBOOST, Resources._4RTools.Icons.ws_cartboost),
                new Buff("Meltdown", EffectStatusIDs.MELTDOWN, Resources._4RTools.Icons.ws_meltdown),
                new Buff("Acceleration", EffectStatusIDs.ACCELERATION, Resources._4RTools.Icons.mec_acceleration),
                new Buff("Cart Boost", EffectStatusIDs.GN_CARTBOOST, Resources._4RTools.Icons.cart_boost),
                new Buff("Research Report", EffectStatusIDs.EFST_RESEARCHREPORT, Resources._4RTools.Icons.researchreport),
                new Buff("Create Hell Tree", EffectStatusIDs.EFST_BO_HELL_DUSTY, Resources._4RTools.Icons.create_hell_tree),
            };

            return skills;
        }

        //Thief Skills
        public static List<Buff> GetThiefSkills()
        {
            List<Buff> skills = new List<Buff>
            {
                new Buff("Hiding", EffectStatusIDs.EFST_HIDING, Resources._4RTools.Icons.tf_hiding),
                new Buff("Poison React", EffectStatusIDs.POISONREACT, Resources._4RTools.Icons.as_poisonreact),
                new Buff("Reject Sword", EffectStatusIDs.SWORDREJECT, Resources._4RTools.Icons.st_rejectsword),
                new Buff("Preserve", EffectStatusIDs.PRESERVE, Resources._4RTools.Icons.st_preserve),
                new Buff("Enchant Deadly Poison", EffectStatusIDs.EDP, Resources._4RTools.Icons.asc_edp),
                new Buff("Weapon Blocking", EffectStatusIDs.WEAPONBLOCKING, Resources._4RTools.Icons.weapon_blocking),
                new Buff("Dancing Knife", EffectStatusIDs.EFST_DANCING_KNIFE, Resources._4RTools.Icons.dancing_knife),
                new Buff("Enchanting Shadow", EffectStatusIDs.EFST_SHADOW_WEAPON, Resources._4RTools.Icons.enchanting_shadow),
                new Buff("Potent Venom", EffectStatusIDs.EFST_POTENT_VENOM, Resources._4RTools.Icons.potent_venom),
                new Buff("Shadow Exceed", EffectStatusIDs.EFST_SHADOW_EXCEED, Resources._4RTools.Icons.shadow_exceed),
                new Buff("Abyss Slayer", EffectStatusIDs.EFST_ABYSS_SLAYER, Resources._4RTools.Icons.abyss_slayer),
                new Buff("Abyss Dagger", EffectStatusIDs.EFST_ABYSS_DAGGER, Resources._4RTools.Icons.abyss_dagger),
            };

            return skills;
        }

        //Acolyte Skills
        public static List<Buff> GetAcolyteSkills()
        {
            List<Buff> skills = new List<Buff>
            {
                new Buff("Blessing", EffectStatusIDs.BLESSING, Resources._4RTools.Icons.al_blessing),
                new Buff("Increase Agility", EffectStatusIDs.INC_AGI, Resources._4RTools.Icons.al_incagi),
                new Buff("Assumptio", EffectStatusIDs.ASSUMPTIO, Resources._4RTools.Icons.assumptio),
                new Buff("Basilica", EffectStatusIDs.EFST_BASILICA, Resources._4RTools.Icons.hp_basilica),
                new Buff("Kyrie Eleison", EffectStatusIDs.EFST_KYRIE, Resources._4RTools.Icons.pr_kyrie),
                new Buff("Suffragium", EffectStatusIDs.EFST_SUFFRAGIUM, Resources._4RTools.Icons.pr_suffragium),
                new Buff("Aspersio", EffectStatusIDs.ASPERSIO, Resources._4RTools.Icons.pr_aspersio),
                new Buff("Gloria", EffectStatusIDs.GLORIA, Resources._4RTools.Icons.pr_gloria),
                new Buff("Magnificat", EffectStatusIDs.MAGNIFICAT, Resources._4RTools.Icons.pr_magnificat),
                new Buff("Angelus", EffectStatusIDs.ANGELUS, Resources._4RTools.Icons.al_angelus),
                new Buff("Rising Dragon", EffectStatusIDs.RAISINGDRAGON, Resources._4RTools.Icons.rising_dragon),
                new Buff("Gentle Touch-Revitalize", EffectStatusIDs.GENTLETOUCH_REVITALIZE, Resources._4RTools.Icons.gentle_touch_revitalize),
                new Buff("Gentle Touch-Convert", EffectStatusIDs.GENTLETOUCH_CHANGE, Resources._4RTools.Icons.gentle_touch_convert),
                new Buff("Fury ", EffectStatusIDs.FURY, Resources._4RTools.Icons.fury),
                new Buff("Impositio Manus",  EffectStatusIDs.IMPOSITIO, Resources._4RTools.Icons.impositio_manus),
                new Buff("Competentia", EffectStatusIDs.EFST_COMPETENTIA, Resources._4RTools.Icons.competentia),
                new Buff("Offertorium", EffectStatusIDs.EFST_OFFERTORIUM, Resources._4RTools.Icons.offertorium),
                new Buff("Sincere Faith", EffectStatusIDs.EFST_SINCERE_FAITH, Resources._4RTools.Icons.sincere_faith),
                new Buff("Firm Faith", EffectStatusIDs.FIRM_FAITH, Resources._4RTools.Icons.firm_faith),
                new Buff("Powerful Faith", EffectStatusIDs.POWERFUL_FAITH, Resources._4RTools.Icons.powerful_faith),
                new Buff("First Faith Power", EffectStatusIDs.EFST_FIRST_FAITH_POWER, Resources._4RTools.Icons.first_faith_power),
                new Buff("Second Judgement", EffectStatusIDs.EFST_SECOND_JUDGE, Resources._4RTools.Icons.second_judgement),
                new Buff("Third Exorcism Flame", EffectStatusIDs.EFST_THIRD_EXOR_FLAME, Resources._4RTools.Icons.third_exorcism_flame),
            };

            return skills;
        }

        //Ninja Skills
        public static List<Buff> GetNinjaSkills()
        {
            List<Buff> skills = new List<Buff>
            {
                new Buff("Cicada Skin Shed", EffectStatusIDs.PEEL_CHANGE, Resources._4RTools.Icons.nj_utsusemi),
                new Buff("Ninja Aura", EffectStatusIDs.AURA_NINJA, Resources._4RTools.Icons.nj_nen),
                new Buff("Mirror Image", EffectStatusIDs.EFST_NJ_BUNSINJYUTSU, Resources._4RTools.Icons.nj_bunsinjyutsu),
                new Buff("Izayoi", EffectStatusIDs.IZAYOI, Resources._4RTools.Icons.izayoi)
            };

            return skills;
        }

        //Taekwon Skills
        public static List<Buff> GetTaekwonSkills()
        {
            List<Buff> skills = new List<Buff>
            {
                new Buff("Mild Wind (Earth)", EffectStatusIDs.PROPERTYGROUND, Resources._4RTools.Icons.tk_mild_earth),
                new Buff("Mild Wind (Fire)", EffectStatusIDs.PROPERTYFIRE, Resources._4RTools.Icons.tk_mild_fire),
                new Buff("Mild Wind (Water)", EffectStatusIDs.PROPERTYWATER, Resources._4RTools.Icons.tk_mild_water),
                new Buff("Mild Wind (Wind)", EffectStatusIDs.PROPERTYWIND, Resources._4RTools.Icons.tk_mild_wind),
                new Buff("Mild Wind (Ghost)", EffectStatusIDs.PROPERTYTELEKINESIS, Resources._4RTools.Icons.tk_mild_ghost),
                new Buff("Mild Wind (Holy)", EffectStatusIDs.ASPERSIO, Resources._4RTools.Icons.tk_mild_holy),
                new Buff("Mild Wind (Shadow)", EffectStatusIDs.PROPERTYDARK, Resources._4RTools.Icons.tk_mild_shadow),
                new Buff("Calor solar", EffectStatusIDs.EFST_SG_SUN_WARM, Resources._4RTools.Icons.SG_SUN_WARM),
                new Buff("Proteção solar", EffectStatusIDs.EFST_SUN_COMFORT, Resources._4RTools.Icons.SG_SUN_COMFORT),
                new Buff("Calor lunar", EffectStatusIDs.EFST_SG_MOON_WARM, Resources._4RTools.Icons.SG_MOON_WARM),
                new Buff("Proteção lunar", EffectStatusIDs.EFST_MOON_COMFORT, Resources._4RTools.Icons.SG_MOON_COMFORT),
                new Buff("Calor estelar", EffectStatusIDs.EFST_SG_STAR_WARM, Resources._4RTools.Icons.SG_STAR_WARM),
                new Buff("Proteção estelar", EffectStatusIDs.EFST_STAR_COMFORT, Resources._4RTools.Icons.SG_STAR_COMFORT),
                new Buff("Tumbling", EffectStatusIDs.DODGE_ON, Resources._4RTools.Icons.tumbling),
                new Buff("Enchanting Sky", EffectStatusIDs.EFST_SKY_ENCHANT, Resources._4RTools.Icons.enchanting_sky),
                new Buff("Universal Stance", EffectStatusIDs.EFST_UNIVERSESTANCE, Resources._4RTools.Icons.universal_stance),
            };

            return skills;
        }


        public static List<Buff> GetGunsSkills()
        {
            List<Buff> skills = new List<Buff>();

            skills.Add(new Buff("Gatling Fever", EffectStatusIDs.GATLINGFEVER, Resources._4RTools.Icons.gatling_fever));
            skills.Add(new Buff("Madness Canceller", EffectStatusIDs.MADNESSCANCEL, Resources._4RTools.Icons.madnesscancel));
            skills.Add(new Buff("Adjustment", EffectStatusIDs.ADJUSTMENT, Resources._4RTools.Icons.adjustment));
            skills.Add(new Buff("Increase Accuracy", EffectStatusIDs.ACCURACY, Resources._4RTools.Icons.increase_accuracy));

            return skills;
        }

        // [Summoner](https://irowiki.org/wiki/Summoner)
        public static List<Buff> GetSummonerSkills()
        {
            List<Buff> skills = new List<Buff>
            {
                new Buff("Bunch of Shrimp", EffectStatusIDs.EFST_SHRIMP, Resources._4RTools.Icons.bunch_of_shrimp),
                new Buff("Temporary Communion", EffectStatusIDs.EFST_TEMPORARY_COMMUNION, Resources._4RTools.Icons.temporary_communion),
                new Buff("Marine Festival of Kisul", EffectStatusIDs.EFST_MARINE_FESTIVAL, Resources._4RTools.Icons.marine_festival_of_kisul),
                new Buff("Sandy Festival of Kisul", EffectStatusIDs.EFST_SANDY_FESTIVAL, Resources._4RTools.Icons.sandy_festival_of_kisul),
                new Buff("Colors of Hyunrok Lv 1", EffectStatusIDs.EFST_COLORS_OF_HYUN_ROK_1, Resources._4RTools.Icons.colors_of_hyunrok_1),
                new Buff("Colors of Hyunrok Lv 2", EffectStatusIDs.EFST_COLORS_OF_HYUN_ROK_2, Resources._4RTools.Icons.colors_of_hyunrok_2),
                new Buff("Colors of Hyunrok Lv 3", EffectStatusIDs.EFST_COLORS_OF_HYUN_ROK_3, Resources._4RTools.Icons.colors_of_hyunrok_3),
                new Buff("Colors of Hyunrok Lv 4", EffectStatusIDs.EFST_COLORS_OF_HYUN_ROK_4, Resources._4RTools.Icons.colors_of_hyunrok_4),
                new Buff("Colors of Hyunrok Lv 5", EffectStatusIDs.EFST_COLORS_OF_HYUN_ROK_5, Resources._4RTools.Icons.colors_of_hyunrok_5),
                new Buff("Colors of Hyunrok Lv 6", EffectStatusIDs.EFST_COLORS_OF_HYUN_ROK_6, Resources._4RTools.Icons.colors_of_hyunrok_6),
            };

            return skills;
        }

        // [Soul Ascetic](https://wiki.historyreborn.org/index.php/Soul_Ascetic)
        public static List<Buff> GetSoulAsceticSkills()
        {
            List<Buff> skills = new List<Buff>
            {
                new Buff("Soul of Heaven and Earth", EffectStatusIDs.EFST_HEAVEN_AND_EARTH, Resources._4RTools.Icons.soul_of_heaven_and_earth),
            };

            return skills;
        }

        // [Night Watch](https://wiki.ragna4th.com/Night_Watch)
        public static List<Buff> GetNightWatchSkills()
        {
            List<Buff> skills = new List<Buff>
            {
                new Buff("Hidden Card", EffectStatusIDs.EFST_HIDDEN_CARD, Resources._4RTools.Icons.hidden_card),
                new Buff("Intensive Aim", EffectStatusIDs.EFST_INTENSIVE_AIM, Resources._4RTools.Icons.intensive_aim),
                new Buff("Auto Firing Launcher", EffectStatusIDs.EFST_AUTO_FIRING_LAUNCHEREFST, Resources._4RTools.Icons.auto_firing_launcher),
                new Buff("Platinum Altar", EffectStatusIDs.EFST_P_ALTER, Resources._4RTools.Icons.platinum_altar),
                new Buff("Heat Barrel", EffectStatusIDs.EFST_HEAT_BARREL, Resources._4RTools.Icons.hit_barrel),
                new Buff("Eternal Chain", EffectStatusIDs.EFST_E_CHAIN, Resources._4RTools.Icons.eternal_chain),
                new Buff("Grenade Fragment Lv 1", EffectStatusIDs.EFST_GRENADE_FRAGMENT_1, Resources._4RTools.Icons.grenade_fragment_1),
                new Buff("Grenade Fragment Lv 2", EffectStatusIDs.EFST_GRENADE_FRAGMENT_2, Resources._4RTools.Icons.grenade_fragment_2),
                new Buff("Grenade Fragment Lv 3", EffectStatusIDs.EFST_GRENADE_FRAGMENT_3, Resources._4RTools.Icons.grenade_fragment_3),
                new Buff("Grenade Fragment Lv 4", EffectStatusIDs.EFST_GRENADE_FRAGMENT_4, Resources._4RTools.Icons.grenade_fragment_4),
                new Buff("Grenade Fragment Lv 5", EffectStatusIDs.EFST_GRENADE_FRAGMENT_5, Resources._4RTools.Icons.grenade_fragment_5),
                new Buff("Grenade Fragment Lv 6", EffectStatusIDs.EFST_GRENADE_FRAGMENT_6, Resources._4RTools.Icons.grenade_fragment_6),
            };

            return skills;
        }

        // [Hyper Novice](https://www.divine-pride.net/tools/skilltree/4307)
        public static List<Buff> GetHyperNoviceSkills()
        {
            List<Buff> skills = new List<Buff>
            {
                new Buff("Rule Break", EffectStatusIDs.EFST_RULEBREAK, Resources._4RTools.Icons.rule_break),
                new Buff("Breaking Limit", EffectStatusIDs.EFST_BREAKINGLIMIT, Resources._4RTools.Icons.breaking_limit),
            };

            return skills;
        }

        //--------------------- STUFFS ------------------------------
        //--------------------- Potions ------------------------------
        public static List<Buff> GetPotionsBuffs()
        {
            List<Buff> skills = new List<Buff>
            {
                new Buff("Concentration Potion", EffectStatusIDs.CONCENTRATION_POTION, Resources._4RTools.Icons.concentration_potiongif, true, BuffOrigin.Common),
                new Buff("Awakening Potion", EffectStatusIDs.AWAKENING_POTION, Resources._4RTools.Icons.awakening_potion, true, BuffOrigin.Common),
                new Buff("Berserk Potion", EffectStatusIDs.BERSERK_POTION, Resources._4RTools.Icons.berserk_potion, true, BuffOrigin.Common),
                new Buff("Regeneration Potion", EffectStatusIDs.REGENERATION_POTION, Resources._4RTools.Icons.regeneration, true, BuffOrigin.Rops),
                new Buff("HP Increase Potion", EffectStatusIDs.HP_INCREASE_POTION_LARGE, Resources._4RTools.Icons.ghp, true, BuffOrigin.Rops),
                new Buff("SP Increase Potion", EffectStatusIDs.SP_INCREASE_POTION_LARGE, Resources._4RTools.Icons.gsp, true, BuffOrigin.Rops),
                new Buff("Red Herb Activator", EffectStatusIDs.RED_HERB_ACTIVATOR, Resources._4RTools.Icons.red_herb_activator, false, BuffOrigin.Rops),
                new Buff("Blue Herb Activator", EffectStatusIDs.BLUE_HERB_ACTIVATOR, Resources._4RTools.Icons.blue_herb_activator, false, BuffOrigin.Rops),
                new Buff("Golden X", EffectStatusIDs.REF_T_POTION, Resources._4RTools.Icons.Golden_X, false, BuffOrigin.Rops),
                new Buff("Energy Drink", EffectStatusIDs.ENERGY_DRINK_RESERCH, Resources._4RTools.Icons.energetic_drink, false, BuffOrigin.Rops),
                new Buff("Mega Resist Potion", EffectStatusIDs.TARGET_BLOOD, Resources._4RTools.Icons.mega_resist_potion, false, BuffOrigin.Rops),
                new Buff("Full SwingK Potion", EffectStatusIDs.FULL_SWINGK, Resources._4RTools.Icons.swing_k, false, BuffOrigin.Rops),
                new Buff("Mana Plus Potion", EffectStatusIDs.MANA_PLUS, Resources._4RTools.Icons.mana_plus, false, BuffOrigin.Rops),
                new Buff("Blessing Of Tyr", EffectStatusIDs.BASICHIT, Resources._4RTools.Icons.blessing_of_tyr, true, BuffOrigin.Rops),
                new Buff("True Limit Power Booster (Poção Ilimitada)", EffectStatusIDs.EFST_LIMIT_POWER_BOOSTER, Resources._4RTools.Icons.true_limit_power_booster, false, BuffOrigin.Rops),
                new Buff("Infinity Drink", EffectStatusIDs.EFST_INFINITY_DRINK, Resources._4RTools.Icons.infinity_drink, false, BuffOrigin.Rops),
                new Buff("Red Booster (ATK +30, MATK +30)", EffectStatusIDs.RWC_2011_SCROLL, Resources._4RTools.Icons.red_booster, true, BuffOrigin.Rops),
                new Buff("Poção Fantástica (Sealed Kiel Card)", EffectStatusIDs.EFST_KIEL_CARD, Resources._4RTools.Icons.pocao_fantastica, false, BuffOrigin.Rops),
                new Buff("Poção do Furor Físico", EffectStatusIDs.EFST_DF_FULLSWINGK, Resources._4RTools.Icons.full_swingK, false, BuffOrigin.Rops),
                new Buff("Poção Mágica", EffectStatusIDs.EFST_DRACULA_CARD, Resources._4RTools.Icons.pocao_magica, false, BuffOrigin.Rops),
                new Buff("True ASPD Intensifying Potion", EffectStatusIDs.EFST_REUSE_LIMIT_ASPD_POTION, Resources._4RTools.Icons.G_ASPD_Potion, false, BuffOrigin.Rops),
                new Buff("True Medium Life Potion", EffectStatusIDs.EFST_L_LIFEPOTION, Resources._4RTools.Icons.g_med_life_potion, true, BuffOrigin.Rops),
            };

            return skills;
        }

        public static List<Buff> GetElementalsBuffs()
        {
            List<Buff> skills = new List<Buff>
            {
                new Buff("Elemental Converter (Fire)", EffectStatusIDs.EFST_ATTACK_PROPERTY_FIRE, Resources._4RTools.Icons.PROPERTY_FIRE, false, BuffOrigin.Common),
                new Buff("Elemental Converter (Wind)", EffectStatusIDs.EFST_ATTACK_PROPERTY_WIND, Resources._4RTools.Icons.PROPERTY_WIND, false, BuffOrigin.Common),
                new Buff("Elemental Converter (Earth)", EffectStatusIDs.EFST_ATTACK_PROPERTY_GROUND, Resources._4RTools.Icons.PROPERTY_GROUND, false, BuffOrigin.Common),
                new Buff("Elemental Converter (Water)", EffectStatusIDs.EFST_ATTACK_PROPERTY_WATER, Resources._4RTools.Icons.PROPERTY_WATER, false, BuffOrigin.Common),
                new Buff("Cursed Water", EffectStatusIDs.EFST_ATTACK_PROPERTY_DARKNESS, Resources._4RTools.Icons.cursed_water, true, BuffOrigin.Common),
                new Buff("Fire Conversor", EffectStatusIDs.PROPERTYFIRE, Resources._4RTools.Icons.ele_fire_converter, true, BuffOrigin.Common),
                new Buff("Wind Conversor", EffectStatusIDs.PROPERTYWIND, Resources._4RTools.Icons.ele_wind_converter, true, BuffOrigin.Common),
                new Buff("Earth Conversor", EffectStatusIDs.PROPERTYGROUND, Resources._4RTools.Icons.ele_earth_converter, true, BuffOrigin.Common),
                new Buff("Water Conversor", EffectStatusIDs.PROPERTYWATER, Resources._4RTools.Icons.ele_water_converter, true, BuffOrigin.Common),
                new Buff("Aspersio Scroll", EffectStatusIDs.ASPERSIO, Resources._4RTools.Icons.ele_holy_converter, true, BuffOrigin.Rops),
                new Buff("GHOST Conversor", EffectStatusIDs.PROPERTYTELEKINESIS, Resources._4RTools.Icons.ele_ghost_converter, false, BuffOrigin.Common),
                new Buff("Fireproof Potion", EffectStatusIDs.RESIST_PROPERTY_FIRE, Resources._4RTools.Icons.fireproof, true, BuffOrigin.Common),
                new Buff("Waterproof Potion", EffectStatusIDs.RESIST_PROPERTY_WATER, Resources._4RTools.Icons.coldproof, true, BuffOrigin.Common),
                new Buff("Windproof Potion", EffectStatusIDs.RESIST_PROPERTY_WIND, Resources._4RTools.Icons.thunderproof, true, BuffOrigin.Common),
                new Buff("Earthproof Potion", EffectStatusIDs.RESIST_PROPERTY_GROUND, Resources._4RTools.Icons.earhproof, true, BuffOrigin.Common)
            };

            return skills;
        }

        public static List<Buff> GetFoodBuffs()
        {
            List<Buff> skills = new List<Buff>
            {
                new Buff("STR Food", EffectStatusIDs.FOOD_STR, Resources._4RTools.Icons.strfood, true, BuffOrigin.Common),
                new Buff("AGI Food", EffectStatusIDs.FOOD_AGI, Resources._4RTools.Icons.agi_food, true, BuffOrigin.Common),
                new Buff("VIT Food", EffectStatusIDs.FOOD_VIT, Resources._4RTools.Icons.vit_food, true, BuffOrigin.Common),
                new Buff("INT Food", EffectStatusIDs.FOOD_INT, Resources._4RTools.Icons.int_food, true, BuffOrigin.Common),
                new Buff("DEX Food", EffectStatusIDs.FOOD_DEX, Resources._4RTools.Icons.dex_food, true, BuffOrigin.Common),
                new Buff("LUK Food", EffectStatusIDs.FOOD_LUK, Resources._4RTools.Icons.luk_food, true, BuffOrigin.Common),

                new Buff("3RD STR Food", EffectStatusIDs.STR_3RD_FOOD, Resources._4RTools.Icons.str_3rd_food, false, BuffOrigin.Rops),
                new Buff("3RD AGI Food", EffectStatusIDs.AGI_3RD_FOOD, Resources._4RTools.Icons.agi_3rd_food, false, BuffOrigin.Rops),
                new Buff("3RD VIT Food", EffectStatusIDs.VIT_3RD_FOOD, Resources._4RTools.Icons.vit_3rd_food, false, BuffOrigin.Rops),
                new Buff("3RD INT Food", EffectStatusIDs.INT_3RD_FOOD, Resources._4RTools.Icons.int_3rd_food, false, BuffOrigin.Rops),
                new Buff("3RD DEX Food", EffectStatusIDs.DEX_3RD_FOOD, Resources._4RTools.Icons.dex_3rd_food, false, BuffOrigin.Rops),
                new Buff("3RD LUK Food", EffectStatusIDs.LUK_3RD_FOOD, Resources._4RTools.Icons.luk_3rd_food, false, BuffOrigin.Rops),
                new Buff("Almighty", EffectStatusIDs.RWC_2011_SCROLL, Resources._4RTools.Icons.almighty, true, BuffOrigin.Rops),
                new Buff("CASH Food", EffectStatusIDs.FOOD_VIT_CASH, Resources._4RTools.Icons.cash_food, true, BuffOrigin.Rops),
                new Buff("Acarajé", EffectStatusIDs.EFST_ACARAJE, Resources._4RTools.Icons.acaraje, true, BuffOrigin.Rops),

                new Buff("STR Biscuit Stick", EffectStatusIDs.STR_Biscuit_Stick, Resources._4RTools.Icons.str_biscuit_stick, false, BuffOrigin.Rops),
                new Buff("AGI Biscuit Stick", EffectStatusIDs.AGI_Biscuit_Stick, Resources._4RTools.Icons.agi_biscuit_stick, false, BuffOrigin.Rops),
                new Buff("VIT Biscuit Stick", EffectStatusIDs.VIT_Biscuit_Stick, Resources._4RTools.Icons.vit_biscuit_stick, false, BuffOrigin.Rops),
                new Buff("INT Biscuit Stick", EffectStatusIDs.INT_Biscuit_Stick, Resources._4RTools.Icons.int_biscuit_stick, false, BuffOrigin.Rops),
                new Buff("DEX Biscuit Stick", EffectStatusIDs.DEX_Biscuit_Stick, Resources._4RTools.Icons.dex_biscuit_stick, false, BuffOrigin.Rops),
                new Buff("LUK Biscuit Stick", EffectStatusIDs.LUK_Biscuit_Stick, Resources._4RTools.Icons.luk_biscuit_stick, false, BuffOrigin.Rops),

                new Buff("STR Bubble Gum Green", EffectStatusIDs.EFST_Bubble_Gum_Green, Resources._4RTools.Icons.str_bubble_gum_green, false, BuffOrigin.Rops),
                new Buff("AGI Bubble Gum Red", EffectStatusIDs.EFST_Bubble_Gum_Red, Resources._4RTools.Icons.agi_bubble_gum_red, false, BuffOrigin.Rops),
                new Buff("INT Bubble Gum Yellow", EffectStatusIDs.EFST_Bubble_Gum_Yellow, Resources._4RTools.Icons.int_bubble_gum_yellow, false, BuffOrigin.Rops),
                new Buff("DEX Bubble Gum Orange", EffectStatusIDs.EFST_Bubble_Gum_Orange, Resources._4RTools.Icons.dex_bubble_gum_orange, false, BuffOrigin.Rops),

                new Buff("Winter Cookie ATK ", EffectStatusIDs.EFST_ATK_POPCORN, Resources._4RTools.Icons.winter_cookie, false, BuffOrigin.Rops),
                new Buff("Flora Cookie MATK", EffectStatusIDs.EFST_MATK_POPCORN, Resources._4RTools.Icons.flora_cookie, false, BuffOrigin.Rops),
                new Buff("Banquete Revigorante", EffectStatusIDs.STR_3RD_FOOD, Resources._4RTools.Icons.banquete, false, BuffOrigin.Rops),
            };

            return skills;
        }

        public static List<Buff> GetBoxesBuffs()
        {
            List<Buff> skills = new List<Buff>
            {
                new Buff("Drowsiness Box", EffectStatusIDs.DROWSINESS_BOX, Resources._4RTools.Icons.drowsiness, true, BuffOrigin.Common),
                new Buff("Resentment Box", EffectStatusIDs.RESENTMENT_BOX, Resources._4RTools.Icons.resentment, true, BuffOrigin.Common),
                new Buff("Sunlight Box", EffectStatusIDs.SUNLIGHT_BOX, Resources._4RTools.Icons.sunbox, true, BuffOrigin.Common),
                new Buff("Box of Gloom", EffectStatusIDs.CONCENTRATION, Resources._4RTools.Icons.gloom, true, BuffOrigin.Common),
                new Buff("Box of Thunder", EffectStatusIDs.BOX_OF_THUNDER, Resources._4RTools.Icons.speed, true, BuffOrigin.Common),
                new Buff("Speed Potion / Guyak", EffectStatusIDs.SPEED_POT, Resources._4RTools.Icons.speedpotion, true, BuffOrigin.Common),
                new Buff("Anodyne", EffectStatusIDs.ENDURE, Resources._4RTools.Icons.anodyne, true, BuffOrigin.Common),
                new Buff("Aloevera", EffectStatusIDs.PROVOKE, Resources._4RTools.Icons.aloevera, true, BuffOrigin.Common),
                new Buff("Abrasivo", EffectStatusIDs.CRITICALPERCENT, Resources._4RTools.Icons.abrasive, true, BuffOrigin.Rops),
                new Buff("Combat Pill", EffectStatusIDs.COMBAT_PILL, Resources._4RTools.Icons.combat_pill, true, BuffOrigin.Rops),
                new Buff("Advance Combat Pill", EffectStatusIDs.EFST_GM_BATTLE2, Resources._4RTools.Icons.advance_combat_pill, true, BuffOrigin.Rops),
                new Buff("Celermine Juice", EffectStatusIDs.ENRICH_CELERMINE_JUICE, Resources._4RTools.Icons.celermine, true, BuffOrigin.Rops),
                new Buff("Guarana Candy", EffectStatusIDs.SPEED_POT, Resources._4RTools.Icons.guarana_candy, true, BuffOrigin.Common),
                new Buff("Poison Bottle", EffectStatusIDs.ASPDPOTIONINFINITY, Resources._4RTools.Icons.poison, true, BuffOrigin.Common),
            };

            return skills;
        }

        public static List<Buff> GetScrollBuffs()
        {
            List<Buff> skills = new List<Buff>
            {
                new Buff("Éden Scroll", EffectStatusIDs.EFST_EDEN, Resources._4RTools.Icons.eden_scroll, false, BuffOrigin.Rops),
                new Buff("Increase Agility Scroll", EffectStatusIDs.INC_AGI, Resources._4RTools.Icons.al_incagi1, true, BuffOrigin.Rops),
                new Buff("Bless Scroll", EffectStatusIDs.BLESSING, Resources._4RTools.Icons.al_blessing1, true, BuffOrigin.Rops),
                new Buff("Full Chemical Protection (Scroll)", EffectStatusIDs.PROTECTARMOR, Resources._4RTools.Icons.cr_fullprotection, true, BuffOrigin.Rops),
                new Buff("Burn Incense",  EffectStatusIDs.EFST_BURNT_INCENSE, Resources._4RTools.Icons.burnt_incense, false, BuffOrigin.Rops),
                new Buff("Link Scroll", EffectStatusIDs.SOULLINK, Resources._4RTools.Icons.sl_soullinker, true, BuffOrigin.Rops),
                new Buff("Monster Transform",  EffectStatusIDs.MONSTER_TRANSFORM, Resources._4RTools.Icons.mob_transform, false, BuffOrigin.Rops),
                new Buff("Assumptio",  EffectStatusIDs.ASSUMPTIO, Resources._4RTools.Icons.assumptio, true, BuffOrigin.Rops),
                new Buff("Holy Armor Scroll",  EffectStatusIDs.EFST_ARMOR_PROPERTY, Resources._4RTools.Icons.holy_armor, true, BuffOrigin.Rops),
                new Buff("Shadow Armor Scroll",  EffectStatusIDs.EFST_ARMOR_PROPERTY, Resources._4RTools.Icons.shadow_armor_scroll, true, BuffOrigin.Rops),
                new Buff("Soul Scroll",  EffectStatusIDs.EFST_SOULSCROLL, Resources._4RTools.Icons.soul_scroll, false, BuffOrigin.Rops),
                new Buff("Undead Element Scroll",  EffectStatusIDs.EFST_RESIST_PROPERTY_UNDEAD, Resources._4RTools.Icons.undead_element_scroll, false, BuffOrigin.Rops),
            };

            return skills;
        }

        public static List<Buff> GetETCBuffs()
        {
            List<Buff> skills = new List<Buff>
            {
                new Buff("THURISAZ Rune", EffectStatusIDs.THURISAZ, Resources._4RTools.Icons.THURISAZ, false, BuffOrigin.Common),
                new Buff("OTHILA Rune", EffectStatusIDs.OTHILA, Resources._4RTools.Icons.OTHILA, false, BuffOrigin.Common),
                new Buff("HAGALAZ Rune", EffectStatusIDs.HAGALAZ, Resources._4RTools.Icons.HAGALAZ, false, BuffOrigin.Common),
                new Buff("LUX AMINA Rune", EffectStatusIDs.LUX_AMINA, Resources._4RTools.Icons.LUX_AMINA, false, BuffOrigin.Common),
                new Buff("Cat Can", EffectStatusIDs.OVERLAPEXPUP, Resources._4RTools.Icons.cat_can, false, BuffOrigin.Rops),
                new Buff("HE Bubble Gum", EffectStatusIDs.CASH_RECEIVEITEM, Resources._4RTools.Icons.he_bubble_gum2, true, BuffOrigin.Rops),
                new Buff("Frost Giant Blood (GvG GIANT)", EffectStatusIDs.EFST_GVG_GIANT, Resources._4RTools.Icons.frost_giant_blood, false, BuffOrigin.Rops),
                new Buff("Battle Manual (GvG GOLEM)", EffectStatusIDs.EFST_GVG_GOLEM, Resources._4RTools.Icons.golem_stone, false, BuffOrigin.Rops),
                new Buff("Magic Candy", EffectStatusIDs.EFST_MAGIC_CANDY, Resources._4RTools.Icons.magic_candy, false, BuffOrigin.Rops),
                new Buff("Ghostring", EffectStatusIDs.EFST_GHOSTRING, Resources._4RTools.Icons.perg_ghostring, false, BuffOrigin.Rops),
                new Buff("Angeling", EffectStatusIDs.EFST_ANGELING, Resources._4RTools.Icons.perg_angeling, false, BuffOrigin.Rops),
                new Buff("Tao Gunka", EffectStatusIDs.EFST_TAO_GUNKA, Resources._4RTools.Icons.perg_taogunka, false, BuffOrigin.Rops),
                new Buff("Orc Lord", EffectStatusIDs.EFST_ORC_LORD, Resources._4RTools.Icons.perg_senhororc, false, BuffOrigin.Rops),
                new Buff("Orc Hero", EffectStatusIDs.EFST_ORC_HERO, Resources._4RTools.Icons.perg_orcheroi, false, BuffOrigin.Rops),
                new Buff("MISTRESS", EffectStatusIDs.EFST_MISTRESS, Resources._4RTools.Icons.perg_abelha, false, BuffOrigin.Rops),
            };

            return skills;
        }

        public static List<Buff> GetCandiesBuffs()
        {
            List<Buff> skills = new List<Buff>
            {
                new Buff("Sweets Macaron Cake", EffectStatusIDs.EFST_SWEETSFAIR_ATK, Resources._4RTools.Icons.sweets_macacake, false, BuffOrigin.Rops),
                new Buff("Sweets Strawberry Parfait", EffectStatusIDs.EFST_SWEETSFAIR_MATK, Resources._4RTools.Icons.sweets_sparfait, false, BuffOrigin.Rops),
                new Buff("Popcorn Festival Buff", EffectStatusIDs.EFST_FLOWER_LEAF2, Resources._4RTools.Icons.pop_corn_fes_buff, false, BuffOrigin.Rops),
                new Buff("Doce Hiper Açucarado", EffectStatusIDs.EFST_STEAMPACK, Resources._4RTools.Icons.spark_candy, false, BuffOrigin.Rops),
                new Buff("Elixir Ultra Milagroso", EffectStatusIDs.EFST_ALMIGHTY, Resources._4RTools.Icons.g_almighty, false, BuffOrigin.Rops),
                new Buff("Cherry Blossom Rice Cake", EffectStatusIDs.EFST_FLOWER_LEAF4, Resources._4RTools.Icons.cherry_blossom_cake, false, BuffOrigin.Rops),
            };

            return skills;
        }

        public static List<Buff> GetEXPBuffs()
        {
            List<Buff> skills = new List<Buff>
            {
                new Buff("Goma de Mascar", EffectStatusIDs.CASH_RECEIVEITEM, Resources._4RTools.Icons.he_bubble_gum, true, BuffOrigin.Rops),
                new Buff("Manual de Combate", EffectStatusIDs.CASH_PLUSEXP, Resources._4RTools.Icons.combat_manual_base, true, BuffOrigin.Rops),
                new Buff("Manual de Combate de classe", EffectStatusIDs.CASH_PLUSECLASSXP, Resources._4RTools.Icons.combat_manual_class, true, BuffOrigin.Rops),
            };

            return skills;
        }

        public static List<Buff> GetCommonPotionsAndUtilities()
        {
            List<Buff> result = new List<Buff>();
            foreach (Buff b in GetPotionsBuffs())
            {
                if (b.IsPreRenewal && b.Origin == BuffOrigin.Common) result.Add(b);
            }
            foreach (Buff b in GetBoxesBuffs())
            {
                if (b.IsPreRenewal && b.Origin == BuffOrigin.Common
                    && (b.name == "Speed Potion / Guyak" || b.name == "Anodyne" || b.name == "Aloevera" || b.name == "Guarana Candy" || b.name == "Poison Bottle"))
                {
                    result.Add(b);
                }
            }
            return result;
        }

        public static List<Buff> GetCommonElementals()
        {
            List<Buff> result = new List<Buff>();
            foreach (Buff b in GetElementalsBuffs())
            {
                if (b.IsPreRenewal && b.Origin == BuffOrigin.Common) result.Add(b);
            }
            return result;
        }

        public static List<Buff> GetCommonFoodsAndBoxes()
        {
            List<Buff> result = new List<Buff>();
            foreach (Buff b in GetFoodBuffs())
            {
                if (b.IsPreRenewal && b.Origin == BuffOrigin.Common) result.Add(b);
            }
            foreach (Buff b in GetBoxesBuffs())
            {
                if (b.IsPreRenewal && b.Origin == BuffOrigin.Common
                    && b.name != "Speed Potion / Guyak" && b.name != "Anodyne" && b.name != "Aloevera" && b.name != "Guarana Candy" && b.name != "Poison Bottle")
                {
                    result.Add(b);
                }
            }
            return result;
        }

        public static List<Buff> GetRopsCombatAndPotions()
        {
            List<Buff> result = new List<Buff>();
            foreach (Buff b in GetPotionsBuffs())
            {
                if (b.IsPreRenewal && b.Origin == BuffOrigin.Rops) result.Add(b);
            }
            foreach (Buff b in GetBoxesBuffs())
            {
                if (b.IsPreRenewal && b.Origin == BuffOrigin.Rops) result.Add(b);
            }
            return result;
        }

        public static List<Buff> GetRopsFoodsAndExp()
        {
            List<Buff> result = new List<Buff>();
            foreach (Buff b in GetFoodBuffs())
            {
                if (b.IsPreRenewal && b.Origin == BuffOrigin.Rops) result.Add(b);
            }
            foreach (Buff b in GetEXPBuffs())
            {
                if (b.IsPreRenewal && b.Origin == BuffOrigin.Rops) result.Add(b);
            }
            foreach (Buff b in GetETCBuffs())
            {
                if (b.IsPreRenewal && b.Origin == BuffOrigin.Rops) result.Add(b);
            }
            return result;
        }

        public static List<Buff> GetRopsScrolls()
        {
            List<Buff> result = new List<Buff>();
            foreach (Buff b in GetScrollBuffs())
            {
                if (b.IsPreRenewal && b.Origin == BuffOrigin.Rops) result.Add(b);
            }
            foreach (Buff b in GetElementalsBuffs())
            {
                if (b.IsPreRenewal && b.Origin == BuffOrigin.Rops) result.Add(b);
            }
            return result;
        }

        public static List<Buff> GetRenewalAndCustomConsumables()
        {
            List<Buff> result = new List<Buff>();
            List<Buff>[] sources = new List<Buff>[]
            {
                GetPotionsBuffs(),
                GetElementalsBuffs(),
                GetFoodBuffs(),
                GetBoxesBuffs(),
                GetScrollBuffs(),
                GetETCBuffs(),
                GetCandiesBuffs(),
                GetEXPBuffs()
            };

            foreach (List<Buff> source in sources)
            {
                foreach (Buff b in source)
                {
                    if (!b.IsPreRenewal)
                    {
                        result.Add(b);
                    }
                }
            }

            return result;
        }

        public static List<Buff> GetHomunculusBuffs()
        {
            List<Buff> skills = new List<Buff>
            {
                new Buff("Pyroclastic", EffectStatusIDs.EFST_PYROCLASTIC, Resources._4RTools.Icons.pyroclastic),
                new Buff("HOMUN_LAST", EffectStatusIDs.EFST_TEMPERING, Resources._4RTools.Icons.homun_last),
            };

            return skills;
        }

        //--------------------- DEBUFFS (Pre-Renewal AureumRO — only item/skill curable) ----------------
        public static List<Buff> GetPanaceaCurableDebuffs()
        {
            return new List<Buff>
            {
                new Buff("Poison", EffectStatusIDs.POISON, Resources._4RTools.Icons.poison_status),
                new Buff("Silence", EffectStatusIDs.SILENCE, Resources._4RTools.Icons.silence),
                new Buff("Blind", EffectStatusIDs.BLIND, Resources._4RTools.Icons.blind),
                new Buff("Curse", EffectStatusIDs.CURSE, Resources._4RTools.Icons.curse),
                new Buff("Confusion / chaos", EffectStatusIDs.CONFUSION, Resources._4RTools.Icons.chaos),
                new Buff("Hallucination", EffectStatusIDs.HALLUCINATION, Resources._4RTools.Icons.hallucination),
            };
        }

        public static List<Buff> GetAgilityDebuffs()
        {
            return new List<Buff>
            {
                new Buff("Decrease Agi", EffectStatusIDs.EFST_DECREASE_AGI, Resources._4RTools.Icons.decrease_agi),
            };
        }

        public static List<Buff> GetDebuffs()
        {
            List<Buff> all = new List<Buff>();
            all.AddRange(GetPanaceaCurableDebuffs());
            all.AddRange(GetAgilityDebuffs());
            return all;
        }
    }
}