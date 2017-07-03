using UnityEngine;
using System;
using System.Collections;


namespace interaction {

    [Serializable]
    public struct CommandModifierType {
        public CommandModifierEnumType type;
        public float parameter;
    }

    public enum CommandModifierEnumType {
        IDENTITY,
        NULLIFIER,
        ALL_INVERTER,
        MAIN_ACTIVATION_MODIFIER,
        MAIN_ACTIVATON_INVERTER,
        MAIN_ACTIVATION_NULLIFIER,
        SECONDARY_ACTIVATION_MODIFIER,
        SECONDARY_ACTIVATION_INVERTER,
        SECONDARY_ACTIVATION_NULLIFIER,
        POWER_MODIFIER,
        POWER_INVERTER,
        POWER_NULLIFIER,
        LEFT_VALUE_MODIFIER,
        RIGHT_VALUE_MODIFIER,
		START_MESSAGE_NULLIFIER,
		UPDATE_MESSAGE_NULLIFIER,
		END_MESSAGE_NULLIFIER,
    }

    public class CommandModifierFactory {

        /// <summary>Create a new CommandModifier, based on a Modifier Type. </summary>
        /// <param name="modifier">The type of modifier.</param>
        /// <returns>The newly created modifier.</returns>
        public static CommandModifier createCommandModifier(CommandModifierType modifier) {
            switch (modifier.type) {
                case CommandModifierEnumType.IDENTITY: return new IdentityModifier();
                case CommandModifierEnumType.NULLIFIER: return new NullifierModifier();
                case CommandModifierEnumType.MAIN_ACTIVATION_NULLIFIER: return new MainActivationNullifierModifier();
                case CommandModifierEnumType.SECONDARY_ACTIVATION_NULLIFIER: return new SecondaryActivationNullifierModifier();
                case CommandModifierEnumType.POWER_NULLIFIER: return new PowerNulliferModifier();
                case CommandModifierEnumType.MAIN_ACTIVATION_MODIFIER: return new MainActivationModifier(modifier.parameter != 0f);
                case CommandModifierEnumType.SECONDARY_ACTIVATION_MODIFIER: return new SecondaryActivationModifier(modifier.parameter != 0f);
                case CommandModifierEnumType.LEFT_VALUE_MODIFIER: return new LeftValueModifier(modifier.parameter);
                case CommandModifierEnumType.RIGHT_VALUE_MODIFIER: return new RightValueModifier(modifier.parameter);
                case CommandModifierEnumType.POWER_MODIFIER: return new PowerModifier(modifier.parameter != 0f);
                case CommandModifierEnumType.ALL_INVERTER: return new InvertAllModifier();
                case CommandModifierEnumType.MAIN_ACTIVATON_INVERTER: return new InvertMainActivationModifier();
                case CommandModifierEnumType.SECONDARY_ACTIVATION_INVERTER: return new InvertSecondaryActivationModifier();
                case CommandModifierEnumType.POWER_INVERTER: return new InvertPowerModifier();
				case CommandModifierEnumType.START_MESSAGE_NULLIFIER: return new StartMessageNullifier ();
				case CommandModifierEnumType.UPDATE_MESSAGE_NULLIFIER: return new StandardMessageNullifier ();
				case CommandModifierEnumType.END_MESSAGE_NULLIFIER: return new EndMessageNullifier ();
                default: Debug.LogError("Unknown command modifier type. Aborting."); break;
            }
            return null;
        }
    }

    #region modifier subtypes
    
    /// <summary> Modifier that returns the same message. </summary>
    public class IdentityModifier : CommandModifier {
        public override CommandMessage applyModifier(CommandMessage msg) { return msg; }
    }

    /// <summary> Modifier that Nullifies all data inside the message. </summary>
    public class NullifierModifier : CommandModifier {
        public override CommandMessage applyModifier(CommandMessage msg) {
            msg.cmdData = new CommandData();
            return msg;
        }
    }

    public class MainActivationNullifierModifier : CommandModifier {
        public override CommandMessage applyModifier(CommandMessage msg) {
            msg.cmdData.mainActivate = null;
            return msg;
        }
    }

    public class SecondaryActivationNullifierModifier : CommandModifier {
        public override CommandMessage applyModifier(CommandMessage msg) {
            msg.cmdData.secondaryActivate = null;
            return msg;
        }
    }

    public class PowerNulliferModifier : CommandModifier {
        public override CommandMessage applyModifier(CommandMessage msg) {
            msg.cmdData.power = null;
            return msg;
        }
    }

