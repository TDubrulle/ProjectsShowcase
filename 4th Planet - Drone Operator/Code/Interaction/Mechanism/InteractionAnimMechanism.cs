using UnityEngine;
using System.Collections;

namespace interaction {

    public class InteractionAnimMechanism : InteractionMechanism {


		public class AnimationCycleProcess : MechanismProcess {

			private AnimationMechanism mechanism;

			public AnimationCycleProcess(InteractionAnimMechanism mp, CommandMessage cmdMsg, AnimationMechanism m) : base(mp, cmdMsg) {
				this.mechanism = m;
			}

			protected override void beginAction(CommandMessage cmdMsg) {
			}

			protected override float updateAction(CommandMessage cmdMsg, float deltaT) {
				return mechanism.animationUpdate();
			}

			protected override void endAction(CommandMessage cmdMsg) {
				if (mechanism.sendFeedbackOnAnimationEnd) {
					linkedInteractionMechanism.sendFeedbackMessage (cmdMsg);
				}
			}

		}

        protected AnimationMechanism linkedMechanism;

        public InteractionAnimMechanism (AnimationMechanism m) {
            linkedMechanism = m;
        }

        protected override CommandMessage processMessage(CommandMessage cmdMessage) {
            if (cmdMessage.cmdData.mainActivate.HasValue) {
				if (linkedMechanism.switchAnimation (cmdMessage.cmdData.mainActivate.Value)) {
					addProcess (new AnimationCycleProcess (this, cmdMessage, linkedMechanism));
					if (!linkedMechanism.sendFeedbackOnAnimationEnd) {
						sendFeedbackMessage (cmdMessage);
					}
				}
            }
			if (cmdMessage.cmdData.secondaryActivate.HasValue) {
				if (linkedMechanism.switchSecondaryAnimation (cmdMessage.cmdData.secondaryActivate.Value)) {
					addProcess (new AnimationCycleProcess (this, cmdMessage, linkedMechanism));
					if (!linkedMechanism.sendFeedbackOnAnimationEnd) {
						sendFeedbackMessage (cmdMessage);
					}
				}
			}
            return cmdMessage;
        }
    }

}
