using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace interaction {

    /// <summary>A pair of interactionMechanism/delay. </summary>
    public class InteractionMechanismDelayPair {
        public InteractionMechanism interactionMechanism;
        public float delay;
        
        public InteractionMechanismDelayPair(InteractionMechanism m, float delay) {
            this.interactionMechanism = m;
            this.delay = delay;
        }
    }
    
    public class InteractionMechanism {

        #region variables
        /// <summary> The destination where messages will be transfered.</summary>
        [NonSerialized]
        protected List<InteractionMechanismDelayPair> destMechanisms = new List<InteractionMechanismDelayPair>();

        /// <summary> The current list of actions linked to this Mechanism. </summary>
        protected List<MechanismProcess> currentProcesses = new List<MechanismProcess>();
        
        /// <summary> Message modifiers for incoming messages. </summary>
        protected List<CommandModifier> INMessageModifiers = new List<CommandModifier>();
        /// <summary> Message modifiers for outcoming messages. </summary>
        protected List<CommandModifier> OUTMessageModifiers = new List<CommandModifier>();

        /// <summary> Tells if the mechanism is powered, or not.</summary>
        protected bool powered = true;
        #endregion

        #region inits
        /// <summary> Initializes (or reinitializes) one destinationMechanism. </summary>
        /// <param name="mechanism">An additionnal mechanism where message will be transfered.</param>
        public void initMessageDestinations(Mechanism mechanism) {
            destMechanisms.Clear();
            addMechanismDelayPair(mechanism);
        }
        /// <summary> Initializes (or reinitializes) destinationMechanisms. </summary>
        /// <param name="mechanism">An additionnal mechanism where message will be transfered.</param>
        public void initMessageDestinations(MechanismDelayPair mechanism) {
            destMechanisms.Clear();
            addMechanismDelayPair(mechanism);
        }
 
        /// <summary> Initializes (or reinitializes) destination mechanisms. </summary>
        /// <param name="linkedMechanism">Additionnal mechanisms where messages will be transfered.</param>
        public void initMessageDestinations(List<Mechanism> linkedMechanisms) {
            destMechanisms.Clear();
            for (int i = 0; i < linkedMechanisms.Count; ++i) {
                addMechanismDelayPair(linkedMechanisms[i]);
            }
        }

        /// <summary> Initializes (or reinitializes) destination mechanisms. </summary>
        /// <param name="linkedMechanism">Additionnal mechanisms where messages will be transfered.</param>
        public void initMessageDestinations(List<MechanismDelayPair> linkedMechanisms) {
            destMechanisms.Clear();
            for (int i = 0; i < linkedMechanisms.Count; ++i) {
                addMechanismDelayPair(linkedMechanisms[i]);
            }
        }

        /// <summary>Add a new Mechanism/delay pair to this mechanism. Delay will be set to 0s.</summary>
        /// <param name="m"></param>
        protected void addMechanismDelayPair(Mechanism m) {
            if (m != null) {
                destMechanisms.Add(new InteractionMechanismDelayPair(m.interactionMechanism, 0.0f));
            }
        }

        /// <summary>Add a new Mechanism/delay pair to this mechanism.</summary>
        /// <param name="mdp"></param>
        protected void addMechanismDelayPair(MechanismDelayPair mdp) {
            if (mdp != null && mdp.mechanism != null) {
                destMechanisms.Add(new InteractionMechanismDelayPair(mdp.mechanism.interactionMechanism, mdp.delay));
            }
        }
        #endregion

        #region modifierAction
        /// <summary>Adds a modifier that will change incoming messages.</summary>
        /// <param name="cmdmod">The modifier to add.</param>
        public void addINModifier(CommandModifier cmdmod) {
            INMessageModifiers.Add(cmdmod);
        }

        /// <summary>Adds a modifier taht will change outgoing messages.</summary>
        /// <param name="cmdmod">The modifier to add.</param>
        public void addOUTModifier(CommandModifier cmdmod) {
            OUTMessageModifiers.Add(cmdmod);
        }

        /// <summary>Apply modifiers to a message.</summary>
        /// <param name="messageModifiers">The modifiers that should change the message.</param>
        /// <param name="cmdMessage">The message that needs to be modified.</param>
        /// <returns></returns>
        protected CommandMessage applyModifiers(List<CommandModifier> messageModifiers, CommandMessage cmdMessage) {
            for (int i = 0; i < messageModifiers.Count; ++i) {
                cmdMessage = messageModifiers[i].applyModifier(cmdMessage);
            }
            return cmdMessage;
        }
        #endregion

        #region communication
        /// <summary>Spreads a message to all linked mechanisms (aka dests). The message will be a unique for each recipients.
        /// It takes into account the needed delay the message should be sent, and apply "OUT" modifiers on the message before sending it.
        /// </summary>
        /// <param name="cmdMsg">The message content to spread</param>
        protected void spreadMessage(CommandMessage cmdMsg) {
            for (int i = 0; i < destMechanisms.Count; ++i) {
                if (destMechanisms[i].delay > 0f) {
                    sendDelayedCommandMessage(new CommandMessage(cmdMsg), destMechanisms[i].interactionMechanism, destMechanisms[i].delay);
                } else {
                    sendCommandMessage(new CommandMessage(cmdMsg), destMechanisms[i].interactionMechanism);
                }
            }
        }


        /// <summary> Process a message, actually making an action. </summary>
        /// <param name="cmdMessage">The message that needs to be processed</param>
        /// <returns>The message, after it has been processed.</returns>
        protected virtual CommandMessage processMessage(CommandMessage cmdMessage) {
            return cmdMessage;
        }

        /// <summary>Receives a message </summary>
        /// <param name="cmdMessage">The message that has been received.</param>
        /// <returns>The processed message.</returns>
        public CommandMessage onMessageReceived(CommandMessage cmdMessage) {
            cmdMessage = applyModifiers(INMessageModifiers, cmdMessage);
            setPower(cmdMessage);
            if(isPowered()) {
                cmdMessage = processMessage(cmdMessage);
                return cmdMessage;
            } else {
                //We weren't powered, so we break the message.
                cmdMessage = new NullifierModifier().applyModifier(cmdMessage);
                return cmdMessage;      
            }
        }

        /// <summary>Sends the message after a certain delay has passed. Uses internally a process.</summary>
        /// <param name="cmdMessage">The message to be sent.</param>
        /// <param name="dest">The recipient of the message.</param>
        /// <param name="delay">The time the message will arrive to the recipient.</param>
        public void sendDelayedCommandMessage(CommandMessage cmdMessage, InteractionMechanism dest, float delay) {
            cmdMessage = applyModifiers(OUTMessageModifiers, cmdMessage);
            App.Process.addProcess(new DelayedMessageProcess(this, dest, cmdMessage, delay));
        }

        /// <summary>Sends the message immediately.</summary>
        /// <param name="cmdMessage">the message to be sent.</param>
        /// <param name="dest">The recipient of the message.</param>
        public void sendCommandMessage(CommandMessage cmdMessage, InteractionMechanism dest) {
            cmdMessage = applyModifiers(OUTMessageModifiers, cmdMessage);
            dest.onMessageReceived(cmdMessage);
        }

        public void sendFeedbackMessage(CommandMessage sourceMessage) {
			CommandMessage feedbackMessage = applyModifiers(OUTMessageModifiers, new CommandMessage(sourceMessage));
            sourceMessage.originalSource.receiveFeedbackMessage(feedbackMessage);
        }
        #endregion

        #region power
        /// <summary> Tells if the mechanism is powered, or not. </summary>
        /// <returns>True if the mechanism is powered, otherwise false.</returns>
        public bool isPowered() {
            return powered;
        }

        /// <summary> Set the power of the mechanism </summary>
        /// <param name="ON">If it is true, the mechanism will be powered, and not powered if it is false.</param>
        public void setPower(bool ON) {
            powered = ON;
        }

        /// <summary>Set the power ON or OFF according to the message data (the power attribute).</summary>
        /// <param name="cmdMsg">The message containing the new power state.</param>
        /// <returns>The same message.</returns>
        public CommandMessage setPower(CommandMessage cmdMsg) {
            setPower(cmdMsg.cmdData.power.GetValueOrDefault(isPowered()));
            return cmdMsg;
        }
        #endregion

        #region processes
        /// <summary> Tells if all the actions on this mechanism are finished, or not. </summary>
        /// <returns>true if all actions finished, otherwise false.</returns>
        public bool areAllProcessesFinished() {
            for (int i = 0; i < currentProcesses.Count; ++i) {
                if (!currentProcesses[i].isCompleted()) return false;
            }
            return true;
        }

        /// <summary>Add a MechanismProcess that is linked to this InteractionMechanism.</summary>
        /// <param name="mp">The process to be added.</param>
        protected void addProcess(MechanismProcess mp) {
            App.Process.addProcess(mp);
            currentProcesses.Add(mp);
        }

        /// <summary>Remove MechanismProcess at i. </summary>
        /// <param name="i">index of the process.</param>
        private void endProcess(int i) {
            currentProcesses[i].End();
            currentProcesses.RemoveAt(i);
        }

        /// <summary>Remove the given MechanismProcess from this InteractionMechanism.</summary>
        /// <param name="mp">the process to be removed.</param>
        public void removeProcess(MechanismProcess mp) {
            currentProcesses.Remove(mp);
        }

        /// <summary> End and remove all actions on this mechanism, even if they have not been processed. </summary>
        public void endAllProcesses() {
            for(int i = 0; i < currentProcesses.Count; ++i) {
                endProcess(i);
            }
        }
        #endregion
    }

}