using UnityEngine;
using System.Collections;

namespace interaction {

    public class InteractionActionnerMechanism : InteractionMechanism {

		protected ActionnerMechanism linkedMechanism;

        public InteractionActionnerMechanism(ActionnerMechanism m) {
			linkedMechanism = m;
			destMechanisms.Add(new InteractionMechanismDelayPair(m.actionner.interactionActionner, 0.0f));
        }

        protected override CommandMessage processMessage(CommandMessage cmdMessage) {
			linkedMechanism.powerActionner (applyModifiers (OUTMessageModifiers, cmdMessage).cmdData.power.GetValueOrDefault());
            spreadMessage(cmdMessage);
            return cmdMessage;
        }
    }

}