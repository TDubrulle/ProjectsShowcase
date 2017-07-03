using UnityEngine;
using System.Collections;

namespace interaction {


    /// <summary>
    /// Mechanism actions are actions that can be used to call functions in objects that depends on the state of an actionner.
    /// </summary>
    public class MechanismProcess : Process {


        protected InteractionMechanism linkedInteractionMechanism;

        protected float currentState = 0.0f;

        protected CommandMessage currentCommandMessage = null;

        public MechanismProcess(InteractionMechanism linkedMechanism, CommandMessage cmdMsg) {
            linkedInteractionMechanism = linkedMechanism;
            currentCommandMessage = cmdMsg;
        }

        protected virtual void beginAction(CommandMessage cmdMsg) { }
        protected virtual float updateAction(CommandMessage cmdMsg, float deltaT) { return 1.0f; }
        protected virtual void endAction(CommandMessage cmdMsg) { }

        /// <summary>
        /// Return the completion state of the action.
        /// </summary>
        /// <returns>A value between 0 and 1 : 0 if the action has just started, 1 if it has completed.</returns>
        public float completionState() {
            return currentState;
        }

        /// <summary> Tells if the process is completed. </summary>
        /// <returns>returns true if it is completed, otherwise false.</returns>
        public bool isCompleted() {
            return completionState() >= 1.0f;
        }

        public void updateCommandMessage(CommandMessage cmdMsg) {
            currentCommandMessage = cmdMsg;
        }

        #region process override
        /// <summary>
        /// Actions made on begin.
        /// </summary>
        public override void OnBegin() {
            base.OnBegin();
            beginAction(currentCommandMessage);
        }

        /// <summary>
        /// Play the action.
        /// </summary>
        public override void OnStep(float deltaT) {
            base.OnStep(deltaT);
            currentState = updateAction(currentCommandMessage, deltaT);
            if (isCompleted()) {
                this.End();
            }
        }

        /// <summary>
        /// Actions made on end.
        /// </summary>
        public override void OnEnd() {
            base.OnEnd();
            if (linkedInteractionMechanism != null) {
                linkedInteractionMechanism.removeProcess(this);
            }
			linkedInteractionMechanism.sendFeedbackMessage (currentCommandMessage);
            endAction(currentCommandMessage);
        }
        #endregion

    }
}