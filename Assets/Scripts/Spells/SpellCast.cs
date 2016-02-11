﻿using UnityEngine;

using System;
using System.Collections.Generic;

namespace Spells {

	// TODO: Auras need completely different handling from SpellCast: Apply to target, then create a handler for each, then propagate everything
	// TODO: Let SpellCast.Update run CastPhase timer to call EndCastPhase()
	// TODO: Notify SpellCast on context updates (projectile impact, context destroyed)
	// TODO: AreaAura (https://github.com/WCell/WCell/blob/master/Services/WCell.RealmServer/Spells/Auras/AreaAura.cs)
	//			(" An AreaAura either applies AreaAura-Effects to everyone in a radius around its center or repeatedly triggers a spell on everyone around it.")

	public class SpellCast : MonoBehaviour, ISpellObject {

		public SpellCast() {
			SpellCastContext = SpellObjectManager.Instance.Obtain<SpellCastContext> ();
		}

		public SpellCastContext SpellCastContext {
			get;
			private set;
		}
		
		public CastPhase CastPhase {
			get;
			private set;
		}
		
		public ProjectilePhase ProjectilePhase {
			get;
			private set;
		}

		public ImpactPhase ImpactPhase {
			get;
			private set;
		}
		
		public float StartTime {
			get;
			private set;
		}

		public float TimeSinceStart {
			get {
				return Time.time - StartTime;
			}
		}

		public float CastTimeLeft {
			get {
				if (CastPhase == null) {
					return 0;
				}
				var castTime = ((CastPhaseTemplate)CastPhase.Template).CastTime;
				return castTime - TimeSinceStart;
			}
		}

		public bool IsCasting {
			get { return CastPhase != null && !CastPhase.Context.HasEnded; }
		}

		public bool StartCasting(GameObject caster, Spell spell, Transform InitialTarget, ref Vector3 initialTargetPosition) {
			SpellCastContext.Caster = caster;
			SpellCastContext.Spell = spell;
			SpellCastContext.InitialTarget = InitialTarget;
			SpellCastContext.InitialTargetPosition = initialTargetPosition;

			StartTime = Time.time;

			StartCastPhase ();

			return true;
		}

		/// <summary>
		/// Interrupt spell cast if still casting
		/// </summary>
		public void Interrupt() {
			if (IsCasting) {
				// hold the presses!
				CastPhase.NotifyFinished(true);
			}
		}

		void Update() {
			if (IsCasting) {
				if (CastTimeLeft <= 0) {
					// finished casting
					EndCastPhase();
				}
			}
		}

		void StartCastPhase() {
			var phaseTemplate = SpellCastContext.Spell.CastPhase;
			if (phaseTemplate != null) {
				CastPhase = SpellObjectManager.Instance.Obtain<CastPhase>();
				var context = CreatePhaseContext (CastPhase, SpellCastContext.Caster, SpellCastContext.Caster.transform.position);
				CastPhase.NotifyStarted(context);
			}
			else {
				// no CastPhase
				StartProjectilePhase ();
			}
		}

		void EndCastPhase() {
			if (IsCasting) {
				CastPhase.NotifyFinished(false); 
			}
		}

		void StartProjectilePhase() {
			var phaseTemplate = SpellCastContext.Spell.ProjectilePhase;
			if (phaseTemplate != null) {
				if (SpellCastContext.Spell.ProjectilePhase.PhaseObjectPrefab == null) {
					// no valid ProjectilePhase
					Debug.LogError ("Spell has ProjectilePhase but ProjectilePhase is missing PhaseObjectPrefab", this);
					Impact (CastPhase != null ? CastPhase.Context : null, null);
					return;
				}

				var targets = SpellObjectManager.Instance.Obtain<SpellTargetCollection>();

				if (ProjectilePhase == null) {
					ProjectilePhase = SpellObjectManager.Instance.Obtain<ProjectilePhase>();
				}

				//nProjectileCount = 

				// create one projectile per target
				var context = CreatePhaseContext (ProjectilePhase, null, SpellCastContext.Caster.transform.position, targets);
				ProjectilePhase.NotifyStarted(context);
			}
			else {
				// no ProjectilePhase
				Impact (CastPhase != null ? CastPhase.Context : null, null);
			}
		}

		void OnProjectileImpact(SpellPhaseContext phaseContext) {
			ProjectilePhase.NotifyFinished (phaseContext);
			Impact (phaseContext, phaseContext.GetComponent<SpellProjectile>());
		}

		void Impact(SpellPhaseContext previousPhaseContext, SpellProjectile projectile) {
			var phaseTemplate = SpellCastContext.Spell.ImpactPhase;
			if (phaseTemplate != null) {
				if (ImpactPhase == null) {
					ImpactPhase = SpellObjectManager.Instance.Obtain<ImpactPhase>();
				}
				var sourcePhaseObject = previousPhaseContext != null ? previousPhaseContext.gameObject : null;
				sourcePhaseObject = sourcePhaseObject ?? gameObject;
				var phaseStartPosition = sourcePhaseObject.transform.position;
				var context = CreatePhaseContext (ImpactPhase, sourcePhaseObject, phaseStartPosition);
				ImpactPhase.NotifyStarted(context);
			}
		}

		SpellPhaseContext CreatePhaseContext(SpellPhase phase, GameObject sourcePhaseObject, Vector3 phaseStartPosition, SpellTargetCollection targets = null) {
			var prefab = phase.Template.PhaseObjectPrefab;
			GameObject phaseObject;
			if (prefab != null) {
				var rotation = sourcePhaseObject != null ? sourcePhaseObject.transform.rotation : Quaternion.identity;
				phaseObject = SpellGameObjectManager.Instance.Obtain(prefab, phaseStartPosition, rotation);
			}
			else {
				phaseObject = sourcePhaseObject;
			}

			Debug.LogError ("Could not create SpellPhaseContext because current and previous phase both did not have a PhaseObjectPrefab", this);

			return SpellPhaseContext.CreatePhaseContext (
				phase,
				phaseObject,
				phaseStartPosition,
				targets);
		}

//		public void CleanUp() {
//			// TODO: Clean up?
//		}
	}

}