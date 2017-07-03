using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using interaction;

public class Actionner : MonoBehaviour
{
    [Tooltip("The Mechanism of the old system. Use otherMechanism for more functionnalities (like modifiers).")]
	[SerializeField] Mechanism m_mechanism;

    [Tooltip("The mechanism of the new system. It works with modifiers.")]
    public MechanismDelayPair otherMechanism;
    //For UI.
    [Tooltip("The modifiers that will change incoming messages. Modifiers are applied one after the another.")]
    public List<CommandModifierType> EnteringMessageModifiers = new List<CommandModifierType>();
    [Tooltip("The modifiers that will change outcoming messages. Modifiers are applied one after the another.")]
    public List<CommandModifierType> ExitingMessageModifiers = new List<CommandModifierType>();

    [Tooltip("Tells if the mechanism is powered or not.")]
    public bool activable = true;
    [Header("Sound")]
    [Tooltip("Sounds played when the actionner is turned on.")]
    public List<OnSceneEvent> onONSoundList = new List<OnSceneEvent>();
    [Tooltip("Sounds played when the actionner is turned off.")]
    public List<OnSceneEvent> onOFFSoundList = new List<OnSceneEvent>();

    [Header("Notification")]
    [Tooltip("Notification launched at the activation of the Actionner")]
    public GuiHud.Notice onONNotification = new GuiHud.Notice("", 1, GuiHud.Notice.Type.None);
    [Tooltip("Notification launched at the deactivation of the Actionner")]
    public GuiHud.Notice onOFFNotification = new GuiHud.Notice("", 1, GuiHud.Notice.Type.None);

    /**Tell if the message should be sent only once*/
    [Tooltip("(Old mechanism system) If the message should be sent only once. Use the sendOnlyOnceModifier for the new Mechanism. system")]
    public bool sendOnlyOnce = true;
    //if the message has already been sent once.
    protected bool sentOnce = false;

    public InteractionActionner interactionActionner = new InteractionActionner();


    public virtual void Awake() {
        App.Game.register(this);
        initModifiers();
        interactionActionner.setPower(activable);
        if (Mechanism == null && otherMechanism == null) {
            Debug.LogWarning("No Mechanism is linked to the Actionner.", this);
        }
		if (Mechanism != null) {
			Debug.LogWarning(this.gameObject.name + " : 'mechanism' attribute of actionners is no longer supported. Use otherMechanism instead.", this.gameObject);
		}
        for (int i = 0; i < onONSoundList.Count; ++i)
        {
            onONSoundList[i].gameObjectAttached = onONSoundList[i].gameObjectAttached == null ? this.gameObject : onONSoundList[i].gameObjectAttached;
        }
        for (int i = 0; i < onOFFSoundList.Count; ++i)
        {
            onOFFSoundList[i].gameObjectAttached = onOFFSoundList[i].gameObjectAttached == null ? this.gameObject : onOFFSoundList[i].gameObjectAttached;
        }
    }

	/// <summary> Initializes modifiers in the interactionMechanism. </summary>
	protected void initModifiers() {
		for (int i = 0; i < EnteringMessageModifiers.Count; ++i) {
			interactionActionner.addINModifier(CommandModifierFactory.createCommandModifier(EnteringMessageModifiers[i]));
		}
		for (int i = 0; i < ExitingMessageModifiers.Count; ++i) {
			interactionActionner.addOUTModifier(CommandModifierFactory.createCommandModifier(ExitingMessageModifiers[i]));
		}
	}

    public virtual void OnDisable()
    {
        App.Game.unregister(this);
    }

    public virtual void init() {
        if (otherMechanism != null && otherMechanism.mechanism != null) {
            interactionActionner.initMessageDestinations(otherMechanism);
        }
    }

    public bool sendOnceCondition() {
        return !sendOnlyOnce || (!sentOnce && sendOnlyOnce);
    }
		
	#region old mechanism system.
	public Mechanism Mechanism {
		get {
			return m_mechanism;
		}
		set {
			m_mechanism = value;
		}
	}

	public bool isActivable()
	{
		if (Mechanism == null)
			return false;
		return activable && interactionActionner.isPowered() && sendOnceCondition();
	}

	public bool Activate(bool _value)
	{
		if (isActivable ()) {
            if (Mechanism.onCommandReceived(new MechanismCommand(_value))) {
                sentOnce = true;
                return true;
            }
        }
		return false;
	}

	public bool Activate(float _value)
	{
		if (isActivable ()) {
            if (Mechanism.onCommandReceived(new MechanismCommand( _value))) {
                sentOnce = true;
                return true;
            }
        }
		return false;
	}

    public bool Activate(bool _active, float _value) {
        if (isActivable()) {
            if (Mechanism.onCommandReceived(new MechanismCommand(_active, _value))) {
                sentOnce = true;
                return true;
            }
        }
        return false;
    }
	#endregion
	 
	/// <summary>
	/// Update the materials linked to the actionner so that it acts as powered.
	/// </summary>
	/// <param name="newState">The new power state.</param>
	public virtual void onPoweredFeedback(bool newState) {
		
	}

	#region sounds and notices
    protected void startONNotice()
    {
        if (!string.IsNullOrEmpty(onONNotification.content))
        {
            App.Events.Enqueue(new Evt.OnNewNoticeReceived(onONNotification.content, onONNotification.priority, onONNotification.typeNotice));
        }
    }

    protected void startOFFNotice()
    {
        if (!string.IsNullOrEmpty(onOFFNotification.content))
        {
            App.Events.Enqueue(new Evt.OnNewNoticeReceived(onOFFNotification.content, onOFFNotification.priority, onOFFNotification.typeNotice));
        }
    }

    protected void startOFFSound()
    {
        for (int i = 0; i < onOFFSoundList.Count; ++i)
        {
            if (!string.IsNullOrEmpty(onOFFSoundList[i].soundId) && !onOFFSoundList[i].isStarted)
                OnSceneEvent.process(onOFFSoundList[i], onOFFSoundList[i].delaySound);
        }
    }

    protected void startONSound()
    {
        for (int i = 0; i < onONSoundList.Count; ++i)
        {
            if (!string.IsNullOrEmpty(onONSoundList[i].soundId) && !onONSoundList[i].isStarted)
                OnSceneEvent.process(onONSoundList[i], onONSoundList[i].delaySound);
        }
    }
	#endregion
}