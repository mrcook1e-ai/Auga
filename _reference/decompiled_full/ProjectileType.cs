using System;

[Flags]
public enum ProjectileType
{
	None = 0,
	Arrow = 1,
	Magic = 2,
	Bomb = 4,
	Physical = 8,
	Bolt = 0x10,
	Missile = 0x20,
	Spear = 0x40,
	Tar = 0x80,
	Fire = 0x100,
	Lava = 0x200,
	Posion = 0x400,
	Smoke = 0x800,
	Frost = 0x1000,
	Catapult = 0x2000,
	AOE = 0x4000,
	Harpoon = 0x8000,
	Nature = 0x10000,
	Lightning = 0x20000,
	Summon = 0x40000
}
