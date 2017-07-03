using UnityEngine;
using System.Collections;

namespace interaction {

    public class InteractionMaterialMechanism : InteractionMechanism {
       
        protected MaterialMechanism linkedMechanism;

        public InteractionMaterialMechanism(MaterialMechanism m) {
            linkedMechanism = m;
        }

        protected override CommandMessage processMessage(CommandMessage cmdMessage) {
            if (cmdMessage.cmdData.mainActivate.HasValue) {
                if (linkedMechanism.changeMaterial(cmdMessage.cmdData.mainActivate.GetValueOrDefault())) {
                    //We finished activating the mechanism : we stop.
                    sendFeedbackMessage(cmdMessage);
                }
            }
            return cmdMessage;
        }
    }
}