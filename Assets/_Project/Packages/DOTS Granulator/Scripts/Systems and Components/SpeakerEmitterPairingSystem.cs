using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;


public class SpeakerEmitterPairingSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        SpeakerManagerComponent speakerManager = GetSingleton<SpeakerManagerComponent>();
        DSPTimerComponent dspTimer = GetSingleton<DSPTimerComponent>();

        // ------------------------------------------------------------------------------------- CHECK SPEAKERS IN RANGE
        Entities.ForEach((ref GrainSpeakerComponent speaker, ref Translation emitterTrans) =>
        {
            bool prevInRange = speaker._InRange;

            // -------------------------------------------  CHECK IN RANGE
            speaker._InRange = math.distance(emitterTrans.Value, speakerManager._ListenerPos) < speakerManager._EmitterToListenerActivationRange;

            // If moving out of range
            if (prevInRange && !speaker._InRange)
            {
                Debug.Log("Speaker moving out of range: " + speaker._Index);
                speaker._ConnectedToEmitter = false;
                emitterTrans.Value = new float3(0);
            }
        });

        // ------------------------------------------------------------------------------------- CHECK EMITTERS IN RANGE AND ATTACH TO SPEAKERS
        Entities.ForEach((ref EmitterComponent emitter, ref Translation emitterTrans) =>
        {
            bool prevInRange = emitter._InRange;

            // -------------------------------------------  CHECK IN RANGE
            emitter._InRange = math.distance(emitterTrans.Value, speakerManager._ListenerPos) < speakerManager._EmitterToListenerActivationRange;

            // If moving out of range
            if (prevInRange && !emitter._InRange)
            {
                Debug.Log("Emitter moving out of range.");
                emitter._AttachedToSpeaker = false;
            }

            // if in range but not active
            if (emitter._InRange && !emitter._AttachedToSpeaker)
            {
                float closestDist = speakerManager._EmitterToSpeakerAttachRadius;
                int foundSpeakerIndex = 0;
                bool speakerFound = false;
                float3 emitterPos = emitterTrans.Value;
                Entity foundSpeakerEntity;

                // Search all currently connected speakers
                Entities.ForEach((Entity speakerEntity, ref GrainSpeakerComponent speaker, ref Translation speakerTrans) =>
                {
                    if (speaker._InRange && speaker._ConnectedToEmitter)
                    {
                        float dist = math.distance(emitterPos, speakerTrans.Value);

                        if (dist < closestDist)
                        {
                            closestDist = dist;

                            foundSpeakerIndex = speaker._Index;
                            foundSpeakerEntity = speakerEntity;
                            speakerFound = true;
                        }
                    }
                });

                if (speakerFound)
                {
                    Debug.Log("Found active speaker index / dist " + foundSpeakerIndex + "   " + closestDist);
                    emitter._SpeakerIndex = foundSpeakerIndex;
                    emitter._AttachedToSpeaker = true;
                    emitter._LastGrainEmissionDSPIndex = dspTimer._CurrentDSPSample;
                    return;
                }

                // Search all inactive speakers with active tag component
                Entities.ForEach((Entity speakerEntity, ref GrainSpeakerComponent speaker, ref Translation speakerTrans) =>
                {
                    // if spaker isnt found, search through inactive speakers
                    if (!speaker._ConnectedToEmitter && !speakerFound)
                    {
                        // Set speaker active and move to emitter positions
                        speaker._InRange = true;
                        speaker._ConnectedToEmitter = true;
                        speakerTrans.Value = emitterPos;

                        //Debug.Log("Emitter pos: " + emitterPos);

                        foundSpeakerIndex = speaker._Index;
                        foundSpeakerEntity = speakerEntity;
                        speakerFound = true;
                    }
                });

                if (speakerFound)
                {
                    //Debug.Log("Found inactive speaker index / dist " + foundSpeakerIndex);
                    emitter._SpeakerIndex = foundSpeakerIndex;
                    emitter._AttachedToSpeaker = true;
                    emitter._LastGrainEmissionDSPIndex = dspTimer._CurrentDSPSample;
                    return;
                }
            }
        });
    }
}


