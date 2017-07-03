using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class MechanismDelayPair {

    public Mechanism mechanism;
    public float delay;
    
    public MechanismDelayPair(Mechanism m, float delay) {
        mechanism = m;
        this.delay = delay;
    }

    public MechanismDelayPair(MechanismDelayPair mdp) {
        mechanism = mdp.mechanism;
        delay = mdp.delay;
    }
}

/// <summary>
/// Multimechanism allows multiple mechanisms to be grouped into a single one.
/// </summary>
public class MultiMechanism : Mechanism
{
	public List<MechanismDelayPair> m_mechanisms = new List<MechanismDelayPair>();

    public override void Awake() {
        base.Awake();
        interactionMechanism = new interaction.InteractionMultiMechanism();
		initModifiers ();
        deleteNullElements();
    }

    public override void init()
    {
		base.init ();
        interactionMechanism.initMessageDestinations(m_mechanisms);
    }
     
    private bool hasAllActivated() {
        for(int i = 0; i < m_mechanisms.Count; ++i) {
            if (!m_mechanisms[i].mechanism.finished) {
                return false;
            }
        }
        return true;
    }

	public override bool onCommandProcess(MechanismCommand _command)
	{
		for (int i = 0; i < m_mechanisms.Count; ++i) {
            if (m_mechanisms[i].delay > 0f) {
                StartCoroutine(triggerMechanism(m_mechanisms[i].mechanism, m_mechanisms[i].delay, _command));
            } else {
                m_mechanisms[i].mechanism.onCommandReceived(_command);
            }
        }
        return hasAllActivated();
	}

    public override void begin() {
        base.begin();
        for(int i = 0; i < m_mechanisms.Count; ++i) {
            m_mechanisms[i].mechanism.begin();
        }
    }

    IEnumerator triggerMechanism(Mechanism m, float delay, MechanismCommand _command) {
        yield return new WaitForSeconds(delay);
        m.onCommandReceived(_command);
    }

	public void deleteNullElements(){
		m_mechanisms.RemoveAll (i => i == null ||i.mechanism == null);
	}

    /// <summary>
    /// Regroups all Mechanisms found in the children gameObjects into this multiMechanism. The mechanisms will be added to the list only if they are not already added.
    /// </summary>
    public void regroupAllMechanismsInChildren() {
        regroupMechanismsInChildren<Mechanism>();
    }

    public void regroupMechanismsInChildren<T>() where T : Mechanism{
        T[] mechanisms = transform.GetComponentsInChildren<T>();
        for (int i = 0; i < mechanisms.Length; ++i) {
            if (mechanisms[i] != this && !this.contains(mechanisms[i])) {
                m_mechanisms.Add(new MechanismDelayPair(mechanisms[i], 0.0f));
            }
        }
    }

    public void setCumulativeDelayOnAll(float delay) {
        float currentDelay = delay;
        for(int i = 0; i < m_mechanisms.Count; ++i) {
            m_mechanisms[i].delay = currentDelay;
            currentDelay += delay;
        }
    }

    public void setDelayOnAll(float delay) {
        for (int i = 0; i < m_mechanisms.Count; ++i) {
            m_mechanisms[i].delay = delay;
        }
    }

    /// <summary>
    /// Check if a mechanism exists in this multimechanism.
    /// </summary>
    /// <param name="m"></param>
    /// <returns></returns>
    protected bool contains(Mechanism m) {
        for (int j = 0; j < m_mechanisms.Count; ++j) {
            if (m_mechanisms[j].mechanism.Equals(m)) {
                return true;
            }
        }
        return false;
    }
}