    /// <summary>Modifier that changes the first float parameter (floatL) of the message. </summary>
    public class LeftValueModifier : CommandModifier {
        private float newValue = 0.0f;

        public LeftValueModifier(float value) { newValue = value; }
        public override CommandMessage applyModifier(CommandMessage msg) {
            msg.cmdData.floatL = newValue;
            return msg;
        }
    }
    
    /// <summary>Modifier that changes the second float parameter (floatR) of the message. </summary>
    public class RightValueModifier : CommandModifier {
        private float newValue = 0.0f;

        public RightValueModifier(float value) { newValue = value; }
        public override CommandMessage applyModifier(CommandMessage msg) {
            msg.cmdData.floatR = newValue;
            return msg;
        }
    }

    /// <summary>Modifier that changes the secondary activation parameter of the message. </summary>
    public class SecondaryActivationModifier : CommandModifier {
        private bool newSecondary = true;

        public SecondaryActivationModifier(bool newMain) { this.newSecondary = newMain; }

        public override CommandMessage applyModifier(CommandMessage msg) {
            msg.cmdData.secondaryActivate = newSecondary;
            return msg;
        }
    }

    /// <summary> Modifier that changes the main activation parameter of the message. </summary>
    public class MainActivationModifier : CommandModifier{
        private bool newMain = true;

        public MainActivationModifier(bool newMain) { this.newMain = newMain; }

        public override CommandMessage applyModifier(CommandMessage msg) {
            msg.cmdData.mainActivate = newMain;
            return msg;
        }
    }

    /// <summary> Modifier that changes the power parameter of the message.</summary>
    public class PowerModifier : CommandModifier {
        private bool newPower = true;

        public PowerModifier(bool newPower) { this.newPower = newPower; }

        public override CommandMessage applyModifier(CommandMessage msg) {
            msg.cmdData.power = newPower;
            return msg;
        }
    }

    /// <summary> Modifier that invert all values of the message (if they are set).</summary>
    public class InvertAllModifier : CommandModifier {
        
        public override CommandMessage applyModifier(CommandMessage msg) {

            msg.cmdData.mainActivate = !msg.cmdData.mainActivate;

            msg.cmdData.secondaryActivate = !msg.cmdData.secondaryActivate;
            msg.cmdData.power = !msg.cmdData.power;

            return msg;
        }
    }

    /// <summary> Modifier that invert main activation of the message (if it is set).</summary>
    public class InvertMainActivationModifier : CommandModifier {

        public override CommandMessage applyModifier(CommandMessage msg) {
            msg.cmdData.mainActivate = !msg.cmdData.mainActivate;
            return msg;
        }
    }

    /// <summary> Modifier that invert secondary activation of the message (if it is set).</summary>
    public class InvertSecondaryActivationModifier : CommandModifier {

        public override CommandMessage applyModifier(CommandMessage msg) {
            msg.cmdData.secondaryActivate = !msg.cmdData.secondaryActivate;
            return msg;
        }
    }

    /// <summary> Modifier that invert power of the message (if it is set).</summary>
    public class InvertPowerModifier : CommandModifier {

        public override CommandMessage applyModifier(CommandMessage msg) {
            msg.cmdData.power = !msg.cmdData.power;
            return msg;
        }
    }

	/// <summary> Modifier that nullify the message if it is a starting message.</summary>
	public class StartMessageNullifier : CommandModifier {

		public override CommandMessage applyModifier(CommandMessage msg) {
			if (msg.isStartingMessage ()) {
				msg.cmdData = new CommandData ();
			}
			return msg;
		}

	}

	/// <summary> Modifier that nullify the message if it is a standard (usually update) message.</summary>
	public class StandardMessageNullifier : CommandModifier {

		public override CommandMessage applyModifier(CommandMessage msg) {
			if (msg.isStandardMessage()) {
				msg.cmdData = new CommandData ();
			}
			return msg;
		}
	}

	/// <summary> Modifier that nullify the message if it is an ending message.</summary>
	public class EndMessageNullifier : CommandModifier {

		public override CommandMessage applyModifier(CommandMessage msg) {
			if (msg.isEndingMessage()) {
				msg.cmdData = new CommandData ();
			}
			return msg;
		}
	}
    #endregion

    /// <summary>
    /// Give the possibility to modify CommandMessages' content and alter their actions.
    /// </summary>
    public class CommandModifier {

        public virtual CommandMessage applyModifier(CommandMessage msg) {
            return msg;
        }

    }
}