using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;

namespace DPSExtreme
{
	internal class DPSExtremeModProjectile : GlobalProjectile
	{
		public override bool InstancePerEntity => true;

		public int whoIsMyParent = -1;
		public int myParentItemType = -1;

		public override void OnSpawn(Projectile projectile, IEntitySource source)
		{
			if (source is EntitySource_Parent parentSource)
			{
				if (parentSource.Entity is NPC parentNPC)
				{
					whoIsMyParent = parentNPC.whoAmI;
				}
				else if (parentSource.Entity is Player parentplayer)
				{
					if (parentSource is EntitySource_ItemUse itemSource)
						myParentItemType = itemSource.Item.type;
				}
				else if (parentSource.Entity is Projectile parentProj)
				{
					myParentItemType = parentProj.GetGlobalProjectile<DPSExtremeModProjectile>().myParentItemType;
					whoIsMyParent = parentProj.whoAmI;
				}
			}

			if (source is EntitySource_Wiring wiring)
			{
				whoIsMyParent = (int)InfoListIndices.Traps;
			}
		}
	}
}
