﻿using UnityEngine;
using System.Collections;

namespace Spells {
	[System.Serializable]
	public class Spell : ScriptableObject {
		public float Cooldown;

		public string[] SubAssets = new string[0];

		/// <summary>
		/// The targets where the spell impacts (where the projectiles fly to, if the spell has a projectile phase)
		/// </summary>
		public SpellTargetSettings Targets;

		public CastPhaseTemplate CastPhase;
		public ProjectilePhaseTemplate ProjectilePhase;
		public ImpactPhaseTemplate ImpactPhase;
	}

}