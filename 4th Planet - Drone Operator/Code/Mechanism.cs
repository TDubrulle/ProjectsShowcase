using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using interaction;

[System.Serializable]
public class MechanismCommand
{
	
	public bool activated = false;
	public float value = 0f;

	public MechanismCommand (bool _active)
	{
		activated = _active;
		value = activated? 1.0f: -0.0f;
	}

	public MechanismCommand (float _value)
	{
		value = _value;
		activated = _value > 0f ? true : false;
	}
	public MechanismCommand (bool _active, float _value)
	{
		activated = _active;
		value = _value;
	}

	public MechanismCommand ()
	{
	}

	public MechanismCommand (MechanismCommand other)
	{
		activated = other.activated;
		value = other.value;
	}

}

public class Mechanism : MonoBehaviour
{
    //For UI.
    [SerializeField]
    public List<CommandModifierType> EnteringMessageModifiers = new List<CommandModifierType>();
    [SerializeField]
    public List<CommandModifierType> ExitingMessageModifiers = new List<CommandModifierType>();

    public InteractionMechanism interactionMechanism = new InteractionMechanism();

    /// <summary> Initializes modifiers in the interactionMechanism. </summary>
    protected void initModifiers() {
        for (int i = 0; i < EnteringMessageModifiers.Count; ++i) {
            interactionMechanism.addINModifier(CommandModifierFactory.createCommandModifier(EnteringMessageModifiers[i]));
        }
        for (int i = 0; i < ExitingMessageModifiers.Count; ++i) {
            interactionMechanism.addOUTModifier(CommandModifierFactory.createCommandModifier(ExitingMessageModifiers[i]));
        }
    }

    public virtual void init()
    {

    }

    public virtual void OnDisable()
    {
        App.Game.unregister(this);
    }

    public virtual void Awake() {
        App.Game.register(this);
    }

    protected bool m_finished = false;

    public bool finished {
        get { return m_finished; }
        protected set { m_finished = value; }
    }

    public bool onCommandReceived (MechanismCommand _command)
	{
        m_finished = onCommandProcess(_command);
		return m_finished;
	}

	public virtual bool onCommandProcess(MechanismCommand _command)
	{
		return false;
	}

    /// <summary>
    /// Reset the mechanism so that it can be triggered again.
    /// </summary>
    public virtual void begin() {
        m_finished = false;
    }

}


	