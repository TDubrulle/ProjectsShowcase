using UnityEngine;
using System.Collections;

namespace interaction {

    public class InteractionElevatorMechanism : InteractionMechanism {

        public class FullCycleElevatorProcess : MechanismProcess {

            private ElevatorMechanism mechanism;

            public FullCycleElevatorProcess(InteractionMechanism mp, CommandMessage cmdMsg, ElevatorMechanism m) : base(mp, cmdMsg) {
                this.mechanism = m;
            }

            protected override void beginAction(CommandMessage cmdMsg) {
            }

            protected override float updateAction(CommandMessage cmdMsg, float deltaT) {
                if (mechanism.updatePosition((cmdMsg.cmdData.floatL.GetValueOrDefault() + cmdMsg.cmdData.floatR.GetValueOrDefault()) * 0.5f * deltaT)) {
                    return 1.0f;
                };
                return mechanism.getCompletionState();
            }

            protected override void endAction(CommandMessage cmdMsg) {
                
            }


        }

        protected ElevatorMechanism linkedMechanism;

        public InteractionElevatorMechanism(ElevatorMechanism m) {
            linkedMechanism = m;
        }

        protected override CommandMessage processMessage(CommandMessage cmdMessage) {

            if (cmdMessage.cmdData.mainActivate.GetValueOrDefault()) {
                if (linkedMechanism.updatePosition((cmdMessage.cmdData.floatL.GetValueOrDefault() + cmdMessage.cmdData.floatR.GetValueOrDefault()) * 0.5f * Time.deltaTime)) {
                    //We finished activating the mechanism : we stop.
                    sendFeedbackMessage(cmdMessage);
                }
            }
            if(cmdMessage.cmdData.secondaryActivate.GetValueOrDefault()) {
                if (this.areAllProcessesFinished()) {
                    addProcess(new FullCycleElevatorProcess(this, cmdMessage, linkedMechanism));
                }
            }
            return cmdMessage;
        }
    }

}