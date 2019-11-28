using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowOffsetArray : MonoBehaviour
{
    public int m_Count = 10;
    public float m_Offset = 1;
    public float m_Smoothing = 8;

    public GameObject _FollowPrefab;

    FollowOffset[] m_Followers;

    public Transform _FollowInputTransform;

    public FollowOffset.OffsetType _OffsetType;

    /*
    public ParticleSystem m_FollowPSys;
    public ParticleSystem m_FollowPSys2;
    public ParticleSystem m_LengthPSys;
    ParticleSystem.Particle[] m_LengthParticles;
    ParticleSystem.EmitParams m_EmitParams;
    ParticleSystem.EmitParams m_EmitParamsLength;

    public AnimationCurve m_EmitCurve;
    public float m_MaxSpeed = 10;

    public int m_LengthParticleCount = 100;
    public AnimationCurve m_LengthSizeCurve;
    public float m_InheritVelSmall = 1;
    public float m_InheritVelLrg = 1;

    public float m_SizeByVel = 1;
    */


    void Start ()
    {      
        m_Followers = new FollowOffset[m_Count];
        for (int i = 0; i < m_Count; i++)
        {
            FollowOffset newFollow = new GameObject(i.ToString()).AddComponent<FollowOffset>();
            newFollow.transform.rotation = transform.rotation;
            newFollow.m_Offset = Vector3.forward * m_Offset;
            newFollow.m_Smoothing = m_Smoothing;
            newFollow._OffsetType = _OffsetType;

            if (_FollowPrefab != null)
            {
                GameObject prefabGO = Instantiate(_FollowPrefab, newFollow.transform);
                prefabGO.transform.localPosition = Vector3.zero;
                prefabGO.transform.localRotation = Quaternion.identity;
            }

            newFollow.transform.SetParent(transform);

            if (i == 0)
                newFollow.m_FollowTform = _FollowInputTransform;
            else
                newFollow.m_FollowTform = m_Followers[i - 1].transform;

            m_Followers[i] = newFollow;
        }

        /*
        m_EmitParams = new ParticleSystem.EmitParams();
        // Add particle systems           
        m_EmitParamsLength = new ParticleSystem.EmitParams();
        m_LengthParticles = new ParticleSystem.Particle[m_LengthParticleCount];

        for (int i = 0; i < m_LengthParticleCount; i++)
        {
            float norm = (float)i/(float)(m_LengthParticleCount-1);
            m_EmitParamsLength.position = GetPosAtLength(norm);
            float size = m_LengthSizeCurve.Evaluate(norm);
            float sizeNorm = norm;
            m_EmitParamsLength.startSize = 1;
            m_EmitParamsLength.startLifetime = float.MaxValue;

            m_LengthPSys.Emit(m_EmitParamsLength, i);           
        }
        */
    }

    private void Update()
    {/*
        #region Length Psys
        // GetParticles is allocation free because we reuse the m_Particles buffer between updates
       m_LengthPSys.GetParticles(m_LengthParticles);

        // Change only the particles that are alive
        for (int i = 0; i < m_LengthParticleCount; i++)
        {       
            float norm = (float)i / (float)m_LengthParticleCount;
            float rand = norm * (m_Count - 1);
            int index = (int)Mathf.Floor(rand);
            float lerp = rand - index;

            Vector3 pos;
            if (index == 0)
                pos = Vector3.Lerp(transform.position, m_Followers[0].transform.position, lerp);
            else
                pos = Vector3.Lerp(m_Followers[index - 1].transform.position, m_Followers[index].transform.position, lerp);
            
            m_LengthParticles[i].position = pos;

            float offsetSize = (((Mathf.Sin((Time.time + i) * .3f) + 1) / 2f) * .95f);
            m_LengthParticles[i].size = .02f + m_LengthSizeCurve.Evaluate(norm) * offsetSize;         

        }

        // Apply the particle changes to the particle system
        m_LengthPSys.SetParticles(m_LengthParticles, m_LengthParticleCount);
        #endregion

        #region Emit Psys
        float speed = 0;       
        for (int i = 0; i < m_Followers.Length; i++)
        {
            speed += m_Followers[i].m_Speed;         
        }

        speed /= m_MaxSpeed;

        int emitCount = (int)m_EmitCurve.Evaluate(speed);

        for (int i = 0; i < emitCount; i++)
        {
            float rand = Random.Range(0f, (float)m_Count - 1);
            int index = (int)Mathf.Floor(rand);
            float lerp = rand - index;

            Vector3 emitPos = Vector3.Lerp(m_Followers[index].transform.position, m_Followers[index + 1].transform.position, lerp);
            m_EmitParams.position = emitPos;
            m_EmitParams.startSize = .07f * (1-(rand/(float)m_Count)) * (m_Followers[index].m_Direction.magnitude * m_SizeByVel);
            m_EmitParams.velocity = m_Followers[index].m_Direction * m_InheritVelLrg;
            m_FollowPSys.Emit(m_EmitParams, 1);

            if (lerp < .5f)
            {
                m_EmitParams.ResetStartSize();
                m_EmitParams.velocity = m_Followers[index].m_Direction * m_InheritVelSmall;
                m_FollowPSys2.Emit(m_EmitParams, 1);
            }
        }
        #endregion
        */
    }

    Vector3 GetPosAtLength(float norm)
    {
        norm = 0;
        float floatIndex = norm * (float)(m_Followers.Length - 1);
        int index = (int)Mathf.Floor(floatIndex);
        float lerp = floatIndex - index;

        Vector3 pos;
        if (index == 0)
            pos = Vector3.Lerp(transform.position, m_Followers[0].transform.position, lerp);
        else
            pos = Vector3.Lerp(m_Followers[index-1].transform.position, m_Followers[index].transform.position, lerp);

        return pos;
    }


    private void OnDrawGizmos()
    {
        Gizmos.DrawLine(transform.position, transform.TransformPoint(Vector3.forward));
        if (Application.isPlaying)
        {
            for (int i = 0; i < m_Count; i++)
            {
                //Gizmos.DrawSphere(m_Followers[i].transform.position, .1f);
            }
        }
    }
}
