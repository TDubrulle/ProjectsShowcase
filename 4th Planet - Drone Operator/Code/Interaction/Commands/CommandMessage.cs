using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace interaction {
    
    /// <summary>
    /// The command data contains information about command messages. It contains values and nullable values that can act in two ways:
    /// -if null, the value shouldn't be considered by the mechanism.
    /// -Otherwise, the value should be considered by the mechanism.
    /// </summary>
    public class CommandData {
        //data for mechanism. More precisely, the actual content of the message.
        public float? floatL = null;
        public float? floatR = null;
        /// <summary> if the mechanism should be powered, or not.</summary>
        public bool? power = null;
        /// <summary> Main Mechanism activation (or use). </summary>
        public bool? mainActivate = null;
        /// <summary> Additionnal, but not compulsory activation. </summary>
        public bool? secondaryActivate = null;

        public CommandData() {

        }

        public CommandData(bool? mainActivate, bool? secondaryActivate = null, bool? power = null,
                                float? floatL = null, float? floatR = null) {
            this.mainActivate = mainActivate;
            this.secondaryActivate = secondaryActivate;
            this.power = power;
            this.floatL = floatL;
            this.floatR = floatR;
        }

    }

    /// <summary>
    /// Contains a message.
    /// </summary>
    public class CommandMessage {

		public enum MessageType
		{
			standard,
			start,
			end,
		}

        public CommandData cmdData = new CommandData();

        protected InteractionActionner source;

		/// <summary> The messageType. </summary>
		private MessageType type = MessageType.standard;

        public InteractionActionner originalSource {
            get { return source; }
        }

        public CommandMessage() {

        }

		/// <summary>
		/// Initializes a new instance of the <see cref="interaction.CommandMessage"/> class, based on another CommandeMessage.
		/// </summary>
		/// <param name="clonedMessage">Message to clone.</param>
        public CommandMessage(CommandMessage clonedMessage) {
            cmdData = new CommandData(clonedMessage.cmdData.mainActivate, clonedMessage.cmdData.secondaryActivate, clonedMessage.cmdData.power,
                                        clonedMessage.cmdData.floatL, clonedMessage.cmdData.floatR);
            this.source = clonedMessage.source;
			this.type = clonedMessage.type;
        }

		/// <summary>
		/// Initializes a new instance of the <see cref="interaction.CommandMessage"/> class with the given commandData and source. It will be considered as a standardMessage.
		/// </summary>
		/// <param name="commandData">Command data.</param>
		/// <param name="source">Source.</param>
        public CommandMessage(CommandData commandData, InteractionActionner source) {
            cmdData = new CommandData(commandData.mainActivate, commandData.secondaryActivate, commandData.power,
                                        commandData.floatL, commandData.floatR);
            this.source = source;
        }

		/// <summary>
		/// Initializes a new instance of the <see cref="interaction.CommandMessage"/> class with the given messageType.
		/// </summary>
		/// <param name="commandData">Command data.</param>
		/// <param name="source">Source.</param>
		/// <param name="type">The kind of message.</param>
		public CommandMessage(CommandData commandData, InteractionActionner source, MessageType type) {
			cmdData = new CommandData(commandData.mainActivate, commandData.secondaryActivate, commandData.power,
				commandData.floatL, commandData.floatR);
			this.source = source;
			this.type = type;
		}

		public MessageType getMessageType() {
			return type;
		}

		/// <summary> If the message is a start message. </summary>
		public bool isStartingMessage() {
			return type == MessageType.start;
		}

		/// <summary> If the message is a end message. </summary>
		public bool isEndingMessage() {
			return type == MessageType.end;
		}
		/// <summary> If the message is a end message. </summary>
		public bool isStandardMessage() {
			return type == MessageType.standard;
		}

    }

}