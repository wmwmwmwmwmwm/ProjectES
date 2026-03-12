using UnityEngine;

public static class Const
{
	public const float TimeDefault = -10000f;
}

public static class SceneName
{
	public const string First = "First";
	public const string Title = "Title";
	public const string Loading = "Loading";
	public const string Battle = "Battle";

	// 환경
	public const string Glacier = "Glacier";
}

public static class StageName
{
	public const string Tutorial = "Tutorial";
}

//public static class SortingLayerName
//{
//	public const string Bg = "Bg";
//	public const string Default = "Default";
//	public const string Effect = "Effect";
//	public const string UI = "UI";
//}

public static class Layer
{
	public const string Terrain = "Terrain";
	public const string Player = "Player";
	public const string Enemy = "Enemy";

	public static LayerMask TerrainLayer => LayerMask.NameToLayer(Terrain);
	public static LayerMask PlayerLayer => LayerMask.NameToLayer(Player);
	public static LayerMask EnemyLayer => LayerMask.NameToLayer(Enemy);

	public static LayerMask TerrainLayerMask => 1 << LayerMask.NameToLayer(Terrain);
	public static LayerMask PlayerLayerMask => 1 << LayerMask.NameToLayer(Player);
	public static LayerMask EnemyLayerMask => 1 << LayerMask.NameToLayer(Enemy);
}

//public static class SortingLayer
//{
//	public const string DeadUnit = "DeadUnit";
//	public const string AliveUnit = "AliveUnit";
//	public const string SkillEffect1 = "SkillEffect1";
//	public const string SkillEffect2 = "SkillEffect2";
//}

//public static class UnitAnimationName
//{
//    public const int Layer_Base = 0;
//    public const int Layer_Damage = 1;
//    public const int LayerCount = 2;

//    public const string Idle = "Idle";
//    public const string BattleIdle = "BattleIdle";
//    public const string Run = "Run";
//    public const string Attack = "Attack";
//    public const string Skill1 = "Skill1";
//    public const string Skill2 = "Skill2";
//    public const string Skill3 = "Skill3";
//    public const string Damage = "Damage";
//    public const string Dead = "Dead";
//    public const string Win = "Win";
//    public const string DamageEnemy = "DamageEnemy";
//    public const string DamagePlayer = "DamagePlayer";
//}