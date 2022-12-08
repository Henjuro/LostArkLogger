﻿namespace InetOptimizer
{    public enum StatusEffectType : int
    {
        STATUS_EFFECT_TYPE_NONE = 0,
        STATUS_EFFECT_TYPE_PHYSICAL_DAMAGE = 1,
        STATUS_EFFECT_TYPE_MAGICAL_DAMAGE = 2,
        STATUS_EFFECT_TYPE_HEAL = 3,
        STATUS_EFFECT_TYPE_FREEZE = 4,
        STATUS_EFFECT_TYPE_STONE = 5,
        STATUS_EFFECT_TYPE_FEAR = 6,
        STATUS_EFFECT_TYPE_STUN = 7,
        STATUS_EFFECT_TYPE_SLEEP = 8,
        STATUS_EFFECT_TYPE_EARTHQUAKE = 9,
        STATUS_EFFECT_TYPE_CURSE = 10,
        STATUS_EFFECT_TYPE_WEAKEN_DEFENSE = 11,
        STATUS_EFFECT_TYPE_WEAKEN_RESISTANCE = 12,
        STATUS_EFFECT_TYPE_DEATH_SENTENCE = 13,
        STATUS_EFFECT_TYPE_SILENCE = 14,
        STATUS_EFFECT_TYPE_DARKNESS = 15,
        STATUS_EFFECT_TYPE_VERMIN = 16,
        STATUS_EFFECT_TYPE_BLEEDING = 17,
        STATUS_EFFECT_TYPE_POISONING = 18,
        STATUS_EFFECT_TYPE_ELECTROCUTION = 19,
        STATUS_EFFECT_TYPE_BURN = 20,
        STATUS_EFFECT_TYPE_MOVE_SPEED_DOWN = 21,
        STATUS_EFFECT_TYPE_ALL_SPEED_DOWN = 22,
        STATUS_EFFECT_TYPE_INVINCIBILITY = 23,
        STATUS_EFFECT_TYPE_SHIELD = 24,
        STATUS_EFFECT_TYPE_REPLENISH_MP = 25,
        STATUS_EFFECT_TYPE_REPLENISH_MP_RATE = 26,
        STATUS_EFFECT_TYPE_ABSORB_AREA = 27,
        STATUS_EFFECT_TYPE_SKILL_DAMAGE_AMPLIFY = 28,
        STATUS_EFFECT_TYPE_MINION_EVENT = 29,
        STATUS_EFFECT_TYPE_CHANGE_HIT_FLAG = 30,
        STATUS_EFFECT_TYPE_IDENTITY_GAUGE = 31,
        STATUS_EFFECT_TYPE_BEATTACKED_DAMAGE_AMPLIFY = 32,
        STATUS_EFFECT_TYPE_POLYMORPH_PC = 33,
        STATUS_EFFECT_TYPE_SUPER_ARMOR = 34,
        STATUS_EFFECT_TYPE_INVOKE_SKILL_EFFECT = 35,
        STATUS_EFFECT_TYPE_FORCED_MOVE = 36,
        STATUS_EFFECT_TYPE_CONFINEMENT = 37,
        STATUS_EFFECT_TYPE_ACTION_DISABLE = 38,
        STATUS_EFFECT_TYPE_HERBALISM_INSTANT_DURABILITY_IGNORE = 39,
        STATUS_EFFECT_TYPE_MINING_ADD_CASTING_SPEED = 40,
        STATUS_EFFECT_TYPE_WOUND = 41,
        STATUS_EFFECT_TYPE_WILD_GROWTH = 42,
        STATUS_EFFECT_TYPE_NEAR_DEATH_EXPERIENCE = 43,
        STATUS_EFFECT_TYPE_CHANGE_FACTION = 44,
        STATUS_EFFECT_TYPE_NPC_PART_INVINCIBILITY = 45,
        STATUS_EFFECT_TYPE_LUMBERING_SHARPEN = 46,
        STATUS_EFFECT_TYPE_DISGUISE = 47,
        STATUS_EFFECT_TYPE_BECHASED_NPC = 48,
        STATUS_EFFECT_TYPE_FISHING_SCHOOL = 49,
        STATUS_EFFECT_TYPE_HERBALISM_LIFE_ETHER = 50,
        STATUS_EFFECT_TYPE_HERBALISM_DELICATE_HANDS = 51,
        STATUS_EFFECT_TYPE_SHIP_BOOST_GAUGE = 52,
        STATUS_EFFECT_TYPE_HUNTING_CHASE = 53,
        STATUS_EFFECT_TYPE_ARCHEOLOGY_DETECTION = 54,
        STATUS_EFFECT_TYPE_ARCHEOLOGY_CONECTRATION = 55,
        STATUS_EFFECT_TYPE_ARCHEOLOGY_SENSE_OF_TOMBRAIDER = 56,
        STATUS_EFFECT_TYPE_RESET_COOLDOWN = 57,
        STATUS_EFFECT_TYPE_PROVOKE = 58,
        STATUS_EFFECT_TYPE_GHOST = 59,
        STATUS_EFFECT_TYPE_LUMBERING_FIND_TREE = 60,
        STATUS_EFFECT_TYPE_LIFE_CASTING_SPEED = 61,
        STATUS_EFFECT_TYPE_LIFE_TOOL_DESTROY_RATE = 62,
        STATUS_EFFECT_TYPE_PROTECT = 63,
        STATUS_EFFECT_TYPE_PART_CORROSION = 64,
        STATUS_EFFECT_TYPE_VOYAGE_SUPPLY_ACCELERATE = 65,
        STATUS_EFFECT_TYPE_VOYAGE_SUPPLY_FLUCTUATE = 66,
        STATUS_EFFECT_TYPE_VOYAGE_ACTION_DISABLE = 67,
        STATUS_EFFECT_TYPE_VOYAGE_BOOST_GAUGE_UNOBTAINABLE = 68,
        STATUS_EFFECT_TYPE_VOYAGE_IMMUNE_EVENT = 69,
        STATUS_EFFECT_TYPE_SKILL_DAMAGE_AMPLIFY_ATTACK = 70,
        STATUS_EFFECT_TYPE_VOYAGE_ADD_EVENT_GAUGE = 71,
        STATUS_EFFECT_TYPE_PROVOKE_RESIST = 72,
        STATUS_EFFECT_TYPE_IGNITE = 73,
        STATUS_EFFECT_TYPE_HERBALISM_VITALITY_ETHER = 74,
        STATUS_EFFECT_TYPE_HERBALISM_GOLDEN_FINGER = 75,
        STATUS_EFFECT_TYPE_LIFE_ADD_SUCCESS_RATE = 76,
        STATUS_EFFECT_TYPE_IGNORE_IMMUNE = 77,
        STATUS_EFFECT_TYPE_FIXED_DAMAGE_SELF = 78,
        STATUS_EFFECT_TYPE_VOYAGE_BOOST_GAUGE_FLUCTUATE = 79,
        STATUS_EFFECT_TYPE_CHANGE_AI_POINT = 80,
        STATUS_EFFECT_TYPE_AURA = 81,
        STATUS_EFFECT_TYPE_LIFE_PLUS_SUCCESS_RATE = 82,
        STATUS_EFFECT_TYPE_LIFE_MULTIPLY_SUCCESS_RATE = 83,
        STATUS_EFFECT_TYPE_FISHING_BARE_HANDS = 84,
        STATUS_EFFECT_TYPE_FISHING_CAST_BAIT = 85,
        STATUS_EFFECT_TYPE_COLLISION_DISABLE = 86,
        STATUS_EFFECT_TYPE_BACK_ATTACK_AMPLIFY = 87,
        STATUS_EFFECT_TYPE_INSTANT_STAT_AMPLIFY = 88,
        STATUS_EFFECT_TYPE_SHIP_WRECK = 89,
        STATUS_EFFECT_TYPE_BURN_MP = 90,
        STATUS_EFFECT_TYPE_VOYAGE_LUCK_RECOVERY_INCREMENT = 91,
        STATUS_EFFECT_TYPE_AI_POINT_AMPLIFY = 92,
        STATUS_EFFECT_TYPE_LIFE_MULTIPLY_EXP_RATE = 93,
        STATUS_EFFECT_TYPE_PVP_TOKEN_REWARD_INCREASE_PERCENT = 94,
        STATUS_EFFECT_TYPE_VOYAGE_LUCK_DROP_AMPLIFY = 95,
        STATUS_EFFECT_TYPE_INCREASE_IDENTITY_GAUGE = 96,
        STATUS_EFFECT_TYPE_PC_STAT_MIN_MAX_FIX = 97,
        STATUS_EFFECT_TYPE_REVERSE_RUIN_DROP_INCREASE_PERCENT = 98,
        STATUS_EFFECT_TYPE_NOTICE_GAUGE = 99,
        STATUS_EFFECT_TYPE_BACK_ATTACK_RESIST = 100,
        STATUS_EFFECT_TYPE_DIRECTIONAL_ATTACK_AMPLIFY = 101,
        STATUS_EFFECT_TYPE_HUNTING_OBSERVE = 102,
        STATUS_EFFECT_TYPE_NOTE_KEY_INPUT = 103,
        STATUS_EFFECT_TYPE_ATTACK_POWER_AMPLIFY = 104,
        STATUS_EFFECT_TYPE_TIME_STOP = 105,
        STATUS_EFFECT_TYPE_PHEROMONE = 106,
        STATUS_EFFECT_TYPE_LIFE_DROP_ADD_RATE = 107,
        STATUS_EFFECT_TYPE_LIFE_DURABILITY_RATE = 108,
        STATUS_EFFECT_TYPE_INSTANT_STAT_AMPLIFY_BY_CONTENTS = 109,
        STATUS_EFFECT_TYPE_VOYAGE_PAUSE_EVENT = 110,
        STATUS_EFFECT_TYPE_LIFE_EXP_ADD_RATE = 111,
        STATUS_EFFECT_TYPE_REVERSE_RUIN_ADD_EXP_RATE = 112,
        STATUS_EFFECT_TYPE_LINKABLE_INVOKE_EFFECT = 113,
        STATUS_EFFECT_TYPE_BULLET_TIME = 114,
        STATUS_EFFECT_TYPE_REFLECT_DAMAGE = 115,
        STATUS_EFFECT_TYPE_REVERSE = 116,
        STATUS_EFFECT_TYPE_DETECT = 117,
        STATUS_EFFECT_TYPE_MAP_SYMBOL = 118,
        STATUS_EFFECT_TYPE_MAP_SYMBOL_HIDE = 119,
        STATUS_EFFECT_TYPE_FORCE_FIELD = 120,
        STATUS_EFFECT_TYPE_MIND_CONTROL = 121,
        STATUS_EFFECT_TYPE_HIDE_TARGET_UI = 122,
        STATUS_EFFECT_TYPE_MASKING = 123,
        STATUS_EFFECT_TYPE_MANA_SHIELD = 124,
        STATUS_EFFECT_TYPE_DETECTED_BUSH = 125,
        STATUS_EFFECT_TYPE_PARALYZATION = 126,
        STATUS_EFFECT_TYPE_CONFUSION = 127,
        STATUS_EFFECT_TYPE_CHANGE_MATERIAL = 128,
        STATUS_EFFECT_TYPE_LINKED_TARGET_INVOKE = 129,
        STATUS_EFFECT_TYPE_MAX = 130,
    }
}